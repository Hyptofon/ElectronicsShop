namespace Domain.Cart;

public record CartId(Guid Value)
{
    public static CartId Empty() => new(Guid.Empty);
    public static CartId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}