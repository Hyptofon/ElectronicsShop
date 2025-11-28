using Application.Users.Exceptions;
using Domain.Users;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Application.Users.Commands;

public record BlockUserCommand(Guid UserId) : IRequest<Either<UserException, ApplicationUser>>;

public class BlockUserCommandHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<BlockUserCommand, Either<UserException, ApplicationUser>>
{
    public async Task<Either<UserException, ApplicationUser>> Handle(
        BlockUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(request.UserId.ToString());

        if (user == null)
        {
            return new UserNotFoundException(request.UserId.ToString());
        }

        try
        {
            user.Block();
            var result = await userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new UserBlockFailedException(errors);
            }

            return user;
        }
        catch (Exception exception)
        {
            return new UnhandledUserException(exception);
        }
    }
}