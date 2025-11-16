// src/Application/Authentication/Commands/RefreshTokenCommand.cs
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
        // 1. Шукаємо користувача, у якого цей RefreshToken записаний в БД
        var user = await userManager.Users
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken, cancellationToken);

        // Якщо користувача немає, або токен не співпадає (зайва перевірка, але безпечна)
        if (user == null || user.RefreshToken != request.RefreshToken)
        {
            return new InvalidCredentialsException();
        }

        // 2. Перевірка на блокування
        if (user.IsBlocked)
        {
            return new UserBlockedException(user.Email!);
        }

        // 3. Перевірка терміну дії
        if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            // Токен прострочений - відкликаємо його
            user.RevokeRefreshToken();
            await userManager.UpdateAsync(user);
            return new InvalidCredentialsException();
        }

        try
        {
            // 4. Генеруємо нову пару
            var roles = await userManager.GetRolesAsync(user);
            var newAccessToken = jwtTokenGenerator.GenerateToken(user, roles.ToList());
            var newRefreshToken = jwtTokenGenerator.GenerateRefreshToken();
            
            // 5. Оновлюємо БД: старий стираємо, новий записуємо
            user.SetRefreshToken(newRefreshToken, DateTime.UtcNow.AddDays(7)); 
            await userManager.UpdateAsync(user);

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
        catch (Exception exception)
        {
            return new UnhandledAuthenticationException(exception);
        }
    }
}