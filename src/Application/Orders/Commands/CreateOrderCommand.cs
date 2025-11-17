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
    IApplicationDbContext dbContext,
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
        
        using var transaction = await dbContext.BeginTransactionAsync(cancellationToken)
            as IDbTransactionWrapper
            ?? throw new InvalidOperationException("Transaction is not IDbTransactionWrapper");

        try
        {
            var orderId = OrderId.New();
            var orderItems = new List<OrderItem>();

            foreach (var cartItem in cart.Items)
            {
                var productOption = await productRepository.GetByIdAsync(
                    cartItem.ProductId, 
                    cancellationToken);
                
                if (productOption.IsNone)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new ProductNotFoundForOrderException(cartItem.ProductId.Value);
                }

                var product = productOption.Match(
                    p => p,
                    () => throw new InvalidOperationException("This should never happen"));

                if (product.StockQuantity < cartItem.Quantity)
                {
                    await transaction.RollbackAsync(cancellationToken);
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
            
            await transaction.CommitAsync(cancellationToken);

            return createdOrder;
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return new UnhandledOrderException(OrderId.Empty(), exception);
        }
    }
}