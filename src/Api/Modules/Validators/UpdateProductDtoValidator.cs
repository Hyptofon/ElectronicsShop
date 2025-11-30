using Api.Dtos;
using FluentValidation;

namespace Api.Modules.Validators;

public class UpdateProductDtoValidator : AbstractValidator<UpdateProductDto>
{
    public UpdateProductDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Product name is required")
            .MinimumLength(3)
            .WithMessage("Product name must be at least 3 characters long")
            .MaximumLength(255)
            .WithMessage("Product name must not exceed 255 characters");

        RuleFor(x => x.Description)
            .NotEmpty()
            .WithMessage("Description is required")
            .MinimumLength(10)
            .WithMessage("Description must be at least 10 characters long")
            .MaximumLength(2000)
            .WithMessage("Description must not exceed 2000 characters");

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Price must be greater than 0")
            .LessThanOrEqualTo(1000000)
            .WithMessage("Price must not exceed 1,000,000");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Stock quantity cannot be negative")
            .LessThanOrEqualTo(100000)
            .WithMessage("Stock quantity must not exceed 100,000");

        RuleFor(x => x.Brand)
            .MaximumLength(100)
            .WithMessage("Brand must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Brand));

        RuleFor(x => x.Model)
            .MaximumLength(100)
            .WithMessage("Model must not exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Model));

        RuleFor(x => x.Categories)
            .NotEmpty()
            .WithMessage("At least one category is required")
            .Must(x => x.Count > 0)
            .WithMessage("At least one category must be provided");
    }
}