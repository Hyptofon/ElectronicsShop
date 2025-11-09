using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.Orders.Exceptions;
using Domain.Orders;
using Domain.Products;
using LanguageExt;
using MediatR;

namespace Application.Orders.Commands;

public record CreateOrderCommand : IRequest<Either<OrderException, Order>>
{
    public required string ShippingAddress { get; init; }
    public string? Notes { get; init; }
}

public class CreateOrderCommandHandler(
    IOrderRepository orderRepository,
    ICartRepository cartRepository,
    IProductRepository productRepository,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateOrderCommand, Either<OrderException, Order>>
{
    public async Task<Either<OrderException, Order>> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || !currentUserService.UserId.HasValue)
        {
            return new UnauthorizedOrderAccessException(OrderId.Empty());
        }

        var userId = currentUserService.UserId.Value;
        var cartOption = await cartRepository.GetByUserIdAsync(userId, cancellationToken);

        return await cartOption.MatchAsync(
            cart => CreateOrderFromCart(cart, userId, request, cancellationToken),
            () => Task.FromResult<Either<OrderException, Order>>(new EmptyCartException()));
    }

    private async Task<Either<OrderException, Order>> CreateOrderFromCart(
        Domain.Cart.Cart cart,
        Guid userId,
        CreateOrderCommand request,
        CancellationToken cancellationToken)
    {
        if (!cart.Items.Any())
        {
            return new EmptyCartException();
        }

        try
        {
            var orderId = OrderId.New();
            var orderItems = new List<OrderItem>();

            foreach (var cartItem in cart.Items)
            {
                var productOption = await productRepository.GetByIdAsync(cartItem.ProductId, cancellationToken);

                var product = await productOption.Match(
                    p => Task.FromResult(p),
                    () => throw new InvalidOperationException($"Product {cartItem.ProductId} not found"));

                if (product.StockQuantity < cartItem.Quantity)
                {
                    return new InsufficientStockForOrderException(
                        product.Id.Value,
                        cartItem.Quantity,
                        product.StockQuantity);
                }

                product.DecreaseStock(cartItem.Quantity);
                await productRepository.UpdateAsync(product, cancellationToken);

                var orderItem = OrderItem.New(orderId, product.Id, cartItem.Quantity, product.Price);
                orderItems.Add(orderItem);
            }

            var order = Order.New(userId, request.ShippingAddress, request.Notes, orderItems);
            var createdOrder = await orderRepository.AddAsync(order, cancellationToken);

            cart.Clear();
            await cartRepository.UpdateAsync(cart, cancellationToken);

            return createdOrder;
        }
        catch (Exception exception)
        {
            return new UnhandledOrderException(OrderId.Empty(), exception);
        }
    }
}