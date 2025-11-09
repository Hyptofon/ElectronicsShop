using Application.Common.Interfaces.Repositories;
using Domain.Products;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class ProductReviewRepository(ApplicationDbContext context) : IProductReviewRepository
{
    public async Task<ProductReview> AddAsync(ProductReview entity, CancellationToken cancellationToken)
    {
        await context.ProductReviews.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<ProductReview> UpdateAsync(ProductReview entity, CancellationToken cancellationToken)
    {
        context.ProductReviews.Update(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<ProductReview> DeleteAsync(ProductReview entity, CancellationToken cancellationToken)
    {
        context.ProductReviews.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
        return entity;
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
            .Where(x => x.ProductId == productId && !x.IsModerated)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProductReview>> GetAllModeratedAsync(
        CancellationToken cancellationToken)
    {
        return await context.ProductReviews
            .AsNoTracking()
            .Where(x => x.IsModerated)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}