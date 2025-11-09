using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.Carts.Exceptions;
using Domain.Cart;
using LanguageExt;
using MediatR;

namespace Application.Carts.Commands;

public record ClearCartCommand : IRequest<Either<CartException, Cart>>;

public class ClearCartCommandHandler(
    ICartRepository cartRepository,
    ICurrentUserService currentUserService)
    : IRequestHandler<ClearCartCommand, Either<CartException, Cart>>
{
    public async Task<Either<CartException, Cart>> Handle(
        ClearCartCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || !currentUserService.UserId.HasValue)
        {
            return new UnauthorizedCartAccessException(CartId.Empty());
        }

        var userId = currentUserService.UserId.Value;
        var cartOption = await cartRepository.GetByUserIdAsync(userId, cancellationToken);

        return await cartOption.MatchAsync(
            cart => ClearCartItems(cart, cancellationToken),
            () => Task.FromResult<Either<CartException, Cart>>(
                new CartNotFoundException(CartId.Empty())));
    }

    private async Task<Either<CartException, Cart>> ClearCartItems(
        Cart cart,
        CancellationToken cancellationToken)
    {
        try
        {
            cart.Clear();
            return await cartRepository.UpdateAsync(cart, cancellationToken);
        }
        catch (Exception exception)
        {
            return new UnhandledCartException(cart.Id, exception);
        }
    }
}