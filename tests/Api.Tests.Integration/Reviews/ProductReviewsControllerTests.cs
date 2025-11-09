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

public class ProductReviewsControllerTests : BaseIntegrationTest, IAsyncLifetime
{
    private readonly Category _testCategory = CategoryData.FirstTestCategory();
    private Product _testProduct;
    private readonly string _testUserId = Guid.NewGuid().ToString();
    private readonly string _otherUserId = Guid.NewGuid().ToString();
    private HttpClient _userClient;
    private HttpClient _otherUserClient;
    private HttpClient _adminClient;
    private ProductReview _testReview;
    private string _baseRoute;

    public ProductReviewsControllerTests(IntegrationTestWebFactory factory) : base(factory)
    {
        _userClient = CreateAuthenticatedClient("User", _testUserId);
        _otherUserClient = CreateAuthenticatedClient("User", _otherUserId);
        _adminClient = CreateAuthenticatedClient("Admin");
        
        _testProduct = ProductData.FirstTestProduct(new List<CategoryId> { _testCategory.Id });
        _testReview = ReviewData.CreateTestReview(_testProduct.Id, Guid.Parse(_testUserId));
        _baseRoute = $"products/{_testProduct.Id.Value}/reviews";
    }

    #region GET Tests (Get Product Reviews)

    [Fact]
    public async Task GetReviews_WithoutAuth_ShouldReturnNonModeratedReviews()
    {
        // Arrange - додаємо модерований відгук
        var moderatedReview = ReviewData.CreateTestReview(_testProduct.Id, Guid.NewGuid());
        moderatedReview.Moderate();
        await Context.ProductReviews.AddAsync(moderatedReview);
        await SaveChangesAsync();

        // Act
        var response = await Client.GetAsync(_baseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reviews = await response.ToResponseModel<List<ProductReviewDto>>();
        reviews.Should().HaveCount(1); // тільки немодерований
        reviews.Should().OnlyContain(r => !r.IsModerated);
    }

    [Fact]
    public async Task GetReviews_ForProductWithNoReviews_ShouldReturnEmptyList()
    {
        // Arrange
        var product2 = ProductData.SecondTestProduct(new List<CategoryId> { _testCategory.Id });
        await Context.Products.AddAsync(product2);
        await SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"products/{product2.Id.Value}/reviews");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reviews = await response.ToResponseModel<List<ProductReviewDto>>();
        reviews.Should().BeEmpty();
    }

    #endregion

    #region POST Tests (Create Review)

    [Fact]
    public async Task CreateReview_AsUser_ShouldCreateReview()
    {
        // Arrange
        var request = ReviewData.CreateTestReviewDto();

        // Act
        var response = await _userClient.PostAsJsonAsync(_baseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var review = await response.ToResponseModel<ProductReviewDto>();
        
        review.ProductId.Should().Be(_testProduct.Id.Value);
        review.UserId.Should().Be(Guid.Parse(_testUserId));
        review.Rating.Should().Be(request.Rating);
        review.Comment.Should().Be(request.Comment);
        review.IsModerated.Should().BeFalse();

        var dbReview = await Context.ProductReviews
            .FirstOrDefaultAsync(r => r.Id.Value == review.Id);
        dbReview.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateReview_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = ReviewData.CreateTestReviewDto();

        // Act
        var response = await Client.PostAsJsonAsync(_baseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateReview_ForNonExistentProduct_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentProductId = Guid.NewGuid();
        var request = ReviewData.CreateTestReviewDto();

        // Act
        var response = await _userClient.PostAsJsonAsync(
            $"products/{nonExistentProductId}/reviews",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData(0)] // Rating too low
    [InlineData(6)] // Rating too high
    public async Task CreateReview_WithInvalidRating_ShouldReturnBadRequest(int rating)
    {
        // Arrange
        var request = new CreateProductReviewDto(rating, "Valid comment");

        // Act
        var response = await _userClient.PostAsJsonAsync(_baseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("")] // Empty comment
    [InlineData(null)] // Null comment
    public async Task CreateReview_WithInvalidComment_ShouldReturnBadRequest(string comment)
    {
        // Arrange
        var request = new CreateProductReviewDto(5, comment);

        // Act
        var response = await _userClient.PostAsJsonAsync(_baseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    public async Task InitializeAsync()
    {
        await Context.Categories.AddAsync(_testCategory);
        await Context.Products.AddAsync(_testProduct);
        await Context.ProductReviews.AddAsync(_testReview);
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