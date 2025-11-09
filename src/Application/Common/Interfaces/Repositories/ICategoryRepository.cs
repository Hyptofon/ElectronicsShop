using Domain.Categories;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface ICategoryRepository
{
    Task<Category> AddAsync(Category category, CancellationToken cancellationToken);
    Task<Category> UpdateAsync(Category category, CancellationToken cancellationToken);
    Task<Category> DeleteAsync(Category category, CancellationToken cancellationToken);
    Task<Option<Category>> GetByNameAsync(string name, CancellationToken cancellationToken);
    Task<Option<Category>> GetByIdAsync(CategoryId id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Category>> GetByIdsAsync(
        IReadOnlyList<CategoryId> categoryIds,
        CancellationToken cancellationToken);
}