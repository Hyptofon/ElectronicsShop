using Application.Authentication.Exceptions;
using Application.Authentication.Interfaces;
using Application.Authentication.Models;
using Domain.Users;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Application.Authentication.Commands;

public record LoginCommand : IRequest<Either<AuthenticationException, AuthenticationResult>>
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}

public class LoginCommandHandler(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IJwtTokenGenerator jwtTokenGenerator)
    : IRequestHandler<LoginCommand, Either<AuthenticationException, AuthenticationResult>>
{
    public async Task<Either<AuthenticationException, AuthenticationResult>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        if (user == null)
        {
            return new InvalidCredentialsException();
        }

        if (user.IsBlocked)
        {
            return new UserBlockedException(request.Email);
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, false);

        if (!result.Succeeded)
        {
            return new InvalidCredentialsException();
        }

        try
        {
            var roles = await userManager.GetRolesAsync(user);
            var token = jwtTokenGenerator.GenerateToken(user, roles.ToList());
            var refreshToken = jwtTokenGenerator.GenerateRefreshToken();
            user.SetRefreshToken(refreshToken, DateTime.UtcNow.AddDays(7)); 
            await userManager.UpdateAsync(user); 
            return new AuthenticationResult
            {
                Token = token,
                RefreshToken = refreshToken,
                UserId = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles.ToList()
            };
        }
        catch (Exception exception)
        {
            return new UnhandledAuthenticationException(exception);
        }
    }
}