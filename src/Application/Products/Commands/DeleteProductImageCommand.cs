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
    IFileStorage fileStorage)
    : IRequestHandler<DeleteProductImageCommand, Either<ProductException, Unit>>
{
    public async Task<Either<ProductException, Unit>> Handle(
        DeleteProductImageCommand request,
        CancellationToken cancellationToken)
    {
        var productId = new ProductId(request.ProductId);
        var imageId = new ProductImageId(request.ImageId);

        var productOption = await productRepository.GetByIdAsync(productId, cancellationToken);

        if (productOption.IsNone)
        {
            return new ProductNotFoundException(productId);
        }

        var product = productOption.Match(
            p => p,
            () => throw new InvalidOperationException("This should never happen"));
        
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
                await productImageRepository.UpdateRangeAsync([newPrimary], cancellationToken);
            }
        }

        try
        {
            await fileStorage.DeleteAsync(image.GetFilePath(), cancellationToken);
            await productImageRepository.DeleteAsync(image, cancellationToken);
            
            return Unit.Default;
        }
        catch (Exception exception)
        {
            return new UnhandledProductException(productId, exception);
        }
    }
}