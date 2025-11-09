using Application.Users.Exceptions;
using Domain.Users;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Application.Users.Commands;

public record UnblockUserCommand(Guid UserId) : IRequest<Either<UserException, ApplicationUser>>;

public class UnblockUserCommandHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<UnblockUserCommand, Either<UserException, ApplicationUser>>
{
    public async Task<Either<UserException, ApplicationUser>> Handle(
        UnblockUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());

        if (user == null)
        {
            return new UserNotFoundException(request.UserId.ToString());
        }

        try
        {
            user.Unblock();
            var result = await userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new UnhandledUserException(
                    new InvalidOperationException($"Failed to unblock user: {errors}"));
            }

            return user;
        }
        catch (Exception exception)
        {
            return new UnhandledUserException(exception);
        }
    }
}