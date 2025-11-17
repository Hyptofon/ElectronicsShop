using Domain.Categories;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface ICategoryRepository
{
    void Add(Category category);
    void Update(Category category);
    void Delete(Category category);
    Task<Option<Category>> GetByNameAsync(string name, CancellationToken cancellationToken);
    Task<Option<Category>> GetByIdAsync(CategoryId id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Category>> GetByIdsAsync(
        IReadOnlyList<CategoryId> categoryIds,
        CancellationToken cancellationToken);
    
    Task<bool> HasProductsAsync(CategoryId id, CancellationToken cancellationToken);
}