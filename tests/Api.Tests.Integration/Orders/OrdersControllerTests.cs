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
    private readonly HttpClient _userClient;
    private readonly HttpClient _adminClient;
    private readonly HttpClient _managerClient;
    private readonly Order _testOrder;

    public OrdersControllerTests(IntegrationTestWebFactory factory) : base(factory)
    {
        _userClient = CreateAuthenticatedClient("User", _testUserId);
        _adminClient = CreateAuthenticatedClient("Admin", _adminUserId);
        _managerClient = CreateAuthenticatedClient("Manager");
        
        _testProduct = ProductData.FirstTestProduct(new List<CategoryId> { _testCategory.Id });
        
        var orderItems = new List<OrderItem>
        {
            OrderData.CreateTestOrderItem(OrderId.New(), _testProduct.Id)
        };
        _testOrder = OrderData.CreateTestOrder(Guid.Parse(_adminUserId), orderItems);
    }

    #region GET Tests (My Orders)

    [Fact]
    public async Task ShouldGetMyOrders()
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
    }

    [Fact]
    public async Task ShouldNotGetOtherUsersOrders()
    {
        // Arrange
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
    public async Task ShouldNotGetMyOrdersBecauseUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"{BaseRoute}/my");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET Tests (Get by Id)

    [Fact]
    public async Task ShouldGetOrderByIdAsOwner()
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
        orderDto.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task ShouldGetAnyOrderAsManager()
    {
        // Act
        var response = await _managerClient.GetAsync($"{BaseRoute}/{_testOrder.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ShouldGetAnyOrderAsAdmin()
    {
        // Act
        var response = await _adminClient.GetAsync($"{BaseRoute}/{_testOrder.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ShouldNotGetOrderByIdBecauseNotOwner()
    {
        // Act
        var response = await _userClient.GetAsync($"{BaseRoute}/{_testOrder.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldNotGetOrderByIdBecauseOrderNotFound()
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
    public async Task ShouldCreateOrderFromCartAndClearCart()
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
    public async Task ShouldNotCreateOrderBecauseCartIsEmpty()
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
    public async Task ShouldNotCreateOrderBecauseNoCart()
    {
        // Arrange
        var request = OrderData.CreateTestOrderDto();

        // Act
        var response = await _userClient.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateOrderBecauseInsufficientStock()
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
    public async Task ShouldNotCreateOrderBecauseInvalidAddress(string address)
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
    public async Task ShouldNotCreateOrderBecauseUnauthorized()
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
    public async Task ShouldCancelOrderAndRestoreStockAsOwner()
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

        var dbProduct = await Context.Products
            .AsNoTracking()
            .FirstAsync(p => p.Id == _testProduct.Id);
        dbProduct.StockQuantity.Should().Be(initialStock + 2);
    }

    [Fact]
    public async Task ShouldCancelAnyOrderAsManager()
    {
        // Act
        var response = await _managerClient.PostAsync($"{BaseRoute}/{_testOrder.Id.Value}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ShouldNotCancelOrderBecauseNotOwner()
    {
        // Act
        var response = await _userClient.PostAsync($"{BaseRoute}/{_testOrder.Id.Value}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
    
    [Fact]
    public async Task ShouldNotCancelOrderBecauseAlreadyDelivered()
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

    #endregion

    #region GET Tests (All Orders)

    [Fact]
    public async Task ShouldGetAllOrdersAsManager()
    {
        // Act
        var response = await _managerClient.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.ToResponseModel<List<OrderDto>>();
        orders.Should().HaveCount(1);
    }

    [Fact]
    public async Task ShouldGetAllOrdersAsAdmin()
    {
        // Act
        var response = await _adminClient.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ShouldNotGetAllOrdersBecauseForbidden()
    {
        // Act
        var response = await _userClient.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region GET Tests (Get by Status)

    [Fact]
    public async Task ShouldGetOrdersByStatusAsManager()
    {
        // Act
        var response = await _managerClient.GetAsync($"{BaseRoute}/status/Pending");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.ToResponseModel<List<OrderDto>>();
        orders.Should().OnlyContain(o => o.Status == OrderStatus.Pending.ToString());
    }

    [Fact]
    public async Task ShouldNotGetOrdersByStatusBecauseInvalidStatus()
    {
        // Act
        var response = await _managerClient.GetAsync($"{BaseRoute}/status/InvalidStatus");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotGetOrdersByStatusBecauseForbidden()
    {
        // Act
        var response = await _userClient.GetAsync($"{BaseRoute}/status/Pending");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region PUT Tests (Update Status)

    [Fact]
    public async Task ShouldUpdateOrderStatusAsManager()
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
    public async Task ShouldUpdateOrderStatusAsAdmin()
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
    public async Task ShouldNotUpdateOrderStatusBecauseForbidden()
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
    public async Task ShouldNotUpdateOrderStatusBecauseInvalidTransition()
    {
        // Arrange
        var request = OrderData.CreateUpdateOrderStatusDto("Delivered");

        // Act
        var response = await _managerClient.PutAsJsonAsync(
            $"{BaseRoute}/{_testOrder.Id.Value}/status",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldUpdateOrderStatusWithValidTransition()
    {
        // Arrange
        var request = OrderData.CreateUpdateOrderStatusDto("Processing");

        // Act
        var response = await _managerClient.PutAsJsonAsync(
            $"{BaseRoute}/{_testOrder.Id.Value}/status",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ShouldNotUpdateOrderStatusBecauseInvalidStatus()
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