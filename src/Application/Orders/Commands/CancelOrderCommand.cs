using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.Orders.Exceptions;
using Domain.Orders;
using LanguageExt;
using MediatR;
using Unit = LanguageExt.Unit;

namespace Application.Orders.Commands;

public record CancelOrderCommand(Guid OrderId)
    : IRequest<Either<OrderException, Order>>;

public class CancelOrderCommandHandler(
    IOrderRepository orderRepository,
    IProductRepository productRepository,
    ICurrentUserService currentUserService)
    : IRequestHandler<CancelOrderCommand, Either<OrderException, Order>>
{
    public async Task<Either<OrderException, Order>> Handle(
        CancelOrderCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || !currentUserService.UserId.HasValue)
        {
            return new UnauthorizedOrderAccessException(OrderId.Empty());
        }

        var orderId = new OrderId(request.OrderId);
        var existingOrder = await orderRepository.GetByIdAsync(orderId, cancellationToken);

        return await existingOrder.MatchAsync(
            order => CancelOrderAndRestoreStock(order, currentUserService.UserId.Value, cancellationToken),
            () => Task.FromResult<Either<OrderException, Order>>(
                new OrderNotFoundException(orderId)));
    }

    private async Task<Either<OrderException, Order>> CancelOrderAndRestoreStock(
        Order order,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (order.UserId != userId && !currentUserService.IsInRole("Manager") && !currentUserService.IsInRole("Admin"))
        {
            return new UnauthorizedOrderAccessException(order.Id);
        }
        if (order.Status == OrderStatus.Delivered)
        {
            return new InvalidOrderStatusTransitionException(order.Id, order.Status, OrderStatus.Cancelled);
        }

        try
        {
            order.Cancel();

            foreach (var item in order.Items)
            {
                var productOption = await productRepository.GetByIdAsync(item.ProductId, cancellationToken);

                await productOption.MatchAsync(
                    async product =>
                    {
                        product.IncreaseStock(item.Quantity);
                        await productRepository.UpdateAsync(product, cancellationToken);
                        return Unit.Default;
                    },
                    () => Task.FromResult(Unit.Default));
            }

            return await orderRepository.UpdateAsync(order, cancellationToken);
        }
        catch (Exception exception)
        {
            return new UnhandledOrderException(order.Id, exception);
        }
    }
}