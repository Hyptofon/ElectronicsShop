using Domain.Products;

namespace Api.Dtos;

public record ProductReviewDto(
    Guid Id,
    Guid ProductId,
    Guid UserId,
    int Rating,
    string Comment,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    bool IsModerated)
{
    public static ProductReviewDto FromDomainModel(ProductReview review)
        => new(
            review.Id.Value,
            review.ProductId.Value,
            review.UserId,
            review.Rating,
            review.Comment,
            review.CreatedAt,
            review.UpdatedAt,
            review.IsModerated);
}

public record CreateProductReviewDto(int Rating, string Comment);

public record UpdateProductReviewDto(int Rating, string Comment);