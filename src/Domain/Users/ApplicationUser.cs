using Microsoft.AspNetCore.Identity;

namespace Domain.Users;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsBlocked { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiryTime { get; private set; }
    public byte[]? RowVersion { get; private set; }

    private ApplicationUser() { }

    public static ApplicationUser Create(string email, string firstName, string lastName, string userName)
    {
        return new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            UserName = userName,
            CreatedAt = DateTime.UtcNow,
            IsBlocked = false,
            EmailConfirmed = true
        };
    }

    public void UpdateProfile(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Block()
    {
        IsBlocked = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Unblock()
    {
        IsBlocked = false;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void SetRefreshToken(string refreshToken, DateTime expiryTime) 
    {
        RefreshToken = refreshToken;
        RefreshTokenExpiryTime = expiryTime;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void RevokeRefreshToken() 
    {
        RefreshToken = null;
        RefreshTokenExpiryTime = null;
        UpdatedAt = DateTime.UtcNow;
    }
}