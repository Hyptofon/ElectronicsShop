using Domain.Products;

namespace Application.Common.Interfaces.Repositories;

public interface IProductImageRepository
{
    Task<ProductImage> AddAsync(ProductImage entity, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductImage>> AddRangeAsync(
        IReadOnlyList<ProductImage> entities,
        CancellationToken cancellationToken);
    
    Task<ProductImage> DeleteAsync(ProductImage entity, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductImage>> UpdateRangeAsync(
        IReadOnlyList<ProductImage> entities,
        CancellationToken cancellationToken);
    
}