using Domain.Products;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface IProductRepository
{
    Task<Product> AddAsync(Product entity, CancellationToken cancellationToken);
    Task<Product> UpdateAsync(Product entity, CancellationToken cancellationToken);
    Task<Product> DeleteAsync(Product entity, CancellationToken cancellationToken);
    Task<Option<Product>> GetByNameAsync(string name, CancellationToken cancellationToken);
    Task<Option<Product>> GetByIdAsync(ProductId id, CancellationToken cancellationToken);
}