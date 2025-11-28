using System.Net;
using System.Net.Http.Json;
using Api.Dtos;
using Domain.Categories;
using Domain.Products;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data.Categories;
using Tests.Data.Products;

namespace Api.Tests.Integration.Products;

public class ProductsControllerTests : BaseIntegrationTest, IAsyncLifetime
{
    private const string BaseRoute = "products";
    
    private readonly Category _testCategory = CategoryData.FirstTestCategory("Product");
    private readonly Category _secondTestCategory = CategoryData.SecondTestCategory("Product");
    private readonly Product _firstTestProduct;
    private readonly Product _secondTestProduct;
    private readonly HttpClient _adminClient;
    private readonly HttpClient _managerClient;

    public ProductsControllerTests(IntegrationTestWebFactory factory) : base(factory)
    {
        _adminClient = CreateAuthenticatedClient("Admin");
        _managerClient = CreateAuthenticatedClient("Manager");
        
        _firstTestProduct = ProductData.FirstTestProduct(new List<CategoryId> { _testCategory.Id });
        _secondTestProduct = ProductData.SecondTestProduct(new List<CategoryId> { _testCategory.Id });
    }

    #region GET Tests

    [Fact]
    public async Task ShouldGetAllProductsWithoutAuth()
    {
        // Act
        var response = await Client.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.ToResponseModel<List<ProductDto>>();
        products.Should().HaveCount(2);
        products.Should().Contain(p => p.Id == _firstTestProduct.Id.Value);
        products.Should().Contain(p => p.Id == _secondTestProduct.Id.Value);
    }

