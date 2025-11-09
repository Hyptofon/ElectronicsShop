using System.Net;
using System.Net.Http.Json;
using Api.Dtos;
using Domain.Cart;
using Domain.Categories;
using Domain.Orders;
using Domain.Products;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data.Categories;
using Tests.Data.Orders;
using Tests.Data.Products;

namespace Api.Tests.Integration.Orders;

public class OrdersControllerTests : BaseIntegrationTest, IAsyncLifetime
{
    private const string BaseRoute = "orders";
    private readonly Category _testCategory = CategoryData.FirstTestCategory("Order");
    private Product _testProduct;
    private readonly string _testUserId = Guid.NewGuid().ToString();
    private readonly string _adminUserId = Guid.NewGuid().ToString();
    private HttpClient _userClient;
    private HttpClient _adminClient;
    private HttpClient _managerClient;
    private Order _testOrder; // Це поле буде ініціалізовано в InitializeAsync

    public OrdersControllerTests(IntegrationTestWebFactory factory) : base(factory)
    {
        _userClient = CreateAuthenticatedClient("User", _testUserId);
        _adminClient = CreateAuthenticatedClient("Admin", _adminUserId);
        _managerClient = CreateAuthenticatedClient("Manager");
        
        _testProduct = ProductData.FirstTestProduct(new List<CategoryId> { _testCategory.Id });
        
        // _testOrder ініціалізується пізніше в InitializeAsync
    }

    #region GET Tests (My Orders)

    [Fact]
    public async Task GetMyOrders_ShouldReturnUserOrders()
    {
        // Arrange - створюємо замовлення для користувача
        var orderItems = new List<OrderItem>
        {
            OrderData.CreateTestOrderItem(OrderId.New(), _testProduct.Id)
        };
        var order = OrderData.CreateTestOrder(Guid.Parse(_testUserId), orderItems);
        await Context.Orders.AddAsync(order);
        await SaveChangesAsync();

        // Act
        var response = await _userClient.GetAsync($"{BaseRoute}/my");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.ToResponseModel<List<OrderDto>>();
        orders.Should().HaveCount(1);
        orders.First().UserId.Should().Be(Guid.Parse(_testUserId));
    }

    [Fact]
    public async Task GetMyOrders_ShouldNotReturnOtherUsersOrders()
    {
        // Arrange - створюємо замовлення для іншого користувача
        var otherUserId = Guid.NewGuid();
        var orderItems = new List<OrderItem>
        {
            OrderData.CreateTestOrderItem(OrderId.New(), _testProduct.Id)
        };
        var order = OrderData.CreateTestOrder(otherUserId, orderItems);
        await Context.Orders.AddAsync(order);
        await SaveChangesAsync();

        // Act
        var response = await _userClient.GetAsync($"{BaseRoute}/my");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.ToResponseModel<List<OrderDto>>();
        orders.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyOrders_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"{BaseRoute}/my");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET Tests (Get by Id)

