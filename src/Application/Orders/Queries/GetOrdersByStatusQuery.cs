using Application.Common.Interfaces.Repositories;
using Domain.Orders;
using MediatR;

namespace Application.Orders.Queries;

public record GetOrdersByStatusQuery(OrderStatus Status) : IRequest<IReadOnlyList<Order>>;

public class GetOrdersByStatusQueryHandler(IOrderRepository orderRepository)
    : IRequestHandler<GetOrdersByStatusQuery, IReadOnlyList<Order>>
{
    public async Task<IReadOnlyList<Order>> Handle(
        GetOrdersByStatusQuery request,
        CancellationToken cancellationToken)
    {
        return await orderRepository.GetByStatusAsync(request.Status, cancellationToken);
    }
}