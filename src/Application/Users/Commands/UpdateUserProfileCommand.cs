using Application.Common.Interfaces;
using Application.Users.Exceptions;
using Domain.Users;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Application.Users.Commands;

public record UpdateUserProfileCommand : IRequest<Either<UserException, ApplicationUser>>
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
}

public class UpdateUserProfileCommandHandler(
    UserManager<ApplicationUser> userManager,
    ICurrentUserService currentUserService)
    : IRequestHandler<UpdateUserProfileCommand, Either<UserException, ApplicationUser>>
{
    public async Task<Either<UserException, ApplicationUser>> Handle(
        UpdateUserProfileCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated || !currentUserService.UserId.HasValue)
        {
            return new UnauthorizedUserAccessException();
        }

        var user = await userManager.FindByIdAsync(currentUserService.UserId.Value.ToString());

        if (user == null)
        {
            return new UserNotFoundException(currentUserService.UserId.Value.ToString());
        }
        
        if (user.IsBlocked)
        {
            return new UserBlockedException(user.Email!);
        }

        try
        {
            user.UpdateProfile(request.FirstName, request.LastName);
            var result = await userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new UserUpdateFailedException(errors);
            }

            return user;
        }
        catch (Exception exception)
        {
            return new UnhandledUserException(exception);
        }
    }
}