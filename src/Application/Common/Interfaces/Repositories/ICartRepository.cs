using Domain.Cart;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface ICartRepository
{
    Task<Cart> AddAsync(Cart entity, CancellationToken cancellationToken);
    Task<Cart> UpdateAsync(Cart entity, CancellationToken cancellationToken);
    Task<Option<Cart>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<Option<Cart>> GetByIdAsync(CartId id, CancellationToken cancellationToken);
}