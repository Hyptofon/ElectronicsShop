using Application.Common.Interfaces.Queries;
using Application.Common.Interfaces.Repositories;
using Domain.Products;
using Infrastructure.Persistence.Extensions;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class ProductRepository(ApplicationDbContext context) : IProductRepository, IProductQueries
{
    public async Task<Option<Product>> GetByIdAsync(ProductId id, CancellationToken cancellationToken)
    {
        var entity = await context.Products
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
            .FirstOrDefaultAsync(x => x.Name.ToLower() == name.ToLower(), cancellationToken);

        return entity ?? Option<Product>.None;
    }

    public void Add(Product entity)
    {
        context.Products.Add(entity);
    }

    public void Update(Product entity)
    {
        context.Products.Update(entity);
    }

    public void Delete(Product entity)
    {
        context.Products.Remove(entity);
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
        return await context.Products
            .AsNoTracking()
            .Include(x => x.Images)
            .Include(x => x.Categories)!
            .ThenInclude(x => x.Category)
            .Include(x => x.Reviews)
            .WithSearchTerm(searchTerm)
            .InCategory(categoryId)
            .WithPriceRange(minPrice, maxPrice)
            .WithBrand(brand)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }
}