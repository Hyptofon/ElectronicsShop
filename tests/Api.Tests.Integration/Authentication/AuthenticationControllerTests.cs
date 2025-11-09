using System.Net;
using System.Net.Http.Json;
using Api.Dtos;
using Domain.Users;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Tests.Common;
using Xunit;

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
        var request = new RegisterDto(
            "newuser@test.com",
            "Test@123",
            "New",
            "User"
        );

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
        await _userManager.CreateAsync(existingUser, "Test@123");

        var request = new RegisterDto(
            existingEmail,
            "Test@123",
            "Another",
            "User"
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
        var request = new RegisterDto(email, password, firstName, lastName);

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
        var password = "Test@123";
        var user = ApplicationUser.Create(email, "Login", "User", "loginuser");
        await _userManager.CreateAsync(user, password);
        await _userManager.AddToRoleAsync(user, ApplicationRole.User);

        var request = new LoginDto(email, password);

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
        var email = "user@test.com";
        var user = ApplicationUser.Create(email, "Test", "User", "testuser");
        await _userManager.CreateAsync(user, "Test@123");

        var request = new LoginDto(email, "WrongPassword@123");

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new LoginDto("nonexistent@test.com", "Test@123");

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
        var password = "Test@123";
        var user = ApplicationUser.Create(email, "Blocked", "User", "blockeduser");
        await _userManager.CreateAsync(user, password);
        user.Block();
        await _userManager.UpdateAsync(user);

        var request = new LoginDto(email, password);

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
        var request = new LoginDto(email, password);

        // Act
        var response = await Client.PostAsJsonAsync($"{BaseRoute}/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}