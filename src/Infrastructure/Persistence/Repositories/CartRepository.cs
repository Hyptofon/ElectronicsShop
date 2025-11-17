using Application.Common.Interfaces.Repositories;
using Domain.Cart;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class CartRepository(ApplicationDbContext context) : ICartRepository
{
    public void Add(Cart entity)
    {
        context.Carts.Add(entity);
    }

    public void Update(Cart entity)
    {
        context.Carts.Update(entity);
    }

    public async Task<Option<Cart>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        var entity = await context.Carts
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        return entity ?? Option<Cart>.None;
    }

    public async Task<Option<Cart>> GetByIdAsync(CartId id, CancellationToken cancellationToken)
    {
        var entity = await context.Carts
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity ?? Option<Cart>.None;
    }
}