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

namespace Api.Tests.Integration.Cart;

public class CartControllerTests : BaseIntegrationTest, IAsyncLifetime
{
    private const string BaseRoute = "cart";
    
    private Category _testCategory = CategoryData.FirstTestCategory("Cart");
    private readonly Product _testProduct;
    private readonly string _testUserId = Guid.NewGuid().ToString();
    private readonly HttpClient _userClient;
    private readonly Domain.Cart.Cart _testCart;

    public CartControllerTests(IntegrationTestWebFactory factory) : base(factory)
    {
        _userClient = CreateAuthenticatedClient("User", _testUserId);
        _testProduct = ProductData.FirstTestProduct(new List<CategoryId> { _testCategory.Id });
        _testCart = CartData.CreateTestCart(Guid.Parse(_testUserId));
    }

    #region GET Tests

    [Fact]
    public async Task ShouldGetMyCart()
    {
        // Arrange
        var cartItem = CartData.CreateTestCartItem(_testCart.Id, _testProduct.Id, 2);
        _testCart.AddItem(cartItem);

        // Додаємо CartItem до DbContext для відстеження
        await Context.CartItems.AddAsync(cartItem);

        // 2. ЗБЕРІГАЄМО ВСІ ЗМІНИ ОДНИМ ВИКЛИКОМ
        // Тепер DbContext збереже і оновлений _testCart (хоча він уже відстежується),
        // і новий cartItem.
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
    public async Task ShouldNotGetMyCartBecauseCartDoesNotExist()
    {
        // Arrange
        Context.Carts.Remove(_testCart);
        await SaveChangesAsync();

        // Act
        var response = await _userClient.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldNotGetMyCartBecauseUnauthorized()
    {
        // Act
        var response = await Client.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST Tests

    [Fact]
    public async Task ShouldAddToCart()
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
            .FirstAsync(c => c.UserId == Guid.Parse(_testUserId));
        dbCart.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task ShouldCreateCartAndAddItemWhenCartDoesNotExist()
    {
        // Arrange
        Context.Carts.Remove(_testCart);
        await SaveChangesAsync();
        
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
    public async Task ShouldIncreaseQuantityWhenProductAlreadyInCart()
    {
        // Arrange
        var cartItem = CartData.CreateTestCartItem(_testCart.Id, _testProduct.Id, 2);
        _testCart.AddItem(cartItem);
        
        var request = CartData.CreateAddToCartDto(_testProduct.Id.Value, 3);

        // Act
        var response = await _userClient.PostAsJsonAsync($"{BaseRoute}/items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cartDto = await response.ToResponseModel<CartDto>();
        cartDto.Items.Should().HaveCount(1);
        cartDto.Items.First().Quantity.Should().Be(3);
    }

    [Fact]
    public async Task ShouldNotAddToCartBecauseProductNotFound()
    {
        // Arrange
        var request = CartData.CreateAddToCartDto(Guid.NewGuid(), 1);

        // Act
        var response = await _userClient.PostAsJsonAsync($"{BaseRoute}/items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldNotAddToCartBecauseInsufficientStock()
    {
        // Arrange
        var request = CartData.CreateAddToCartDto(_testProduct.Id.Value, 1000);

        // Act
        var response = await _userClient.PostAsJsonAsync($"{BaseRoute}/items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ShouldNotAddToCartBecauseInvalidQuantity(int quantity)
    {
        // Arrange
        var request = CartData.CreateAddToCartDto(_testProduct.Id.Value, quantity);

        // Act
        var response = await _userClient.PostAsJsonAsync($"{BaseRoute}/items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotAddToCartBecauseUnauthorized()
    {
        // Arrange
        var request = CartData.CreateAddToCartDto(_testProduct.Id.Value, 1);

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region PUT Tests

    [Fact]
    public async Task ShouldUpdateCartItemQuantity()
    {
        // Arrange
        var cartItem = CartData.CreateTestCartItem(_testCart.Id, _testProduct.Id, 2);
        _testCart.AddItem(cartItem);
        await Context.CartItems.AddAsync(cartItem);
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
            .FirstAsync(c => c.Id == _testCart.Id);
        dbCart.Items.First().Quantity.Should().Be(5);
    }

    [Fact]
    public async Task ShouldNotUpdateCartItemBecauseItemNotFound()
    {
        // Arrange
        var request = CartData.CreateUpdateCartItemDto(5);
        var nonExistentItemId = Guid.NewGuid();

        // Act
        var response = await _userClient.PutAsJsonAsync(
            $"{BaseRoute}/items/{nonExistentItemId}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ShouldNotUpdateCartItemBecauseInvalidQuantity(int quantity)
    {
        // Arrange
        var cartItem = CartData.CreateTestCartItem(_testCart.Id, _testProduct.Id, 2);
        _testCart.AddItem(cartItem);
        
        var request = CartData.CreateUpdateCartItemDto(quantity);

        // Act
        var response = await _userClient.PutAsJsonAsync(
            $"{BaseRoute}/items/{cartItem.Id.Value}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region DELETE Tests (Remove Item)

    [Fact]
    public async Task ShouldRemoveFromCart()
    {
        // Arrange
        var cartItem = CartData.CreateTestCartItem(_testCart.Id, _testProduct.Id, 2);
        _testCart.AddItem(cartItem);

        // Act
        var response = await _userClient.DeleteAsync($"{BaseRoute}/items/{cartItem.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cartDto = await response.ToResponseModel<CartDto>();
        cartDto.Items.Should().BeEmpty();

        var dbCart = await Context.Carts
            .Include(c => c.Items)
            .AsNoTracking()
            .FirstAsync(c => c.Id == _testCart.Id);
        dbCart.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldNotFailWhenRemovingNonExistentItem()
    {
        // Arrange
        var nonExistentItemId = Guid.NewGuid();

        // Act
        var response = await _userClient.DeleteAsync($"{BaseRoute}/items/{nonExistentItemId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region DELETE Tests (Clear Cart)

    [Fact]
    public async Task ShouldClearCart()
    {
        // Arrange
        var cartItem1 = CartData.CreateTestCartItem(_testCart.Id, _testProduct.Id, 2);
        _testCart.AddItem(cartItem1);
        
        var product2 = ProductData.SecondTestProduct(new List<CategoryId> { _testCategory.Id });
        await Context.Products.AddAsync(product2);
        await SaveChangesAsync();
        
        var cartItem2 = CartData.CreateTestCartItem(_testCart.Id, product2.Id, 1);
        _testCart.AddItem(cartItem2);
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
            .FirstAsync(c => c.Id == _testCart.Id);
        dbCart.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldNotClearCartBecauseCartDoesNotExist()
    {
        // Arrange
        Context.Carts.Remove(_testCart);
        await SaveChangesAsync();

        // Act
        var response = await _userClient.DeleteAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    public async Task InitializeAsync()
    {
        var existing = await Context.Categories
            .FirstOrDefaultAsync(c => c.Name == _testCategory.Name);
    
        if (existing == null)
        {
            await Context.Categories.AddAsync(_testCategory);
        }
        else
        {
            _testCategory = existing;
        }
    
        await Context.Products.AddAsync(_testProduct);
        await Context.Carts.AddAsync(_testCart);
        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        Context.Carts.RemoveRange(Context.Carts);
        Context.Products.RemoveRange(Context.Products);
        
        await SaveChangesAsync();
    }
}