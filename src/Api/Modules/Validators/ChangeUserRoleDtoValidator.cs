using Api.Dtos;
using FluentValidation;

namespace Api.Modules.Validators;

public class ChangeUserRoleDtoValidator : AbstractValidator<ChangeUserRoleDto>
{
    public ChangeUserRoleDtoValidator()
    {
        RuleFor(x => x.RoleName)
            .NotEmpty()
            .WithMessage("Role name is required")
            .Must(BeValidRole)
            .WithMessage("Role must be one of: User, Manager, Admin");
    }

    private bool BeValidRole(string roleName)
    {
        var validRoles = new[] { "User", "Manager", "Admin" };
        return validRoles.Contains(roleName, StringComparer.OrdinalIgnoreCase);
    }
}