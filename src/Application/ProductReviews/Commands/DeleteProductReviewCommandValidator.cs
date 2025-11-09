using FluentValidation;

namespace Application.ProductReviews.Commands;

public class DeleteProductReviewCommandValidator : AbstractValidator<DeleteProductReviewCommand>
{
    public DeleteProductReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
    }
}