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

        await _userManager.CreateAsync(user, password);
        await _userManager.AddToRoleAsync(user, role);

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
    public async Task GetMyProfile_AsUser_ShouldReturnUserProfile()
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
        userDto.IsBlocked.Should().BeFalse();
        userDto.Roles.Should().Contain(ApplicationRole.User);
        userDto.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetMyProfile_AsManager_ShouldReturnManagerProfile()
    {
        // Arrange
        var (managerUser, managerClient) = await CreateTestUserAndClientAsync("Manager", ApplicationRole.Manager);

        // Act
        var response = await managerClient.GetAsync($"{BaseRoute}/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userDto = await response.ToResponseModel<UserDto>();
        userDto.Id.Should().Be(managerUser.Id);
        userDto.Roles.Should().Contain(ApplicationRole.Manager);
    }



    [Fact]
    public async Task GetMyProfile_WhenUnauthorized_ShouldReturnUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"{BaseRoute}/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyProfile_WhenBlocked_ShouldStillReturnProfile()
    {
        // Arrange
        using (var scope = Factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(_testUserId);
            user!.Block();
            await userManager.UpdateAsync(user);
        }

        // Act
        var response = await _userClient.GetAsync($"{BaseRoute}/profile");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userDto = await response.ToResponseModel<UserDto>();
        userDto.IsBlocked.Should().BeTrue();
    }

    #endregion

    #region PUT Tests (Update Profile)

    [Fact]
    public async Task UpdateProfile_WithValidData_ShouldUpdateProfile()
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
        userDto.Email.Should().Be(_testUser.Email);

        // Перевірка в БД
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
    public async Task UpdateProfile_WithInvalidData_ShouldReturnBadRequest(
        string firstName, string lastName)
    {
        // Arrange
        var request = new UpdateUserProfileDto(firstName, lastName);

        // Act
        var response = await _userClient.PutAsJsonAsync($"{BaseRoute}/profile", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        // Перевірка що дані не змінились
        var dbUser = await ReloadUserAsync(_testUserId);
        dbUser!.FirstName.Should().Be(_testUser.FirstName);
        dbUser.LastName.Should().Be(_testUser.LastName);
    }

    [Fact]
    public async Task UpdateProfile_WithTooLongNames_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new UpdateUserProfileDto(
            new string('A', 101), 
            "LastName"
        );

        // Act
        var response = await _userClient.PutAsJsonAsync($"{BaseRoute}/profile", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateProfile_WhenUnauthorized_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new UpdateUserProfileDto("Updated", "Name");

        // Act
        var response = await Client.PutAsJsonAsync($"{BaseRoute}/profile", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProfile_WhenBlocked_ShouldReturnForbidden()
    {
        // Arrange
        using (var scope = Factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(_testUserId);
            user!.Block();
            await userManager.UpdateAsync(user);
        }

        var request = new UpdateUserProfileDto("Updated", "Name");

        // Act
        var response = await _userClient.PutAsJsonAsync($"{BaseRoute}/profile", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateProfile_MultipleTimes_ShouldUpdateSuccessfully()
    {
        // Arrange
        var request1 = new UpdateUserProfileDto("First", "Update");
        var request2 = new UpdateUserProfileDto("Second", "Update");

        // Act
        var response1 = await _userClient.PutAsJsonAsync($"{BaseRoute}/profile", request1);
        var response2 = await _userClient.PutAsJsonAsync($"{BaseRoute}/profile", request2);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var userDto = await response2.ToResponseModel<UserDto>();
        userDto.FirstName.Should().Be("Second");
        userDto.LastName.Should().Be("Update");
    }

    #endregion

    #region GET Tests (All Users)

    [Fact]
    public async Task GetAllUsers_AsAdmin_ShouldReturnAllUsers()
    {
        // Act
        var response = await _adminClient.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = await response.ToResponseModel<List<UserDto>>();
        
        users.Should().HaveCountGreaterThanOrEqualTo(2);
        users.Should().Contain(u => u.Id == Guid.Parse(_testUserId));
        users.Should().Contain(u => u.Id == Guid.Parse(_otherUserId));
        users.Should().OnlyContain(u => !string.IsNullOrEmpty(u.Email));
        users.Should().OnlyContain(u => u.Roles.Any());
    }

    [Fact]
    public async Task GetAllUsers_AsUser_ShouldReturnForbidden()
    {
        // Act
        var response = await _userClient.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllUsers_AsManager_ShouldReturnForbidden()
    {
        // Arrange
        var (_, managerClient) = await CreateTestUserAndClientAsync("Manager", ApplicationRole.Manager);

        // Act
        var response = await managerClient.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAllUsers_WhenUnauthorized_ShouldReturnUnauthorized()
    {
        // Act
        var response = await Client.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAllUsers_ShouldIncludeBlockedUsers()
    {
        // Arrange
        using (var scope = Factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(_otherUserId);
            user!.Block();
            await userManager.UpdateAsync(user);
        }

        // Act
        var response = await _adminClient.GetAsync(BaseRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var users = await response.ToResponseModel<List<UserDto>>();
        users.Should().Contain(u => u.Id == Guid.Parse(_otherUserId) && u.IsBlocked);
    }

    #endregion

    #region GET Tests (Get by Id)

    [Fact]
    public async Task GetUserById_AsAdmin_ShouldReturnUser()
    {
        // Act
        var response = await _adminClient.GetAsync($"{BaseRoute}/{_testUserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userDto = await response.ToResponseModel<UserDto>();
        userDto.Id.Should().Be(Guid.Parse(_testUserId));
        userDto.Email.Should().Be(_testUser.Email);
        userDto.FirstName.Should().Be(_testUser.FirstName);
        userDto.LastName.Should().Be(_testUser.LastName);
        userDto.Roles.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetUserById_AsUser_ShouldReturnForbidden()
    {
        // Act
        var response = await _userClient.GetAsync($"{BaseRoute}/{_otherUserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUserById_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _adminClient.GetAsync($"{BaseRoute}/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUserById_WithEmptyGuid_ShouldReturnNotFound() 
    {
        // Act
        var response = await _adminClient.GetAsync($"{BaseRoute}/{Guid.Empty}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound); 
    }

    [Fact]
    public async Task GetUserById_WhenUnauthorized_ShouldReturnUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"{BaseRoute}/{_testUserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserById_ForBlockedUser_ShouldReturnUser()
    {
        // Arrange
        using (var scope = Factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(_otherUserId);
            user!.Block();
            await userManager.UpdateAsync(user);
        }

        // Act
        var response = await _adminClient.GetAsync($"{BaseRoute}/{_otherUserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userDto = await response.ToResponseModel<UserDto>();
        userDto.IsBlocked.Should().BeTrue();
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

        // Перевірка в БД
        var dbUser = await ReloadUserAsync(_otherUserId);
        dbUser.Should().NotBeNull();
        dbUser.IsBlocked.Should().BeTrue();
    }

    [Fact]
    public async Task BlockUser_AlreadyBlocked_ShouldSucceed()
    {
        // Arrange
        using (var scope = Factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(_otherUserId);
            user!.Block();
            await userManager.UpdateAsync(user);
        }

        // Act
        var response = await _adminClient.PostAsync($"{BaseRoute}/{_otherUserId}/block", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userDto = await response.ToResponseModel<UserDto>();
        userDto.IsBlocked.Should().BeTrue();
    }

    [Fact]
    public async Task BlockUser_AsUser_ShouldReturnForbidden()
    {
        // Act
        var response = await _userClient.PostAsync($"{BaseRoute}/{_otherUserId}/block", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        
        // Перевірка що користувач не заблокований
        var dbUser = await ReloadUserAsync(_otherUserId);
        dbUser!.IsBlocked.Should().BeFalse();
    }

    [Fact]
    public async Task BlockUser_AsManager_ShouldReturnForbidden()
    {
        // Arrange
        var (_, managerClient) = await CreateTestUserAndClientAsync("Manager", ApplicationRole.Manager);

        // Act
        var response = await managerClient.PostAsync($"{BaseRoute}/{_otherUserId}/block", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task BlockUser_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _adminClient.PostAsync($"{BaseRoute}/{nonExistentId}/block", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task BlockUser_WhenUnauthorized_ShouldReturnUnauthorized()
    {
        // Act
        var response = await Client.PostAsync($"{BaseRoute}/{_otherUserId}/block", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task BlockUser_WithEmptyGuid_ShouldReturnBadRequest()
    {
        // Act
        var response = await _adminClient.PostAsync($"{BaseRoute}/{Guid.Empty}/block", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region POST Tests (Unblock User)

    [Fact]
    public async Task UnblockUser_AsAdmin_ShouldUnblockUser()
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

        // Перевірка в БД
        var dbUser = await ReloadUserAsync(_otherUserId);
        dbUser.Should().NotBeNull();
        dbUser.IsBlocked.Should().BeFalse();
    }

    [Fact]
    public async Task UnblockUser_AlreadyUnblocked_ShouldSucceed()
    {
        // Act
        var response = await _adminClient.PostAsync($"{BaseRoute}/{_otherUserId}/unblock", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userDto = await response.ToResponseModel<UserDto>();
        userDto.IsBlocked.Should().BeFalse();
    }

    [Fact]
    public async Task UnblockUser_AsUser_ShouldReturnForbidden()
    {
        // Act
        var response = await _userClient.PostAsync($"{BaseRoute}/{_otherUserId}/unblock", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UnblockUser_WithNonExistentId_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _adminClient.PostAsync($"{BaseRoute}/{nonExistentId}/unblock", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnblockUser_WhenUnauthorized_ShouldReturnUnauthorized()
    {
        // Act
        var response = await Client.PostAsync($"{BaseRoute}/{_otherUserId}/unblock", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region PUT Tests (Change Role)

    [Fact]
    public async Task ChangeUserRole_ToManager_ShouldChangeRole()
    {
        // Arrange
        var request = new ChangeUserRoleDto(ApplicationRole.Manager);

        // Act
        var response = await _adminClient.PutAsJsonAsync(
            $"{BaseRoute}/{_otherUserId}/role",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Перевірка в БД
        var updatedUser = await _userManager.FindByIdAsync(_otherUserId);
        var roles = await _userManager.GetRolesAsync(updatedUser!);
        roles.Should().Contain(ApplicationRole.Manager);
        roles.Should().NotContain(ApplicationRole.User);
    }

    [Fact]
    public async Task ChangeUserRole_ToAdmin_ShouldChangeRole()
    {
        // Arrange
        var request = new ChangeUserRoleDto(ApplicationRole.Admin);

        // Act
        var response = await _adminClient.PutAsJsonAsync(
            $"{BaseRoute}/{_otherUserId}/role",
            request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedUser = await _userManager.FindByIdAsync(_otherUserId);
        var roles = await _userManager.GetRolesAsync(updatedUser!);
        roles.Should().Contain(ApplicationRole.Admin);
    }

    [Fact]
    public async Task ChangeUserRole_ToUser_ShouldChangeRole()
    {
        // Arrange
        var managerRequest = new ChangeUserRoleDto(ApplicationRole.Manager);
        await _adminClient.PutAsJsonAsync($"{BaseRoute}/{_otherUserId}/role", managerRequest);

        var userRequest = new ChangeUserRoleDto(ApplicationRole.User);

        // Act
        var response = await _adminClient.PutAsJsonAsync(
            $"{BaseRoute}/{_otherUserId}/role",
            userRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedUser = await _userManager.FindByIdAsync(_otherUserId);
        var roles = await _userManager.GetRolesAsync(updatedUser!);
        roles.Should().Contain(ApplicationRole.User);
        roles.Should().NotContain(ApplicationRole.Manager);
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
        
        // Перевірка що роль не змінилась
        var user = await _userManager.FindByIdAsync(_otherUserId);
        var roles = await _userManager.GetRolesAsync(user!);
        roles.Should().Contain(ApplicationRole.User);
    }

    [Fact]
    public async Task ChangeUserRole_WithEmptyRole_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new ChangeUserRoleDto("");

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
    public async Task ChangeUserRole_WithNonExistentUser_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var request = new ChangeUserRoleDto(ApplicationRole.Manager);

        // Act
        var response = await _adminClient.PutAsJsonAsync($"{BaseRoute}/{nonExistentId}/role", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ChangeUserRole_WhenUnauthorized_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new ChangeUserRoleDto(ApplicationRole.Manager);

        // Act
        var response = await Client.PutAsJsonAsync($"{BaseRoute}/{_otherUserId}/role", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangeUserRole_MultipleTimes_ShouldUpdateSuccessfully()
    {
        // Arrange
        var managerRequest = new ChangeUserRoleDto(ApplicationRole.Manager);
        var adminRequest = new ChangeUserRoleDto(ApplicationRole.Admin);
        var userRequest = new ChangeUserRoleDto(ApplicationRole.User);

        // Act
        await _adminClient.PutAsJsonAsync($"{BaseRoute}/{_otherUserId}/role", managerRequest);
        await _adminClient.PutAsJsonAsync($"{BaseRoute}/{_otherUserId}/role", adminRequest);
        var finalResponse = await _adminClient.PutAsJsonAsync($"{BaseRoute}/{_otherUserId}/role", userRequest);

        // Assert
        finalResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var user = await _userManager.FindByIdAsync(_otherUserId);
        var roles = await _userManager.GetRolesAsync(user!);
        roles.Should().ContainSingle();
        roles.Should().Contain(ApplicationRole.User);
    }

    #endregion

    #region Complex Scenarios

    [Fact]
    public async Task ComplexScenario_BlockUnblockAndChangeRole_ShouldWorkCorrectly()
    {
        // Act 1 - Блокуємо користувача
        var blockResponse = await _adminClient.PostAsync($"{BaseRoute}/{_otherUserId}/block", null);
        blockResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act 2 - Змінюємо роль заблокованого користувача
        var roleRequest = new ChangeUserRoleDto(ApplicationRole.Manager);
        var roleResponse = await _adminClient.PutAsJsonAsync($"{BaseRoute}/{_otherUserId}/role", roleRequest);
        roleResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act 3 - Розблоковуємо користувача
        var unblockResponse = await _adminClient.PostAsync($"{BaseRoute}/{_otherUserId}/unblock", null);
        unblockResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert
        var user = await ReloadUserAsync(_otherUserId);
        user!.IsBlocked.Should().BeFalse();
        var roles = await _userManager.GetRolesAsync(user);
        roles.Should().Contain(ApplicationRole.Manager);
    }

    [Fact]
    public async Task BlockedUser_ShouldNotBeAbleToUpdateProfile()
    {
        // Arrange
        using (var scope = Factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(_testUserId);
            user!.Block();
            await userManager.UpdateAsync(user);
        }

        var request = new UpdateUserProfileDto("Should", "Fail");

        // Act
        var response = await _userClient.PutAsJsonAsync($"{BaseRoute}/profile", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DifferentRoles_ShouldHaveDifferentPermissions()
    {
        // Arrange
        var (_, managerClient) = await CreateTestUserAndClientAsync("Manager", ApplicationRole.Manager);

        // Act - User спробує отримати всіх користувачів
        var userResponse = await _userClient.GetAsync(BaseRoute);
        
        // Act - Manager спробує отримати всіх користувачів
        var managerResponse = await managerClient.GetAsync(BaseRoute);
        
        // Act - Admin отримує всіх користувачів
        var adminResponse = await _adminClient.GetAsync(BaseRoute);

        // Assert
        userResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        managerResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        adminResponse.StatusCode.Should().Be(HttpStatusCode.OK);
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