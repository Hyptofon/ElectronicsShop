using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.Orders.Exceptions;
using Domain.Orders;
using LanguageExt;
using MediatR;

namespace Application.Orders.Commands;

public record UpdateOrderStatusCommand(Guid OrderId, OrderStatus NewStatus)
    : IRequest<Either<OrderException, Order>>;

public class UpdateOrderStatusCommandHandler(
    IOrderRepository orderRepository,
    IApplicationDbContext dbContext)
    : IRequestHandler<UpdateOrderStatusCommand, Either<OrderException, Order>>
{
    public async Task<Either<OrderException, Order>> Handle(
        UpdateOrderStatusCommand request,
        CancellationToken cancellationToken)
    {
        var orderId = new OrderId(request.OrderId);
        var existingOrder = await orderRepository.GetByIdAsync(orderId, cancellationToken);

        return await existingOrder.MatchAsync(
            order => UpdateStatus(order, request.NewStatus, cancellationToken),
            () => Task.FromResult<Either<OrderException, Order>>(
                new OrderNotFoundException(orderId)));
    }

    private async Task<Either<OrderException, Order>> UpdateStatus(
        Order order,
        OrderStatus newStatus,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!IsValidStatusTransition(order.Status, newStatus))
            {
                return new InvalidOrderStatusTransitionException(order.Id, order.Status, newStatus);
            }

            order.UpdateStatus(newStatus);
            orderRepository.Update(order);
            
            await dbContext.SaveChangesAsync(cancellationToken);
            
            return order;
        }
        catch (Exception exception)
        {
            return new UnhandledOrderException(order.Id, exception);
        }
    }

    private static bool IsValidStatusTransition(OrderStatus currentStatus, OrderStatus newStatus)
    {
        return (currentStatus, newStatus) switch
        {
            (OrderStatus.Pending, OrderStatus.Processing) => true,
            (OrderStatus.Pending, OrderStatus.Cancelled) => true,
            (OrderStatus.Processing, OrderStatus.Shipped) => true,
            (OrderStatus.Processing, OrderStatus.Cancelled) => true,
            (OrderStatus.Shipped, OrderStatus.Delivered) => true,
            _ => false
        };
    }
}