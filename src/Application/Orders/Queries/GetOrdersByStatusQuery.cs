using Application.Common.Interfaces.Repositories;
using Application.Orders.Exceptions;
using Domain.Orders;
using LanguageExt;
using MediatR;

namespace Application.Orders.Queries;

public record GetOrdersByStatusQuery(string Status) 
    : IRequest<Either<OrderException, IReadOnlyList<Order>>>;

public class GetOrdersByStatusQueryHandler(IOrderRepository orderRepository)
    : IRequestHandler<GetOrdersByStatusQuery, Either<OrderException, IReadOnlyList<Order>>>
{
    public async Task<Either<OrderException, IReadOnlyList<Order>>> Handle(
        GetOrdersByStatusQuery request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<OrderStatus>(request.Status, true, out var orderStatus))
        {
            return new InvalidOrderStatusException(request.Status);
        }
        var orders = await orderRepository.GetByStatusAsync(orderStatus, cancellationToken);
        
        return orders.ToList(); 
    }
}