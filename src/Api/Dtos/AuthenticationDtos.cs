using Application.Authentication.Models;

namespace Api.Dtos;

public record RegisterDto(string Email, string Password, string FirstName, string LastName);

public record LoginDto(string Email, string Password);
public record RefreshTokenDto(string Token, string RefreshToken);

public record AuthenticationResponseDto(
    string Token,
    string RefreshToken,
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyList<string> Roles)
{
    public static AuthenticationResponseDto FromResult(AuthenticationResult result)
        => new(
            result.Token,
            result.RefreshToken,
            result.UserId,
            result.Email,
            result.FirstName,
            result.LastName,
            result.Roles);
}