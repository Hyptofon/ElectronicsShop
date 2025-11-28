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
    private readonly Product _testProduct;
    private readonly string _testUserId = Guid.NewGuid().ToString();
    private readonly string _adminUserId = Guid.NewGuid().ToString();
    private readonly string _managerUserId = Guid.NewGuid().ToString();
    private readonly HttpClient _userClient;
    private readonly HttpClient _adminClient;
    private readonly HttpClient _managerClient;
    private readonly Order _testOrder;

    public OrdersControllerTests(IntegrationTestWebFactory factory) : base(factory)
    {
        _userClient = CreateAuthenticatedClient("User", _testUserId);
        _adminClient = CreateAuthenticatedClient("Admin", _adminUserId);
        _managerClient = CreateAuthenticatedClient("Manager", _managerUserId);
        
        _testProduct = ProductData.FirstTestProduct(new List<CategoryId> { _testCategory.Id });
        
        var orderItems = new List<OrderItem>
        {
            OrderData.CreateTestOrderItem(OrderId.New(), _testProduct.Id)
        };
        _testOrder = OrderData.CreateTestOrder(Guid.Parse(_adminUserId), orderItems);
    }

    #region GET Tests (My Orders)

    [Fact]
    public async Task GetMyOrders_WhenUserHasOrders_ReturnsOwnOrders()
    {
        // Arrange
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
        orders.First().Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetMyOrders_WhenUserHasNoOrders_ReturnsEmptyList()
    {
        // Act
        var response = await _userClient.GetAsync($"{BaseRoute}/my");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.ToResponseModel<List<OrderDto>>();
        orders.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyOrders_WhenOtherUsersHaveOrders_ReturnsOnlyOwnOrders()
    {
        // Arrange
        var otherUserId = Guid.NewGuid();
        var orderItems = new List<OrderItem>
        {
            OrderData.CreateTestOrderItem(OrderId.New(), _testProduct.Id)
        };
        var otherOrder = OrderData.CreateTestOrder(otherUserId, orderItems);
        await Context.Orders.AddAsync(otherOrder);
        await SaveChangesAsync();

        // Act
        var response = await _userClient.GetAsync($"{BaseRoute}/my");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.ToResponseModel<List<OrderDto>>();
        orders.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyOrders_WhenUnauthorized_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"{BaseRoute}/my");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyOrders_ReturnsOrdersOrderedByCreatedAtDescending()
    {
        // Arrange
        var orderItems1 = new List<OrderItem>
        {
            OrderData.CreateTestOrderItem(OrderId.New(), _testProduct.Id)
        };
        var order1 = OrderData.CreateTestOrder(Guid.Parse(_testUserId), orderItems1);
        
        await Context.Orders.AddAsync(order1);
        await SaveChangesAsync();
        
        await Task.Delay(100);
        
        var orderItems2 = new List<OrderItem>
        {
            OrderData.CreateTestOrderItem(OrderId.New(), _testProduct.Id)
        };
        var order2 = OrderData.CreateTestOrder(Guid.Parse(_testUserId), orderItems2);
        await Context.Orders.AddAsync(order2);
        await SaveChangesAsync();

        // Act
        var response = await _userClient.GetAsync($"{BaseRoute}/my");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.ToResponseModel<List<OrderDto>>();
        orders.Should().HaveCount(2);
        orders[0].CreatedAt.Should().BeAfter(orders[1].CreatedAt);
    }

    #endregion

    #region GET Tests (Get by Id)

    [Fact]
    public async Task GetOrderById_WhenOwner_ReturnsOrder()
    {
        // Arrange
        var orderItems = new List<OrderItem>
        {
            OrderData.CreateTestOrderItem(OrderId.New(), _testProduct.Id)
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
        orderDto.UserId.Should().Be(Guid.Parse(_testUserId));
        orderDto.Items.Should().HaveCount(1);
        orderDto.TotalAmount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetOrderById_WhenManager_ReturnsAnyOrder()
    {
        // Act
        var response = await _managerClient.GetAsync($"{BaseRoute}/{_testOrder.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orderDto = await response.ToResponseModel<OrderDto>();
        orderDto.Id.Should().Be(_testOrder.Id.Value);
    }

    [Fact]
    public async Task GetOrderById_WhenAdmin_ReturnsAnyOrder()
    {
        // Act
        var response = await _adminClient.GetAsync($"{BaseRoute}/{_testOrder.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orderDto = await response.ToResponseModel<OrderDto>();
        orderDto.Id.Should().Be(_testOrder.Id.Value);
    }

    [Fact]
    public async Task GetOrderById_WhenNotOwnerAndNotManagerOrAdmin_ReturnsNotFound()
    {
        // Act
        var response = await _userClient.GetAsync($"{BaseRoute}/{_testOrder.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrderById_WhenOrderNotFound_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _userClient.GetAsync($"{BaseRoute}/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrderById_WhenUnauthorized_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"{BaseRoute}/{_testOrder.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST Tests (Create Order)

    [Fact]
    public async Task CreateOrder_WithValidCart_CreatesOrderAndClearsCart()
    {
        // Arrange
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
        orderDto.ShippingAddress.Should().Be(request.ShippingAddress);
        orderDto.Notes.Should().Be(request.Notes);

        var dbCart = await Context.Carts
            .Include(c => c.Items)
            .FirstAsync(c => c.Id == cart.Id);
        dbCart.Items.Should().BeEmpty();

        var dbProduct = await Context.Products
            .AsNoTracking()
            .FirstAsync(p => p.Id == _testProduct.Id);
        dbProduct.StockQuantity.Should().Be(initialStock - 2);
    }

    [Fact]
    public async Task CreateOrder_WithMultipleItemsInCart_CreatesOrderWithAllItems()
    {
        // Arrange
        var product2 = ProductData.SecondTestProduct(new List<CategoryId> { _testCategory.Id });
        await Context.Products.AddAsync(product2);
        await SaveChangesAsync();

        var cart = Domain.Cart.Cart.New(Guid.Parse(_testUserId));
        var cartItem1 = CartItem.New(cart.Id, _testProduct.Id, 1);
        var cartItem2 = CartItem.New(cart.Id, product2.Id, 3);
        cart.AddItem(cartItem1);
        cart.AddItem(cartItem2);
        await Context.Carts.AddAsync(cart);
        await SaveChangesAsync();

        var request = OrderData.CreateTestOrderDto();

        // Act
        var response = await _userClient.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orderDto = await response.ToResponseModel<OrderDto>();
        orderDto.Items.Should().HaveCount(2);
        orderDto.TotalAmount.Should().Be((_testProduct.Price * 1) + (product2.Price * 3));
    }

    [Fact]
    public async Task CreateOrder_WhenCartIsEmpty_ReturnsBadRequest()
    {
        // Arrange
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
    public async Task CreateOrder_WhenNoCart_ReturnsBadRequest()
    {
        // Arrange
        var request = OrderData.CreateTestOrderDto();

        // Act
        var response = await _userClient.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateOrder_WhenInsufficientStock_ReturnsBadRequest()
    {
        // Arrange
        var cart = Domain.Cart.Cart.New(Guid.Parse(_testUserId));
        var cartItem = CartItem.New(cart.Id, _testProduct.Id, 1000);
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
    [InlineData("")]
    [InlineData(null)]
    public async Task CreateOrder_WithInvalidAddress_ReturnsBadRequest(string address)
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
    public async Task CreateOrder_WhenUnauthorized_ReturnsUnauthorized()
    {
        // Arrange
        var request = OrderData.CreateTestOrderDto();

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateOrder_WithoutNotes_CreatesOrderSuccessfully()
    {
        // Arrange
        var cart = Domain.Cart.Cart.New(Guid.Parse(_testUserId));
        var cartItem = CartItem.New(cart.Id, _testProduct.Id, 1);
        cart.AddItem(cartItem);
        await Context.Carts.AddAsync(cart);
        await SaveChangesAsync();

        var request = new CreateOrderDto("123 Test St", null);

        // Act
        var response = await _userClient.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orderDto = await response.ToResponseModel<OrderDto>();
        orderDto.Notes.Should().BeNull();
    }

    #endregion

    #region POST Tests (Cancel Order)

    [Fact]
    public async Task CancelOrder_AsOwner_CancelsOrderAndRestoresStock()
    {
        // Arrange
        var orderItems = new List<OrderItem>
        {
            OrderData.CreateTestOrderItem(OrderId.New(), _testProduct.Id)
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
        orderDto.UpdatedAt.Should().NotBeNull();

        var dbProduct = await Context.Products
            .AsNoTracking()
            .FirstAsync(p => p.Id == _testProduct.Id);
        dbProduct.StockQuantity.Should().Be(initialStock + 2);
    }

    [Fact]
    public async Task CancelOrder_AsManager_CancelsAnyOrder()
    {
        // Act
        var response = await _managerClient.PostAsync($"{BaseRoute}/{_testOrder.Id.Value}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orderDto = await response.ToResponseModel<OrderDto>();
        orderDto.Status.Should().Be(OrderStatus.Cancelled.ToString());
    }

    [Fact]
    public async Task CancelOrder_AsAdmin_CancelsAnyOrder()
    {
        // Arrange
        var orderItems = new List<OrderItem>
        {
            OrderData.CreateTestOrderItem(OrderId.New(), _testProduct.Id)
        };
        var userOrder = OrderData.CreateTestOrder(Guid.Parse(_testUserId), orderItems);
        await Context.Orders.AddAsync(userOrder);
        await SaveChangesAsync();

        // Act
        var response = await _adminClient.PostAsync($"{BaseRoute}/{userOrder.Id.Value}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orderDto = await response.ToResponseModel<OrderDto>();
        orderDto.Status.Should().Be(OrderStatus.Cancelled.ToString());
    }

    [Fact]
    public async Task CancelOrder_WhenNotOwner_ReturnsForbidden()
    {
        // Act
        var response = await _userClient.PostAsync($"{BaseRoute}/{_testOrder.Id.Value}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CancelOrder_WhenAlreadyDelivered_ReturnsBadRequest()
    {
        // Arrange
        var orderItems = new List<OrderItem>
        {
            OrderData.CreateTestOrderItem(OrderId.New(), _testProduct.Id)
        };
        var order = OrderData.CreateTestOrder(Guid.Parse(_testUserId), orderItems);
        order.UpdateStatus(OrderStatus.Processing);
        order.UpdateStatus(OrderStatus.Shipped);
        order.UpdateStatus(OrderStatus.Delivered);
        
        await Context.Orders.AddAsync(order);
        await SaveChangesAsync();

        // Act
        var response = await _userClient.PostAsync($"{BaseRoute}/{order.Id.Value}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CancelOrder_WhenOrderNotFound_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _userClient.PostAsync($"{BaseRoute}/{nonExistentId}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CancelOrder_WhenUnauthorized_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.PostAsync($"{BaseRoute}/{_testOrder.Id.Value}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CancelOrder_WhenOrderInProcessing_CancelsSuccessfully()
    {
        // Arrange
        var orderItems = new List<OrderItem>
        {
            OrderData.CreateTestOrderItem(OrderId.New(), _testProduct.Id)
        };
        var order = OrderData.CreateTestOrder(Guid.Parse(_testUserId), orderItems);
        order.UpdateStatus(OrderStatus.Processing);
        await Context.Orders.AddAsync(order);
        await SaveChangesAsync();

        // Act
        var response = await _userClient.PostAsync($"{BaseRoute}/{order.Id.Value}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orderDto = await response.ToResponseModel<OrderDto>();
        orderDto.Status.Should().Be(OrderStatus.Cancelled.ToString());
    }

    #endregion

    #region GET Tests (All Orders)

    [Fact]
    public async Task GetAllOrders_AsManager_ReturnsAllOrders()
    {
        // Act
        var response = await _managerClient.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.ToResponseModel<List<OrderDto>>();
        orders.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAllOrders_AsAdmin_ReturnsAllOrders()
    {
        // Act
        var response = await _adminClient.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.ToResponseModel<List<OrderDto>>();
        orders.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetAllOrders_AsRegularUser_ReturnsForbidden()
    {
        // Act
        var response = await _userClient.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllOrders_WhenUnauthorized_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAllOrders_ReturnsOrdersOrderedByCreatedAtDescending()
    {
        // Arrange
        var orderItems = new List<OrderItem>
        {
            OrderData.CreateTestOrderItem(OrderId.New(), _testProduct.Id)
        };
        var order = OrderData.CreateTestOrder(Guid.Parse(_testUserId), orderItems);
        await Context.Orders.AddAsync(order);
        await SaveChangesAsync();

        // Act
        var response = await _managerClient.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.ToResponseModel<List<OrderDto>>();
        orders.Should().HaveCountGreaterThan(1);
        for (int i = 0; i < orders.Count - 1; i++)
        {
            orders[i].CreatedAt.Should().BeOnOrAfter(orders[i + 1].CreatedAt);
        }
    }

    #endregion

    #region GET Tests (Get by Status)

    [Fact]
    public async Task GetOrdersByStatus_AsManager_ReturnsOrdersWithStatus()
    {
        // Act
        var response = await _managerClient.GetAsync($"{BaseRoute}/status/Pending");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.ToResponseModel<List<OrderDto>>();
        orders.Should().OnlyContain(o => o.Status == OrderStatus.Pending.ToString());
    }

    [Fact]
    public async Task GetOrdersByStatus_AsAdmin_ReturnsOrdersWithStatus()
    {
        // Act
        var response = await _adminClient.GetAsync($"{BaseRoute}/status/Pending");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.ToResponseModel<List<OrderDto>>();
        orders.Should().OnlyContain(o => o.Status == OrderStatus.Pending.ToString());
    }

    [Fact]
    public async Task GetOrdersByStatus_WithInvalidStatus_ReturnsBadRequest()
    {
        // Act
        var response = await _managerClient.GetAsync($"{BaseRoute}/status/InvalidStatus");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetOrdersByStatus_AsRegularUser_ReturnsForbidden()
    {
        // Act
        var response = await _userClient.GetAsync($"{BaseRoute}/status/Pending");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetOrdersByStatus_WhenUnauthorized_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"{BaseRoute}/status/Pending");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("Pending")]
    [InlineData("Processing")]
    [InlineData("Shipped")]
    [InlineData("Delivered")]
    [InlineData("Cancelled")]
    public async Task GetOrdersByStatus_WithValidStatus_ReturnsCorrectOrders(string status)
    {
        // Act
        var response = await _managerClient.GetAsync($"{BaseRoute}/status/{status}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.ToResponseModel<List<OrderDto>>();
        orders.Should().OnlyContain(o => o.Status == status);
    }

    #endregion

    #region PUT Tests (Update Status)

    [Fact]
    public async Task UpdateOrderStatus_AsManager_UpdatesStatus()
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
        orderDto.UpdatedAt.Should().NotBeNull();

        var dbOrder = await Context.Orders
            .AsNoTracking()
            .FirstAsync(o => o.Id == _testOrder.Id);
        dbOrder.Status.Should().Be(OrderStatus.Processing);
    }

    [Fact]
    public async Task UpdateOrderStatus_AsAdmin_UpdatesStatus()
    {
        // Arrange
        var request = OrderData.CreateUpdateOrderStatusDto("Processing");

        // Act
        var response = await _adminClient.PutAsJsonAsync(
            $"{BaseRoute}/{_testOrder.Id.Value}/status",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orderDto = await response.ToResponseModel<OrderDto>();
        orderDto.Status.Should().Be(OrderStatus.Processing.ToString());
    }

    [Fact]
    public async Task UpdateOrderStatus_AsRegularUser_ReturnsForbidden()
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
    public async Task UpdateOrderStatus_WithInvalidStatus_ReturnsBadRequest()
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

    [Fact]
    public async Task UpdateOrderStatus_WhenOrderNotFound_ReturnsNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var request = OrderData.CreateUpdateOrderStatusDto("Processing");

        // Act
        var response = await _managerClient.PutAsJsonAsync(
            $"{BaseRoute}/{nonExistentId}/status",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateOrderStatus_WhenUnauthorized_ReturnsUnauthorized()
    {
        // Arrange
        var request = OrderData.CreateUpdateOrderStatusDto("Processing");

        // Act
        var response = await Client.PutAsJsonAsync(
            $"{BaseRoute}/{_testOrder.Id.Value}/status",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("Pending", "Processing")]
    [InlineData("Processing", "Shipped")]
    [InlineData("Shipped", "Delivered")]
    public async Task UpdateOrderStatus_WithValidTransition_UpdatesSuccessfully(
        string currentStatus, 
        string newStatus)
    {
        // Arrange
        var orderItems = new List<OrderItem>
        {
            OrderData.CreateTestOrderItem(OrderId.New(), _testProduct.Id)
        };
        var order = OrderData.CreateTestOrder(Guid.Parse(_adminUserId), orderItems);
        
        if (Enum.TryParse<OrderStatus>(currentStatus, out var status))
        {
            order.UpdateStatus(status);
        }
        
        await Context.Orders.AddAsync(order);
        await SaveChangesAsync();

        var request = OrderData.CreateUpdateOrderStatusDto(newStatus);

        // Act
        var response = await _managerClient.PutAsJsonAsync(
            $"{BaseRoute}/{order.Id.Value}/status",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orderDto = await response.ToResponseModel<OrderDto>();
        orderDto.Status.Should().Be(newStatus);
    }

    #endregion

    public async Task InitializeAsync()
    {
        await Context.Categories.AddAsync(_testCategory);
        await Context.Products.AddAsync(_testProduct);
        await Context.Orders.AddAsync(_testOrder);
        
        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        Context.Orders.RemoveRange(Context.Orders);
        Context.Carts.RemoveRange(Context.Carts);
        Context.Products.RemoveRange(Context.Products);
        Context.Categories.RemoveRange(Context.Categories);
        
        await SaveChangesAsync();
    }
}