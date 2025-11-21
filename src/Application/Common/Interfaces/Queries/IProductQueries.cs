using Domain.Products;
using LanguageExt;

namespace Application.Common.Interfaces.Queries;

public interface IProductQueries
{
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Product>> SearchAsync(
        string? searchTerm,
        Guid? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        string? brand,
        CancellationToken cancellationToken);
    
    Task<Option<Product>> GetByIdAsync(ProductId id, CancellationToken cancellationToken);
}