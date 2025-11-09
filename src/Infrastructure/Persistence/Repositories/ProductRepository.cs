using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.Products;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class ProductRepository(ApplicationDbContext context) : IProductRepository, IProductQueries
{
    public async Task<Option<Product>> GetByIdAsync(ProductId id, CancellationToken cancellationToken)
    {
        var entity = await context.Products
            .AsNoTracking()
            .Include(x => x.Images)
            .Include(x => x.Categories)!
                .ThenInclude(x => x.Category)
            .Include(x => x.Reviews)
            .FirstOrDefaultAsync(x => x.Id.Equals(id), cancellationToken);

        return entity ?? Option<Product>.None;
    }

    public async Task<Option<Product>> GetByNameAsync(string name, CancellationToken cancellationToken)
    {
        var entity = await context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Name == name, cancellationToken);

        return entity ?? Option<Product>.None;
    }

    public async Task<Product> AddAsync(Product entity, CancellationToken cancellationToken)
    {
        await context.Products.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<Product> UpdateAsync(Product entity, CancellationToken cancellationToken)
    {
        context.Products.Update(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<Product> DeleteAsync(Product entity, CancellationToken cancellationToken)
    {
        context.Products.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await context.Products
            .Include(x => x.Images)
            .Include(x => x.Categories)!
                .ThenInclude(x => x.Category)
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> SearchAsync(
        string? searchTerm,
        Guid? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        string? brand,
        CancellationToken cancellationToken)
    {
        var query = context.Products
            .Include(x => x.Images)
            .Include(x => x.Categories)!
                .ThenInclude(x => x.Category)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(x => 
                x.Name.Contains(searchTerm) || 
                x.Description.Contains(searchTerm) ||
                (x.Brand != null && x.Brand.Contains(searchTerm)) ||
                (x.Model != null && x.Model.Contains(searchTerm)));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.Categories!.Any(c => c.CategoryId.Value == categoryId.Value));
        }

        if (minPrice.HasValue)
        {
            query = query.Where(x => x.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(x => x.Price <= maxPrice.Value);
        }

        if (!string.IsNullOrWhiteSpace(brand))
        {
            query = query.Where(x => x.Brand != null && x.Brand == brand);
        }

        return await query
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }
}