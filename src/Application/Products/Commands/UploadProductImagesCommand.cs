using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.Products.Exceptions;
using Domain.Products;
using LanguageExt;
using MediatR;

namespace Application.Products.Commands;

public record UploadProductImagesCommand : IRequest<Either<ProductException, Product>>
{
    public required Guid ProductId { get; init; }
    public required IReadOnlyList<ProductImageUpload> Images { get; init; }
}

public record ProductImageUpload
{
    public required string OriginalName { get; init; }
    public required Stream FileStream { get; init; }
    public bool IsPrimary { get; init; }
}

public class UploadProductImagesCommandHandler(
    IProductRepository productRepository,
    IProductImageRepository productImageRepository,
    IFileStorage fileStorage)
    : IRequestHandler<UploadProductImagesCommand, Either<ProductException, Product>>
{
    public async Task<Either<ProductException, Product>> Handle(
        UploadProductImagesCommand request,
        CancellationToken cancellationToken)
    {
        var productId = new ProductId(request.ProductId);
        var existingProduct = await productRepository.GetByIdAsync(productId, cancellationToken);

        return await existingProduct.MatchAsync(
            product => UploadImages(product, request.Images, cancellationToken),
            () => Task.FromResult<Either<ProductException, Product>>(
                new ProductNotFoundException(productId)));
    }

    private async Task<Either<ProductException, Product>> UploadImages(
        Product product,
        IReadOnlyList<ProductImageUpload> images,
        CancellationToken cancellationToken)
    {
        try
        {
            var productImages = new List<ProductImage>();

            foreach (var imageUpload in images)
            {
                var productImage = ProductImage.New(
                    product.Id,
                    imageUpload.OriginalName,
                    imageUpload.IsPrimary);

                await fileStorage.UploadAsync(
                    imageUpload.FileStream,
                    productImage.GetFilePath(),
                    cancellationToken);

                productImages.Add(productImage);
            }

            await productImageRepository.AddRangeAsync(productImages, cancellationToken);

            return await productRepository.GetByIdAsync(product.Id, cancellationToken)
                .Match(
                    p => p,
                    () => throw new InvalidOperationException("Product not found after image upload"));
        }
        catch (Exception exception)
        {
            return new UnhandledProductException(product.Id, exception);
        }
    }
}