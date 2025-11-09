using System.Net;
using System.Net.Http.Json;
using Api.Dtos;
using Domain.Users;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Tests.Common;

namespace Api.Tests.Integration.Users;

public class UsersControllerTests : BaseIntegrationTest, IAsyncLifetime
{
    private const string BaseRoute = "users";
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly string _testUserId = Guid.NewGuid().ToString();
    private readonly string _otherUserId = Guid.NewGuid().ToString();
    private HttpClient _userClient;
    private HttpClient _otherUserClient;
    private HttpClient _adminClient;
    private ApplicationUser _testUser;
    private ApplicationUser _otherUser;

    public UsersControllerTests(IntegrationTestWebFactory factory) : base(factory)
    {
        var scope = factory.Services.CreateScope();
        _userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        _userClient = CreateAuthenticatedClient("User", _testUserId);
        _otherUserClient = CreateAuthenticatedClient("User", _otherUserId);
        _adminClient = CreateAuthenticatedClient("Admin");
    }

    #region GET Tests (Profile)

    [Fact]
    public async Task GetMyProfile_AsUser_ShouldReturnOwnProfile()
    {
        // Act
        var response = await _userClient.GetAsync($"{BaseRoute}/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userDto = await response.ToResponseModel<UserDto>();
        userDto.Id.Should().Be(Guid.Parse(_testUserId));
        userDto.Email.Should().Be(_testUser.Email);
        userDto.FirstName.Should().Be(_testUser.FirstName);
        userDto.LastName.Should().Be(_testUser.LastName);
        userDto.Roles.Should().Contain(ApplicationRole.User);
    }

    [Fact]
    public async Task GetMyProfile_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"{BaseRoute}/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region PUT Tests (Update Profile)

    [Fact]
    public async Task UpdateProfile_AsUser_ShouldUpdateOwnProfile()
    {
        // Arrange
        var request = new UpdateUserProfileDto("Updated", "Name");

        // Act
        var response = await _userClient.PutAsJsonAsync($"{BaseRoute}/profile", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userDto = await response.ToResponseModel<UserDto>();
        userDto.FirstName.Should().Be(request.FirstName);
        userDto.LastName.Should().Be(request.LastName);

        var dbUser = await _userManager.FindByIdAsync(_testUserId);
        dbUser.Should().NotBeNull();
        dbUser!.FirstName.Should().Be(request.FirstName);
        dbUser.LastName.Should().Be(request.LastName);
    }

    [Theory]
    [InlineData("", "LastName")] // Empty first name
    [InlineData("FirstName", "")] // Empty last name
    [InlineData(null, "LastName")] // Null first name
    [InlineData("FirstName", null)] // Null last name
    public async Task UpdateProfile_WithInvalidData_ShouldReturnBadRequest(
        string firstName, string lastName)
    {
        // Arrange
        var request = new UpdateUserProfileDto(firstName, lastName);

        // Act
        var response = await _userClient.PutAsJsonAsync($"{BaseRoute}/profile", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateProfile_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new UpdateUserProfileDto("Updated", "Name");

        // Act
        var response = await Client.PutAsJsonAsync($"{BaseRoute}/profile", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET Tests (All Users - Admin)

    [Fact]
    public async Task GetAll_AsAdmin_ShouldReturnAllUsers()
    {
        // Act
        var response = await _adminClient.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = await response.ToResponseModel<List<UserDto>>();
        
        // ВИПРАВЛЕНО: використовуємо Count.Should().BeGreaterOrEqualTo()
        users.Should().HaveCountGreaterThanOrEqualTo(2); // Як мінімум наші 2 тестові користувачі
        users.Should().Contain(u => u.Id == Guid.Parse(_testUserId));
        users.Should().Contain(u => u.Id == Guid.Parse(_otherUserId));
    }

    [Fact]
    public async Task GetAll_AsUser_ShouldReturnForbidden()
    {
        // Act
        var response = await _userClient.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAll_WithoutAuth_ShouldReturnUnauthorized()
    {
        // Act
        var response = await Client.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET Tests (Get by Id - Admin)

    [Fact]
    public async Task GetById_AsAdmin_ShouldReturnUser()
    {
        // Act
        var response = await _adminClient.GetAsync($"{BaseRoute}/{_testUserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userDto = await response.ToResponseModel<UserDto>();
        userDto.Id.Should().Be(Guid.Parse(_testUserId));
    }

    [Fact]
    public async Task GetById_AsUser_ShouldReturnForbidden()
    {
        // Act
        var response = await _userClient.GetAsync($"{BaseRoute}/{_otherUserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetById_NonExistentUser_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _adminClient.GetAsync($"{BaseRoute}/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST Tests (Block User)

    [Fact]
    public async Task BlockUser_AsAdmin_ShouldBlockUser()
    {
        // Act
        var response = await _adminClient.PostAsync($"{BaseRoute}/{_otherUserId}/block", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userDto = await response.ToResponseModel<UserDto>();
        userDto.IsBlocked.Should().BeTrue();

        var dbUser = await _userManager.FindByIdAsync(_otherUserId);
        dbUser.Should().NotBeNull();
        dbUser!.IsBlocked.Should().BeTrue();
    }

    [Fact]
    public async Task BlockUser_AsUser_ShouldReturnForbidden()
    {
        // Act
        var response = await _userClient.PostAsync($"{BaseRoute}/{_otherUserId}/block", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task BlockUser_NonExistentUser_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _adminClient.PostAsync($"{BaseRoute}/{nonExistentId}/block", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST Tests (Unblock User)

    [Fact]
    public async Task UnblockUser_AsAdmin_ShouldUnblockUser()
    {
        // Arrange - спочатку блокуємо користувача
        _otherUser.Block();
        await _userManager.UpdateAsync(_otherUser);

        // Act
        var response = await _adminClient.PostAsync($"{BaseRoute}/{_otherUserId}/unblock", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userDto = await response.ToResponseModel<UserDto>();
        userDto.IsBlocked.Should().BeFalse();

        var dbUser = await _userManager.FindByIdAsync(_otherUserId);
        dbUser.Should().NotBeNull();
        dbUser!.IsBlocked.Should().BeFalse();
    }

    [Fact]
    public async Task UnblockUser_AsUser_ShouldReturnForbidden()
    {
        // Act
        var response = await _userClient.PostAsync($"{BaseRoute}/{_otherUserId}/unblock", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region PUT Tests (Change Role)

    [Fact]
    public async Task ChangeUserRole_AsAdmin_ShouldChangeRole()
    {
        // Arrange
        var request = new ChangeUserRoleDto(ApplicationRole.Manager);

        // Act
        var response = await _adminClient.PutAsJsonAsync(
            $"{BaseRoute}/{_otherUserId}/role",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var roles = await _userManager.GetRolesAsync(_otherUser);
        roles.Should().Contain(ApplicationRole.Manager);
        roles.Should().NotContain(ApplicationRole.User);
    }

    [Fact]
    public async Task ChangeUserRole_WithInvalidRole_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new ChangeUserRoleDto("InvalidRole");

        // Act
        var response = await _adminClient.PutAsJsonAsync(
            $"{BaseRoute}/{_otherUserId}/role",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ChangeUserRole_AsUser_ShouldReturnForbidden()
    {
        // Arrange
        var request = new ChangeUserRoleDto(ApplicationRole.Admin);

        // Act
        var response = await _userClient.PutAsJsonAsync(
            $"{BaseRoute}/{_otherUserId}/role",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ChangeUserRole_NonExistentUser_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var request = new ChangeUserRoleDto(ApplicationRole.Manager);

        // Act
        var response = await _adminClient.PutAsJsonAsync($"{BaseRoute}/{nonExistentId}/role", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    public async Task InitializeAsync()
    {
        // Створюємо тестових користувачів
        _testUser = ApplicationUser.Create(
            "testuser@test.com",
            "Test",
            "User",
            "testuser"
        );
        
        // Встановлюємо ID для тестового користувача
        typeof(ApplicationUser).GetProperty("Id")!.SetValue(_testUser, Guid.Parse(_testUserId));
        
        await _userManager.CreateAsync(_testUser, "Test@123");
        await _userManager.AddToRoleAsync(_testUser, ApplicationRole.User);

        _otherUser = ApplicationUser.Create(
            "otheruser@test.com",
            "Other",
            "User",
            "otheruser"
        );
        
        typeof(ApplicationUser).GetProperty("Id")!.SetValue(_otherUser, Guid.Parse(_otherUserId));
        
        await _userManager.CreateAsync(_otherUser, "Test@123");
        await _userManager.AddToRoleAsync(_otherUser, ApplicationRole.User);
    }

    public async Task DisposeAsync()
    {
        if (_testUser != null)
        {
            await _userManager.DeleteAsync(_testUser);
        }
        
        if (_otherUser != null)
        {
            await _userManager.DeleteAsync(_otherUser);
        }
    }
}