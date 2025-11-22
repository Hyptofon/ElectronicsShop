namespace Domain.Orders;

public class Order
{
    public OrderId Id { get; }
    public Guid UserId { get; }
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string ShippingAddress { get; private set; }
    public string? Notes { get; private set; }
    
    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }

    public ICollection<OrderItem> Items { get; private set; } = [];

    private Order(OrderId id, Guid userId, OrderStatus status, decimal totalAmount, 
        string shippingAddress, string? notes, DateTime createdAt, DateTime? updatedAt)
    {
        Id = id;
        UserId = userId;
        Status = status;
        TotalAmount = totalAmount;
        ShippingAddress = shippingAddress;
        Notes = notes;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public static Order New(OrderId id, Guid userId, string shippingAddress, string? notes, 
        ICollection<OrderItem> items)
    {
        var totalAmount = items.Sum(x => x.Quantity * x.UnitPrice);
        
        var order = new Order(id, userId, OrderStatus.Pending, totalAmount, 
            shippingAddress, notes, DateTime.UtcNow, null)
        {
            Items = items
        };
        return order;
    }

    public void UpdateStatus(OrderStatus newStatus)
    {
        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == OrderStatus.Delivered)
            throw new InvalidOperationException("Cannot cancel delivered order");

        Status = OrderStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }
}