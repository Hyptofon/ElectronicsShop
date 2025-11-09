using FluentValidation;

namespace Application.ProductReviews.Commands;

public class CreateProductReviewCommandValidator : AbstractValidator<CreateProductReviewCommand>
{
    public CreateProductReviewCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("Rating must be between 1 and 5");
        
        RuleFor(x => x.Comment)
            .NotEmpty()
            .MaximumLength(2000);
    }
}