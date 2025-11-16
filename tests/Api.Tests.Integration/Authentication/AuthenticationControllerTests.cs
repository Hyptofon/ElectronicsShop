using System.Net;
using System.Net.Http.Json;
using Api.Dtos;
using Domain.Users;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tests.Common;
using Tests.Data.Authentication; 

namespace Api.Tests.Integration.Authentication;

public class AuthenticationControllerTests : BaseIntegrationTest
{
    private const string BaseRoute = "auth";
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthenticationControllerTests(IntegrationTestWebFactory factory) : base(factory)
    {
        var scope = factory.Services.CreateScope();
        _userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    }

    #region Register Tests

    [Fact]
    public async Task Register_WithValidData_ShouldCreateUserAndReturnToken()
    {
        // Arrange
        var request = AuthData.CreateRegisterDto(email: "unique_register@test.com");

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResponse = await response.ToResponseModel<AuthenticationResponseDto>();
        
        authResponse.Token.Should().NotBeNullOrEmpty();
        authResponse.RefreshToken.Should().NotBeNullOrEmpty();
        authResponse.Email.Should().Be(request.Email);
        authResponse.FirstName.Should().Be(request.FirstName);
        authResponse.LastName.Should().Be(request.LastName);
        authResponse.Roles.Should().Contain(ApplicationRole.User);

        // Перевірка в БД
        var user = await _userManager.FindByEmailAsync(request.Email);
        user.Should().NotBeNull();
        user!.Email.Should().Be(request.Email);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ShouldReturnConflict()
    {
        // Arrange
        var existingEmail = "existing@test.com";
        var existingUser = ApplicationUser.Create(existingEmail, "Existing", "User", "existing");
        await _userManager.CreateAsync(existingUser, AuthData.DefaultPassword);

        var request = AuthData.CreateRegisterDto(
            email: existingEmail,
            firstName: "Another"
        );

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("", "Test@123", "First", "Last")] // Empty email
    [InlineData("invalid-email", "Test@123", "First", "Last")] // Invalid email format
    [InlineData("test@test.com", "weak", "First", "Last")] // Weak password
    [InlineData("test@test.com", "Test@123", "", "Last")] // Empty first name
    [InlineData("test@test.com", "Test@123", "First", "")] // Empty last name
    public async Task Register_WithInvalidData_ShouldReturnBadRequest(
        string email, string password, string firstName, string lastName)
    {
        // Arrange
        var request = AuthData.CreateRegisterDto(email, password, firstName, lastName);

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Login Tests

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnToken()
    {
        // Arrange
        var email = "login@test.com";
        var user = ApplicationUser.Create(email, "Login", "User", "loginuser");
        await _userManager.CreateAsync(user, AuthData.DefaultPassword);
        await _userManager.AddToRoleAsync(user, ApplicationRole.User);

        var request = AuthData.CreateLoginDto(email, AuthData.DefaultPassword);

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResponse = await response.ToResponseModel<AuthenticationResponseDto>();
        
        authResponse.Token.Should().NotBeNullOrEmpty();
        authResponse.RefreshToken.Should().NotBeNullOrEmpty();
        authResponse.Email.Should().Be(email);
        authResponse.Roles.Should().Contain(ApplicationRole.User);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturnUnauthorized()
    {
        // Arrange
        var email = "user_wrong_pass@test.com";
        var user = ApplicationUser.Create(email, "Test", "User", "testuser_wrongpass");
        await _userManager.CreateAsync(user, AuthData.DefaultPassword);

        var request = AuthData.CreateLoginDto(email, "WrongPassword@123");

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = AuthData.CreateLoginDto("nonexistent@test.com");

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithBlockedUser_ShouldReturnForbidden()
    {
        // Arrange
        var email = "blocked@test.com";
        var user = ApplicationUser.Create(email, "Blocked", "User", "blockeduser");
        await _userManager.CreateAsync(user, AuthData.DefaultPassword);
        user.Block();
        await _userManager.UpdateAsync(user);

        var request = AuthData.CreateLoginDto(email, AuthData.DefaultPassword);

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("", "Test@123")] // Empty email
    [InlineData("test@test.com", "")] // Empty password
    public async Task Login_WithInvalidData_ShouldReturnBadRequest(string email, string password)
    {
        // Arrange
        var request = AuthData.CreateLoginDto(email, password);

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
    
    #region Refresh Token Tests

    [Fact]
    public async Task RefreshToken_WithValidTokens_ShouldReturnNewTokens()
    {
        // Arrange
        var email = "refresh_valid@test.com";
        var user = ApplicationUser.Create(email, "Refresh", "User", "refreshvaliduser");
        await _userManager.CreateAsync(user, AuthData.DefaultPassword);
        await _userManager.AddToRoleAsync(user, ApplicationRole.User);

        // 1. Логін (API запише токен в БД)
        var loginRequest = AuthData.CreateLoginDto(email, AuthData.DefaultPassword);
        var loginResponse = await Client.PostAsJsonAsync($"{BaseRoute}/login", loginRequest);
        var originalAuth = await loginResponse.ToResponseModel<AuthenticationResponseDto>();

        // Act
        var refreshRequest = AuthData.CreateRefreshTokenDto(originalAuth.Token, originalAuth.RefreshToken);
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var newAuth = await response.ToResponseModel<AuthenticationResponseDto>();

        newAuth.Token.Should().NotBe(originalAuth.Token);
        newAuth.RefreshToken.Should().NotBe(originalAuth.RefreshToken);
        
        // Перевірка в БД
        Context.ChangeTracker.Clear();
        var dbUser = await Context.Users.FirstAsync(u => u.Email == email);
        dbUser.RefreshToken.Should().Be(newAuth.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_WithInvalidRefreshToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = AuthData.CreateRefreshTokenDto(refreshToken: "non-existent-refresh-token");

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/refresh", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    [Fact]
    public async Task RefreshToken_WithRevokedToken_ShouldReturnUnauthorized()
    {
        // Arrange
        var email = "refresh_revoked@test.com";
        var user = ApplicationUser.Create(email, "Refresh", "User", "refreshrevokeduser");
        await _userManager.CreateAsync(user, AuthData.DefaultPassword);
        await _userManager.AddToRoleAsync(user, ApplicationRole.User);
        
        // Логін
        var loginRequest = AuthData.CreateLoginDto(email, AuthData.DefaultPassword);
        var loginResponse = await Client.PostAsJsonAsync($"{BaseRoute}/login", loginRequest);
        var originalAuth = await loginResponse.ToResponseModel<AuthenticationResponseDto>();
        
        // 2. Симулюємо відкликання
        Context.ChangeTracker.Clear();
        var dbUser = await Context.Users.FirstAsync(u => u.Email == email);
        dbUser.RevokeRefreshToken();
        await Context.SaveChangesAsync();

        // Act
        var refreshRequest = AuthData.CreateRefreshTokenDto(originalAuth.Token, originalAuth.RefreshToken);
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        
        Context.ChangeTracker.Clear();
        var dbUserAfter = await Context.Users.FirstAsync(u => u.Email == email);
        dbUserAfter.RefreshToken.Should().BeNull();
    }
    
    [Fact]
    public async Task RefreshToken_WithBlockedUser_ShouldReturnForbidden()
    {
        // Arrange
        var email = "refresh_blocked@test.com";
        var user = ApplicationUser.Create(email, "Refresh", "User", "refreshblockeduser");
        await _userManager.CreateAsync(user, AuthData.DefaultPassword);
        await _userManager.AddToRoleAsync(user, ApplicationRole.User);
        
        var loginRequest = AuthData.CreateLoginDto(email, AuthData.DefaultPassword);
        var loginResponse = await Client.PostAsJsonAsync($"{BaseRoute}/login", loginRequest);
        var originalAuth = await loginResponse.ToResponseModel<AuthenticationResponseDto>();
        
        // 2. Блокуємо користувача
        Context.ChangeTracker.Clear();
        var dbUser = await Context.Users.FirstAsync(u => u.Email == email);
        dbUser.Block();
        await Context.SaveChangesAsync();
        
        // Act
        var refreshRequest = AuthData.CreateRefreshTokenDto(originalAuth.Token, originalAuth.RefreshToken);
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
    
    [Theory]
    [InlineData("", "valid_refresh")] 
    [InlineData("valid_access", "")] 
    public async Task RefreshToken_WithInvalidData_ShouldReturnBadRequest(string token, string refreshToken)
    {
        // Arrange
        var request = AuthData.CreateRefreshTokenDto(token, refreshToken);

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/refresh", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}