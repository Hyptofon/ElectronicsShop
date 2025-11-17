using Application.Common.Interfaces.Repositories;
using Domain.Products;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class ProductReviewRepository(ApplicationDbContext context) : IProductReviewRepository
{
    public void Add(ProductReview entity)
    {
        context.ProductReviews.Add(entity);
    }

    public void Update(ProductReview entity)
    {
        context.ProductReviews.Update(entity);
    }

    public void Delete(ProductReview entity)
    {
        context.ProductReviews.Remove(entity);
    }

    public async Task<Option<ProductReview>> GetByIdAsync(
        ProductReviewId id,
        CancellationToken cancellationToken)
    {
        var entity = await context.ProductReviews
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity ?? Option<ProductReview>.None;
    }
    
    public async Task<IReadOnlyList<ProductReview>> GetByProductIdAsync(
        ProductId productId,
        CancellationToken cancellationToken)
    {
        return await context.ProductReviews
            .AsNoTracking()
            .Where(x => x.ProductId == productId && x.IsModerated)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductReview>> GetAllUnmoderatedAsync(
        CancellationToken cancellationToken)
    {
        return await context.ProductReviews
            .AsNoTracking()
            .Where(x => !x.IsModerated) 
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<Option<ProductReview>> GetByProductAndUserAsync(
        ProductId productId, 
        Guid userId, 
        CancellationToken cancellationToken)
    {
        var entity = await context.ProductReviews
            .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId, cancellationToken);
    
        return entity ?? Option<ProductReview>.None;
    }
}