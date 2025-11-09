using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Domain.Orders;
using MediatR;

namespace Application.Orders.Queries;

public record GetMyOrdersQuery : IRequest<IReadOnlyList<Order>>;

public class GetMyOrdersQueryHandler(
    IOrderRepository orderRepository,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetMyOrdersQuery, IReadOnlyList<Order>>
{
    public async Task<IReadOnlyList<Order>> Handle(
        GetMyOrdersQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || !currentUserService.UserId.HasValue)
        {
            return Array.Empty<Order>();
        }

        return await orderRepository.GetByUserIdAsync(
            currentUserService.UserId.Value,
            cancellationToken);
    }
}