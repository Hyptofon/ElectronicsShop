using FluentValidation;

namespace Application.Carts.Commands;

public class RemoveFromCartCommandValidator : AbstractValidator<RemoveFromCartCommand>
{
    public RemoveFromCartCommandValidator()
    {
        RuleFor(x => x.CartItemId).NotEmpty();
    }
}