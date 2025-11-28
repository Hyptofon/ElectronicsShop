using FluentValidation;

namespace Application.Authentication.Commands;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage("Access token is required.");

        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token is required.");
    }
}