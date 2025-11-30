using Api.Dtos;
using FluentValidation;

namespace Api.Modules.Validators;

public class UpdateCategoryDtoValidator : AbstractValidator<UpdateCategoryDto>
{
    public UpdateCategoryDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Category name is required")
            .MinimumLength(3)
            .WithMessage("Category name must be at least 3 characters long")
            .MaximumLength(255)
            .WithMessage("Category name must not exceed 255 characters");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Description must not exceed 1000 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}