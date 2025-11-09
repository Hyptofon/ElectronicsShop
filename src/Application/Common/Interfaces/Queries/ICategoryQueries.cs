using Domain.Categories;
using LanguageExt;

namespace Application.Common.Interfaces.Queries;

public interface ICategoryQueries
{
    Task<Option<Category>> GetByIdAsync(CategoryId id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken);
}