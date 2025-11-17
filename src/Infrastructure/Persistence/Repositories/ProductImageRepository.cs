using Application.Common.Interfaces.Repositories;
using Domain.Products;

namespace Infrastructure.Persistence.Repositories;

public class ProductImageRepository(ApplicationDbContext context) : IProductImageRepository
{
    public void Add(ProductImage entity)
    {
        context.ProductImages.Add(entity);
    }

    public void AddRange(IReadOnlyList<ProductImage> entities)
    {
        context.ProductImages.AddRange(entities);
    }
    
    public void Delete(ProductImage entity)
    {
        context.ProductImages.Remove(entity);
    }

    public void UpdateRange(IReadOnlyList<ProductImage> entities)
    {
        context.ProductImages.UpdateRange(entities);
    }
}