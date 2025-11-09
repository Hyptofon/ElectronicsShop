using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Domain.Orders;
using LanguageExt;
using MediatR;

namespace Application.Orders.Queries;

public record GetOrderByIdQuery(Guid OrderId) : IRequest<Option<Order>>;

public class GetOrderByIdQueryHandler(
    IOrderRepository orderRepository,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetOrderByIdQuery, Option<Order>>
{
    public async Task<Option<Order>> Handle(
        GetOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        var orderId = new OrderId(request.OrderId);
        var orderOption = await orderRepository.GetByIdAsync(orderId, cancellationToken);

        return await orderOption.MatchAsync(
            order =>
            {
                if (!currentUserService.IsAuthenticated || !currentUserService.UserId.HasValue)
                {
                    return Task.FromResult(Option<Order>.None);
                }

                if (order.UserId == currentUserService.UserId.Value ||
                    currentUserService.IsInRole("Manager") ||
                    currentUserService.IsInRole("Admin"))
                {
                    return Task.FromResult<Option<Order>>(order);
                }

                return Task.FromResult(Option<Order>.None);
            },
            () => Task.FromResult(Option<Order>.None));
    }
}