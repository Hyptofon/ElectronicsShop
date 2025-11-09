namespace Domain.Products;

public class ProductReview
{
    public ProductReviewId Id { get; }
    public ProductId ProductId { get; }
    public Guid UserId { get; }
    public int Rating { get; private set; }
    public string Comment { get; private set; }
    public DateTime CreatedAt { get; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsModerated { get; private set; }

    public Product? Product { get; private set; }

    private ProductReview(ProductReviewId id, ProductId productId, Guid userId, 
        int rating, string comment, DateTime createdAt, DateTime? updatedAt, bool isModerated)
    {
        Id = id;
        ProductId = productId;
        UserId = userId;
        Rating = rating;
        Comment = comment;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        IsModerated = isModerated;
    }

    public static ProductReview New(ProductId productId, Guid userId, int rating, string comment)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5");

        return new(ProductReviewId.New(), productId, userId, rating, comment, 
            DateTime.UtcNow, null, false);
    }

    public void UpdateReview(int rating, string comment)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5");

        Rating = rating;
        Comment = comment;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Moderate()
    {
        IsModerated = true;
        UpdatedAt = DateTime.UtcNow;
    }
}