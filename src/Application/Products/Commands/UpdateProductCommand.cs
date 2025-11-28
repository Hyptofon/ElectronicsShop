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

            // ✅ ВИПРАВЛЕНО: Використовуємо новий спеціальний тип помилки
            if (product.Categories == null) 
            {
                 return new ProductCategoriesNotLoadedException(product.Id);
            }

            var itemsToRemove = product.Categories
                .Where(cp => !categoryIds.Contains(cp.CategoryId))
                .ToList();

            foreach (var item in itemsToRemove)
            {
                product.Categories.Remove(item);
            }
            
            var currentCategoryIds = product.Categories.Select(c => c.CategoryId).ToList();
            var newCategoryIds = categoryIds.Except(currentCategoryIds);

            foreach (var newId in newCategoryIds)
            {
                product.Categories.Add(CategoryProduct.New(newId, product.Id));
            }
            
            productRepository.Update(product);
            
            await dbContext.SaveChangesAsync(cancellationToken);

            var updatedProductOption = await productRepository.GetByIdAsync(product.Id, cancellationToken);

            return updatedProductOption.Match<Either<ProductException, Product>>(
                p => p,
                () => new ProductNotFoundException(product.Id)
            );
        }
        catch (Exception exception)
        {
            return new UnhandledProductException(product.Id, exception);
        }
    }
}