using Application.Common.Interfaces.Repositories;
using Domain.Orders;
using MediatR;

namespace Application.Orders.Queries;

public record GetAllOrdersQuery : IRequest<IReadOnlyList<Order>>;

public class GetAllOrdersQueryHandler(IOrderRepository orderRepository)
    : IRequestHandler<GetAllOrdersQuery, IReadOnlyList<Order>>
{
    public async Task<IReadOnlyList<Order>> Handle(
        GetAllOrdersQuery request,
        CancellationToken cancellationToken)
    {
        return await orderRepository.GetAllAsync(cancellationToken);
    }
}