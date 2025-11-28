using Domain.Users;

namespace Tests.Data.Users;

public static class UserData
{
    public static ApplicationUser CreateTestUser(string role = ApplicationRole.User, Guid? id = null)
    {
        var user = ApplicationUser.Create(
            $"test-{role.ToLower()}@test.com",
            $"Test{role}",
            "User",
            $"test{role.ToLower()}"
        );
        if (id.HasValue)
        {
            user.Id = id.Value;
        }

        return user;
    }
    
    public static ApplicationUser CreateAdminUser(Guid? id = null) => CreateTestUser(ApplicationRole.Admin, id);
    public static ApplicationUser CreateManagerUser(Guid? id = null) => CreateTestUser(ApplicationRole.Manager, id);
    public static ApplicationUser CreateRegularUser(Guid? id = null) => CreateTestUser(ApplicationRole.User, id);
}