using Domain.Products;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface IProductRepository
{
    void Add(Product entity);
    void Update(Product entity);
    void Delete(Product entity);
    Task<Option<Product>> GetByNameAsync(string name, CancellationToken cancellationToken);
    Task<Option<Product>> GetByIdAsync(ProductId id, CancellationToken cancellationToken);
}