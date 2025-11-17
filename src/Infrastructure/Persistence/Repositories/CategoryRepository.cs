using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.Categories;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class CategoryRepository(ApplicationDbContext context) : ICategoryRepository, ICategoryQueries
{
    public void Add(Category category)
    {
        context.Categories.Add(category);
    }

    public void Update(Category category)
    {
        context.Categories.Update(category);
    }

    public void Delete(Category category)
    {
        context.Categories.Remove(category);
    }

    public async Task<Option<Category>> GetByNameAsync(string name, CancellationToken cancellationToken)
    {
        var entity = await context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Name == name, cancellationToken);

        return entity ?? Option<Category>.None;
    }

    public async Task<IReadOnlyList<Category>> GetByIdsAsync(
        IReadOnlyList<CategoryId> categoryIds,
        CancellationToken cancellationToken)
    {
        return await context.Categories
            .AsNoTracking()
            .Where(x => categoryIds.Any(y => y == x.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<Option<Category>> GetByIdAsync(CategoryId id, CancellationToken cancellationToken)
    {
        var entity = await context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity ?? Option<Category>.None;
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await context.Categories
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<bool> HasProductsAsync(CategoryId id, CancellationToken cancellationToken)
    {
        return await context.CategoryProducts
            .AnyAsync(cp => cp.CategoryId == id, cancellationToken);
    }
}