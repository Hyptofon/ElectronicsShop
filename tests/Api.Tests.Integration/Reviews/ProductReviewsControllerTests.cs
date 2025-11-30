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

public class ProductReviewsControllerTests : BaseIntegrationTest, IAsyncLifetime
{
    private readonly Category _testCategory = CategoryData.FirstTestCategory("Review");
    private readonly Product _testProduct;
    private readonly string _testUserId = Guid.NewGuid().ToString();
    private readonly string _otherUserId = Guid.NewGuid().ToString();
    private readonly string _thirdUserId = Guid.NewGuid().ToString();
    private readonly string _adminUserId = Guid.NewGuid().ToString();
    private readonly string _managerUserId = Guid.NewGuid().ToString();
    private readonly HttpClient _userClient;
    private readonly HttpClient _otherUserClient;
    private readonly HttpClient _thirdUserClient;
    private readonly HttpClient _adminClient;
    private readonly HttpClient _managerClient;
    private readonly ProductReview _testReview;
    private readonly string _baseRoute;

    public ProductReviewsControllerTests(IntegrationTestWebFactory factory) : base(factory)
    {
        _userClient = CreateAuthenticatedClient("User", _testUserId);
        _otherUserClient = CreateAuthenticatedClient("User", _otherUserId);
        _thirdUserClient = CreateAuthenticatedClient("User", _thirdUserId);
        _adminClient = CreateAuthenticatedClient("Admin", _adminUserId);
        _managerClient = CreateAuthenticatedClient("Manager", _managerUserId);
        
        _testProduct = ProductData.FirstTestProduct(new List<CategoryId> { _testCategory.Id });
        _testReview = ReviewData.CreateTestReview(_testProduct.Id, Guid.Parse(_testUserId));
        _baseRoute = $"products/{_testProduct.Id.Value}/reviews";
    }

    #region GET Tests (Get Product Reviews)

