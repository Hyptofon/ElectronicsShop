using Application.Common.Interfaces.Repositories;
using Domain.Products;

namespace Infrastructure.Persistence.Repositories;

public class ProductImageRepository(ApplicationDbContext context) : IProductImageRepository
{
    public async Task<ProductImage> AddAsync(ProductImage entity, CancellationToken cancellationToken)
    {
        await context.ProductImages.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<IReadOnlyList<ProductImage>> AddRangeAsync(
        IReadOnlyList<ProductImage> entities,
        CancellationToken cancellationToken)
    {
        await context.ProductImages.AddRangeAsync(entities, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return entities;
    }
    
    public async Task<ProductImage> DeleteAsync(ProductImage entity, CancellationToken cancellationToken)
    {
        context.ProductImages.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<IReadOnlyList<ProductImage>> UpdateRangeAsync(
        IReadOnlyList<ProductImage> entities, 
        CancellationToken cancellationToken)
    {
        context.ProductImages.UpdateRange(entities);
        await context.SaveChangesAsync(cancellationToken);
        return entities;
    }
}