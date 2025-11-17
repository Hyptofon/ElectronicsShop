using Application.Common.Interfaces.Repositories;
using Domain.Categories;
using Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class CategoryProductRepository(ApplicationDbContext context) : ICategoryProductRepository
{
    public void AddRange(IReadOnlyList<CategoryProduct> entities)
    {
        context.CategoryProducts.AddRange(entities);
    }

    public void RemoveRange(IReadOnlyList<CategoryProduct> entities)
    {
        context.CategoryProducts.RemoveRange(entities);
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