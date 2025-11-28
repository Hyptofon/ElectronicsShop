using Application.Authentication.Exceptions;
using Application.Authentication.Interfaces;
using Application.Authentication.Models;
using Domain.Users;
using LanguageExt;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Authentication.Commands;

public record RefreshTokenCommand : IRequest<Either<AuthenticationException, AuthenticationResult>>
{
    public required string Token { get; init; }
    public required string RefreshToken { get; init; }
}

public class RefreshTokenCommandHandler(
    UserManager<ApplicationUser> userManager,
    IJwtTokenGenerator jwtTokenGenerator)
    : IRequestHandler<RefreshTokenCommand, Either<AuthenticationException, AuthenticationResult>>
{
    public async Task<Either<AuthenticationException, AuthenticationResult>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var userIdFromToken = jwtTokenGenerator.GetUserIdFromToken(request.Token);
        
        if (userIdFromToken == null)
        {
            return new InvalidCredentialsException();
        }
        
        var user = await userManager.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken, cancellationToken);
        
        if (user == null || user.RefreshToken != request.RefreshToken)
        {
            return new InvalidCredentialsException();
        }
        
        if (user.Id != userIdFromToken)
        {
            user.RevokeRefreshToken();
            await userManager.UpdateAsync(user);
            
            return new InvalidCredentialsException(); 
        }

        if (user.IsBlocked)
        {
            return new UserBlockedException(user.Email!);
        }
        
        if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            user.RevokeRefreshToken();
            
            try
            {
                await userManager.UpdateAsync(user);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Ігноруємо конфлікти при відкликанні старого токена
            }
            
            return new InvalidCredentialsException();
        }

        try
        {
            var roles = await userManager.GetRolesAsync(user);
            var newAccessToken = jwtTokenGenerator.GenerateToken(user, roles.ToList());
            var newRefreshToken = jwtTokenGenerator.GenerateRefreshToken();
            
            user.SetRefreshToken(newRefreshToken, DateTime.UtcNow.AddDays(7));
            var updateResult = await userManager.UpdateAsync(user);
            
            if (!updateResult.Succeeded)
            {
                return HandleUpdateError(updateResult); 
            }

            return new AuthenticationResult
            {
                Token = newAccessToken,
                RefreshToken = newRefreshToken,
                UserId = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = roles.ToList()
            };
        }
        catch (DbUpdateConcurrencyException)
        {
            return new InvalidCredentialsException();
        }
        catch (Exception exception)
        {
            return new UnhandledAuthenticationException(exception);
        }
    }
    private static AuthenticationException HandleUpdateError(IdentityResult result)
    {
        if (result.Errors.Any(e => e.Code.Contains("Concurrency")))
        {
            return new InvalidCredentialsException();
        }
        
        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
        return new UnhandledAuthenticationException(
            new InvalidOperationException($"Failed to refresh token: {errors}"));
    }
}