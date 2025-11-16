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
    
    private UserManager<ApplicationUser> _userManager; 
    private string _testUserId;
    private string _otherUserId;
    private HttpClient _userClient;
    private readonly HttpClient _adminClient;
    private ApplicationUser _testUser;
    private readonly string _adminUserId = Guid.NewGuid().ToString();

    public UsersControllerTests(IntegrationTestWebFactory factory) : base(factory)
    {
        _adminClient = CreateAuthenticatedClient("Admin", _adminUserId);
    }
    
    private async Task<(ApplicationUser User, HttpClient Client)> CreateTestUserAndClientAsync(
        string firstNamePrefix, string role = ApplicationRole.User, string password = "Test@123")
    {
        var user = ApplicationUser.Create(
            $"{firstNamePrefix.ToLower()}_{Guid.NewGuid()}@test.com",
            firstNamePrefix,
            "User",
            $"{firstNamePrefix.ToLower()}_{Guid.NewGuid()}"
        );

        // 2. Збереження у БД через UserManager
        await _userManager.CreateAsync(user, password);
        
        // 3. Присвоєння ролі
        await _userManager.AddToRoleAsync(user, role);

        // 4. Створення автентифікованого клієнта
        var client = CreateAuthenticatedClient(role, user.Id.ToString());

        return (user, client);
    }

    private async Task<ApplicationUser?> ReloadUserAsync(string userId)
    {
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return await userManager.FindByIdAsync(userId);
    }

    #region GET Tests (Profile)

    [Fact]
    public async Task ShouldGetMyProfileAsUser()
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
    public async Task ShouldNotGetMyProfileBecauseUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"{BaseRoute}/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region PUT Tests (Update Profile)

    [Fact]
    public async Task ShouldUpdateMyProfileAsUser()
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

        var dbUser = await ReloadUserAsync(_testUserId);
        dbUser.Should().NotBeNull();
        dbUser.FirstName.Should().Be(request.FirstName);
        dbUser.LastName.Should().Be(request.LastName);
    }

    [Theory]
    [InlineData("", "LastName")]
    [InlineData("FirstName", "")]
    [InlineData(null, "LastName")]
    [InlineData("FirstName", null)]
    public async Task ShouldNotUpdateProfileBecauseInvalidData(
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
    public async Task ShouldNotUpdateProfileBecauseUnauthorized()
    {
        // Arrange
        var request = new UpdateUserProfileDto("Updated", "Name");

        // Act
        var response = await Client.PutAsJsonAsync($"{BaseRoute}/profile", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    #endregion

    #region GET Tests (All Users)

    [Fact]
    public async Task ShouldGetAllUsersAsAdmin()
    {
        // Act
        var response = await _adminClient.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = await response.ToResponseModel<List<UserDto>>();
        
        users.Should().HaveCountGreaterThanOrEqualTo(2);
        users.Should().Contain(u => u.Id == Guid.Parse(_testUserId));
        users.Should().Contain(u => u.Id == Guid.Parse(_otherUserId));
    }

    [Fact]
    public async Task ShouldNotGetAllUsersBecauseForbidden()
    {
        // Act
        var response = await _userClient.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ShouldNotGetAllUsersBecauseUnauthorized()
    {
        // Act
        var response = await Client.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET Tests (Get by Id)

    [Fact]
    public async Task ShouldGetUserByIdAsAdmin()
    {
        // Act
        var response = await _adminClient.GetAsync($"{BaseRoute}/{_testUserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userDto = await response.ToResponseModel<UserDto>();
        userDto.Id.Should().Be(Guid.Parse(_testUserId));
    }

    [Fact]
    public async Task ShouldNotGetUserByIdBecauseForbidden()
    {
        // Act
        var response = await _userClient.GetAsync($"{BaseRoute}/{_otherUserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ShouldNotGetUserByIdBecauseUserNotFound()
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
    public async Task ShouldBlockUserAsAdmin()
    {
        // Act
        var response = await _adminClient.PostAsync($"{BaseRoute}/{_otherUserId}/block", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userDto = await response.ToResponseModel<UserDto>();
        userDto.IsBlocked.Should().BeTrue();

        var dbUser = await ReloadUserAsync(_otherUserId);
        dbUser.Should().NotBeNull();
        dbUser.IsBlocked.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldNotBlockUserBecauseForbidden()
    {
        // Act
        var response = await _userClient.PostAsync($"{BaseRoute}/{_otherUserId}/block", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ShouldNotBlockUserBecauseUserNotFound()
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
    public async Task ShouldUnblockUserAsAdmin()
    {
        // Arrange
        using (var scope = Factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var userToBlock = await userManager.FindByIdAsync(_otherUserId);
            userToBlock!.Block();
            await userManager.UpdateAsync(userToBlock);
        }

        // Act
        var response = await _adminClient.PostAsync($"{BaseRoute}/{_otherUserId}/unblock", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userDto = await response.ToResponseModel<UserDto>();
        userDto.IsBlocked.Should().BeFalse();

        var dbUser = await ReloadUserAsync(_otherUserId);
        dbUser.Should().NotBeNull();
        dbUser.IsBlocked.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldNotUnblockUserBecauseForbidden()
    {
        // Act
        var response = await _userClient.PostAsync($"{BaseRoute}/{_otherUserId}/unblock", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region PUT Tests (Change Role)

    [Fact]
    public async Task ShouldChangeUserRoleAsAdmin()
    {
        // Arrange
        var request = new ChangeUserRoleDto(ApplicationRole.Manager);

        // Act
        var response = await _adminClient.PutAsJsonAsync(
            $"{BaseRoute}/{_otherUserId}/role",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedUser = await _userManager.FindByIdAsync(_otherUserId);
        var roles = await _userManager.GetRolesAsync(updatedUser!);
        roles.Should().Contain(ApplicationRole.Manager);
        roles.Should().NotContain(ApplicationRole.User);
    }

    [Fact]
    public async Task ShouldNotChangeUserRoleBecauseInvalidRole()
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
    public async Task ShouldNotChangeUserRoleBecauseForbidden()
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
    public async Task ShouldNotChangeUserRoleBecauseUserNotFound()
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
        var scope = Factory.Services.CreateScope();
        _userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var (testUser, userClient) = await CreateTestUserAndClientAsync("Test");
        _testUser = testUser;
        _testUserId = testUser.Id.ToString();
        _userClient = userClient;
        
        var (otherUser, _) = await CreateTestUserAndClientAsync("Other");
        _otherUserId = otherUser.Id.ToString();
    }

    public async Task DisposeAsync()
    {
        await CleanupDatabaseAsync(); 
    }
}