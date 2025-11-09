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
    ICurrentUserService currentUserService)
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

            cartItem.UpdateQuantity(quantity);
            return await cartRepository.UpdateAsync(cart, cancellationToken);
        }
        catch (Exception exception)
        {
            return new UnhandledCartException(cart.Id, exception);
        }
    }
}