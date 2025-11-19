using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.Products.Exceptions;
using Domain.Categories;
using Domain.Products;
using LanguageExt;
using MediatR;

namespace Application.Products.Commands;

public record UpdateProductCommand : IRequest<Either<ProductException, Product>>
{
    public required Guid ProductId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required decimal Price { get; init; }
    public required int StockQuantity { get; init; }
    public string? Brand { get; init; }
    public string? Model { get; init; }
    public required IReadOnlyList<Guid> Categories { get; init; }
}

public class UpdateProductCommandHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    ICategoryProductRepository categoryProductRepository,
    IApplicationDbContext dbContext)
    : IRequestHandler<UpdateProductCommand, Either<ProductException, Product>>
{
    public async Task<Either<ProductException, Product>> Handle(
        UpdateProductCommand request,
        CancellationToken cancellationToken)
    {
        var productId = new ProductId(request.ProductId);
        var existingProduct = await productRepository.GetByIdAsync(productId, cancellationToken);

        return await existingProduct.MatchAsync(
            product => UpdateEntity(product, request, cancellationToken),
            () => Task.FromResult<Either<ProductException, Product>>(
                new ProductNotFoundException(productId)));
    }

    private async Task<Either<ProductException, Product>> UpdateEntity(
        Product product,
        UpdateProductCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var categoryIds = request.Categories.Select(x => new CategoryId(x)).ToList();
            var categories = await categoryRepository.GetByIdsAsync(categoryIds, cancellationToken);

            if (categories.Count != categoryIds.Count)
            {
                return new ProductCategoriesNotFoundException();
            }

            product.UpdateDetails(
                request.Name,
                request.Description,
                request.Price,
                request.StockQuantity,
                request.Brand,
                request.Model);

            var existingCategoryProducts = await categoryProductRepository
                .GetByProductIdAsync(product.Id, cancellationToken);

            var categoriesToRemove = existingCategoryProducts
                .Where(x => !categoryIds.Contains(x.CategoryId))
                .ToList();

            var categoriesToAdd = categoryIds
                .Where(categoryId => !existingCategoryProducts.Any(x => x.CategoryId == categoryId))
                .Select(categoryId => CategoryProduct.New(categoryId, product.Id))
                .ToList();

            if (categoriesToRemove.Any())
            {
                categoryProductRepository.RemoveRange(categoriesToRemove);
            }

            if (categoriesToAdd.Any())
            {
                categoryProductRepository.AddRange(categoriesToAdd);
            }
            product.Categories?.Clear();
            productRepository.Update(product);
            
            await dbContext.SaveChangesAsync(cancellationToken);
            
            
            return product;
        }
        catch (Exception exception)
        {
            return new UnhandledProductException(product.Id, exception);
        }
    }
}