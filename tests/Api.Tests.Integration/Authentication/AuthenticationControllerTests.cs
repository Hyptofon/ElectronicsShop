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
        user.Email.Should().Be(request.Email);
        user.FirstName.Should().Be(request.FirstName);
        user.LastName.Should().Be(request.LastName);
        user.RefreshToken.Should().NotBeNullOrEmpty();
        user.RefreshTokenExpiryTime.Should().BeAfter(DateTime.UtcNow);
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
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("already exists");
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

    [Fact]
    public async Task Register_WithPasswordMissingUppercase_ShouldReturnBadRequest()
    {
        // Arrange
        var request = AuthData.CreateRegisterDto(
            email: "test@test.com",
            password: "test@123" 
        );

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithPasswordMissingSpecialCharacter_ShouldReturnBadRequest()
    {
        // Arrange
        var request = AuthData.CreateRegisterDto(
            email: "test@test.com",
            password: "Test1234" 
        );

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithTooLongEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var longEmail = new string('a', 250) + "@test.com"; // >256 символів
        var request = AuthData.CreateRegisterDto(email: longEmail);

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

        var request = AuthData.CreateLoginDto(email);

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var authResponse = await response.ToResponseModel<AuthenticationResponseDto>();
        
        authResponse.Token.Should().NotBeNullOrEmpty();
        authResponse.RefreshToken.Should().NotBeNullOrEmpty();
        authResponse.Email.Should().Be(email);
        authResponse.Roles.Should().Contain(ApplicationRole.User);
        authResponse.UserId.Should().Be(user.Id);

        // Перевірка що refresh token збережено в БД
        Context.ChangeTracker.Clear();
        var dbUser = await Context.Users.FirstAsync(u => u.Email == email);
        dbUser.RefreshToken.Should().Be(authResponse.RefreshToken);
        dbUser.RefreshTokenExpiryTime.Should().BeAfter(DateTime.UtcNow);
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
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid email or password");
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

        var request = AuthData.CreateLoginDto(email);

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("blocked");
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

    [Fact]
    public async Task Login_MultipleTimes_ShouldUpdateRefreshToken()
    {
        // Arrange
        var email = "login_multiple@test.com";
        var user = ApplicationUser.Create(email, "Multiple", "User", "multipleuser");
        await _userManager.CreateAsync(user, AuthData.DefaultPassword);
        await _userManager.AddToRoleAsync(user, ApplicationRole.User);

        var request = AuthData.CreateLoginDto(email);

        // Act - перший логін
        var firstResponse = await Client.PostAsJsonAsync($"{BaseRoute}/login", request);
        var firstAuth = await firstResponse.ToResponseModel<AuthenticationResponseDto>();

        await Task.Delay(100); // невелика затримка

        // Act - другий логін
        var secondResponse = await Client.PostAsJsonAsync($"{BaseRoute}/login", request);
        var secondAuth = await secondResponse.ToResponseModel<AuthenticationResponseDto>();

        // Assert
        firstAuth.RefreshToken.Should().NotBe(secondAuth.RefreshToken);
        firstAuth.Token.Should().NotBe(secondAuth.Token);

        // Перевірка що в БД останній токен
        Context.ChangeTracker.Clear();
        var dbUser = await Context.Users.FirstAsync(u => u.Email == email);
        dbUser.RefreshToken.Should().Be(secondAuth.RefreshToken);
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

        // Логін
        var loginRequest = AuthData.CreateLoginDto(email);
        var loginResponse = await Client.PostAsJsonAsync($"{BaseRoute}/login", loginRequest);
        var originalAuth = await loginResponse.ToResponseModel<AuthenticationResponseDto>();

        await Task.Delay(100); // щоб токени точно відрізнялись

        // Act
        var refreshRequest = AuthData.CreateRefreshTokenDto(originalAuth.Token, originalAuth.RefreshToken);
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var newAuth = await response.ToResponseModel<AuthenticationResponseDto>();

        newAuth.Token.Should().NotBe(originalAuth.Token);
        newAuth.RefreshToken.Should().NotBe(originalAuth.RefreshToken);
        newAuth.Email.Should().Be(email);
        newAuth.UserId.Should().Be(originalAuth.UserId);
        
        // Перевірка в БД
        Context.ChangeTracker.Clear();
        var dbUser = await Context.Users.FirstAsync(u => u.Email == email);
        dbUser.RefreshToken.Should().Be(newAuth.RefreshToken);
        dbUser.RefreshTokenExpiryTime.Should().BeAfter(DateTime.UtcNow);
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
        var loginRequest = AuthData.CreateLoginDto(email);
        var loginResponse = await Client.PostAsJsonAsync($"{BaseRoute}/login", loginRequest);
        var originalAuth = await loginResponse.ToResponseModel<AuthenticationResponseDto>();
        
        // Відкликання токену
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
        
        var loginRequest = AuthData.CreateLoginDto(email);
        var loginResponse = await Client.PostAsJsonAsync($"{BaseRoute}/login", loginRequest);
        var originalAuth = await loginResponse.ToResponseModel<AuthenticationResponseDto>();
        
        
        Context.ChangeTracker.Clear();
        var dbUser = await Context.Users.FirstAsync(u => u.Email == email);
        dbUser.Block();
        await Context.SaveChangesAsync();
        
        // Act
        var refreshRequest = AuthData.CreateRefreshTokenDto(originalAuth.Token, originalAuth.RefreshToken);
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("blocked");
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

    [Fact]
    public async Task RefreshToken_WithExpiredRefreshToken_ShouldReturnUnauthorizedAndRevokeToken()
    {
        // Arrange
        var email = "refresh_expired@test.com";
        var user = ApplicationUser.Create(email, "Refresh", "User", "refreshexpireduser");
        await _userManager.CreateAsync(user, AuthData.DefaultPassword);
        await _userManager.AddToRoleAsync(user, ApplicationRole.User);
        
        var loginRequest = AuthData.CreateLoginDto(email);
        var loginResponse = await Client.PostAsJsonAsync($"{BaseRoute}/login", loginRequest);
        var originalAuth = await loginResponse.ToResponseModel<AuthenticationResponseDto>();
        
        // Симуляція закінчення терміну дії токену
        Context.ChangeTracker.Clear();
        var dbUser = await Context.Users.FirstAsync(u => u.Email == email);
        dbUser.SetRefreshToken(originalAuth.RefreshToken, DateTime.UtcNow.AddDays(-1)); 
        await Context.SaveChangesAsync();

        // Act
        var refreshRequest = AuthData.CreateRefreshTokenDto(originalAuth.Token, originalAuth.RefreshToken);
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/refresh", refreshRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        
        // Перевірка що токен відкликано
        Context.ChangeTracker.Clear();
        var dbUserAfter = await Context.Users.FirstAsync(u => u.Email == email);
        dbUserAfter.RefreshToken.Should().BeNull();
        dbUserAfter.RefreshTokenExpiryTime.Should().BeNull();
    }

    [Fact]
    public async Task RefreshToken_MultipleTimes_ShouldInvalidateOldTokens()
    {
        // Arrange
        var email = "refresh_multiple@test.com";
        var user = ApplicationUser.Create(email, "Refresh", "User", "refreshmultipleuser");
        await _userManager.CreateAsync(user, AuthData.DefaultPassword);
        await _userManager.AddToRoleAsync(user, ApplicationRole.User);
        
        var loginRequest = AuthData.CreateLoginDto(email);
        var loginResponse = await Client.PostAsJsonAsync($"{BaseRoute}/login", loginRequest);
        var firstAuth = await loginResponse.ToResponseModel<AuthenticationResponseDto>();

        await Task.Delay(100);

        // Перше оновлення
        var firstRefreshRequest = AuthData.CreateRefreshTokenDto(firstAuth.Token, firstAuth.RefreshToken);
        var firstRefreshResponse = await Client.PostAsJsonAsync($"{BaseRoute}/refresh", firstRefreshRequest);
        var secondAuth = await firstRefreshResponse.ToResponseModel<AuthenticationResponseDto>();

        await Task.Delay(100);

        // Act - спроба використати старий refresh token
        var oldRefreshRequest = AuthData.CreateRefreshTokenDto(firstAuth.Token, firstAuth.RefreshToken);
        var oldRefreshResponse = await Client.PostAsJsonAsync($"{BaseRoute}/refresh", oldRefreshRequest);

        // Assert
        oldRefreshResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Перевірка що новий токен все ще працює
        var newRefreshRequest = AuthData.CreateRefreshTokenDto(secondAuth.Token, secondAuth.RefreshToken);
        var newRefreshResponse = await Client.PostAsJsonAsync($"{BaseRoute}/refresh", newRefreshRequest);
        newRefreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RefreshToken_WithDifferentUserTokens_ShouldNotWork()
    {
        // Arrange
        var email1 = "user1@test.com";
        var user1 = ApplicationUser.Create(email1, "User", "One", "userone");
        await _userManager.CreateAsync(user1, AuthData.DefaultPassword);
        await _userManager.AddToRoleAsync(user1, ApplicationRole.User);

        var email2 = "user2@test.com";
        var user2 = ApplicationUser.Create(email2, "User", "Two", "usertwo");
        await _userManager.CreateAsync(user2, AuthData.DefaultPassword);
        await _userManager.AddToRoleAsync(user2, ApplicationRole.User);

        // Логін обох користувачів
        var login1 = await Client.PostAsJsonAsync($"{BaseRoute}/login", 
            AuthData.CreateLoginDto(email1));
        var auth1 = await login1.ToResponseModel<AuthenticationResponseDto>();

        var login2 = await Client.PostAsJsonAsync($"{BaseRoute}/login", 
            AuthData.CreateLoginDto(email2));
        var auth2 = await login2.ToResponseModel<AuthenticationResponseDto>();

        // Act - спроба використати токен першого користувача з refresh token другого
        var mixedRequest = AuthData.CreateRefreshTokenDto(auth1.Token, auth2.RefreshToken);
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/refresh", mixedRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
}