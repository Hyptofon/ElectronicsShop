using System.Net;
using System.Net.Http.Json;
using Api.Dtos;
using Domain.Cart;
using Domain.Categories;
using Domain.Products;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data.Carts;
using Tests.Data.Categories;
using Tests.Data.Products;
using Xunit;

namespace Api.Tests.Integration.Cart;

public class CartControllerTests : BaseIntegrationTest, IAsyncLifetime
{
    private const string BaseRoute = "cart";
    private readonly Category _testCategory = CategoryData.FirstTestCategory();
    private Product _testProduct;
    private readonly string _testUserId = Guid.NewGuid().ToString();
    private HttpClient _userClient;

    public CartControllerTests(IntegrationTestWebFactory factory) : base(factory)
    {
        _userClient = CreateAuthenticatedClient("User", _testUserId);
        _testProduct = ProductData.FirstTestProduct(new List<CategoryId> { _testCategory.Id });
    }

    #region GET Tests

    [Fact]
    public async Task GetMyCart_WhenCartExists_ShouldReturnCart()
    {
        // Arrange
        var cart = Domain.Cart.Cart.New(Guid.Parse(_testUserId));
        var cartItem = CartItem.New(cart.Id, _testProduct.Id, 2);
        cart.AddItem(cartItem);
        await Context.Carts.AddAsync(cart);
        await SaveChangesAsync();

        // Act
        var response = await _userClient.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cartDto = await response.ToResponseModel<CartDto>();
        cartDto.Items.Should().HaveCount(1);
        cartDto.Items.First().ProductId.Should().Be(_testProduct.Id.Value);
        cartDto.Items.First().Quantity.Should().Be(2);
    }

