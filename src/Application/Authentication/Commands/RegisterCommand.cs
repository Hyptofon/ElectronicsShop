using Application.Authentication.Exceptions;
using Application.Authentication.Interfaces;
using Application.Authentication.Models;
using Domain.Users;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Application.Authentication.Commands;

public record RegisterCommand : IRequest<Either<AuthenticationException, AuthenticationResult>>
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
}

public class RegisterCommandHandler(
    UserManager<ApplicationUser> userManager,
    IJwtTokenGenerator jwtTokenGenerator)
    : IRequestHandler<RegisterCommand, Either<AuthenticationException, AuthenticationResult>>
{
    public async Task<Either<AuthenticationException, AuthenticationResult>> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        var existingUser = await userManager.FindByEmailAsync(request.Email);

        if (existingUser != null)
        {
            return new UserAlreadyExistsException(request.Email);
        }

        try
        {
            var user = ApplicationUser.Create(
                request.Email,
                request.FirstName,
                request.LastName,
                request.Email.Split('@')[0]);

            var result = await userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return new UnhandledAuthenticationException(
                    new InvalidOperationException($"Failed to create user: {errors}"));
            }

            await userManager.AddToRoleAsync(user, ApplicationRole.User);

            var roles = await userManager.GetRolesAsync(user);
            var token = jwtTokenGenerator.GenerateToken(user, roles.ToList());
            var refreshToken = jwtTokenGenerator.GenerateRefreshToken();

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