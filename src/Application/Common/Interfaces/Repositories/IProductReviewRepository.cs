using Domain.Products;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface IProductReviewRepository
{
    void Add(ProductReview entity);
    void Update(ProductReview entity);
    void Delete(ProductReview entity);
    Task<Option<ProductReview>> GetByIdAsync(ProductReviewId id, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductReview>> GetByProductIdAsync(
        ProductId productId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<ProductReview>> GetAllUnmoderatedAsync(CancellationToken cancellationToken);
    
    Task<Option<ProductReview>> GetByProductAndUserAsync(ProductId productId, Guid userId, CancellationToken cancellationToken);
    
    Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<bool> HasReviewsForProductAsync(ProductId productId, CancellationToken cancellationToken);
}