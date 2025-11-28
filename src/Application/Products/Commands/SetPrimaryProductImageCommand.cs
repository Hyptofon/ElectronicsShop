using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.Products.Exceptions;
using Domain.Products;
using LanguageExt;
using MediatR;

namespace Application.Products.Commands;

public record SetPrimaryProductImageCommand(Guid ProductId, Guid ImageId)
    : IRequest<Either<ProductException, Product>>;

public class SetPrimaryProductImageCommandHandler(
    IProductRepository productRepository,
    IProductImageRepository productImageRepository,
    IApplicationDbContext dbContext)
    : IRequestHandler<SetPrimaryProductImageCommand, Either<ProductException, Product>>
{
    public async Task<Either<ProductException, Product>> Handle(
        SetPrimaryProductImageCommand request,
        CancellationToken cancellationToken)
    {
        var productId = new ProductId(request.ProductId);

        var productOption = await productRepository.GetByIdAsync(productId, cancellationToken);

        return await productOption.MatchAsync(
            product => SetPrimaryImage(product, request.ImageId, cancellationToken),
            () => Task.FromResult<Either<ProductException, Product>>(
                new ProductNotFoundException(productId)));
    }

    private async Task<Either<ProductException, Product>> SetPrimaryImage(
        Product product,
        Guid rawImageId,
        CancellationToken cancellationToken)
    {
        var imageId = new ProductImageId(rawImageId);
        
        var newPrimary = product.Images?.FirstOrDefault(i => i.Id == imageId);

        if (newPrimary == null)
        {
            return new ProductImageNotFoundException(imageId);
        }

        var currentPrimary = product.Images?.FirstOrDefault(i => i.IsPrimary);

        if (currentPrimary != null && currentPrimary.Id == newPrimary.Id)
        {
            return product;
        }

        var imagesToUpdate = new List<ProductImage>();

        if (currentPrimary != null)
        {
            currentPrimary.RemoveAsPrimary();
            imagesToUpdate.Add(currentPrimary);
        }

        newPrimary.SetAsPrimary();
        imagesToUpdate.Add(newPrimary);

        try
        {
            productImageRepository.UpdateRange(imagesToUpdate);

            await dbContext.SaveChangesAsync(cancellationToken);

            var updatedProductOption = await productRepository.GetByIdAsync(product.Id, cancellationToken);
            
            return updatedProductOption.Match<Either<ProductException, Product>>(
                p => p,
                () => new ProductNotFoundException(product.Id));
        }
        catch (Exception exception)
        {
            return new UnhandledProductException(product.Id, exception);
        }
    }
}