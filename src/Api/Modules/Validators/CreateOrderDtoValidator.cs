using Api.Dtos;
using FluentValidation;

namespace Api.Modules.Validators;

public class CreateOrderDtoValidator : AbstractValidator<CreateOrderDto>
{
    public CreateOrderDtoValidator()
    {
        RuleFor(x => x.ShippingAddress)
            .NotEmpty()
            .WithMessage("Shipping address is required")
            .MinimumLength(10)
            .WithMessage("Shipping address must be at least 10 characters long")
            .MaximumLength(500)
            .WithMessage("Shipping address must not exceed 500 characters");

        RuleFor(x => x.Notes)
            .MaximumLength(1000)
            .WithMessage("Notes must not exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Notes));
    }
}