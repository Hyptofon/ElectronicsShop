namespace Domain.Cart;

public record CartItemId(Guid Value)
{
    public static CartItemId Empty() => new(Guid.Empty);
    public static CartItemId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}