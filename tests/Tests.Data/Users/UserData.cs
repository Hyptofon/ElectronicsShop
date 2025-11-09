using Domain.Users;

namespace Tests.Data.Users;

public static class UserData
{
    public static ApplicationUser CreateTestUser(string role = ApplicationRole.User)
    {
        var user = ApplicationUser.Create(
            $"test-{role.ToLower()}@test.com",
            $"Test{role}",
            "User",
            $"test{role.ToLower()}"
        );
        return user;
    }

    public static ApplicationUser CreateAdminUser() => CreateTestUser(ApplicationRole.Admin);
    public static ApplicationUser CreateManagerUser() => CreateTestUser(ApplicationRole.Manager);
    public static ApplicationUser CreateRegularUser() => CreateTestUser(ApplicationRole.User);
}