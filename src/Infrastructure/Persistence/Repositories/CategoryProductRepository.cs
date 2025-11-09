using Application.Common.Interfaces.Repositories;
using Domain.Categories;
using Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class CategoryProductRepository(ApplicationDbContext context) : ICategoryProductRepository
{
    public async Task<IReadOnlyList<CategoryProduct>> AddRangeAsync(
        IReadOnlyList<CategoryProduct> entities,
        CancellationToken cancellationToken)
    {
        await context.CategoryProducts.AddRangeAsync(entities, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return entities;
    }

    public async Task<IReadOnlyList<CategoryProduct>> RemoveRangeAsync(
        IReadOnlyList<CategoryProduct> entities,
        CancellationToken cancellationToken)
    {
        context.CategoryProducts.RemoveRange(entities);
        await context.SaveChangesAsync(cancellationToken);
        return entities;
    }

    public async Task<IReadOnlyList<CategoryProduct>> GetByProductIdAsync(
        ProductId productId,
        CancellationToken cancellationToken)
    {
        return await context.CategoryProducts
            .AsNoTracking()
            .Where(x => x.ProductId.Equals(productId))
            .ToListAsync(cancellationToken);
    }
}