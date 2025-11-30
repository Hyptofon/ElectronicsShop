using Api.Dtos;
using FluentValidation;

namespace Api.Modules.Validators;

public class CreateProductReviewDtoValidator : AbstractValidator<CreateProductReviewDto>
{
    public CreateProductReviewDtoValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("Rating must be between 1 and 5");

        RuleFor(x => x.Comment)
            .NotEmpty()
            .WithMessage("Comment is required")
            .MinimumLength(10)
            .WithMessage("Comment must be at least 10 characters long")
            .MaximumLength(2000)
            .WithMessage("Comment must not exceed 2000 characters");
    }
}