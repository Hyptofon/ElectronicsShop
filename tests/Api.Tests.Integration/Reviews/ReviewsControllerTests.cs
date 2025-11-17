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
using Tests.Data.Reviews;

namespace Api.Tests.Integration.Reviews;

public class ReviewsControllerTests : BaseIntegrationTest, IAsyncLifetime
{
    private const string BaseRoute = "reviews";
    
    private readonly Category _testCategory = CategoryData.FirstTestCategory("ReviewCtrl");
    private readonly Product _testProduct;
    private readonly string _testUserId = Guid.NewGuid().ToString();
    private readonly string _otherUserId = Guid.NewGuid().ToString();
    private readonly HttpClient _userClient;
    private readonly HttpClient _otherUserClient;
    private readonly HttpClient _adminClient;
    private readonly ProductReview _userReview;
    private readonly ProductReview _otherUserReview;

    public ReviewsControllerTests(IntegrationTestWebFactory factory) : base(factory)
    {
        _userClient = CreateAuthenticatedClient("User", _testUserId);
        _otherUserClient = CreateAuthenticatedClient("User", _otherUserId);
        _adminClient = CreateAuthenticatedClient("Admin");
        
        _testProduct = ProductData.FirstTestProduct(new List<CategoryId> { _testCategory.Id });
        _userReview = ReviewData.CreateTestReview(_testProduct.Id, Guid.Parse(_testUserId));
        _otherUserReview = ReviewData.CreateTestReview(_testProduct.Id, Guid.Parse(_otherUserId));
    }

    #region PUT Tests

