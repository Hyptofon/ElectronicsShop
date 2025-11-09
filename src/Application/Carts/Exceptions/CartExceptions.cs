using Domain.Cart;

namespace Application.Carts.Exceptions;

public abstract class CartException(CartId cartId, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public CartId CartId { get; } = cartId;
}

public class CartNotFoundException(CartId cartId)
    : CartException(cartId, $"Cart not found under id {cartId}");

public class CartItemNotFoundException(CartId cartId)
    : CartException(cartId, "Cart item not found");

public class ProductNotFoundForCartException(Guid productId)
    : CartException(CartId.Empty(), $"Product with id {productId} not found for cart operation");

public class InsufficientStockForCartException(Guid productId, int requested, int available)
    : CartException(CartId.Empty(), 
        $"Insufficient stock for product {productId}. Requested: {requested}, Available: {available}");

public class UnauthorizedCartAccessException(CartId cartId)
    : CartException(cartId, "User is not authorized to access this cart");

public class UnhandledCartException(CartId cartId, Exception? innerException)
    : CartException(cartId, "Unexpected error occurred while processing cart", innerException);