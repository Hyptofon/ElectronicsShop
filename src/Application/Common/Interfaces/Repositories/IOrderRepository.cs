using Domain.Orders;
using Domain.Products;
using LanguageExt;

namespace Application.Common.Interfaces.Repositories;

public interface IOrderRepository
{
    void Add(Order entity);
    void Update(Order entity);
    Task<Option<Order>> GetByIdAsync(OrderId id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<Order>> GetByStatusAsync(OrderStatus status, CancellationToken cancellationToken);
    Task<bool> HasOrderItemsWithProductAsync(ProductId productId, CancellationToken cancellationToken);
}