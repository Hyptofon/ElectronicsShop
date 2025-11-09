using Api.Dtos;
using Domain.Products;

namespace Tests.Data.Reviews;

public static class ReviewData
{
    public static ProductReview CreateTestReview(ProductId productId, Guid userId) =>
        ProductReview.New(
            productId,
            userId,
            5,
            "Test excellent product! Highly recommended."
        );

    public static CreateProductReviewDto CreateTestReviewDto() =>
        new(4, "Test great product, works as expected");

    public static UpdateProductReviewDto UpdateTestReviewDto() =>
        new(3, "Updated review: product is decent but has some issues");
}