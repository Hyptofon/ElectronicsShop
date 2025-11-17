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
    private readonly string _adminUserId = Guid.NewGuid().ToString();
    private readonly HttpClient _userClient;
    private readonly HttpClient _otherUserClient;
    private readonly HttpClient _adminClient;
    private readonly ProductReview _testReview;
    private readonly string _baseRoute;
    

    public ProductReviewsControllerTests(IntegrationTestWebFactory factory) : base(factory)
    {
        _userClient = CreateAuthenticatedClient("User", _testUserId);
        _otherUserClient = CreateAuthenticatedClient("User", _otherUserId);
        _adminClient = CreateAuthenticatedClient("Admin", _adminUserId);
        
        _testProduct = ProductData.FirstTestProduct(new List<CategoryId> { _testCategory.Id });
        _testReview = ReviewData.CreateTestReview(_testProduct.Id, Guid.Parse(_testUserId));
        _baseRoute = $"products/{_testProduct.Id.Value}/reviews";
    }

    #region GET Tests

    [Fact]
    public async Task ShouldGetOnlyApprovedReviewsWithoutAuth()
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
    }

    [Fact]
    public async Task ShouldReturnEmptyListForProductWithNoReviews()
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

    #region POST Tests

    [Fact]
    public async Task ShouldCreateReviewAsUser()
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
        
        var reviewId = new ProductReviewId(reviewDto.Id);
        var dbReview = await Context.ProductReviews
            .FirstOrDefaultAsync(r => r.Id == reviewId);
        
        dbReview.Should().NotBeNull();
    }

    [Fact]
    public async Task ShouldNotCreateReviewBecauseUnauthorized()
    {
        // Arrange
        var request = ReviewData.CreateTestReviewDto();

        // Act
        var response = await Client.PostAsJsonAsync(_baseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    
    [Fact]
    public async Task ShouldNotCreateReviewBecauseAlreadyExists()
    {
        // Arrange: _testReview вже існує від _testUserId (див. конструктор)
        var request = ReviewData.CreateTestReviewDto(); 

        // Act: _testUserId намагається створити другий відгук
        var response = await _userClient.PostAsJsonAsync(_baseRoute, request);

        // Assert: Очікуємо конфлікт або Bad Request (залежно від реалізації обробки помилок у команді)
        // Якщо ви використовуєте ProductReviewAlreadyExistsException, очікується 409 Conflict.
        response.StatusCode.Should().Be(HttpStatusCode.Conflict); 
    }
    
    
    [Fact]
    public async Task ShouldCreateReviewAsAdmin()
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
        // Використовуємо збережене приватне поле
        reviewDto.UserId.Should().Be(Guid.Parse(_adminUserId)); 
    }
    
    
    [Fact]
    public async Task ShouldNotCreateReviewBecauseProductNotFound()
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
    public async Task ShouldNotCreateReviewBecauseInvalidRating(int rating)
    {
        // Arrange
        var request = new CreateProductReviewDto(rating, "Valid comment");

        // Act
        var response = await _userClient.PostAsJsonAsync(_baseRoute, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task ShouldNotCreateReviewBecauseInvalidComment(string comment)
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