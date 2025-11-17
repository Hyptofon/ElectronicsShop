using Domain.Products;

namespace Application.Common.Interfaces.Repositories;

public interface IProductImageRepository
{
    void Add(ProductImage entity);
    void AddRange(IReadOnlyList<ProductImage> entities);
    void Delete(ProductImage entity);
    void UpdateRange(IReadOnlyList<ProductImage> entities);
}