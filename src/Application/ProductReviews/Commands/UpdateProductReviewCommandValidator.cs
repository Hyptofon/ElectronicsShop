using FluentValidation;

namespace Application.ProductReviews.Commands;

public class UpdateProductReviewCommandValidator : AbstractValidator<UpdateProductReviewCommand>
{
    public UpdateProductReviewCommandValidator()
    {
        RuleFor(x => x.ReviewId).NotEmpty();
        
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("Rating must be between 1 and 5");
        
        RuleFor(x => x.Comment)
            .NotEmpty()
            .MaximumLength(2000);
    }
}