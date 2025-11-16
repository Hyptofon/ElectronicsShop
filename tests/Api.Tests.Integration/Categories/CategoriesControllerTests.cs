using System.Net;
using System.Net.Http.Json;
using Api.Dtos;
using Domain.Categories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data.Categories;

namespace Api.Tests.Integration.Categories;

public class CategoriesControllerTests : BaseIntegrationTest, IAsyncLifetime
{
    private const string BaseRoute = "categories";
    
    private readonly Category _firstTestCategory = CategoryData.FirstTestCategory();
    private readonly Category _secondTestCategory = CategoryData.SecondTestCategory();
    private readonly HttpClient _adminClient;
    private readonly HttpClient _managerClient;

    public CategoriesControllerTests(IntegrationTestWebFactory factory) : base(factory)
    {
        _adminClient = CreateAuthenticatedClient("Admin");
        _managerClient = CreateAuthenticatedClient("Manager");
    }

    #region GET Tests

    [Fact]
    public async Task ShouldGetAllCategoriesWithoutAuth()
    {
        // Act
        var response = await Client.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await response.ToResponseModel<List<CategoryDto>>();
        categories.Should().HaveCount(2);
        categories.Should().Contain(c => c.Id == _firstTestCategory.Id.Value);
        categories.Should().Contain(c => c.Id == _secondTestCategory.Id.Value);
    }

    [Fact]
    public async Task ShouldGetCategoryByIdWithoutAuth()
    {
        // Act
        var response = await Client.GetAsync($"{BaseRoute}/{_firstTestCategory.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var category = await response.ToResponseModel<CategoryDto>();
        category.Id.Should().Be(_firstTestCategory.Id.Value);
        category.Name.Should().Be(_firstTestCategory.Name);
        category.Description.Should().Be(_firstTestCategory.Description);
    }

    [Fact]
    public async Task ShouldNotGetCategoryByIdBecauseCategoryNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"{BaseRoute}/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST Tests

    [Fact]
    public async Task ShouldCreateCategoryAsAdmin()
    {
        // Arrange
        var request = CategoryData.CreateTestCategoryDto();

        // Act
        var response = await _adminClient.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categoryDto = await response.ToResponseModel<CategoryDto>();
        
        categoryDto.Name.Should().Be(request.Name);
        categoryDto.Description.Should().Be(request.Description);

        var categoryId = new CategoryId(categoryDto.Id);
        var dbCategory = await Context.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId);
            
        dbCategory.Should().NotBeNull();
        dbCategory!.Name.Should().Be(request.Name);
    }

    [Fact]
    public async Task ShouldCreateCategoryAsManager()
    {
        // Arrange
        var request = CategoryData.CreateTestCategoryDto();

        // Act
        var response = await _managerClient.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ShouldNotCreateCategoryBecauseForbidden()
    {
        // Arrange
        var request = CategoryData.CreateTestCategoryDto();

        // Act
        var response = await AuthenticatedClient.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ShouldNotCreateCategoryBecauseUnauthorized()
    {
        // Arrange
        var request = CategoryData.CreateTestCategoryDto();

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ShouldNotCreateCategoryBecauseDuplicateName()
    {
        // Arrange
        var request = new CreateCategoryDto(_firstTestCategory.Name, "Duplicate category");

        // Act
        var response = await _adminClient.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("", "Valid description")]
    [InlineData(null, "Valid description")]
    public async Task ShouldNotCreateCategoryBecauseInvalidData(string name, string description)
    {
        // Arrange
        var request = new CreateCategoryDto(name, description);

        // Act
        var response = await _adminClient.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region PUT Tests

    [Fact]
    public async Task ShouldUpdateCategoryAsAdmin()
    {
        // Arrange
        var request = CategoryData.UpdateTestCategoryDto();

        // Act
        var response = await _adminClient.PutAsJsonAsync(
            $"{BaseRoute}/{_firstTestCategory.Id.Value}", 
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var category = await response.ToResponseModel<CategoryDto>();
        
        category.Name.Should().Be(request.Name);
        category.Description.Should().Be(request.Description);

        var dbCategory = await Context.Categories
            .AsNoTracking()
            .FirstAsync(c => c.Id == _firstTestCategory.Id);
        dbCategory.Name.Should().Be(request.Name);
    }

    [Fact]
    public async Task ShouldUpdateCategoryAsManager()
    {
        // Arrange
        var request = CategoryData.UpdateTestCategoryDto();

        // Act
        var response = await _managerClient.PutAsJsonAsync(
            $"{BaseRoute}/{_secondTestCategory.Id.Value}", 
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ShouldNotUpdateCategoryBecauseForbidden()
    {
        // Arrange
        var request = CategoryData.UpdateTestCategoryDto();

        // Act
        var response = await AuthenticatedClient.PutAsJsonAsync(
            $"{BaseRoute}/{_firstTestCategory.Id.Value}", 
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ShouldNotUpdateCategoryBecauseCategoryNotFound()
    {
        // Arrange
        var request = CategoryData.UpdateTestCategoryDto();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _adminClient.PutAsJsonAsync($"{BaseRoute}/{nonExistentId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task ShouldNotUpdateCategoryBecauseUnauthorized()
    {
        // Arrange
        var request = CategoryData.UpdateTestCategoryDto();

        // Act
        var response = await Client.PutAsJsonAsync(
            $"{BaseRoute}/{_firstTestCategory.Id.Value}", 
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    [Fact]
    public async Task ShouldNotUpdateCategoryBecauseDuplicateName()
    {
        // Arrange: Намагаємося оновити першу категорію, надавши їй ім'я другої
        var request = new UpdateCategoryDto(_secondTestCategory.Name, "Updated description");

        // Act
        var response = await _adminClient.PutAsJsonAsync(
            $"{BaseRoute}/{_firstTestCategory.Id.Value}", 
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict); 
    }

    #endregion

    #region DELETE Tests

    [Fact]
    public async Task ShouldDeleteCategoryAsAdmin()
    {
        // Act
        var response = await _adminClient.DeleteAsync($"{BaseRoute}/{_secondTestCategory.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dbCategory = await Context.Categories
            .FirstOrDefaultAsync(c => c.Id == _secondTestCategory.Id);
        dbCategory.Should().BeNull();
    }

    [Fact]
    public async Task ShouldDeleteCategoryAsManager()
    {
        // Arrange
        var tempCategory = Category.New(CategoryId.New(), "Temp Category", "Temp");
        await Context.Categories.AddAsync(tempCategory);
        await SaveChangesAsync();

        // Act
        var response = await _managerClient.DeleteAsync($"{BaseRoute}/{tempCategory.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ShouldNotDeleteCategoryBecauseForbidden()
    {
        // Act
        var response = await AuthenticatedClient.DeleteAsync($"{BaseRoute}/{_firstTestCategory.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ShouldNotDeleteCategoryBecauseCategoryNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _adminClient.DeleteAsync($"{BaseRoute}/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    public async Task InitializeAsync()
    {
        await Context.Categories.AddRangeAsync(_firstTestCategory, _secondTestCategory);
        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        Context.Categories.RemoveRange(Context.Categories);
        
        await SaveChangesAsync();
    }
}