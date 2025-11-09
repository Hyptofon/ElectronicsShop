using Application.Users.Exceptions;
using Domain.Users;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Application.Users.Commands;

public record ChangeUserRoleCommand(Guid UserId, string RoleName) 
    : IRequest<Either<UserException, ApplicationUser>>;

public class ChangeUserRoleCommandHandler(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager)
    : IRequestHandler<ChangeUserRoleCommand, Either<UserException, ApplicationUser>>
{
    public async Task<Either<UserException, ApplicationUser>> Handle(
        ChangeUserRoleCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());

        if (user == null)
        {
            return new UserNotFoundException(request.UserId.ToString());
        }

        var roleExists = await roleManager.RoleExistsAsync(request.RoleName);
        if (!roleExists)
        {
            return new InvalidRoleException(request.RoleName);
        }

        try
        {
            var currentRoles = await userManager.GetRolesAsync(user);
            await userManager.RemoveFromRolesAsync(user, currentRoles);

            var result = await userManager.AddToRoleAsync(user, request.RoleName);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new UnhandledUserException(
                    new InvalidOperationException($"Failed to change user role: {errors}"));
            }

            return user;
        }
        catch (Exception exception)
        {
            return new UnhandledUserException(exception);
        }
    }
}