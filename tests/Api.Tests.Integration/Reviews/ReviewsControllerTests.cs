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
    private readonly HttpClient _managerClient;
    private readonly ProductReview _userReview;
    private readonly ProductReview _otherUserReview;

    public ReviewsControllerTests(IntegrationTestWebFactory factory) : base(factory)
    {
        _userClient = CreateAuthenticatedClient("User", _testUserId);
        _otherUserClient = CreateAuthenticatedClient("User", _otherUserId);
        _adminClient = CreateAuthenticatedClient("Admin");
        _managerClient = CreateAuthenticatedClient("Manager");
        
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
        review.UpdatedAt.Should().NotBeNull();
        review.UpdatedAt.Should().BeAfter(review.CreatedAt);

        // Перевірка в БД
        var dbReview = await Context.ProductReviews
            .AsNoTracking()
            .FirstAsync(r => r.Id == _userReview.Id);
        dbReview.Rating.Should().Be(request.Rating);
        dbReview.Comment.Should().Be(request.Comment);
        dbReview.UpdatedAt.Should().NotBeNull();
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
        var review = await response.ToResponseModel<ProductReviewDto>();
        review.Rating.Should().Be(request.Rating);
        review.Comment.Should().Be(request.Comment);
    }

    [Fact]
    public async Task UpdateReview_AsManager_ShouldUpdateAnyReview()
    {
        // Arrange
        var request = ReviewData.UpdateTestReviewDto();

        // Act
        var response = await _managerClient.PutAsJsonAsync(
            $"{BaseRoute}/{_otherUserReview.Id.Value}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var review = await response.ToResponseModel<ProductReviewDto>();
        review.Rating.Should().Be(request.Rating);
        review.Comment.Should().Be(request.Comment);
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
        
        // Перевірка що дані не змінились
        var dbReview = await Context.ProductReviews
            .AsNoTracking()
            .FirstAsync(r => r.Id == _userReview.Id);
        dbReview.Rating.Should().Be(5);
        dbReview.Comment.Should().Be("Test excellent product! Highly recommended.");
    }

    [Fact]
    public async Task UpdateReview_WhenUnauthorized_ShouldReturnUnauthorized()
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
    public async Task UpdateReview_WithNonExistentId_ShouldReturnNotFound()
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
    [InlineData(-1, "Valid comment")]
    public async Task UpdateReview_WithInvalidRating_ShouldReturnBadRequest(int rating, string comment)
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

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task UpdateReview_WithInvalidComment_ShouldReturnBadRequest(string comment)
    {
        // Arrange
        var request = new UpdateProductReviewDto(5, comment);

        // Act
        var response = await _userClient.PutAsJsonAsync(
            $"{BaseRoute}/{_userReview.Id.Value}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateReview_WithTooLongComment_ShouldReturnBadRequest()
    {
        // Arrange
        var longComment = new string('A', 2001); // >2000 символів
        var request = new UpdateProductReviewDto(5, longComment);

        // Act
        var response = await _userClient.PutAsJsonAsync(
            $"{BaseRoute}/{_userReview.Id.Value}",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateReview_WithEmptyGuid_ShouldReturnBadRequest()
    {
        // Arrange
        var request = ReviewData.UpdateTestReviewDto();

        // Act
        var response = await _userClient.PutAsJsonAsync($"{BaseRoute}/{Guid.Empty}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateReview_MultipleTimes_ShouldUpdateSuccessfully()
    {
        // Arrange
        var request1 = new UpdateProductReviewDto(4, "First update");
        var request2 = new UpdateProductReviewDto(2, "Second update");

        // Act
        var response1 = await _userClient.PutAsJsonAsync($"{BaseRoute}/{_userReview.Id.Value}", request1);
        await Task.Delay(100);
        var response2 = await _userClient.PutAsJsonAsync($"{BaseRoute}/{_userReview.Id.Value}", request2);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var review = await response2.ToResponseModel<ProductReviewDto>();
        review.Rating.Should().Be(2);
        review.Comment.Should().Be("Second update");
    }

    #endregion

    #region DELETE Tests (Delete Review)

    [Fact]
    public async Task DeleteReview_AsOwner_ShouldDeleteReview()
    {
        // Act
        var response = await _userClient.DeleteAsync($"{BaseRoute}/{_userReview.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var deletedReview = await response.ToResponseModel<ProductReviewDto>();
        deletedReview.Id.Should().Be(_userReview.Id.Value);

        // Перевірка в БД
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

        // Перевірка в БД
        var dbReview = await Context.ProductReviews
            .FirstOrDefaultAsync(r => r.Id == _otherUserReview.Id);
        dbReview.Should().BeNull();
    }

    [Fact]
    public async Task DeleteReview_AsManager_ShouldDeleteAnyReview()
    {
        // Act
        var response = await _managerClient.DeleteAsync($"{BaseRoute}/{_userReview.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Перевірка в БД
        var dbReview = await Context.ProductReviews
            .FirstOrDefaultAsync(r => r.Id == _userReview.Id);
        dbReview.Should().BeNull();
    }

    [Fact]
    public async Task DeleteReview_AsNonOwner_ShouldReturnForbidden()
    {
        // Act
        var response = await _otherUserClient.DeleteAsync($"{BaseRoute}/{_userReview.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        
        // Перевірка що відгук не видалено
        var dbReview = await Context.ProductReviews
            .FirstOrDefaultAsync(r => r.Id == _userReview.Id);
        dbReview.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteReview_WhenUnauthorized_ShouldReturnUnauthorized()
    {
        // Act
        var response = await Client.DeleteAsync($"{BaseRoute}/{_userReview.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteReview_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _userClient.DeleteAsync($"{BaseRoute}/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteReview_WithEmptyGuid_ShouldReturnBadRequest()
    {
        // Act
        var response = await _userClient.DeleteAsync($"{BaseRoute}/{Guid.Empty}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteReview_AfterDeletion_ShouldReturnNotFound()
    {
        // Arrange
        await _userClient.DeleteAsync($"{BaseRoute}/{_userReview.Id.Value}");

        // Act
        var response = await _userClient.DeleteAsync($"{BaseRoute}/{_userReview.Id.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GET Tests (Unmoderated Reviews)

    [Fact]
    public async Task GetUnmoderatedReviews_AsAdmin_ShouldReturnUnmoderatedReviews()
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
        reviews.Should().Contain(r => r.Id == _userReview.Id.Value);
        reviews.Should().Contain(r => r.Id == _otherUserReview.Id.Value);
    }

    [Fact]
    public async Task GetUnmoderatedReviews_AsManager_ShouldReturnUnmoderatedReviews()
    {
        // Act
        var response = await _managerClient.GetAsync($"{BaseRoute}/unmoderated");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reviews = await response.ToResponseModel<List<ProductReviewDto>>();
        reviews.Should().HaveCount(2);
        reviews.Should().OnlyContain(r => !r.IsModerated);
    }

    [Fact]
    public async Task GetUnmoderatedReviews_AsUser_ShouldReturnForbidden()
    {
        // Act
        var response = await _userClient.GetAsync($"{BaseRoute}/unmoderated");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUnmoderatedReviews_WhenUnauthorized_ShouldReturnUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"{BaseRoute}/unmoderated");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUnmoderatedReviews_WhenAllModerated_ShouldReturnEmptyList()
    {
        // Arrange
        _userReview.Moderate();
        _otherUserReview.Moderate();
        Context.ProductReviews.UpdateRange(_userReview, _otherUserReview);
        await SaveChangesAsync();

        // Act
        var response = await _adminClient.GetAsync($"{BaseRoute}/unmoderated");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reviews = await response.ToResponseModel<List<ProductReviewDto>>();
        reviews.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUnmoderatedReviews_WithMixedReviews_ShouldReturnOnlyUnmoderated()
    {
        // Arrange
        _userReview.Moderate();
        Context.ProductReviews.Update(_userReview);
        await SaveChangesAsync();

        // Act
        var response = await _adminClient.GetAsync($"{BaseRoute}/unmoderated");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reviews = await response.ToResponseModel<List<ProductReviewDto>>();
        reviews.Should().HaveCount(1);
        reviews.First().Id.Should().Be(_otherUserReview.Id.Value);
        reviews.First().IsModerated.Should().BeFalse();
    }

    [Fact]
    public async Task GetUnmoderatedReviews_ShouldBeOrderedByCreatedAtDescending()
    {
        // Arrange
        var product2 = ProductData.SecondTestProduct(new List<CategoryId> { _testCategory.Id });
        await Context.Products.AddAsync(product2);
        await SaveChangesAsync();

        var newestReview = ReviewData.CreateTestReview(product2.Id, Guid.NewGuid());
        await Context.ProductReviews.AddAsync(newestReview);
        await SaveChangesAsync();

        // Act
        var response = await _adminClient.GetAsync($"{BaseRoute}/unmoderated");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reviews = await response.ToResponseModel<List<ProductReviewDto>>();
        reviews.Should().HaveCount(3);
        reviews.First().Id.Should().Be(newestReview.Id.Value);
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
        review.Id.Should().Be(_userReview.Id.Value);

        // Перевірка в БД
        var dbReview = await Context.ProductReviews
            .AsNoTracking()
            .FirstAsync(r => r.Id == _userReview.Id);
        dbReview.IsModerated.Should().BeTrue();
    }

    [Fact]
    public async Task ModerateReview_AsManager_ShouldModerateReview()
    {
        // Act
        var response = await _managerClient.PostAsync(
            $"{BaseRoute}/{_userReview.Id.Value}/moderate",
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var review = await response.ToResponseModel<ProductReviewDto>();
        review.IsModerated.Should().BeTrue();
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
        
        // Перевірка що статус не змінився
        var dbReview = await Context.ProductReviews
            .AsNoTracking()
            .FirstAsync(r => r.Id == _userReview.Id);
        dbReview.IsModerated.Should().BeFalse();
    }

    [Fact]
    public async Task ModerateReview_WhenUnauthorized_ShouldReturnUnauthorized()
    {
        // Act
        var response = await Client.PostAsync(
            $"{BaseRoute}/{_userReview.Id.Value}/moderate",
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ModerateReview_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _adminClient.PostAsync($"{BaseRoute}/{nonExistentId}/moderate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ModerateReview_WithEmptyGuid_ShouldReturnBadRequest()
    {
        // Act
        var response = await _adminClient.PostAsync($"{BaseRoute}/{Guid.Empty}/moderate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ModerateReview_AlreadyModerated_ShouldSucceed()
    {
        // Arrange
        _userReview.Moderate();
        Context.ProductReviews.Update(_userReview);
        await SaveChangesAsync();

        // Act
        var response = await _adminClient.PostAsync(
            $"{BaseRoute}/{_userReview.Id.Value}/moderate",
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var review = await response.ToResponseModel<ProductReviewDto>();
        review.IsModerated.Should().BeTrue();
    }

    [Fact]
    public async Task ModerateReview_MultipleReviews_ShouldModerateIndependently()
    {
        // Act
        var response1 = await _adminClient.PostAsync($"{BaseRoute}/{_userReview.Id.Value}/moderate", null);
        var response2 = await _adminClient.PostAsync($"{BaseRoute}/{_otherUserReview.Id.Value}/moderate", null);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var dbReview1 = await Context.ProductReviews.AsNoTracking().FirstAsync(r => r.Id == _userReview.Id);
        var dbReview2 = await Context.ProductReviews.AsNoTracking().FirstAsync(r => r.Id == _otherUserReview.Id);
        
        dbReview1.IsModerated.Should().BeTrue();
        dbReview2.IsModerated.Should().BeTrue();
    }

    #endregion

    #region Complex Scenarios

    [Fact]
    public async Task ComplexScenario_UpdateModerateAndDelete_ShouldWorkCorrectly()
    {
        // Act 1 
        var updateRequest = new UpdateProductReviewDto(4, "Updated content");
        var updateResponse = await _userClient.PutAsJsonAsync($"{BaseRoute}/{_userReview.Id.Value}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act 2 
        var moderateResponse = await _adminClient.PostAsync($"{BaseRoute}/{_userReview.Id.Value}/moderate", null);
        moderateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act 3 
        var deleteResponse = await _userClient.DeleteAsync($"{BaseRoute}/{_userReview.Id.Value}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert
        var dbReview = await Context.ProductReviews.FirstOrDefaultAsync(r => r.Id == _userReview.Id);
        dbReview.Should().BeNull();
    }

    [Fact]
    public async Task DifferentRoles_ShouldHaveDifferentPermissions()
    {
        // Arrange
        var anotherUserReview = ReviewData.CreateTestReview(_testProduct.Id, Guid.NewGuid());
        await Context.ProductReviews.AddAsync(anotherUserReview);
        await SaveChangesAsync();

        // Act 
        var userUpdateResponse = await _userClient.PutAsJsonAsync(
            $"{BaseRoute}/{anotherUserReview.Id.Value}",
            ReviewData.UpdateTestReviewDto());

        // Act 
        var managerUpdateResponse = await _managerClient.PutAsJsonAsync(
            $"{BaseRoute}/{anotherUserReview.Id.Value}",
            ReviewData.UpdateTestReviewDto());

        // Act 
        var adminUpdateResponse = await _adminClient.PutAsJsonAsync(
            $"{BaseRoute}/{anotherUserReview.Id.Value}",
            ReviewData.UpdateTestReviewDto());

        // Assert
        userUpdateResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        managerUpdateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        adminUpdateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ModeratedAndUnmoderatedReviews_ShouldBeSeparated()
    {
        // Arrange
        _userReview.Moderate();
        Context.ProductReviews.Update(_userReview);
        await SaveChangesAsync();

        // Act 
        var unmoderatedResponse = await _adminClient.GetAsync($"{BaseRoute}/unmoderated");

        // Assert
        var unmoderatedReviews = await unmoderatedResponse.ToResponseModel<List<ProductReviewDto>>();
        unmoderatedReviews.Should().HaveCount(1);
        unmoderatedReviews.Should().NotContain(r => r.Id == _userReview.Id.Value);
        unmoderatedReviews.Should().Contain(r => r.Id == _otherUserReview.Id.Value);
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