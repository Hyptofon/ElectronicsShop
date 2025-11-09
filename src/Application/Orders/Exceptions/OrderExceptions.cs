using Domain.Orders;

namespace Application.Orders.Exceptions;

public abstract class OrderException(OrderId orderId, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public OrderId OrderId { get; } = orderId;
}

public class OrderNotFoundException(OrderId orderId)
    : OrderException(orderId, $"Order not found under id {orderId}");

public class EmptyCartException()
    : OrderException(OrderId.Empty(), "Cannot create order from empty cart");

public class InsufficientStockForOrderException(Guid productId, int requested, int available)
    : OrderException(OrderId.Empty(),
        $"Insufficient stock for product {productId}. Requested: {requested}, Available: {available}");

public class UnauthorizedOrderAccessException(OrderId orderId)
    : OrderException(orderId, "User is not authorized to access this order");

public class InvalidOrderStatusTransitionException(OrderId orderId, OrderStatus currentStatus, OrderStatus newStatus)
    : OrderException(orderId, $"Cannot transition order from {currentStatus} to {newStatus}");

public class UnhandledOrderException(OrderId orderId, Exception? innerException)
    : OrderException(orderId, "Unexpected error occurred while processing order", innerException);