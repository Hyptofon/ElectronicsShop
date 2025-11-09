namespace Domain.Orders;

public record OrderId(Guid Value)
{
    public static OrderId Empty() => new(Guid.Empty);
    public static OrderId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}