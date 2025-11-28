using System.Net;
using Api.Dtos;
using Domain.Categories;
using Domain.Products;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.Common;
using Tests.Data.Categories;
using Tests.Data.Products;

namespace Api.Tests.Integration.Products;

public class ProductImagesIntegrationTests : BaseIntegrationTest, IAsyncLifetime
{
    private const string BaseRoute = "products";
    
    private readonly Category _testCategory = CategoryData.FirstTestCategory("ProductImages");
    private readonly Product _testProduct;
    private readonly HttpClient _adminClient;
    private readonly HttpClient _managerClient;

    public ProductImagesIntegrationTests(IntegrationTestWebFactory factory) : base(factory)
    {
        _adminClient = CreateAuthenticatedClient("Admin");
        _managerClient = CreateAuthenticatedClient("Manager");
        
        _testProduct = ProductData.FirstTestProduct(new List<CategoryId> { _testCategory.Id });
    }

    #region Upload Images Tests

    [Fact]
    public async Task ShouldUploadSingleImageAsManager()
    {
        // Arrange
        var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4, 5 });
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        content.Add(imageContent, "files", "single-test.jpg");

        // Act
        var response = await _managerClient.PostAsync(
            $"{BaseRoute}/{_testProduct.Id.Value}/images",
            content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = await response.ToResponseModel<ProductDto>();
        
        product.Images.Should().HaveCount(1);
        product.Images!.First().OriginalName.Should().Be("single-test.jpg");
        product.Images.First().IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldUploadMultipleImagesAsManager()
    {
        // Arrange
        var content = new MultipartFormDataContent();
        
        var imageContent1 = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        imageContent1.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        content.Add(imageContent1, "files", "image1.jpg");

        var imageContent2 = new ByteArrayContent(new byte[] { 5, 6, 7, 8 });
        imageContent2.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(imageContent2, "files", "image2.png");

        var imageContent3 = new ByteArrayContent(new byte[] { 9, 10, 11, 12 });
        imageContent3.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        content.Add(imageContent3, "files", "image3.jpg");

        // Act
        var response = await _managerClient.PostAsync(
            $"{BaseRoute}/{_testProduct.Id.Value}/images",
            content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = await response.ToResponseModel<ProductDto>();
        
        product.Images.Should().HaveCount(3);
        product.Images!.Count(i => i.IsPrimary).Should().Be(1);
        product.Images.First().IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldUploadImagesAsAdmin()
    {
        // Arrange
        var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        content.Add(imageContent, "files", "admin-test.jpg");

        // Act
        var response = await _adminClient.PostAsync(
            $"{BaseRoute}/{_testProduct.Id.Value}/images",
            content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var product = await response.ToResponseModel<ProductDto>();
        product.Images.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ShouldNotUploadImagesBecauseNoFiles()
    {
        // Arrange
        var content = new MultipartFormDataContent();

        // Act
        var response = await _managerClient.PostAsync(
            $"{BaseRoute}/{_testProduct.Id.Value}/images",
            content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ShouldNotUploadImagesBecauseProductNotFound()
    {
        // Arrange
        var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        content.Add(imageContent, "files", "test.jpg");
        
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _managerClient.PostAsync(
            $"{BaseRoute}/{nonExistentId}/images",
            content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldNotUploadImagesBecauseForbidden()
    {
        // Arrange
        var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        content.Add(imageContent, "files", "test.jpg");

        // Act
        var response = await AuthenticatedClient.PostAsync(
            $"{BaseRoute}/{_testProduct.Id.Value}/images",
            content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ShouldNotUploadImagesBecauseUnauthorized()
    {
        // Arrange
        var content = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        content.Add(imageContent, "files", "test.jpg");

        // Act
        var response = await Client.PostAsync(
            $"{BaseRoute}/{_testProduct.Id.Value}/images",
            content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Delete Image Tests

    [Fact]
    public async Task ShouldDeleteImageAsManager()
    {
        // Arrange 
        var uploadContent = new MultipartFormDataContent();
        var imageContent1 = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        imageContent1.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        uploadContent.Add(imageContent1, "files", "delete-test1.jpg");

        var imageContent2 = new ByteArrayContent(new byte[] { 5, 6, 7, 8 });
        imageContent2.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        uploadContent.Add(imageContent2, "files", "delete-test2.jpg");

        var uploadResponse = await _managerClient.PostAsync(
            $"{BaseRoute}/{_testProduct.Id.Value}/images",
            uploadContent);
        var product = await uploadResponse.ToResponseModel<ProductDto>();
        var imageToDelete = product.Images!.Last();

        // Act
        var response = await _managerClient.DeleteAsync(
            $"{BaseRoute}/{_testProduct.Id.Value}/images/{imageToDelete.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var updatedProduct = await Context.Products
            .Include(p => p.Images)
            .FirstAsync(p => p.Id == _testProduct.Id);
        
        updatedProduct.Images!.Should().HaveCount(1);
        updatedProduct.Images.Should().NotContain(i => i.Id.Value == imageToDelete.Id);
    }

    [Fact]
    public async Task ShouldDeleteImageAsAdmin()
    {
        // Arrange 
        var uploadContent = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        uploadContent.Add(imageContent, "files", "admin-delete-test.jpg");

        var uploadResponse = await _adminClient.PostAsync(
            $"{BaseRoute}/{_testProduct.Id.Value}/images",
            uploadContent);
        var product = await uploadResponse.ToResponseModel<ProductDto>();
        var imageId = product.Images!.First().Id;

        // Act
        var response = await _adminClient.DeleteAsync(
            $"{BaseRoute}/{_testProduct.Id.Value}/images/{imageId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ShouldDeletePrimaryImageAndSetNewPrimary()
    {
        // Arrange
        var uploadContent = new MultipartFormDataContent();
        var imageContent1 = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        imageContent1.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        uploadContent.Add(imageContent1, "files", "primary1.jpg");

        var imageContent2 = new ByteArrayContent(new byte[] { 5, 6, 7, 8 });
        imageContent2.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        uploadContent.Add(imageContent2, "files", "primary2.jpg");

        var uploadResponse = await _managerClient.PostAsync(
            $"{BaseRoute}/{_testProduct.Id.Value}/images",
            uploadContent);
        var product = await uploadResponse.ToResponseModel<ProductDto>();
        var primaryImage = product.Images!.First(i => i.IsPrimary);

        // Act 
        var response = await _managerClient.DeleteAsync(
            $"{BaseRoute}/{_testProduct.Id.Value}/images/{primaryImage.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var updatedProduct = await Context.Products
            .Include(p => p.Images)
            .FirstAsync(p => p.Id == _testProduct.Id);
        
        updatedProduct.Images!.Should().HaveCount(1);
        updatedProduct.Images.Count(i => i.IsPrimary).Should().Be(1);
    }

    [Fact]
    public async Task ShouldNotDeleteImageBecauseImageNotFound()
    {
        // Arrange
        var nonExistentImageId = Guid.NewGuid();

        // Act
        var response = await _managerClient.DeleteAsync(
            $"{BaseRoute}/{_testProduct.Id.Value}/images/{nonExistentImageId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldNotDeleteImageBecauseProductNotFound()
    {
        // Arrange
        var nonExistentProductId = Guid.NewGuid();
        var imageId = Guid.NewGuid();

        // Act
        var response = await _managerClient.DeleteAsync(
            $"{BaseRoute}/{nonExistentProductId}/images/{imageId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldNotDeleteImageBecauseForbidden()
    {
        // Arrange 
        var uploadContent = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        uploadContent.Add(imageContent, "files", "forbidden-test.jpg");

        var uploadResponse = await _managerClient.PostAsync(
            $"{BaseRoute}/{_testProduct.Id.Value}/images",
            uploadContent);
        var product = await uploadResponse.ToResponseModel<ProductDto>();
        var imageId = product.Images!.First().Id;

        // Act
        var response = await AuthenticatedClient.DeleteAsync(
            $"{BaseRoute}/{_testProduct.Id.Value}/images/{imageId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ShouldNotDeleteImageBecauseUnauthorized()
    {
        // Arrange
        var imageId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync(
            $"{BaseRoute}/{_testProduct.Id.Value}/images/{imageId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Set Primary Image Tests

    [Fact]
    public async Task ShouldSetPrimaryImageAsManager()
    {
        // Arrange 
        var uploadContent = new MultipartFormDataContent();
        var imageContent1 = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        imageContent1.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        uploadContent.Add(imageContent1, "files", "set-primary1.jpg");

        var imageContent2 = new ByteArrayContent(new byte[] { 5, 6, 7, 8 });
        imageContent2.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        uploadContent.Add(imageContent2, "files", "set-primary2.jpg");

        var uploadResponse = await _managerClient.PostAsync(
            $"{BaseRoute}/{_testProduct.Id.Value}/images",
            uploadContent);
        var product = await uploadResponse.ToResponseModel<ProductDto>();
        var newPrimaryImage = product.Images!.Last();

        // Act
        var response = await _managerClient.PostAsync(
            $"{BaseRoute}/{_testProduct.Id.Value}/images/{newPrimaryImage.Id}/set-primary",
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedProduct = await response.ToResponseModel<ProductDto>();
        
        updatedProduct.Images!.Count(i => i.IsPrimary).Should().Be(1);
        updatedProduct.Images.First(i => i.Id == newPrimaryImage.Id).IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldSetPrimaryImageAsAdmin()
    {
        // Arrange 
        var uploadContent = new MultipartFormDataContent();
        var imageContent1 = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        imageContent1.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        uploadContent.Add(imageContent1, "files", "admin-primary1.jpg");

        var imageContent2 = new ByteArrayContent(new byte[] { 5, 6, 7, 8 });
        imageContent2.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        uploadContent.Add(imageContent2, "files", "admin-primary2.jpg");

        var uploadResponse = await _adminClient.PostAsync(
            $"{BaseRoute}/{_testProduct.Id.Value}/images",
            uploadContent);
        var product = await uploadResponse.ToResponseModel<ProductDto>();
        var newPrimaryImage = product.Images!.Last();

        // Act
        var response = await _adminClient.PostAsync(
            $"{BaseRoute}/{_testProduct.Id.Value}/images/{newPrimaryImage.Id}/set-primary",
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ShouldReturnSameProductWhenImageAlreadyPrimary()
    {
        // Arrange 
        var uploadContent = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        uploadContent.Add(imageContent, "files", "already-primary.jpg");

        var uploadResponse = await _managerClient.PostAsync(
            $"{BaseRoute}/{_testProduct.Id.Value}/images",
            uploadContent);
        var product = await uploadResponse.ToResponseModel<ProductDto>();
        var primaryImage = product.Images!.First(i => i.IsPrimary);

        // Act 
        var response = await _managerClient.PostAsync(
            $"{BaseRoute}/{_testProduct.Id.Value}/images/{primaryImage.Id}/set-primary",
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedProduct = await response.ToResponseModel<ProductDto>();
        updatedProduct.Images!.First(i => i.Id == primaryImage.Id).IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldNotSetPrimaryImageBecauseImageNotFound()
    {
        // Arrange
        var nonExistentImageId = Guid.NewGuid();

        // Act
        var response = await _managerClient.PostAsync(
            $"{BaseRoute}/{_testProduct.Id.Value}/images/{nonExistentImageId}/set-primary",
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldNotSetPrimaryImageBecauseProductNotFound()
    {
        // Arrange
        var nonExistentProductId = Guid.NewGuid();
        var imageId = Guid.NewGuid();

        // Act
        var response = await _managerClient.PostAsync(
            $"{BaseRoute}/{nonExistentProductId}/images/{imageId}/set-primary",
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShouldNotSetPrimaryImageBecauseForbidden()
    {
        // Arrange
        var uploadContent = new MultipartFormDataContent();
        var imageContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
        uploadContent.Add(imageContent, "files", "forbidden-primary.jpg");

        var uploadResponse = await _managerClient.PostAsync(
            $"{BaseRoute}/{_testProduct.Id.Value}/images",
            uploadContent);
        var product = await uploadResponse.ToResponseModel<ProductDto>();
        var imageId = product.Images!.First().Id;

        // Act
        var response = await AuthenticatedClient.PostAsync(
            $"{BaseRoute}/{_testProduct.Id.Value}/images/{imageId}/set-primary",
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ShouldNotSetPrimaryImageBecauseUnauthorized()
    {
        // Arrange
        var imageId = Guid.NewGuid();

        // Act
        var response = await Client.PostAsync(
            $"{BaseRoute}/{_testProduct.Id.Value}/images/{imageId}/set-primary",
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ShouldOnlyHaveOnePrimaryImageAfterSettingNew()
    {
        // Arrange 
        var uploadContent = new MultipartFormDataContent();
        for (int i = 1; i <= 3; i++)
        {
            var imageContent = new ByteArrayContent(new byte[] { (byte)i, 2, 3, 4 });
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
            uploadContent.Add(imageContent, "files", $"multi-primary{i}.jpg");
        }

        var uploadResponse = await _managerClient.PostAsync(
            $"{BaseRoute}/{_testProduct.Id.Value}/images",
            uploadContent);
        var product = await uploadResponse.ToResponseModel<ProductDto>();
        var thirdImage = product.Images![2];

        // Act 
        var response = await _managerClient.PostAsync(
            $"{BaseRoute}/{_testProduct.Id.Value}/images/{thirdImage.Id}/set-primary",
            null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var dbProduct = await Context.Products
            .Include(p => p.Images)
            .FirstAsync(p => p.Id == _testProduct.Id);
        
        dbProduct.Images!.Count(i => i.IsPrimary).Should().Be(1);
        dbProduct.Images.First(i => i.Id.Value == thirdImage.Id).IsPrimary.Should().BeTrue();
    }

    #endregion

    public async Task InitializeAsync()
    {
        await Context.Categories.AddAsync(_testCategory);
        await Context.Products.AddAsync(_testProduct);
        await SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        Context.Products.RemoveRange(Context.Products);
        Context.Categories.RemoveRange(Context.Categories);
        
        await SaveChangesAsync();
    }
}