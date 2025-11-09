using Application.Common.Interfaces;
using Application.Common.Interfaces.Repositories;
using Application.Carts.Exceptions;
using Domain.Cart;
using Domain.Products;
using LanguageExt;
using MediatR;

namespace Application.Carts.Commands;

public record AddToCartCommand(Guid ProductId, int Quantity)
    : IRequest<Either<CartException, Cart>>;

public class AddToCartCommandHandler(
    ICartRepository cartRepository,
    IProductRepository productRepository,
    ICurrentUserService currentUserService)
    : IRequestHandler<AddToCartCommand, Either<CartException, Cart>>
{
    public async Task<Either<CartException, Cart>> Handle(
        AddToCartCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || !currentUserService.UserId.HasValue)
        {
            return new UnauthorizedCartAccessException(CartId.Empty());
        }

        var userId = currentUserService.UserId.Value;
        var productId = new ProductId(request.ProductId);

        var product = await productRepository.GetByIdAsync(productId, cancellationToken);

        return await product.MatchAsync(
            p => AddProductToCart(p, userId, request.Quantity, cancellationToken),
            () => Task.FromResult<Either<CartException, Cart>>(
                new ProductNotFoundForCartException(request.ProductId)));
    }

    private async Task<Either<CartException, Cart>> AddProductToCart(
        Product product,
        Guid userId,
        int quantity,
        CancellationToken cancellationToken)
    {
        try
        {
            if (product.StockQuantity < quantity)
            {
                return new InsufficientStockForCartException(
                    product.Id.Value,
                    quantity,
                    product.StockQuantity);
            }

            var cartOption = await cartRepository.GetByUserIdAsync(userId, cancellationToken);

            var cart = await cartOption.MatchAsync(
                existingCart => Task.FromResult(existingCart),
                async () => await cartRepository.AddAsync(Cart.New(userId), cancellationToken));

            var cartItem = CartItem.New(cart.Id, product.Id, quantity);
            cart.AddItem(cartItem);

            return await cartRepository.UpdateAsync(cart, cancellationToken);
        }
        catch (Exception exception)
        {
            return new UnhandledCartException(CartId.Empty(), exception);
        }
    }
}