using FluentValidation;

namespace Application.ProductReviews.Commands;

public class ModerateProductReviewCommandValidator : AbstractValidator<ModerateProductReviewCommand>
{
    public ModerateProductReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
    }
}