    [Fact]
    public async Task ShouldUpdateReviewAsOwner()
    {
        // Arrange
        var request = ReviewData.UpdateTestReviewDto();

        // Act
        var response = await _userClient.PutAsJsonAsync(
            $"{BaseRoute}/{_userReview.Id.Value}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var review = await response.ToResponseModel<ProductReviewDto>();
        
        review.Rating.Should().Be(request.Rating);
        review.Comment.Should().Be(request.Comment);

        var dbReview = await Context.ProductReviews
            .AsNoTracking()
            .FirstAsync(r => r.Id == _userReview.Id);
        dbReview.Rating.Should().Be(request.Rating);
        dbReview.Comment.Should().Be(request.Comment);
    }

    [Fact]
    public async Task ShouldUpdateAnyReviewAsAdmin()
    {
        // Arrange
        var request = ReviewData.UpdateTestReviewDto();

        // Act
        var response = await _adminClient.PutAsJsonAsync(
            $"{BaseRoute}/{_otherUserReview.Id.Value}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ShouldNotUpdateReviewBecauseNotOwner()
    {
        // Arrange
        var request = ReviewData.UpdateTestReviewDto();

        // Act
        var response = await _otherUserClient.PutAsJsonAsync(
            $"{BaseRoute}/{_userReview.Id.Value}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ShouldNotUpdateReviewBecauseUnauthorized()
    {
        // Arrange
        var request = ReviewData.UpdateTestReviewDto();

        // Act
        var response = await Client.PutAsJsonAsync(
            $"{BaseRoute}/{_userReview.Id.Value}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ShouldNotUpdateReviewBecauseReviewNotFound()
    {
        // Arrange
        var request = ReviewData.UpdateTestReviewDto();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _userClient.PutAsJsonAsync($"{BaseRoute}/{nonExistentId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(0, "Valid comment")]
    [InlineData(6, "Valid comment")]
    [InlineData(5, "")]
    public async Task ShouldNotUpdateReviewBecauseInvalidData(int rating, string comment)
    {
        // Arrange
        var request = new UpdateProductReviewDto(rating, comment);

        // Act
        var response = await _userClient.PutAsJsonAsync(
            $"{BaseRoute}/{_userReview.Id.Value}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    
    [Fact]
    public async Task ShouldUpdateAnyReviewAsManager()
    {
        // Arrange
        var managerClient = CreateAuthenticatedClient("Manager"); // Створюємо Manager Client
        var request = ReviewData.UpdateTestReviewDto();

        // Act: Manager оновлює відгук іншого користувача (_otherUserReview)
        var response = await managerClient.PutAsJsonAsync(
            $"{BaseRoute}/{_otherUserReview.Id.Value}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // (Можна додати перевірку в DB, але це вже перевірено в ShouldUpdateReviewAsOwner)
    }

    #endregion

    #region DELETE Tests

    [Fact]
    public async Task ShouldDeleteReviewAsOwner()
    {
        // Act
        var response = await _userClient.DeleteAsync($"{BaseRoute}/{_userReview.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dbReview = await Context.ProductReviews
            .FirstOrDefaultAsync(r => r.Id == _userReview.Id);
        dbReview.Should().BeNull();
    }

    [Fact]
    public async Task ShouldDeleteAnyReviewAsAdmin()
    {
        // Act
        var response = await _adminClient.DeleteAsync($"{BaseRoute}/{_otherUserReview.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dbReview = await Context.ProductReviews
            .FirstOrDefaultAsync(r => r.Id == _otherUserReview.Id);
        dbReview.Should().BeNull();
    }

    [Fact]
    public async Task ShouldNotDeleteReviewBecauseNotOwner()
    {
        // Act
        var response = await _otherUserClient.DeleteAsync($"{BaseRoute}/{_userReview.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ShouldNotDeleteReviewBecauseUnauthorized()
    {
        // Act
        var response = await Client.DeleteAsync($"{BaseRoute}/{_userReview.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ShouldNotDeleteReviewBecauseReviewNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _userClient.DeleteAsync($"{BaseRoute}/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    [Fact]
    public async Task ShouldDeleteAnyReviewAsManager()
    {
        // Arrange
        var managerClient = CreateAuthenticatedClient("Manager");
    
        // Act: Manager видаляє відгук іншого користувача (_userReview)
        var response = await managerClient.DeleteAsync($"{BaseRoute}/{_userReview.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dbReview = await Context.ProductReviews
            .FirstOrDefaultAsync(r => r.Id == _userReview.Id);
        dbReview.Should().BeNull();
    }
    #endregion

    #region GET Tests (Moderated)

    [Fact]
    public async Task ShouldGetUnmoderatedReviewsAsAdmin()
    {
        // Arrange
        Context.ProductReviews.Update(_userReview);
        await SaveChangesAsync();

        // Act
        var response = await _adminClient.GetAsync($"{BaseRoute}/unmoderated");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reviews = await response.ToResponseModel<List<ProductReviewDto>>();
        reviews.Should().HaveCount(2);
        reviews.Should().OnlyContain(r => !r.IsModerated);
    }

    [Fact]
    public async Task ShouldNotGetModeratedReviewsBecauseForbidden()
    {
        // Act
        var response = await _userClient.GetAsync($"{BaseRoute}/unmoderated");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ShouldNotGetModeratedReviewsBecauseUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"{BaseRoute}/unmoderated");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST Tests (Moderate)

    [Fact]
    public async Task ShouldModerateReviewAsAdmin()
    {
        // Act
        var response = await _adminClient.PostAsync(
            $"{BaseRoute}/{_userReview.Id.Value}/moderate",
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var review = await response.ToResponseModel<ProductReviewDto>();
        review.IsModerated.Should().BeTrue();

        var dbReview = await Context.ProductReviews
            .AsNoTracking()
            .FirstAsync(r => r.Id == _userReview.Id);
        dbReview.IsModerated.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldNotModerateReviewBecauseForbidden()
    {
        // Act
        var response = await _userClient.PostAsync(
            $"{BaseRoute}/{_userReview.Id.Value}/moderate",
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ShouldNotModerateReviewBecauseReviewNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _adminClient.PostAsync($"{BaseRoute}/{nonExistentId}/moderate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    public async Task InitializeAsync()
    {
        await Context.Categories.AddAsync(_testCategory);
        await Context.Products.AddAsync(_testProduct);
        await Context.ProductReviews.AddRangeAsync(_userReview, _otherUserReview);
        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        Context.ProductReviews.RemoveRange(Context.ProductReviews);
        Context.Products.RemoveRange(Context.Products);
        Context.Categories.RemoveRange(Context.Categories);
        
        await SaveChangesAsync();
    }
}