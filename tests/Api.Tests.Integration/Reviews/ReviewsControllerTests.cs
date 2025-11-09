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
using Xunit;

namespace Api.Tests.Integration.Reviews;

public class ReviewsControllerTests : BaseIntegrationTest, IAsyncLifetime
{
    private const string BaseRoute = "reviews";
    private readonly Category _testCategory = CategoryData.FirstTestCategory("ReviewCtrl");
    private Product _testProduct;
    private readonly string _testUserId = Guid.NewGuid().ToString();
    private readonly string _otherUserId = Guid.NewGuid().ToString();
    private HttpClient _userClient;
    private HttpClient _otherUserClient;
    private HttpClient _adminClient;
    private ProductReview _userReview;
    private ProductReview _otherUserReview;

    public ReviewsControllerTests(IntegrationTestWebFactory factory) : base(factory)
    {
        _userClient = CreateAuthenticatedClient("User", _testUserId);
        _otherUserClient = CreateAuthenticatedClient("User", _otherUserId);
        _adminClient = CreateAuthenticatedClient("Admin");
        
        _testProduct = ProductData.FirstTestProduct(new List<CategoryId> { _testCategory.Id });
        _userReview = ReviewData.CreateTestReview(_testProduct.Id, Guid.Parse(_testUserId));
        _otherUserReview = ReviewData.CreateTestReview(_testProduct.Id, Guid.Parse(_otherUserId));
    }

    #region PUT Tests (Update Review)

    [Fact]
    public async Task UpdateReview_AsOwner_ShouldUpdateReview()
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
    public async Task UpdateReview_AsAdmin_ShouldUpdateAnyReview()
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
    public async Task UpdateReview_AsNonOwner_ShouldReturnForbidden()
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
    public async Task UpdateReview_WithoutAuth_ShouldReturnUnauthorized()
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
    public async Task UpdateReview_NonExistentReview_ShouldReturnNotFound()
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
    [InlineData(0, "Valid comment")] // Invalid rating
    [InlineData(6, "Valid comment")] // Invalid rating
    [InlineData(5, "")] // Empty comment
    public async Task UpdateReview_WithInvalidData_ShouldReturnBadRequest(int rating, string comment)
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

    #endregion

    #region DELETE Tests

    [Fact]
    public async Task DeleteReview_AsOwner_ShouldDeleteReview()
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
    public async Task DeleteReview_AsAdmin_ShouldDeleteAnyReview()
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
    public async Task DeleteReview_AsNonOwner_ShouldReturnForbidden()
    {
        // Act
        var response = await _otherUserClient.DeleteAsync($"{BaseRoute}/{_userReview.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteReview_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Act
        var response = await Client.DeleteAsync($"{BaseRoute}/{_userReview.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteReview_NonExistentReview_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _userClient.DeleteAsync($"{BaseRoute}/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GET Tests (Moderated Reviews)

    [Fact]
    public async Task GetModeratedReviews_AsAdmin_ShouldReturnModeratedReviews()
    {
        // Arrange - модеруємо один відгук
        _userReview.Moderate();
        Context.ProductReviews.Update(_userReview);
        await SaveChangesAsync();

        // Act
        var response = await _adminClient.GetAsync($"{BaseRoute}/moderated");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reviews = await response.ToResponseModel<List<ProductReviewDto>>();
        reviews.Should().HaveCount(1);
        reviews.Should().OnlyContain(r => r.IsModerated);
    }

    [Fact]
    public async Task GetModeratedReviews_AsUser_ShouldReturnForbidden()
    {
        // Act
        var response = await _userClient.GetAsync($"{BaseRoute}/moderated");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetModeratedReviews_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"{BaseRoute}/moderated");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST Tests (Moderate Review)

    [Fact]
    public async Task ModerateReview_AsAdmin_ShouldModerateReview()
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
    public async Task ModerateReview_AsUser_ShouldReturnForbidden()
    {
        // Act
        var response = await _userClient.PostAsync(
            $"{BaseRoute}/{_userReview.Id.Value}/moderate",
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ModerateReview_NonExistentReview_ShouldReturnNotFound()
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
        // ВИПРАВЛЕННЯ: Використовуємо метод CleanupDatabaseAsync
        await CleanupDatabaseAsync();
    }
}