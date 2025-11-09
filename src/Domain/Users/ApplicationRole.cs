using Microsoft.AspNetCore.Identity;

namespace Domain.Users;

public class ApplicationRole : IdentityRole<Guid>
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string User = "User";

    public ApplicationRole() { }

    public ApplicationRole(string roleName) : base(roleName)
    {
        Id = Guid.NewGuid();
    }
}