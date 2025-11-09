namespace Domain.Orders;

public record OrderItemId(Guid Value)
{
    public static OrderItemId Empty() => new(Guid.Empty);
    public static OrderItemId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}