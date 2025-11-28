using Domain.Users;

namespace Application.Authentication.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(ApplicationUser user, List<string> roles);
    string GenerateRefreshToken();
    
    Guid? GetUserIdFromToken(string token);
}