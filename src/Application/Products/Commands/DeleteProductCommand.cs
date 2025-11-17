using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.Products.Exceptions;
using Domain.Products;
using LanguageExt;
using MediatR;

namespace Application.Products.Commands;

public record DeleteProductCommand(Guid ProductId) 
    : IRequest<Either<ProductException, Product>>;

public class DeleteProductCommandHandler(
    IProductRepository productRepository,
    IFileStorage fileStorage)
    : IRequestHandler<DeleteProductCommand, Either<ProductException, Product>>
{
    public async Task<Either<ProductException, Product>> Handle(
        DeleteProductCommand request,
        CancellationToken cancellationToken)
    {
        var productId = new ProductId(request.ProductId);
        var existingProduct = await productRepository.GetByIdAsync(productId, cancellationToken);

        return await existingProduct.MatchAsync(
            product => DeleteEntity(product, cancellationToken),
            () => Task.FromResult<Either<ProductException, Product>>(
                new ProductNotFoundException(productId)));
    }
    private async Task<Either<ProductException, Product>> DeleteEntity(
        Product product,
        CancellationToken cancellationToken)
    {
        try
        {
            if (product.Images != null && product.Images.Any())
            {
                var deleteTasks = product.Images
                    .Select(image => fileStorage.DeleteAsync(image.GetFilePath(), cancellationToken))
                    .ToList();
                await Task.WhenAll(deleteTasks);
            }
            return await productRepository.DeleteAsync(product, cancellationToken);
        }
        catch (Exception exception)
        {
            return new UnhandledProductException(product.Id, exception);
        }
    }
}