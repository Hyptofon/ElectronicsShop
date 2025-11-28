using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.Products.Exceptions;
using Domain.Categories;
using Domain.Products;
using LanguageExt;
using MediatR;

namespace Application.Products.Commands;

public record CreateProductCommand : IRequest<Either<ProductException, Product>>
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required decimal Price { get; init; }
    public required int StockQuantity { get; init; }
    public string? Brand { get; init; }
    public string? Model { get; init; }
    public required IReadOnlyList<Guid> Categories { get; init; }
}

public class CreateProductCommandHandler(
    IProductRepository productRepository,
    ICategoryRepository categoryRepository,
    IApplicationDbContext dbContext)
    : IRequestHandler<CreateProductCommand, Either<ProductException, Product>>
{
    public async Task<Either<ProductException, Product>> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        return await CreateEntity(request, cancellationToken);
    }

    private async Task<Either<ProductException, Product>> CreateEntity(
        CreateProductCommand request,
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

            var productId = ProductId.New();
            var categoryProducts = categoryIds
                .Select(categoryId => CategoryProduct.New(categoryId, productId))
                .ToList();

            var product = Product.New(
                productId,
                request.Name,
                request.Description,
                request.Price,
                request.StockQuantity,
                request.Brand,
                request.Model,
                categoryProducts);

            productRepository.Add(product);
            
            await dbContext.SaveChangesAsync(cancellationToken);

            var createdProductOption = await productRepository.GetByIdAsync(product.Id, cancellationToken);

            return createdProductOption.Match<Either<ProductException, Product>>(
                p => p, 
                () => new UnhandledProductException(product.Id, new InvalidOperationException("Product not found after creation")) 
            );
        }
        catch (Exception exception)
        {
            return new UnhandledProductException(ProductId.Empty(), exception);
        }
    }
}