using Application.Common.Interfaces.Repositories;
using Domain.Orders;
using Domain.Products;
using LanguageExt;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories;

public class OrderRepository(ApplicationDbContext context) : IOrderRepository
{
    public void Add(Order entity)
    {
        context.Orders.Add(entity);
    }

    public void Update(Order entity)
    {
        context.Orders.Update(entity);
    }

    public async Task<Option<Order>> GetByIdAsync(OrderId id, CancellationToken cancellationToken)
    {
        var entity = await context.Orders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return entity ?? Option<Order>.None;
    }

    public async Task<IReadOnlyList<Order>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await context.Orders
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Include(x => x.Items)
            .ThenInclude(i => i.Product)     
            .ThenInclude(p => p!.Images)  
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await context.Orders
            .Include(x => x.Items)
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetByStatusAsync(
        OrderStatus status,
        CancellationToken cancellationToken)
    {
        return await context.Orders
            .Include(x => x.Items)
            .AsNoTracking()
            .Where(x => x.Status == status)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<bool> HasOrderItemsWithProductAsync(
        ProductId productId,
        CancellationToken cancellationToken)
    {
        return await context.OrderItems
            .AsNoTracking()
            .AnyAsync(oi => oi.ProductId == productId, cancellationToken);
    }
}