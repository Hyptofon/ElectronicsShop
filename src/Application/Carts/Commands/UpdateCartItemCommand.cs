using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.Carts.Exceptions;
using Domain.Cart;
using LanguageExt;
using MediatR;

namespace Application.Carts.Commands;

public record UpdateCartItemCommand(Guid CartItemId, int Quantity)
    : IRequest<Either<CartException, Cart>>;

public class UpdateCartItemCommandHandler(
    ICartRepository cartRepository,
    IProductRepository productRepository,
    ICurrentUserService currentUserService,
    IApplicationDbContext dbContext)
    : IRequestHandler<UpdateCartItemCommand, Either<CartException, Cart>>
{
    public async Task<Either<CartException, Cart>> Handle(
        UpdateCartItemCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || !currentUserService.UserId.HasValue)
        {
            return new UnauthorizedCartAccessException(CartId.Empty());
        }

        var userId = currentUserService.UserId.Value;
        var cartOption = await cartRepository.GetByUserIdAsync(userId, cancellationToken);

        return await cartOption.MatchAsync(
            cart => UpdateItem(cart, request.CartItemId, request.Quantity, cancellationToken),
            () => Task.FromResult<Either<CartException, Cart>>(
                new CartNotFoundException(CartId.Empty())));
    }

    private async Task<Either<CartException, Cart>> UpdateItem(
        Cart cart,
        Guid cartItemId,
        int quantity,
        CancellationToken cancellationToken)
    {
        try
        {
            var cartItem = cart.Items.FirstOrDefault(x => x.Id.Value == cartItemId);
            
            if (cartItem == null)
            {
                return new CartItemNotFoundException(cart.Id);
            }
            var productOption = await productRepository.GetByIdAsync(
                cartItem.ProductId, 
                cancellationToken);

            return await productOption.MatchAsync(
                async product =>
                {
                    if (product.StockQuantity < quantity)
                    {
                        return new InsufficientStockForCartException(
                            product.Id.Value,
                            quantity,
                            product.StockQuantity);
                    }

                    cartItem.UpdateQuantity(quantity);
                    cartRepository.Update(cart);
                    
                    await dbContext.SaveChangesAsync(cancellationToken);
                    
                    return cart;
                },
                () => Task.FromResult<Either<CartException, Cart>>(
                    new ProductNotFoundForCartException(cartItem.ProductId.Value)));
        }
        catch (Exception exception)
        {
            return new UnhandledCartException(cart.Id, exception);
        }
    }
}