    [Fact]
    public async Task GetById_AsOwner_ShouldReturnOrder()
    {
        // Arrange
        var orderItems = new List<OrderItem>
        {
            OrderData.CreateTestOrderItem(_testOrder.Id, _testProduct.Id)
        };
        var order = OrderData.CreateTestOrder(Guid.Parse(_testUserId), orderItems);
        await Context.Orders.AddAsync(order);
        await SaveChangesAsync();

        // Act
        var response = await _userClient.GetAsync($"{BaseRoute}/{order.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orderDto = await response.ToResponseModel<OrderDto>();
        orderDto.Id.Should().Be(order.Id.Value);
        orderDto.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetById_AsManager_ShouldReturnAnyOrder()
    {
        // Act
        var response = await _managerClient.GetAsync($"{BaseRoute}/{_testOrder.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_AsAdmin_ShouldReturnAnyOrder()
    {
        // Act
        var response = await _adminClient.GetAsync($"{BaseRoute}/{_testOrder.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetById_AsNonOwner_ShouldReturnNotFound()
    {
        // Act
        var response = await _userClient.GetAsync($"{BaseRoute}/{_testOrder.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_NonExistentOrder_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _userClient.GetAsync($"{BaseRoute}/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST Tests (Create Order)

    [Fact]
    public async Task CreateOrder_FromCart_ShouldCreateOrderAndClearCart()
    {
        // Arrange - створюємо кошик з товарами
        var cart = Domain.Cart.Cart.New(Guid.Parse(_testUserId));
        var cartItem = CartItem.New(cart.Id, _testProduct.Id, 2);
        cart.AddItem(cartItem);
        await Context.Carts.AddAsync(cart);
        await SaveChangesAsync();

        var initialStock = _testProduct.StockQuantity;
        var request = OrderData.CreateTestOrderDto();

        // Act
        var response = await _userClient.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orderDto = await response.ToResponseModel<OrderDto>();
        
        orderDto.Items.Should().HaveCount(1);
        orderDto.Items.First().Quantity.Should().Be(2);
        orderDto.Status.Should().Be(OrderStatus.Pending.ToString());
        orderDto.TotalAmount.Should().Be(_testProduct.Price * 2);

        // Перевірка, що кошик очищено
        var dbCart = await Context.Carts
            .Include(c => c.Items)
            .FirstAsync(c => c.Id == cart.Id);
        dbCart.Items.Should().BeEmpty();

        // Перевірка, що запаси зменшилися
        var dbProduct = await Context.Products
            .AsNoTracking()
            .FirstAsync(p => p.Id == _testProduct.Id);
        dbProduct.StockQuantity.Should().Be(initialStock - 2);
    }

    [Fact]
    public async Task CreateOrder_WithEmptyCart_ShouldReturnBadRequest()
    {
        // Arrange - порожній кошик
        var cart = Domain.Cart.Cart.New(Guid.Parse(_testUserId));
        await Context.Carts.AddAsync(cart);
        await SaveChangesAsync();

        var request = OrderData.CreateTestOrderDto();

        // Act
        var response = await _userClient.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_WithNoCart_ShouldReturnBadRequest()
    {
        // Arrange
        var request = OrderData.CreateTestOrderDto();

        // Act
        var response = await _userClient.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_WithInsufficientStock_ShouldReturnBadRequest()
    {
        // Arrange - кошик з кількістю більшою за запаси
        var cart = Domain.Cart.Cart.New(Guid.Parse(_testUserId));
        var cartItem = CartItem.New(cart.Id, _testProduct.Id, 1000); // більше за stock
        cart.AddItem(cartItem);
        await Context.Carts.AddAsync(cart);
        await SaveChangesAsync();

        var request = OrderData.CreateTestOrderDto();

        // Act
        var response = await _userClient.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("")] // Empty address
    [InlineData(null)] // Null address
    public async Task CreateOrder_WithInvalidAddress_ShouldReturnBadRequest(string address)
    {
        // Arrange
        var cart = Domain.Cart.Cart.New(Guid.Parse(_testUserId));
        var cartItem = CartItem.New(cart.Id, _testProduct.Id, 1);
        cart.AddItem(cartItem);
        await Context.Carts.AddAsync(cart);
        await SaveChangesAsync();

        var request = new CreateOrderDto(address, "Notes");

        // Act
        var response = await _userClient.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = OrderData.CreateTestOrderDto();

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST Tests (Cancel Order)

    [Fact]
    public async Task CancelOrder_AsOwner_ShouldCancelOrderAndRestoreStock()
    {
        // Arrange
        var orderItems = new List<OrderItem>
        {
            OrderData.CreateTestOrderItem(_testOrder.Id, _testProduct.Id)
        };
        var order = OrderData.CreateTestOrder(Guid.Parse(_testUserId), orderItems);
        await Context.Orders.AddAsync(order);
        await SaveChangesAsync();

        var initialStock = await Context.Products
            .Where(p => p.Id == _testProduct.Id)
            .Select(p => p.StockQuantity)
            .FirstAsync();

        // Act
        var response = await _userClient.PostAsync($"{BaseRoute}/{order.Id.Value}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orderDto = await response.ToResponseModel<OrderDto>();
        orderDto.Status.Should().Be(OrderStatus.Cancelled.ToString());

        // Перевірка, що запаси відновлено
        var dbProduct = await Context.Products
            .AsNoTracking()
            .FirstAsync(p => p.Id == _testProduct.Id);
        dbProduct.StockQuantity.Should().Be(initialStock + 2);
    }

    [Fact]
    public async Task CancelOrder_AsManager_ShouldCancelAnyOrder()
    {
        // Act
        var response = await _managerClient.PostAsync($"{BaseRoute}/{_testOrder.Id.Value}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CancelOrder_AsNonOwner_ShouldReturnForbidden()
    {
        // Act
        var response = await _userClient.PostAsync($"{BaseRoute}/{_testOrder.Id.Value}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
    
    [Fact]
    public async Task CancelOrder_DeliveredOrder_ShouldReturnBadRequest()
    {
        // Arrange
        // Використовуємо новий унікальний ID для створення items, 
        // щоб уникнути конфліктів з існуючими замовленнями в EF Core.
        var tempOrderId = OrderId.New(); 
        var orderItems = new List<OrderItem>
        {
            OrderData.CreateTestOrderItem(tempOrderId, _testProduct.Id)
        };
    
        // Створюємо замовлення. Воно згенерує свій власний фінальний ID.
        var order = OrderData.CreateTestOrder(Guid.Parse(_testUserId), orderItems);
    
        order.UpdateStatus(OrderStatus.Processing);
        order.UpdateStatus(OrderStatus.Shipped);
        order.UpdateStatus(OrderStatus.Delivered);
    
        await Context.Orders.AddAsync(order);
        await SaveChangesAsync();

        // Act
        // Використовуємо ID новоствореного замовлення
        var response = await _userClient.PostAsync($"{BaseRoute}/{order.Id.Value}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GET Tests (All Orders - Admin/Manager)

    [Fact]
    public async Task GetAll_AsManager_ShouldReturnAllOrders()
    {
        // Act
        var response = await _managerClient.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.ToResponseModel<List<OrderDto>>();
        orders.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAll_AsAdmin_ShouldReturnAllOrders()
    {
        // Act
        var response = await _adminClient.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetAll_AsUser_ShouldReturnForbidden()
    {
        // Act
        var response = await _userClient.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region GET Tests (Get by Status)

    [Fact]
    public async Task GetByStatus_AsManager_ShouldReturnOrdersWithStatus()
    {
        // Act
        var response = await _managerClient.GetAsync($"{BaseRoute}/status/Pending");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.ToResponseModel<List<OrderDto>>();
        orders.Should().OnlyContain(o => o.Status == OrderStatus.Pending.ToString());
    }

    [Fact]
    public async Task GetByStatus_WithInvalidStatus_ShouldReturnBadRequest()
    {
        // Act
        var response = await _managerClient.GetAsync($"{BaseRoute}/status/InvalidStatus");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetByStatus_AsUser_ShouldReturnForbidden()
    {
        // Act
        var response = await _userClient.GetAsync($"{BaseRoute}/status/Pending");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region PUT Tests (Update Status)

    [Fact]
    public async Task UpdateStatus_AsManager_ShouldUpdateOrderStatus()
    {
        // Arrange
        var request = OrderData.CreateUpdateOrderStatusDto("Processing");

        // Act
        var response = await _managerClient.PutAsJsonAsync(
            $"{BaseRoute}/{_testOrder.Id.Value}/status",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orderDto = await response.ToResponseModel<OrderDto>();
        orderDto.Status.Should().Be(OrderStatus.Processing.ToString());

        var dbOrder = await Context.Orders
            .AsNoTracking()
            .FirstAsync(o => o.Id == _testOrder.Id);
        dbOrder.Status.Should().Be(OrderStatus.Processing);
    }

    [Fact]
    public async Task UpdateStatus_AsAdmin_ShouldUpdateOrderStatus()
    {
        // Arrange
        var request = OrderData.CreateUpdateOrderStatusDto("Processing");

        // Act
        var response = await _adminClient.PutAsJsonAsync(
            $"{BaseRoute}/{_testOrder.Id.Value}/status",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateStatus_AsUser_ShouldReturnForbidden()
    {
        // Arrange
        var request = OrderData.CreateUpdateOrderStatusDto("Processing");

        // Act
        var response = await _userClient.PutAsJsonAsync(
            $"{BaseRoute}/{_testOrder.Id.Value}/status",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateStatus_WithInvalidTransition_ShouldReturnBadRequest()
    {
        // Arrange - спроба перейти з Pending одразу в Delivered
        var request = OrderData.CreateUpdateOrderStatusDto("Delivered");

        // Act
        var response = await _managerClient.PutAsJsonAsync(
            $"{BaseRoute}/{_testOrder.Id.Value}/status",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateStatus_ValidTransition_ShouldSucceed()
    {
        // Arrange - валідний перехід Pending -> Processing
        var request = OrderData.CreateUpdateOrderStatusDto("Processing");

        // Act
        var response = await _managerClient.PutAsJsonAsync(
            $"{BaseRoute}/{_testOrder.Id.Value}/status",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateStatus_WithInvalidStatus_ShouldReturnBadRequest()
    {
        // Arrange
        var request = OrderData.CreateUpdateOrderStatusDto("InvalidStatus");

        // Act
        var response = await _managerClient.PutAsJsonAsync(
            $"{BaseRoute}/{_testOrder.Id.Value}/status",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    public async Task InitializeAsync()
    {
        await Context.Categories.AddAsync(_testCategory);
        await Context.Products.AddAsync(_testProduct);
        
        // Створюємо тестове замовлення для адміна
        var orderItems = new List<OrderItem>
        {
            OrderData.CreateTestOrderItem(OrderId.New(), _testProduct.Id)
        };
        _testOrder = OrderData.CreateTestOrder(Guid.Parse(_adminUserId), orderItems);
        await Context.Orders.AddAsync(_testOrder);
        
        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        // ВИПРАВЛЕННЯ: Використовуємо метод CleanupDatabaseAsync
        await CleanupDatabaseAsync();
    }
}