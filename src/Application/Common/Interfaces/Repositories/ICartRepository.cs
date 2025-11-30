using Domain.Cart;
using Domain.Products;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface ICartRepository
{
    void Add(Cart entity);
    void Update(Cart entity);
    Task<Option<Cart>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<Option<Cart>> GetByIdAsync(CartId id, CancellationToken cancellationToken);
    
    Task<bool> HasCartItemsWithProductAsync(ProductId productId, CancellationToken cancellationToken);
    Task<bool> ExistsByUserIdAsync(Guid userId, CancellationToken cancellationToken);
}