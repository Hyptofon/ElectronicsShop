using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.Carts.Exceptions;
using Domain.Cart;
using LanguageExt;
using MediatR;

namespace Application.Carts.Commands;

public record RemoveFromCartCommand(Guid CartItemId)
    : IRequest<Either<CartException, Cart>>;

public class RemoveFromCartCommandHandler(
    ICartRepository cartRepository,
    IProductRepository productRepository,
    ICurrentUserService currentUserService,
    IApplicationDbContext dbContext)
    : IRequestHandler<RemoveFromCartCommand, Either<CartException, Cart>>
{
    public async Task<Either<CartException, Cart>> Handle(
        RemoveFromCartCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || !currentUserService.UserId.HasValue)
        {
            return new UnauthorizedCartAccessException(CartId.Empty());
        }

        var userId = currentUserService.UserId.Value;
        var cartOption = await cartRepository.GetByUserIdAsync(userId, cancellationToken);

        return await cartOption.MatchAsync(
            cart => RemoveItem(cart, request.CartItemId, cancellationToken),
            () => Task.FromResult<Either<CartException, Cart>>(
                new CartNotFoundException(CartId.Empty())));
    }

    private async Task<Either<CartException, Cart>> RemoveItem(
        Cart cart,
        Guid cartItemId,
        CancellationToken cancellationToken)
    {
        try
        {
            var cartItem = cart.Items.FirstOrDefault(i => i.Id.Value == cartItemId);

            if (cartItem != null)
            {
                var productOption = await productRepository.GetByIdAsync(cartItem.ProductId, cancellationToken);
                productOption.IfSome(product =>
                {
                    product.IncreaseStock(cartItem.Quantity);
                    productRepository.Update(product);
                });
            }

            cart.RemoveItem(cartItemId);
            cartRepository.Update(cart);
            
            await dbContext.SaveChangesAsync(cancellationToken);
            
            return cart;
        }
        catch (Exception exception)
        {
            return new UnhandledCartException(cart.Id, exception);
        }
    }
}