    [Fact]
    public async Task ShouldGetProductByIdWithoutAuth()
    {
        // Act
        var response = await Client.GetAsync($"{BaseRoute}/{_firstTestProduct.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = await response.ToResponseModel<ProductDto>();
        
        product.Id.Should().Be(_firstTestProduct.Id.Value);
        product.Name.Should().Be(_firstTestProduct.Name);
        product.Description.Should().Be(_firstTestProduct.Description);
        product.Price.Should().Be(_firstTestProduct.Price);
        product.StockQuantity.Should().Be(_firstTestProduct.StockQuantity);
        product.Brand.Should().Be(_firstTestProduct.Brand);
        product.Model.Should().Be(_firstTestProduct.Model);
        product.Categories.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ShouldReturnNotFoundWhenProductDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"{BaseRoute}/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldGetProductWithImages()
    {
        // Arrange 
        var uploadContent = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        uploadContent.Add(imageContent, "files", "test-image.jpg");

        await _managerClient.PostAsync(
            $"{BaseRoute}/{_firstTestProduct.Id.Value}/images",
            uploadContent);

        // Act
        var response = await Client.GetAsync($"{BaseRoute}/{_firstTestProduct.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = await response.ToResponseModel<ProductDto>();
        
        product.Images.Should().NotBeEmpty();
        product.Images!.Should().HaveCount(1);
        product.Images.First().OriginalName.Should().Be("test-image.jpg");
    }

    [Fact]
    public async Task ShouldSearchProductsBySearchTerm()
    {
        // Act
        var response = await Client.GetAsync($"{BaseRoute}/search?searchTerm=iPhone");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.ToResponseModel<List<ProductDto>>();
        products.Should().HaveCount(1);
        products.First().Name.Should().Contain("iPhone");
    }

    [Fact]
    public async Task ShouldSearchProductsByCategory()
    {
        // Act
        var response = await Client.GetAsync($"{BaseRoute}/search?categoryId={_testCategory.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.ToResponseModel<List<ProductDto>>();
        products.Should().HaveCount(2);
        products.Should().OnlyContain(p => p.Categories!.Any(c => c.Category!.Id == _testCategory.Id.Value));
    }

    [Fact]
    public async Task ShouldSearchProductsByPriceRange()
    {
        // Act
        var response = await Client.GetAsync($"{BaseRoute}/search?minPrice=900&maxPrice=1000");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.ToResponseModel<List<ProductDto>>();
        products.Should().HaveCount(1);
        products.Should().OnlyContain(p => p.Price >= 900 && p.Price <= 1000);
    }

    [Fact]
    public async Task ShouldSearchProductsByBrand()
    {
        // Act
        var response = await Client.GetAsync($"{BaseRoute}/search?brand=Apple");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.ToResponseModel<List<ProductDto>>();
        products.Should().HaveCount(1);
        products.Should().OnlyContain(p => p.Brand == "Apple");
    }

    [Fact]
    public async Task ShouldSearchProductsByMultipleFilters()
    {
        // Act
        var response = await Client.GetAsync(
            $"{BaseRoute}/search?searchTerm=iPhone&minPrice=900&maxPrice=1100&brand=Apple");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.ToResponseModel<List<ProductDto>>();
        products.Should().HaveCount(1);
    }

    [Fact]
    public async Task ShouldReturnEmptyListWhenNoSearchResults()
    {
        // Act
        var response = await Client.GetAsync($"{BaseRoute}/search?searchTerm=NonExistentProduct");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.ToResponseModel<List<ProductDto>>();
        products.Should().BeEmpty();
    }

    [Fact]
    public async Task ShouldSearchProductsByDescriptionKeyword()
    {
        // Act
        var response = await Client.GetAsync($"{BaseRoute}/search?searchTerm=flagship");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.ToResponseModel<List<ProductDto>>();
        products.Should().HaveCount(1);
        products.First().Description.Should().Contain("flagship");
    }

    [Fact]
    public async Task ShouldSearchProductsByModel()
    {
        // Act
        var response = await Client.GetAsync($"{BaseRoute}/search?searchTerm=Galaxy");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.ToResponseModel<List<ProductDto>>();
        products.Should().HaveCount(1);
        products.First().Model.Should().Contain("Galaxy");
    }

    [Fact]
    public async Task ShouldReturnProductsOrderedByName()
    {
        // Act
        var response = await Client.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var products = await response.ToResponseModel<List<ProductDto>>();
        
        products.Should().BeInAscendingOrder(p => p.Name);
    }

    #endregion

    #region POST Tests (Create)

    [Fact]
    public async Task ShouldCreateProductAsManager()
    {
        // Arrange
        var request = ProductData.CreateTestProductDto(new List<Guid> { _testCategory.Id.Value });

        // Act
        var response = await _managerClient.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var productDto = await response.ToResponseModel<ProductDto>();
    
        productDto.Name.Should().Be(request.Name);
        productDto.Description.Should().Be(request.Description);
        productDto.Price.Should().Be(request.Price);
        productDto.StockQuantity.Should().Be(request.StockQuantity);
        productDto.Brand.Should().Be(request.Brand);
        productDto.Model.Should().Be(request.Model);

        var productId = new ProductId(productDto.Id);
        var dbProduct = await Context.Products
            .Include(p => p.Categories)
            .FirstOrDefaultAsync(p => p.Id == productId);

        dbProduct.Should().NotBeNull();
        dbProduct!.Categories.Should().HaveCount(1);
    }

    [Fact]
    public async Task ShouldCreateProductAsAdmin()
    {
        // Arrange
        var request = ProductData.CreateTestProductDto(new List<Guid> { _testCategory.Id.Value });

        // Act
        var response = await _adminClient.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ShouldCreateProductWithMultipleCategories()
    {
        // Arrange
        var request = new CreateProductDto(
            $"Multi-Category-{Guid.NewGuid().ToString()[..8]}-Product",
            "Product with multiple categories",
            799.99m,
            15,
            "TestBrand",
            "TestModel",
            new List<Guid> { _testCategory.Id.Value, _secondTestCategory.Id.Value }
        );

        // Act
        var response = await _managerClient.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var productDto = await response.ToResponseModel<ProductDto>();
        
        productDto.Categories.Should().HaveCount(2);
        productDto.Categories.Should().Contain(c => c.Category!.Id == _testCategory.Id.Value);
        productDto.Categories.Should().Contain(c => c.Category!.Id == _secondTestCategory.Id.Value);
    }

    [Fact]
    public async Task ShouldNotCreateProductBecauseForbidden()
    {
        // Arrange
        var request = ProductData.CreateTestProductDto(new List<Guid> { _testCategory.Id.Value });

        // Act
        var response = await AuthenticatedClient.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ShouldNotCreateProductBecauseUnauthorized()
    {
        // Arrange
        var request = ProductData.CreateTestProductDto(new List<Guid> { _testCategory.Id.Value });

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ShouldNotCreateProductBecauseDuplicateName()
    {
        // Arrange
        var request = new CreateProductDto(
            _firstTestProduct.Name,
            "Duplicate product",
            599.99m,
            10,
            "Brand",
            "Model",
            new List<Guid> { _testCategory.Id.Value }
        );

        // Act
        var response = await _managerClient.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateProductBecauseCategoryNotFound()
    {
        // Arrange
        var request = ProductData.CreateTestProductDto(new List<Guid> { Guid.NewGuid() });

        // Act
        var response = await _managerClient.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("", "Description", 100, 10)]
    [InlineData("Name", "", 100, 10)]
    [InlineData("Name", "Description", 0, 10)]
    [InlineData("Name", "Description", -1, 10)]
    [InlineData("Name", "Description", 100, -1)]
    public async Task ShouldNotCreateProductBecauseInvalidData(
        string name, string description, decimal price, int stock)
    {
        // Arrange
        var request = new CreateProductDto(
            name,
            description,
            price,
            stock,
            "Brand",
            "Model",
            new List<Guid> { _testCategory.Id.Value }
        );

        // Act
        var response = await _managerClient.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateProductBecauseEmptyCategoryList()
    {
        // Arrange
        var request = new CreateProductDto(
            "Test Product",
            "Test Description",
            100m,
            10,
            "Brand",
            "Model",
            new List<Guid>()
        );

        // Act
        var response = await _managerClient.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateProductBecauseNameTooLong()
    {
        // Arrange
        var request = new CreateProductDto(
            new string('A', 256), 
            "Test Description",
            100m,
            10,
            "Brand",
            "Model",
            new List<Guid> { _testCategory.Id.Value }
        );

        // Act
        var response = await _managerClient.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotCreateProductBecauseDescriptionTooLong()
    {
        // Arrange
        var request = new CreateProductDto(
            "Test Product",
            new string('A', 2001),
            100m,
            10,
            "Brand",
            "Model",
            new List<Guid> { _testCategory.Id.Value }
        );

        // Act
        var response = await _managerClient.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region PUT Tests (Update)

    [Fact]
    public async Task ShouldUpdateProductAsManager()
    {
        // Arrange
        var request = ProductData.UpdateTestProductDto(new List<Guid> { _secondTestCategory.Id.Value });

        // Act
        var response = await _managerClient.PutAsJsonAsync(
            $"{BaseRoute}/{_firstTestProduct.Id.Value}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = await response.ToResponseModel<ProductDto>();
        
        product.Name.Should().Be(request.Name);
        product.Description.Should().Be(request.Description);
        product.Price.Should().Be(request.Price);

        var dbProduct = await Context.Products
            .Include(p => p.Categories)
            .AsNoTracking()
            .FirstAsync(p => p.Id == _firstTestProduct.Id);
        dbProduct.Name.Should().Be(request.Name);
        dbProduct.Categories.Should().HaveCount(1);
        dbProduct.Categories!.First().CategoryId.Should().Be(_secondTestCategory.Id);
    }

    [Fact]
    public async Task ShouldUpdateProductAsAdmin()
    {
        // Arrange
        var request = ProductData.UpdateTestProductDto(new List<Guid> { _testCategory.Id.Value });

        // Act
        var response = await _adminClient.PutAsJsonAsync(
            $"{BaseRoute}/{_secondTestProduct.Id.Value}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ShouldUpdateProductWithSameName()
    {
        // Arrange 
        var request = new UpdateProductDto(
            _firstTestProduct.Name,
            "Updated Description",
            1299.99m,
            25,
            "Updated Brand",
            "Updated Model",
            new List<Guid> { _testCategory.Id.Value }
        );

        // Act
        var response = await _managerClient.PutAsJsonAsync(
            $"{BaseRoute}/{_firstTestProduct.Id.Value}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ShouldNotUpdateProductBecauseForbidden()
    {
        // Arrange
        var request = ProductData.UpdateTestProductDto(new List<Guid> { _testCategory.Id.Value });

        // Act
        var response = await AuthenticatedClient.PutAsJsonAsync(
            $"{BaseRoute}/{_firstTestProduct.Id.Value}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ShouldNotUpdateProductBecauseUnauthorized()
    {
        // Arrange
        var request = ProductData.UpdateTestProductDto(new List<Guid> { _testCategory.Id.Value });

        // Act
        var response = await Client.PutAsJsonAsync(
            $"{BaseRoute}/{_firstTestProduct.Id.Value}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ShouldNotUpdateProductBecauseProductNotFound()
    {
        // Arrange
        var request = ProductData.UpdateTestProductDto(new List<Guid> { _testCategory.Id.Value });
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _managerClient.PutAsJsonAsync($"{BaseRoute}/{nonExistentId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldUpdateProductWithMultipleCategories()
    {
        // Arrange
        var request = ProductData.UpdateTestProductDto(
            new List<Guid> { _testCategory.Id.Value, _secondTestCategory.Id.Value });

        // Act
        var response = await _managerClient.PutAsJsonAsync(
            $"{BaseRoute}/{_firstTestProduct.Id.Value}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dbProduct = await Context.Products
            .Include(p => p.Categories)
            .AsNoTracking()
            .FirstAsync(p => p.Id == _firstTestProduct.Id);
        dbProduct.Categories.Should().HaveCount(2);
    }
    
    [Fact]
    public async Task ShouldNotUpdateProductBecauseDuplicateName()
    {
        // Arrange
        var request = new UpdateProductDto(
            _firstTestProduct.Name, 
            "New Description",
            999.99m,
            15,
            "New Brand",
            "New Model",
            new List<Guid> { _testCategory.Id.Value }
        );

        // Act
        var response = await _managerClient.PutAsJsonAsync(
            $"{BaseRoute}/{_secondTestProduct.Id.Value}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotUpdateProductBecauseInvalidPrice()
    {
        // Arrange
        var request = new UpdateProductDto(
            "Valid Name",
            "Valid Description",
            0m,
            10,
            "Brand",
            "Model",
            new List<Guid> { _testCategory.Id.Value }
        );

        // Act
        var response = await _managerClient.PutAsJsonAsync(
            $"{BaseRoute}/{_firstTestProduct.Id.Value}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotUpdateProductBecauseCategoryNotFound()
    {
        // Arrange
        var request = ProductData.UpdateTestProductDto(new List<Guid> { Guid.NewGuid() });

        // Act
        var response = await _managerClient.PutAsJsonAsync(
            $"{BaseRoute}/{_firstTestProduct.Id.Value}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldUpdateProductAndChangeCategoryFromOneToAnother()
    {
        // Arrange 
        var request = ProductData.UpdateTestProductDto(new List<Guid> { _secondTestCategory.Id.Value });

        // Act
        var response = await _managerClient.PutAsJsonAsync(
            $"{BaseRoute}/{_firstTestProduct.Id.Value}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dbProduct = await Context.Products
            .Include(p => p.Categories)
            .AsNoTracking()
            .FirstAsync(p => p.Id == _firstTestProduct.Id);
            
        dbProduct.Categories.Should().HaveCount(1);
        dbProduct.Categories!.First().CategoryId.Should().Be(_secondTestCategory.Id);
    }

    #endregion

    #region DELETE Tests

    [Fact]
    public async Task ShouldDeleteProductAsManager()
    {
        // Act
        var response = await _managerClient.DeleteAsync($"{BaseRoute}/{_secondTestProduct.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dbProduct = await Context.Products
            .FirstOrDefaultAsync(p => p.Id == _secondTestProduct.Id);
        dbProduct.Should().BeNull();
    }

    [Fact]
    public async Task ShouldDeleteProductAsAdmin()
    {
        // Arrange
        var tempProduct = ProductData.FirstTestProduct(new List<CategoryId> { _testCategory.Id });
        await Context.Products.AddAsync(tempProduct);
        await SaveChangesAsync();

        // Act
        var response = await _adminClient.DeleteAsync($"{BaseRoute}/{tempProduct.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ShouldDeleteProductWithImages()
    {
        // Arrange 
        var uploadContent = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        uploadContent.Add(imageContent, "files", "delete-with-image.jpg");

        await _managerClient.PostAsync(
            $"{BaseRoute}/{_firstTestProduct.Id.Value}/images",
            uploadContent);

        // Act
        var response = await _managerClient.DeleteAsync($"{BaseRoute}/{_firstTestProduct.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dbProduct = await Context.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == _firstTestProduct.Id);
        dbProduct.Should().BeNull();
    }

    [Fact]
    public async Task ShouldNotDeleteProductBecauseForbidden()
    {
        // Act
        var response = await AuthenticatedClient.DeleteAsync($"{BaseRoute}/{_firstTestProduct.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ShouldNotDeleteProductBecauseUnauthorized()
    {
        // Act
        var response = await Client.DeleteAsync($"{BaseRoute}/{_firstTestProduct.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ShouldNotDeleteProductBecauseProductNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _managerClient.DeleteAsync($"{BaseRoute}/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldReturnDeletedProductInResponse()
    {
        // Arrange
        var tempProduct = ProductData.FirstTestProduct(new List<CategoryId> { _testCategory.Id });
        await Context.Products.AddAsync(tempProduct);
        await SaveChangesAsync();

        // Act
        var response = await _managerClient.DeleteAsync($"{BaseRoute}/{tempProduct.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var deletedProduct = await response.ToResponseModel<ProductDto>();
        
        deletedProduct.Id.Should().Be(tempProduct.Id.Value);
        deletedProduct.Name.Should().Be(tempProduct.Name);
    }

    #endregion

    public async Task InitializeAsync()
    {
        await Context.Categories.AddRangeAsync(_testCategory, _secondTestCategory);
        await Context.Products.AddRangeAsync(_firstTestProduct, _secondTestProduct);
        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        Context.Products.RemoveRange(Context.Products);
        Context.Categories.RemoveRange(Context.Categories);
        
        await SaveChangesAsync();
    }
}