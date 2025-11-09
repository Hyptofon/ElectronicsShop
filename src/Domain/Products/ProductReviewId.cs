namespace Domain.Products;

public record ProductReviewId(Guid Value)
{
    public static ProductReviewId Empty() => new(Guid.Empty);
    public static ProductReviewId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}