using Domain.Products;

namespace Application.ProductReviews.Exceptions;

public abstract class ProductReviewException(ProductReviewId reviewId, string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    public ProductReviewId ReviewId { get; } = reviewId;
}

public class ProductReviewNotFoundException(ProductReviewId reviewId)
    : ProductReviewException(reviewId, $"Product review not found under id {reviewId}");

public class ProductNotFoundForReviewException(ProductId productId)
    : ProductReviewException(ProductReviewId.Empty(), $"Product with id {productId} not found for review operation");

public class UnauthorizedReviewAccessException(ProductReviewId reviewId)
    : ProductReviewException(reviewId, "User is not authorized to modify this review");

public class UnhandledProductReviewException(ProductReviewId reviewId, Exception? innerException)
    : ProductReviewException(reviewId, "Unexpected error occurred while processing product review", innerException);