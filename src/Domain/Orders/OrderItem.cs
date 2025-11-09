using Domain.Products;

namespace Domain.Orders;

public class OrderItem
{
    public OrderItemId Id { get; }
    public OrderId OrderId { get; }
    public ProductId ProductId { get; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public Order? Order { get; private set; }

    private OrderItem(OrderItemId id, OrderId orderId, ProductId productId, 
        int quantity, decimal unitPrice)
    {
        Id = id;
        OrderId = orderId;
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public static OrderItem New(OrderId orderId, ProductId productId, int quantity, decimal unitPrice)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0");
        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative");

        return new(OrderItemId.New(), orderId, productId, quantity, unitPrice);
    }
}