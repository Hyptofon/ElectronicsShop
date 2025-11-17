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
    IProductImageRepository productImageRepository)
    : IRequestHandler<SetPrimaryProductImageCommand, Either<ProductException, Product>>
{
    public async Task<Either<ProductException, Product>> Handle(
        SetPrimaryProductImageCommand request,
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
        
        var currentPrimary = product.Images?.FirstOrDefault(i => i.IsPrimary);
        var newPrimary = product.Images?.FirstOrDefault(i => i.Id == imageId);
        
        if (newPrimary == null)
        {
            return new ProductImageNotFoundException(imageId);
        }
        
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
            await productImageRepository.UpdateRangeAsync(imagesToUpdate, cancellationToken);
        
            var updatedProduct = await productRepository.GetByIdAsync(productId, cancellationToken);
            
            return updatedProduct.Match<Either<ProductException, Product>>(
                p => p,
                () => new ProductNotFoundException(productId)); 
        }
        catch (Exception exception)
        {
            return new UnhandledProductException(productId, exception);
        }
    }
}