using Api.Dtos;

namespace Tests.Data.Authentication;

public static class AuthData
{
    public const string DefaultPassword = "Test@123";

    public static RegisterDto CreateRegisterDto(
        string email = "newuser@test.com",
        string password = DefaultPassword,
        string firstName = "New",
        string lastName = "User") 
        => new(email, password, firstName, lastName);

    public static LoginDto CreateLoginDto(
        string email = "login@test.com",
        string password = DefaultPassword) 
        => new(email, password);

    public static RefreshTokenDto CreateRefreshTokenDto(
        string token = "valid_access_token",
        string refreshToken = "valid_refresh_token") 
        => new(token, refreshToken);
}