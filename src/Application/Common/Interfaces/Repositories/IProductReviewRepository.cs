using Domain.Products;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface IProductReviewRepository
{
    Task<ProductReview> AddAsync(ProductReview entity, CancellationToken cancellationToken);
    Task<ProductReview> UpdateAsync(ProductReview entity, CancellationToken cancellationToken);
    Task<ProductReview> DeleteAsync(ProductReview entity, CancellationToken cancellationToken);
    Task<Option<ProductReview>> GetByIdAsync(ProductReviewId id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductReview>> GetByProductIdAsync(
        ProductId productId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductReview>> GetAllUnmoderatedAsync(CancellationToken cancellationToken);
    
    Task<Option<ProductReview>> GetByProductAndUserAsync(ProductId productId, Guid userId, CancellationToken cancellationToken);
}