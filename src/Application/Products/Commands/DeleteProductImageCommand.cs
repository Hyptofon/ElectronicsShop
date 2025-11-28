using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.Products.Exceptions;
using Domain.Products;
using LanguageExt;
using MediatR;
using Unit = LanguageExt.Unit;

namespace Application.Products.Commands;

public record DeleteProductImageCommand(Guid ProductId, Guid ImageId)
    : IRequest<Either<ProductException, Unit>>;

public class DeleteProductImageCommandHandler(
    IProductRepository productRepository,
    IProductImageRepository productImageRepository,
    IFileStorage fileStorage,
    IApplicationDbContext dbContext)
    : IRequestHandler<DeleteProductImageCommand, Either<ProductException, Unit>>
{
    public async Task<Either<ProductException, Unit>> Handle(
        DeleteProductImageCommand request,
        CancellationToken cancellationToken)
    {
        var productId = new ProductId(request.ProductId);

        var existingProduct = await productRepository.GetByIdAsync(productId, cancellationToken);
        
        return await existingProduct.MatchAsync(
            product => DeleteImage(product, request.ImageId, cancellationToken),
            () => Task.FromResult<Either<ProductException, Unit>>(
                new ProductNotFoundException(productId)));
    }

    private async Task<Either<ProductException, Unit>> DeleteImage(
        Product product,
        Guid rawImageId,
        CancellationToken cancellationToken)
    {
        var imageId = new ProductImageId(rawImageId);
        
        var image = product.Images?.FirstOrDefault(i => i.Id == imageId);

        if (image == null)
        {
            return new ProductImageNotFoundException(imageId);
        }
        
        if (image.IsPrimary && product.Images!.Count > 1)
        {
            var newPrimary = product.Images.FirstOrDefault(i => i.Id != image.Id);
            if (newPrimary != null)
            {
                newPrimary.SetAsPrimary();
                productImageRepository.UpdateRange([newPrimary]);
            }
        }

        try
        {
            await fileStorage.DeleteAsync(image.GetFilePath(), cancellationToken);
            
            productImageRepository.Delete(image);
            
            await dbContext.SaveChangesAsync(cancellationToken);
            
            return Unit.Default;
        }
        catch (Exception exception)
        {
            return new UnhandledProductException(product.Id, exception);
        }
    }
}