    [Fact]
    public async Task GetMyCart_WhenCartDoesNotExist_ShouldReturnNotFound()
    {
        // Act
        var response = await _userClient.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMyCart_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Act
        var response = await Client.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST Tests (Add to Cart)

    [Fact]
    public async Task AddToCart_WithValidProduct_ShouldAddItemToCart()
    {
        // Arrange
        var request = CartData.CreateAddToCartDto(_testProduct.Id.Value, 3);

        // Act
        var response = await _userClient.PostAsJsonAsync($"{BaseRoute}/items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cartDto = await response.ToResponseModel<CartDto>();
        cartDto.Items.Should().HaveCount(1);
        cartDto.Items.First().ProductId.Should().Be(_testProduct.Id.Value);
        cartDto.Items.First().Quantity.Should().Be(3);

        var dbCart = await Context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == Guid.Parse(_testUserId));
        dbCart.Should().NotBeNull();
        dbCart!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddToCart_WhenCartDoesNotExist_ShouldCreateCartAndAddItem()
    {
        // Arrange
        var request = CartData.CreateAddToCartDto(_testProduct.Id.Value, 1);

        // Act
        var response = await _userClient.PostAsJsonAsync($"{BaseRoute}/items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var dbCart = await Context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == Guid.Parse(_testUserId));
        dbCart.Should().NotBeNull();
    }

    [Fact]
    public async Task AddToCart_WhenProductAlreadyInCart_ShouldIncreaseQuantity()
    {
        // Arrange
        var cart = Domain.Cart.Cart.New(Guid.Parse(_testUserId));
        var cartItem = CartItem.New(cart.Id, _testProduct.Id, 2);
        cart.AddItem(cartItem);
        await Context.Carts.AddAsync(cart);
        await SaveChangesAsync();

        var request = CartData.CreateAddToCartDto(_testProduct.Id.Value, 3);

        // Act
        var response = await _userClient.PostAsJsonAsync($"{BaseRoute}/items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cartDto = await response.ToResponseModel<CartDto>();
        cartDto.Items.Should().HaveCount(1);
        cartDto.Items.First().Quantity.Should().Be(5); // 2 + 3
    }

    [Fact]
    public async Task AddToCart_WithNonExistentProduct_ShouldReturnNotFound()
    {
        // Arrange
        var request = CartData.CreateAddToCartDto(Guid.NewGuid(), 1);

        // Act
        var response = await _userClient.PostAsJsonAsync($"{BaseRoute}/items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddToCart_WithInsufficientStock_ShouldReturnBadRequest()
    {
        // Arrange
        var request = CartData.CreateAddToCartDto(_testProduct.Id.Value, 1000); // More than stock

        // Act
        var response = await _userClient.PostAsJsonAsync($"{BaseRoute}/items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(0)] // Zero quantity
    [InlineData(-1)] // Negative quantity
    public async Task AddToCart_WithInvalidQuantity_ShouldReturnBadRequest(int quantity)
    {
        // Arrange
        var request = CartData.CreateAddToCartDto(_testProduct.Id.Value, quantity);

        // Act
        var response = await _userClient.PostAsJsonAsync($"{BaseRoute}/items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddToCart_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = CartData.CreateAddToCartDto(_testProduct.Id.Value, 1);

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region PUT Tests (Update Cart Item)

    [Fact]
    public async Task UpdateCartItem_WithValidQuantity_ShouldUpdateQuantity()
    {
        // Arrange
        var cart = Domain.Cart.Cart.New(Guid.Parse(_testUserId));
        var cartItem = CartItem.New(cart.Id, _testProduct.Id, 2);
        cart.AddItem(cartItem);
        await Context.Carts.AddAsync(cart);
        await SaveChangesAsync();

        var request = CartData.CreateUpdateCartItemDto(5);

        // Act
        var response = await _userClient.PutAsJsonAsync(
            $"{BaseRoute}/items/{cartItem.Id.Value}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cartDto = await response.ToResponseModel<CartDto>();
        cartDto.Items.First().Quantity.Should().Be(5);

        var dbCart = await Context.Carts
            .Include(c => c.Items)
            .AsNoTracking()
            .FirstAsync(c => c.Id == cart.Id);
        dbCart.Items.First().Quantity.Should().Be(5);
    }

    [Fact]
    public async Task UpdateCartItem_NonExistentItem_ShouldReturnBadRequest()
    {
        // Arrange
        var cart = Domain.Cart.Cart.New(Guid.Parse(_testUserId));
        await Context.Carts.AddAsync(cart);
        await SaveChangesAsync();

        var request = CartData.CreateUpdateCartItemDto(5);
        var nonExistentItemId = Guid.NewGuid();

        // Act
        var response = await _userClient.PutAsJsonAsync(
            $"{BaseRoute}/items/{nonExistentItemId}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(0)] // Zero quantity
    [InlineData(-1)] // Negative quantity
    public async Task UpdateCartItem_WithInvalidQuantity_ShouldReturnBadRequest(int quantity)
    {
        // Arrange
        var cart = Domain.Cart.Cart.New(Guid.Parse(_testUserId));
        var cartItem = CartItem.New(cart.Id, _testProduct.Id, 2);
        cart.AddItem(cartItem);
        await Context.Carts.AddAsync(cart);
        await SaveChangesAsync();

        var request = CartData.CreateUpdateCartItemDto(quantity);

        // Act
        var response = await _userClient.PutAsJsonAsync(
            $"{BaseRoute}/items/{cartItem.Id.Value}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region DELETE Tests (Remove from Cart)

    [Fact]
    public async Task RemoveFromCart_WithValidItem_ShouldRemoveItem()
    {
        // Arrange
        var cart = Domain.Cart.Cart.New(Guid.Parse(_testUserId));
        var cartItem = CartItem.New(cart.Id, _testProduct.Id, 2);
        cart.AddItem(cartItem);
        await Context.Carts.AddAsync(cart);
        await SaveChangesAsync();

        // Act
        var response = await _userClient.DeleteAsync($"{BaseRoute}/items/{cartItem.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cartDto = await response.ToResponseModel<CartDto>();
        cartDto.Items.Should().BeEmpty();

        var dbCart = await Context.Carts
            .Include(c => c.Items)
            .AsNoTracking()
            .FirstAsync(c => c.Id == cart.Id);
        dbCart.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveFromCart_NonExistentItem_ShouldNotFail()
    {
        // Arrange
        var cart = Domain.Cart.Cart.New(Guid.Parse(_testUserId));
        await Context.Carts.AddAsync(cart);
        await SaveChangesAsync();

        var nonExistentItemId = Guid.NewGuid();

        // Act
        var response = await _userClient.DeleteAsync($"{BaseRoute}/items/{nonExistentItemId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region DELETE Tests (Clear Cart)

    [Fact]
    public async Task ClearCart_WithItems_ShouldRemoveAllItems()
    {
        // Arrange
        var cart = Domain.Cart.Cart.New(Guid.Parse(_testUserId));
        var cartItem1 = CartItem.New(cart.Id, _testProduct.Id, 2);
        cart.AddItem(cartItem1);
        
        var product2 = ProductData.SecondTestProduct(new List<CategoryId> { _testCategory.Id });
        await Context.Products.AddAsync(product2);
        await SaveChangesAsync();
        
        var cartItem2 = CartItem.New(cart.Id, product2.Id, 1);
        cart.AddItem(cartItem2);
        
        await Context.Carts.AddAsync(cart);
        await SaveChangesAsync();

        // Act
        var response = await _userClient.DeleteAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cartDto = await response.ToResponseModel<CartDto>();
        cartDto.Items.Should().BeEmpty();

        var dbCart = await Context.Carts
            .Include(c => c.Items)
            .AsNoTracking()
            .FirstAsync(c => c.Id == cart.Id);
        dbCart.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearCart_WhenCartDoesNotExist_ShouldReturnNotFound()
    {
        // Act
        var response = await _userClient.DeleteAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    public async Task InitializeAsync()
    {
        await Context.Categories.AddAsync(_testCategory);
        await Context.Products.AddAsync(_testProduct);
        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        Context.Carts.RemoveRange(Context.Carts);
        Context.Products.RemoveRange(Context.Products);
        Context.Categories.RemoveRange(Context.Categories);
        await SaveChangesAsync();
    }
}