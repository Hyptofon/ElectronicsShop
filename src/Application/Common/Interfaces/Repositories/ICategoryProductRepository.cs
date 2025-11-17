using Domain.Categories;
using Domain.Products;

namespace Application.Common.Interfaces.Repositories;

public interface ICategoryProductRepository
{
    void AddRange(IReadOnlyList<CategoryProduct> entities);
    void RemoveRange(IReadOnlyList<CategoryProduct> entities);
    Task<IReadOnlyList<CategoryProduct>> GetByProductIdAsync(
        ProductId productId,
        CancellationToken cancellationToken);
}