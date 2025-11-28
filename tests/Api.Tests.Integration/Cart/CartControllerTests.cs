using System.Net;
using System.Net.Http.Json;
using Api.Dtos;
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
    public async Task GetMyCart_WhenCartExists_ShouldReturnCart()
    {
        // Arrange
        var cartItem = CartData.CreateTestCartItem(_testCart.Id, _testProduct.Id, 2);
        _testCart.AddItem(cartItem);
        await Context.CartItems.AddAsync(cartItem);
        await SaveChangesAsync();
        
        // Act
        var response = await _userClient.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cartDto = await response.ToResponseModel<CartDto>();
        cartDto.Should().NotBeNull();
        cartDto.Id.Should().Be(_testCart.Id.Value);
        cartDto.Items.Should().HaveCount(1);
        cartDto.Items.First().ProductId.Should().Be(_testProduct.Id.Value);
        cartDto.Items.First().Quantity.Should().Be(2);
        cartDto.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetMyCart_WhenCartDoesNotExist_ShouldReturnEmptyCart()
    {
        // Arrange
        Context.Carts.Remove(_testCart);
        await SaveChangesAsync();

        // Act
        var response = await _userClient.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cartDto = await response.ToResponseModel<CartDto>();
        cartDto.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyCart_WhenUnauthorized_ShouldReturnUnauthorized()
    {
        // Act
        var response = await Client.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyCart_WithMultipleItems_ShouldReturnAllItems()
    {
        // Arrange
        var product2 = ProductData.SecondTestProduct(new List<CategoryId> { _testCategory.Id });
        await Context.Products.AddAsync(product2);
        await SaveChangesAsync();

        var cartItem1 = CartData.CreateTestCartItem(_testCart.Id, _testProduct.Id, 2);
        var cartItem2 = CartData.CreateTestCartItem(_testCart.Id, product2.Id, 3);
        _testCart.AddItem(cartItem1);
        _testCart.AddItem(cartItem2);
        await Context.CartItems.AddRangeAsync(cartItem1, cartItem2);
        await SaveChangesAsync();

        // Act
        var response = await _userClient.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cartDto = await response.ToResponseModel<CartDto>();
        cartDto.Items.Should().HaveCount(2);
        cartDto.Items.Should().Contain(i => i.ProductId == _testProduct.Id.Value && i.Quantity == 2);
        cartDto.Items.Should().Contain(i => i.ProductId == product2.Id.Value && i.Quantity == 3);
    }

    [Fact]
    public async Task GetMyCart_AsManager_ShouldReturnManagerCart()
    {
        // Arrange
        var managerUserId = Guid.NewGuid().ToString();
        var managerClient = CreateAuthenticatedClient("Manager", managerUserId);
        
        var managerCart = CartData.CreateTestCart(Guid.Parse(managerUserId));
        var cartItem = CartData.CreateTestCartItem(managerCart.Id, _testProduct.Id);
        managerCart.AddItem(cartItem);
        
        await Context.Carts.AddAsync(managerCart);
        await Context.CartItems.AddAsync(cartItem);
        await SaveChangesAsync();
        
        // Act
        var response = await managerClient.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cartDto = await response.ToResponseModel<CartDto>();
        cartDto.Items.Should().HaveCount(1);
        cartDto.Id.Should().Be(managerCart.Id.Value);
    }

    [Fact]
    public async Task GetMyCart_AsAdmin_ShouldReturnAdminCart()
    {
        // Arrange
        var adminUserId = Guid.NewGuid().ToString();
        var adminClient = CreateAuthenticatedClient("Admin", adminUserId);
        
        var adminCart = CartData.CreateTestCart(Guid.Parse(adminUserId));
        await Context.Carts.AddAsync(adminCart);
        await SaveChangesAsync();
        
        // Act
        var response = await adminClient.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cartDto = await response.ToResponseModel<CartDto>();
        cartDto.Id.Should().Be(adminCart.Id.Value);
    }

    #endregion

    #region POST Tests (Add To Cart)

    [Fact]
    public async Task AddToCart_WithValidData_ShouldAddItemToCart()
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

        // Перевірка в БД
        var dbCart = await Context.Carts
            .Include(c => c.Items)
            .AsNoTracking()
            .FirstAsync(c => c.UserId == Guid.Parse(_testUserId));
        dbCart.Items.Should().HaveCount(1);
        dbCart.Items.First().ProductId.Should().Be(_testProduct.Id);
        dbCart.Items.First().Quantity.Should().Be(3);
    }

    [Fact]
    public async Task AddToCart_WhenCartDoesNotExist_ShouldCreateCartAndAddItem()
    {
        // Arrange
        Context.Carts.Remove(_testCart);
        await SaveChangesAsync();
        
        var request = CartData.CreateAddToCartDto(_testProduct.Id.Value);

        // Act
        var response = await _userClient.PostAsJsonAsync($"{BaseRoute}/items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var dbCart = await Context.Carts
            .Include(c => c.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == Guid.Parse(_testUserId));
        dbCart.Should().NotBeNull();
        dbCart.Items.Should().HaveCount(1);
        dbCart.Items.First().ProductId.Should().Be(_testProduct.Id);
    }

    [Fact]
    public async Task AddToCart_WhenProductAlreadyInCart_ShouldUpdateQuantity()
    {
        // Arrange
        var cartItem = CartData.CreateTestCartItem(_testCart.Id, _testProduct.Id, 2);
        _testCart.AddItem(cartItem);
        await Context.CartItems.AddAsync(cartItem);
        await SaveChangesAsync();
        
        var request = CartData.CreateAddToCartDto(_testProduct.Id.Value, 5);

        // Act
        var response = await _userClient.PostAsJsonAsync($"{BaseRoute}/items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cartDto = await response.ToResponseModel<CartDto>();
        cartDto.Items.Should().HaveCount(1);
        cartDto.Items.First().Quantity.Should().Be(7);
    }

    [Fact]
    public async Task AddToCart_WithNonExistentProduct_ShouldReturnNotFound()
    {
        // Arrange
        var request = CartData.CreateAddToCartDto(Guid.NewGuid());

        // Act
        var response = await _userClient.PostAsJsonAsync($"{BaseRoute}/items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("not found");
    }

    [Fact]
    public async Task AddToCart_WithInsufficientStock_ShouldReturnBadRequest()
    {
        // Arrange
        var request = CartData.CreateAddToCartDto(_testProduct.Id.Value, 1000);

        // Act
        var response = await _userClient.PostAsJsonAsync($"{BaseRoute}/items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Insufficient stock");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
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
    public async Task AddToCart_WithEmptyProductId_ShouldReturnBadRequest()
    {
        // Arrange
        var request = CartData.CreateAddToCartDto(Guid.Empty);

        // Act
        var response = await _userClient.PostAsJsonAsync($"{BaseRoute}/items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddToCart_WhenUnauthorized_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = CartData.CreateAddToCartDto(_testProduct.Id.Value);

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddToCart_MultipleProducts_ShouldAddAllProducts()
    {
        // Arrange
        var product2 = ProductData.SecondTestProduct(new List<CategoryId> { _testCategory.Id });
        await Context.Products.AddAsync(product2);
        await SaveChangesAsync();

        var request1 = CartData.CreateAddToCartDto(_testProduct.Id.Value, 2);
        var request2 = CartData.CreateAddToCartDto(product2.Id.Value, 3);

        // Act
        var response1 = await _userClient.PostAsJsonAsync($"{BaseRoute}/items", request1);
        var response2 = await _userClient.PostAsJsonAsync($"{BaseRoute}/items", request2);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var cart = await response2.ToResponseModel<CartDto>();
        cart.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddToCart_WithExactStockAmount_ShouldSucceed()
    {
        // Arrange
        var request = CartData.CreateAddToCartDto(_testProduct.Id.Value, _testProduct.StockQuantity);

        // Act
        var response = await _userClient.PostAsJsonAsync($"{BaseRoute}/items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cartDto = await response.ToResponseModel<CartDto>();
        cartDto.Items.First().Quantity.Should().Be(_testProduct.StockQuantity);
    }

    #endregion

    #region PUT Tests (Update Cart Item)

    [Fact]
    public async Task UpdateCartItem_WithValidData_ShouldUpdateQuantity()
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

        // Перевірка в БД
        var dbCart = await Context.Carts
            .Include(c => c.Items)
            .AsNoTracking()
            .FirstAsync(c => c.Id == _testCart.Id);
        dbCart.Items.First().Quantity.Should().Be(5);
    }

    [Fact]
    public async Task UpdateCartItem_WithNonExistentItem_ShouldReturnNotFound()
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

    [Fact]
    public async Task UpdateCartItem_FromAnotherUser_ShouldReturnNotFound()
    {
        // Arrange
        var anotherUserId = Guid.NewGuid().ToString();
        var anotherCart = CartData.CreateTestCart(Guid.Parse(anotherUserId));
        await Context.Carts.AddAsync(anotherCart);
        
        var anotherCartItem = CartData.CreateTestCartItem(anotherCart.Id, _testProduct.Id, 5);
        anotherCart.AddItem(anotherCartItem);
        await Context.CartItems.AddAsync(anotherCartItem);
        await SaveChangesAsync();
        
        var request = CartData.CreateUpdateCartItemDto(10);

        // Act
        var response = await _userClient.PutAsJsonAsync(
            $"{BaseRoute}/items/{anotherCartItem.Id.Value}", 
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        // Перевірка що кількість не змінилась
        var dbCartItem = await Context.CartItems
            .AsNoTracking()
            .FirstAsync(ci => ci.Id == anotherCartItem.Id);
        dbCartItem.Quantity.Should().Be(5);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5)]
    public async Task UpdateCartItem_WithInvalidQuantity_ShouldReturnBadRequest(int quantity)
    {
        // Arrange
        var cartItem = CartData.CreateTestCartItem(_testCart.Id, _testProduct.Id, 2);
        _testCart.AddItem(cartItem);
        await Context.CartItems.AddAsync(cartItem);
        await SaveChangesAsync();
        
        var request = CartData.CreateUpdateCartItemDto(quantity);

        // Act
        var response = await _userClient.PutAsJsonAsync(
            $"{BaseRoute}/items/{cartItem.Id.Value}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCartItem_WithInsufficientStock_ShouldReturnBadRequest()
    {
        // Arrange
        var cartItem = CartData.CreateTestCartItem(_testCart.Id, _testProduct.Id, 2);
        _testCart.AddItem(cartItem);
        await Context.CartItems.AddAsync(cartItem);
        await SaveChangesAsync();
        
        var request = CartData.CreateUpdateCartItemDto(1000);

        // Act
        var response = await _userClient.PutAsJsonAsync(
            $"{BaseRoute}/items/{cartItem.Id.Value}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Insufficient stock");
    }

    [Fact]
    public async Task UpdateCartItem_WhenUnauthorized_ShouldReturnUnauthorized()
    {
        // Arrange
        var cartItem = CartData.CreateTestCartItem(_testCart.Id, _testProduct.Id, 2);
        var request = CartData.CreateUpdateCartItemDto(5);

        // Act
        var response = await Client.PutAsJsonAsync(
            $"{BaseRoute}/items/{cartItem.Id.Value}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateCartItem_WithEmptyCartItemId_ShouldReturnBadRequest()
    {
        // Arrange
        var request = CartData.CreateUpdateCartItemDto(5);

        // Act
        var response = await _userClient.PutAsJsonAsync(
            $"{BaseRoute}/items/{Guid.Empty}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCartItem_ToExactStockAmount_ShouldSucceed()
    {
        // Arrange
        var cartItem = CartData.CreateTestCartItem(_testCart.Id, _testProduct.Id, 2);
        _testCart.AddItem(cartItem);
        await Context.CartItems.AddAsync(cartItem);
        await SaveChangesAsync();
        
        var request = CartData.CreateUpdateCartItemDto(_testProduct.StockQuantity);

        // Act
        var response = await _userClient.PutAsJsonAsync(
            $"{BaseRoute}/items/{cartItem.Id.Value}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cartDto = await response.ToResponseModel<CartDto>();
        cartDto.Items.First().Quantity.Should().Be(_testProduct.StockQuantity);
    }

    [Fact]
    public async Task UpdateCartItem_ShouldUpdateTimestamp()
    {
        // Arrange
        var cartItem = CartData.CreateTestCartItem(_testCart.Id, _testProduct.Id, 2);
        _testCart.AddItem(cartItem);
        await Context.CartItems.AddAsync(cartItem);
        await SaveChangesAsync();

        var originalUpdatedAt = _testCart.UpdatedAt;
        await Task.Delay(100);
        
        var request = CartData.CreateUpdateCartItemDto(5);

        // Act
        var response = await _userClient.PutAsJsonAsync(
            $"{BaseRoute}/items/{cartItem.Id.Value}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cartDto = await response.ToResponseModel<CartDto>();
        cartDto.UpdatedAt.Should().NotBeNull();
        cartDto.UpdatedAt.Should().BeAfter(originalUpdatedAt ?? DateTime.MinValue);
    }

    #endregion

    #region DELETE Tests (Remove Item)

    [Fact]
    public async Task RemoveFromCart_WithValidItem_ShouldRemoveItem()
    {
        // Arrange
        var cartItem = CartData.CreateTestCartItem(_testCart.Id, _testProduct.Id, 2);
        _testCart.AddItem(cartItem);
        await Context.CartItems.AddAsync(cartItem);
        await SaveChangesAsync();

        // Act
        var response = await _userClient.DeleteAsync($"{BaseRoute}/items/{cartItem.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cartDto = await response.ToResponseModel<CartDto>();
        cartDto.Items.Should().BeEmpty();

        // Перевірка в БД
        var dbCart = await Context.Carts
            .Include(c => c.Items)
            .AsNoTracking()
            .FirstAsync(c => c.Id == _testCart.Id);
        dbCart.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveFromCart_WithNonExistentItem_ShouldReturnSuccess()
    {
        // Arrange
        var nonExistentItemId = Guid.NewGuid();

        // Act
        var response = await _userClient.DeleteAsync($"{BaseRoute}/items/{nonExistentItemId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RemoveFromCart_OneOfMultipleItems_ShouldRemoveOnlyOne()
    {
        // Arrange
        var product2 = ProductData.SecondTestProduct(new List<CategoryId> { _testCategory.Id });
        await Context.Products.AddAsync(product2);
        await SaveChangesAsync();

        var cartItem1 = CartData.CreateTestCartItem(_testCart.Id, _testProduct.Id, 2);
        var cartItem2 = CartData.CreateTestCartItem(_testCart.Id, product2.Id, 3);
        _testCart.AddItem(cartItem1);
        _testCart.AddItem(cartItem2);
        await Context.CartItems.AddRangeAsync(cartItem1, cartItem2);
        await SaveChangesAsync();

        // Act
        var response = await _userClient.DeleteAsync($"{BaseRoute}/items/{cartItem1.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cartDto = await response.ToResponseModel<CartDto>();
        cartDto.Items.Should().HaveCount(1);
        cartDto.Items.First().ProductId.Should().Be(product2.Id.Value);
    }

    [Fact]
    public async Task RemoveFromCart_WhenUnauthorized_ShouldReturnUnauthorized()
    {
        // Arrange
        var cartItem = CartData.CreateTestCartItem(_testCart.Id, _testProduct.Id, 2);

        // Act
        var response = await Client.DeleteAsync($"{BaseRoute}/items/{cartItem.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RemoveFromCart_ItemFromAnotherUser_ShouldNotRemove()
    {
        // Arrange
        var anotherUserId = Guid.NewGuid().ToString();
        var anotherCart = CartData.CreateTestCart(Guid.Parse(anotherUserId));
        var anotherCartItem = CartData.CreateTestCartItem(anotherCart.Id, _testProduct.Id, 5);
        anotherCart.AddItem(anotherCartItem);
        
        await Context.Carts.AddAsync(anotherCart);
        await Context.CartItems.AddAsync(anotherCartItem);
        await SaveChangesAsync();

        // Act
        var response = await _userClient.DeleteAsync($"{BaseRoute}/items/{anotherCartItem.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        // Перевірка що елемент все ще існує
        var dbCartItem = await Context.CartItems
            .AsNoTracking()
            .FirstOrDefaultAsync(ci => ci.Id == anotherCartItem.Id);
        dbCartItem.Should().NotBeNull();
    }

    #endregion

    #region DELETE Tests (Clear Cart)

    [Fact]
    public async Task ClearCart_WithItems_ShouldRemoveAllItems()
    {
        // Arrange
        var cartItem1 = CartData.CreateTestCartItem(_testCart.Id, _testProduct.Id, 2);
        _testCart.AddItem(cartItem1);
        
        var product2 = ProductData.SecondTestProduct(new List<CategoryId> { _testCategory.Id });
        await Context.Products.AddAsync(product2);
        await SaveChangesAsync();
        
        var cartItem2 = CartData.CreateTestCartItem(_testCart.Id, product2.Id);
        _testCart.AddItem(cartItem2);
        await Context.CartItems.AddRangeAsync(cartItem1, cartItem2);
        await SaveChangesAsync();

        // Act
        var response = await _userClient.DeleteAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cartDto = await response.ToResponseModel<CartDto>();
        cartDto.Items.Should().BeEmpty();

        // Перевірка в БД
        var dbCart = await Context.Carts
            .Include(c => c.Items)
            .AsNoTracking()
            .FirstAsync(c => c.Id == _testCart.Id);
        dbCart.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearCart_WhenCartDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        Context.Carts.Remove(_testCart);
        await SaveChangesAsync();

        // Act
        var response = await _userClient.DeleteAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ClearCart_WhenAlreadyEmpty_ShouldReturnSuccess()
    {
        // Act
        var response = await _userClient.DeleteAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cartDto = await response.ToResponseModel<CartDto>();
        cartDto.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task ClearCart_WhenUnauthorized_ShouldReturnUnauthorized()
    {
        // Act
        var response = await Client.DeleteAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ClearCart_MultipleTimes_ShouldSucceed()
    {
        // Arrange
        var cartItem = CartData.CreateTestCartItem(_testCart.Id, _testProduct.Id, 2);
        _testCart.AddItem(cartItem);
        await Context.CartItems.AddAsync(cartItem);
        await SaveChangesAsync();

        // Act - перше очищення
        var response1 = await _userClient.DeleteAsync(BaseRoute);
        // Act - друге очищення
        var response2 = await _userClient.DeleteAsync(BaseRoute);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ClearCart_ShouldUpdateTimestamp()
    {
        // Arrange
        var cartItem = CartData.CreateTestCartItem(_testCart.Id, _testProduct.Id, 2);
        _testCart.AddItem(cartItem);
        await Context.CartItems.AddAsync(cartItem);
        await SaveChangesAsync();

        var originalUpdatedAt = _testCart.UpdatedAt;
        await Task.Delay(100);

        // Act
        var response = await _userClient.DeleteAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cartDto = await response.ToResponseModel<CartDto>();
        cartDto.UpdatedAt.Should().NotBeNull();
        cartDto.UpdatedAt.Should().BeAfter(originalUpdatedAt ?? DateTime.MinValue);
    }

    #endregion

    #region Complex Scenarios

    [Fact]
    public async Task ComplexScenario_AddUpdateRemove_ShouldWorkCorrectly()
    {
        // Arrange
        var product2 = ProductData.SecondTestProduct(new List<CategoryId> { _testCategory.Id });
        await Context.Products.AddAsync(product2);
        await SaveChangesAsync();

        // Act 1 - Додаємо перший продукт
        var addRequest1 = CartData.CreateAddToCartDto(_testProduct.Id.Value, 2);
        var addResponse1 = await _userClient.PostAsJsonAsync($"{BaseRoute}/items", addRequest1);
        addResponse1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act 2 - Додаємо другий продукт
        var addRequest2 = CartData.CreateAddToCartDto(product2.Id.Value, 3);
        var addResponse2 = await _userClient.PostAsJsonAsync($"{BaseRoute}/items", addRequest2);
        addResponse2.StatusCode.Should().Be(HttpStatusCode.OK);

        var cart = await addResponse2.ToResponseModel<CartDto>();
        var firstItemId = cart.Items.First(i => i.ProductId == _testProduct.Id.Value).Id;

        // Act 3 - Оновлюємо кількість першого
        var updateRequest = CartData.CreateUpdateCartItemDto(5);
        var updateResponse = await _userClient.PutAsJsonAsync($"{BaseRoute}/items/{firstItemId}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act 4 - Видаляємо другий
        var secondItemId = cart.Items.First(i => i.ProductId == product2.Id.Value).Id;
        var removeResponse = await _userClient.DeleteAsync($"{BaseRoute}/items/{secondItemId}");
        removeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert
        var finalCart = await removeResponse.ToResponseModel<CartDto>();
        finalCart.Items.Should().HaveCount(1);
        finalCart.Items.First().ProductId.Should().Be(_testProduct.Id.Value);
        finalCart.Items.First().Quantity.Should().Be(5);
    }
    [Fact]
    public async Task MultipleUsers_ShouldHaveIndependentCarts()
    {
        // Arrange
        var user2Id = Guid.NewGuid().ToString();
        var user2Client = CreateAuthenticatedClient("User", user2Id);
        var user2Cart = CartData.CreateTestCart(Guid.Parse(user2Id));
        await Context.Carts.AddAsync(user2Cart);
        await SaveChangesAsync();

        // Act - обидва користувачі додають той самий продукт
        var request1 = CartData.CreateAddToCartDto(_testProduct.Id.Value, 2);
        var request2 = CartData.CreateAddToCartDto(_testProduct.Id.Value, 3);

        var response1 = await _userClient.PostAsJsonAsync($"{BaseRoute}/items", request1);
        var response2 = await user2Client.PostAsJsonAsync($"{BaseRoute}/items", request2);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var cart1 = await response1.ToResponseModel<CartDto>();
        var cart2 = await response2.ToResponseModel<CartDto>();

        cart1.Items.First().Quantity.Should().Be(2);
        cart2.Items.First().Quantity.Should().Be(3);
        cart1.Id.Should().NotBe(cart2.Id);
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