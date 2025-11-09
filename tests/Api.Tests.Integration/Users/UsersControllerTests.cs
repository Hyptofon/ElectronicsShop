using System.Net;
using System.Net.Http.Json;
using Api.Dtos;
using Domain.Users;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Tests.Common;
using Xunit;

namespace Api.Tests.Integration.Users;

public class UsersControllerTests : BaseIntegrationTest, IAsyncLifetime
{
    private const string BaseRoute = "users";
    private UserManager<ApplicationUser> _userManager;
    // ВИДАЛЯЄМО: жорстко задані ID
    // private readonly string _testUserId = Guid.NewGuid().ToString();
    // private readonly string _otherUserId = Guid.NewGuid().ToString();
    
    // ДОДАЄМО: поля для зберігання ID після створення
    private string _testUserId;
    private string _otherUserId;
    
    private HttpClient _userClient;
    private HttpClient _otherUserClient;
    private HttpClient _adminClient;
    private ApplicationUser _testUser;
    private ApplicationUser _otherUser;
    private async Task<ApplicationUser?> ReloadUserAsync(string userId)
    {
        // Створюємо новий scope для отримання свіжих даних з БД
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return await userManager.FindByIdAsync(userId);
    }
    public UsersControllerTests(IntegrationTestWebFactory factory) : base(factory)
    {
        // Ініціалізацію клієнтів переносимо в InitializeAsync, 
        // бо нам потрібні справжні ID користувачів
    }

    public async Task InitializeAsync()
    {
        // 1. Ініціалізуємо UserManager
        var scope = Factory.Services.CreateScope();
        _userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // 2. Створюємо тестових користувачів БЕЗ примусового встановлення ID
        _testUser = ApplicationUser.Create(
            $"testuser_{Guid.NewGuid()}@test.com", // Унікальний email
            "Test",
            "User",
            $"testuser_{Guid.NewGuid()}" // Унікальний username
        );
        
        await _userManager.CreateAsync(_testUser, "Test@123");
        await _userManager.AddToRoleAsync(_testUser, ApplicationRole.User);
        _testUserId = _testUser.Id.ToString(); // Зберігаємо справжній ID

        _otherUser = ApplicationUser.Create(
            $"otheruser_{Guid.NewGuid()}@test.com",
            "Other",
            "User",
            $"otheruser_{Guid.NewGuid()}"
        );
        
        await _userManager.CreateAsync(_otherUser, "Test@123");
        await _userManager.AddToRoleAsync(_otherUser, ApplicationRole.User);
        _otherUserId = _otherUser.Id.ToString(); // Зберігаємо справжній ID

        // 3. Тепер ініціалізуємо клієнтів з правильними ID
        _userClient = CreateAuthenticatedClient("User", _testUserId);
        _otherUserClient = CreateAuthenticatedClient("User", _otherUserId);
        _adminClient = CreateAuthenticatedClient("Admin");
    }

    public async Task DisposeAsync()
    {
        await CleanupDatabaseAsync();
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

        // ВИПРАВЛЕННЯ: Перезавантажуємо користувача
        var dbUser = await ReloadUserAsync(_testUserId);
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
        
        users.Should().HaveCountGreaterThanOrEqualTo(2);
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

        // ВИПРАВЛЕННЯ: Перезавантажуємо користувача через новий scope
        var dbUser = await ReloadUserAsync(_otherUserId);
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
        // Arrange
        // Тут теж варто використати окремий scope для початкового налаштування,
        // щоб основний _userManager не кешував цей стан, але для Arrange це менш критично,
        // якщо ми потім все одно перезавантажимо.
        // Для надійності зробимо так:
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

        // ВИПРАВЛЕННЯ: Перезавантажуємо користувача
        var dbUser = await ReloadUserAsync(_otherUserId);
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

        // Отримуємо оновленого користувача з бази
        var updatedUser = await _userManager.FindByIdAsync(_otherUserId);
        var roles = await _userManager.GetRolesAsync(updatedUser!);
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
}