    [Fact]
    public async Task GetProductReviews_WithoutAuth_ShouldReturnOnlyModeratedReviews()
    {
        // Arrange
        var moderatedReview = ReviewData.CreateTestReview(_testProduct.Id, Guid.NewGuid());
        moderatedReview.Moderate();
        await Context.ProductReviews.AddAsync(moderatedReview);
        await SaveChangesAsync();

        // Act
        var response = await Client.GetAsync(_baseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reviews = await response.ToResponseModel<List<ProductReviewDto>>();
        reviews.Should().HaveCount(1);
        reviews.Should().OnlyContain(r => r.IsModerated);
        reviews.First().Id.Should().Be(moderatedReview.Id.Value);
    }

    [Fact]
    public async Task GetProductReviews_WithMultipleModeratedReviews_ShouldReturnAll()
    {
        // Arrange
        var review1 = ReviewData.CreateTestReview(_testProduct.Id, Guid.NewGuid());
        var review2 = ReviewData.CreateTestReview(_testProduct.Id, Guid.NewGuid());
        var review3 = ReviewData.CreateTestReview(_testProduct.Id, Guid.NewGuid());
        
        review1.Moderate();
        review2.Moderate();
        review3.Moderate();
        
        await Context.ProductReviews.AddRangeAsync(review1, review2, review3);
        await SaveChangesAsync();

        // Act
        var response = await Client.GetAsync(_baseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reviews = await response.ToResponseModel<List<ProductReviewDto>>();
        reviews.Should().HaveCount(3);
        reviews.Should().OnlyContain(r => r.IsModerated);
    }

    [Fact]
    public async Task GetProductReviews_WithMixedReviews_ShouldReturnOnlyModerated()
    {
        // Arrange
        var moderatedReview = ReviewData.CreateTestReview(_testProduct.Id, Guid.NewGuid());
        var unmoderatedReview = ReviewData.CreateTestReview(_testProduct.Id, Guid.NewGuid());
        
        moderatedReview.Moderate();
        
        await Context.ProductReviews.AddRangeAsync(moderatedReview, unmoderatedReview);
        await SaveChangesAsync();

        // Act
        var response = await Client.GetAsync(_baseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reviews = await response.ToResponseModel<List<ProductReviewDto>>();
        reviews.Should().HaveCount(1);
        reviews.First().IsModerated.Should().BeTrue();
        reviews.Should().NotContain(r => r.Id == unmoderatedReview.Id.Value);
    }

    [Fact]
    public async Task GetProductReviews_ForProductWithNoReviews_ShouldReturnEmptyList()
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

    [Fact]
    public async Task GetProductReviews_ShouldBeOrderedByCreatedAtDescending()
    {
        // Arrange
        var oldReview = ReviewData.CreateTestReview(_testProduct.Id, Guid.NewGuid());
        oldReview.Moderate();
        await Context.ProductReviews.AddAsync(oldReview);
        await SaveChangesAsync();
        
        await Task.Delay(100);
        
        var newReview = ReviewData.CreateTestReview(_testProduct.Id, Guid.NewGuid());
        newReview.Moderate();
        await Context.ProductReviews.AddAsync(newReview);
        await SaveChangesAsync();

        // Act
        var response = await Client.GetAsync(_baseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reviews = await response.ToResponseModel<List<ProductReviewDto>>();
        reviews.Should().HaveCount(2);
        reviews.First().Id.Should().Be(newReview.Id.Value);
        reviews.Last().Id.Should().Be(oldReview.Id.Value);
    }

    [Fact]
    public async Task GetProductReviews_ForNonExistentProduct_ShouldReturnEmptyList()
    {
        // Arrange
        var nonExistentProductId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"products/{nonExistentProductId}/reviews");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reviews = await response.ToResponseModel<List<ProductReviewDto>>();
        reviews.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProductReviews_WithAuthenticatedUser_ShouldStillReturnOnlyModerated()
    {
        // Arrange
        var moderatedReview = ReviewData.CreateTestReview(_testProduct.Id, Guid.NewGuid());
        moderatedReview.Moderate();
        await Context.ProductReviews.AddAsync(moderatedReview);
        await SaveChangesAsync();

        // Act
        var response = await _userClient.GetAsync(_baseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reviews = await response.ToResponseModel<List<ProductReviewDto>>();
        reviews.Should().HaveCount(1);
        reviews.Should().OnlyContain(r => r.IsModerated);
    }

    #endregion

    #region POST Tests (Create Review)

    [Fact]
    public async Task CreateReview_WithValidData_ShouldCreateReview()
    {
        // Arrange
        var request = ReviewData.CreateTestReviewDto();

        // Act
        var response = await _otherUserClient.PostAsJsonAsync(_baseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reviewDto = await response.ToResponseModel<ProductReviewDto>();
    
        reviewDto.ProductId.Should().Be(_testProduct.Id.Value);
        reviewDto.UserId.Should().Be(Guid.Parse(_otherUserId));
        reviewDto.Rating.Should().Be(request.Rating);
        reviewDto.Comment.Should().Be(request.Comment);
        reviewDto.IsModerated.Should().BeFalse();
        reviewDto.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        reviewDto.UpdatedAt.Should().BeNull();
        
        // Перевірка в БД
        var reviewId = new ProductReviewId(reviewDto.Id);
        var dbReview = await Context.ProductReviews
            .FirstOrDefaultAsync(r => r.Id == reviewId);
        
        dbReview.Should().NotBeNull();
        dbReview.ProductId.Should().Be(_testProduct.Id);
        dbReview.UserId.Should().Be(Guid.Parse(_otherUserId));
    }

    [Fact]
    public async Task CreateReview_AsAdmin_ShouldCreateReview()
    {
        // Arrange
        var product3 = ProductData.FirstTestProduct(new List<CategoryId> { _testCategory.Id });
        await Context.Products.AddAsync(product3);
        await SaveChangesAsync();
    
        var adminBaseRoute = $"products/{product3.Id.Value}/reviews";
        var request = ReviewData.CreateTestReviewDto();

        // Act
        var response = await _adminClient.PostAsJsonAsync(adminBaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reviewDto = await response.ToResponseModel<ProductReviewDto>();
        reviewDto.UserId.Should().Be(Guid.Parse(_adminUserId)); 
        reviewDto.IsModerated.Should().BeFalse();
    }

    [Fact]
    public async Task CreateReview_AsManager_ShouldCreateReview()
    {
        // Arrange
        var product4 = ProductData.SecondTestProduct(new List<CategoryId> { _testCategory.Id });
        await Context.Products.AddAsync(product4);
        await SaveChangesAsync();
    
        var managerBaseRoute = $"products/{product4.Id.Value}/reviews";
        var request = ReviewData.CreateTestReviewDto();

        // Act
        var response = await _managerClient.PostAsJsonAsync(managerBaseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var reviewDto = await response.ToResponseModel<ProductReviewDto>();
        reviewDto.UserId.Should().Be(Guid.Parse(_managerUserId));
    }

    [Fact]
    public async Task CreateReview_WhenUnauthorized_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = ReviewData.CreateTestReviewDto();

        // Act
        var response = await Client.PostAsJsonAsync(_baseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateReview_WhenAlreadyExists_ShouldUpdateExisting()
    {
        // Arrange
        var request1 = ReviewData.CreateTestReviewDto(); 
        await _userClient.PostAsJsonAsync(_baseRoute, request1);
       
        var request2 = new CreateProductReviewDto(1, "Updated via create endpoint");

        // Act
        var response = await _userClient.PostAsJsonAsync(_baseRoute, request2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    
        var reviewDto = await response.ToResponseModel<ProductReviewDto>();
        reviewDto.Rating.Should().Be(1);
        reviewDto.Comment.Should().Be("Updated via create endpoint");
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
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    [InlineData(10)]
    public async Task CreateReview_WithInvalidRating_ShouldReturnBadRequest(int rating)
    {
        // Arrange
        var request = new CreateProductReviewDto(rating, "Valid comment");

        // Act
        var response = await _otherUserClient.PostAsJsonAsync(_baseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task CreateReview_WithInvalidComment_ShouldReturnBadRequest(string comment)
    {
        // Arrange
        var request = new CreateProductReviewDto(5, comment);

        // Act
        var response = await _otherUserClient.PostAsJsonAsync(_baseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateReview_WithTooLongComment_ShouldReturnBadRequest()
    {
        // Arrange
        var longComment = new string('A', 2001); 
        var request = new CreateProductReviewDto(5, longComment);

        // Act
        var response = await _otherUserClient.PostAsJsonAsync(_baseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateReview_WithEmptyProductId_ShouldReturnBadRequest()
    {
        // Arrange
        var request = ReviewData.CreateTestReviewDto();

        // Act
        var response = await _userClient.PostAsJsonAsync($"products/{Guid.Empty}/reviews", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateReview_WithMinimumValidRating_ShouldSucceed()
    {
        // Arrange
        var request = new CreateProductReviewDto(1, "Minimum rating");

        // Act
        var response = await _otherUserClient.PostAsJsonAsync(_baseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var review = await response.ToResponseModel<ProductReviewDto>();
        review.Rating.Should().Be(1);
    }

    [Fact]
    public async Task CreateReview_WithMaximumValidRating_ShouldSucceed()
    {
        // Arrange
        var request = new CreateProductReviewDto(5, "Maximum rating");

        // Act
        var response = await _otherUserClient.PostAsJsonAsync(_baseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var review = await response.ToResponseModel<ProductReviewDto>();
        review.Rating.Should().Be(5);
    }

    [Fact]
    public async Task CreateReview_MultipleUsersForSameProduct_ShouldSucceed()
    {
        // Arrange
        var request = ReviewData.CreateTestReviewDto();

        // Act
        var response1 = await _otherUserClient.PostAsJsonAsync(_baseRoute, request);
        var response2 = await _thirdUserClient.PostAsJsonAsync(_baseRoute, request);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var review1 = await response1.ToResponseModel<ProductReviewDto>();
        var review2 = await response2.ToResponseModel<ProductReviewDto>();

        review1.UserId.Should().Be(Guid.Parse(_otherUserId));
        review2.UserId.Should().Be(Guid.Parse(_thirdUserId));
        review1.Id.Should().NotBe(review2.Id);
    }

    [Fact]
    public async Task CreateReview_WithMaxLengthComment_ShouldSucceed()
    {
        // Arrange
        var maxLengthComment = new string('A', 2000); 
        var request = new CreateProductReviewDto(5, maxLengthComment);

        // Act
        var response = await _otherUserClient.PostAsJsonAsync(_baseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var review = await response.ToResponseModel<ProductReviewDto>();
        review.Comment.Length.Should().Be(2000);
    }

    #endregion

    #region Complex Scenarios

    [Fact]
    public async Task ComplexScenario_CreateAndVerifyNotVisibleUntilModerated()
    {
        // Arrange
        var request = ReviewData.CreateTestReviewDto();

        // Act 1 - Створюємо відгук
        var createResponse = await _otherUserClient.PostAsJsonAsync(_baseRoute, request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var createdReview = await createResponse.ToResponseModel<ProductReviewDto>();

        // Act 2 - Перевіряємо що відгук не відображається для публіки
        var getResponse = await Client.GetAsync(_baseRoute);
        var reviews = await getResponse.ToResponseModel<List<ProductReviewDto>>();

        // Assert
        reviews.Should().NotContain(r => r.Id == createdReview.Id);
    }

    [Fact]
    public async Task DifferentUsersCanReviewSameProduct()
    {
        // Arrange
        var request = ReviewData.CreateTestReviewDto();

        // Act
        var response1 = await _otherUserClient.PostAsJsonAsync(_baseRoute, request);
        var response2 = await _thirdUserClient.PostAsJsonAsync(_baseRoute, request);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var dbReviews = await Context.ProductReviews
            .Where(r => r.ProductId == _testProduct.Id)
            .ToListAsync();

        dbReviews.Should().HaveCountGreaterThanOrEqualTo(3); 
        dbReviews.Select(r => r.UserId).Distinct().Should().HaveCountGreaterThanOrEqualTo(3);
    }
    [Fact]
    public async Task SameUser_CanUpdateReview_BySendingPostRequestAgain()
    {
        // Arrange
        var request1 = new CreateProductReviewDto(5, "First review");
        var request2 = new CreateProductReviewDto(4, "Second review");

        // Act 

        var response1 = await _otherUserClient.PostAsJsonAsync(_baseRoute, request1); 
        var response2 = await _userClient.PostAsJsonAsync(_baseRoute, request2); 
        var response3 = await _otherUserClient.PostAsJsonAsync(_baseRoute, request1);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK); 
        response2.StatusCode.Should().Be(HttpStatusCode.OK); 
        response3.StatusCode.Should().Be(HttpStatusCode.OK); 
    }

    [Fact]
    public async Task ReviewsForDifferentProducts_ShouldBeIndependent()
    {
        // Arrange
        var product2 = ProductData.SecondTestProduct(new List<CategoryId> { _testCategory.Id });
        await Context.Products.AddAsync(product2);
        await SaveChangesAsync();

        var request = ReviewData.CreateTestReviewDto();

        // Act
        var response1 = await _otherUserClient.PostAsJsonAsync(_baseRoute, request);
        var response2 = await _otherUserClient.PostAsJsonAsync($"products/{product2.Id.Value}/reviews", request);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        var review1 = await response1.ToResponseModel<ProductReviewDto>();
        var review2 = await response2.ToResponseModel<ProductReviewDto>();

        review1.ProductId.Should().Be(_testProduct.Id.Value);
        review2.ProductId.Should().Be(product2.Id.Value);
        review1.UserId.Should().Be(review2.UserId);
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