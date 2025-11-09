using Domain.Categories;
using Domain.Products;

namespace Application.Common.Interfaces.Repositories;

public interface ICategoryProductRepository
{
    Task<IReadOnlyList<CategoryProduct>> AddRangeAsync(
        IReadOnlyList<CategoryProduct> entities,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CategoryProduct>> RemoveRangeAsync(
        IReadOnlyList<CategoryProduct> entities,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CategoryProduct>> GetByProductIdAsync(
        ProductId productId,
        CancellationToken cancellationToken);
}