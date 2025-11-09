using System.Net;
using System.Net.Http.Json;
using Api.Dtos;
using Domain.Categories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data.Categories;
using Xunit;

namespace Api.Tests.Integration.Categories;

public class CategoriesControllerTests : BaseIntegrationTest, IAsyncLifetime
{
    private readonly Category _firstTestCategory = CategoryData.FirstTestCategory();
    private readonly Category _secondTestCategory = CategoryData.SecondTestCategory();

    private const string BaseRoute = "categories";
    private HttpClient _adminClient;
    private HttpClient _managerClient;

    public CategoriesControllerTests(IntegrationTestWebFactory factory) : base(factory)
    {
        _adminClient = CreateAuthenticatedClient("Admin");
        _managerClient = CreateAuthenticatedClient("Manager");
    }

    #region GET Tests

    [Fact]
    public async Task GetAll_WithoutAuth_ShouldReturnAllCategories()
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
    public async Task GetById_WithoutAuth_ShouldReturnCategory()
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
    public async Task GetById_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"{BaseRoute}/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST Tests (Create)

    [Fact]
    public async Task Create_AsAdmin_ShouldCreateCategory()
    {
        // Arrange
        var request = CategoryData.CreateTestCategoryDto();

        // Act
        var response = await _adminClient.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var category = await response.ToResponseModel<CategoryDto>();
        
        category.Name.Should().Be(request.Name);
        category.Description.Should().Be(request.Description);

        var dbCategory = await Context.Categories
            .FirstOrDefaultAsync(c => c.Id.Value == category.Id);
        dbCategory.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_AsManager_ShouldCreateCategory()
    {
        // Arrange
        var request = CategoryData.CreateTestCategoryDto();

        // Act
        var response = await _managerClient.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_AsUser_ShouldReturnForbidden()
    {
        // Arrange
        var request = CategoryData.CreateTestCategoryDto();

        // Act
        var response = await AuthenticatedClient.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = CategoryData.CreateTestCategoryDto();

        // Act
        var response = await Client.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_WithDuplicateName_ShouldReturnConflict()
    {
        // Arrange
        var request = new CreateCategoryDto(_firstTestCategory.Name, "Duplicate category");

        // Act
        var response = await _adminClient.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("", "Valid description")] // Empty name
    [InlineData(null, "Valid description")] // Null name
    public async Task Create_WithInvalidData_ShouldReturnBadRequest(string name, string description)
    {
        // Arrange
        var request = new CreateCategoryDto(name, description);

        // Act
        var response = await _adminClient.PostAsJsonAsync(BaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region PUT Tests (Update)

    [Fact]
    public async Task Update_AsAdmin_ShouldUpdateCategory()
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
    public async Task Update_AsManager_ShouldUpdateCategory()
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
    public async Task Update_AsUser_ShouldReturnForbidden()
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
    public async Task Update_NonExistentCategory_ShouldReturnNotFound()
    {
        // Arrange
        var request = CategoryData.UpdateTestCategoryDto();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _adminClient.PutAsJsonAsync($"{BaseRoute}/{nonExistentId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region DELETE Tests

    [Fact]
    public async Task Delete_AsAdmin_ShouldDeleteCategory()
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
    public async Task Delete_AsManager_ShouldDeleteCategory()
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
    public async Task Delete_AsUser_ShouldReturnForbidden()
    {
        // Act
        var response = await AuthenticatedClient.DeleteAsync($"{BaseRoute}/{_firstTestCategory.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_NonExistentCategory_ShouldReturnNotFound()
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