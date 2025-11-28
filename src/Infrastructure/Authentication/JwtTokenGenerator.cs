using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.Authentication.Interfaces;
using Domain.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;

namespace Infrastructure.Authentication;

public class JwtTokenGenerator(IConfiguration configuration) : IJwtTokenGenerator
{
    public string GenerateToken(ApplicationUser user, List<string> roles)
    {
        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["JwtSettings:SecretKey"]!));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var token = new JwtSecurityToken(
            issuer: configuration["JwtSettings:Issuer"],
            audience: configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(
                double.Parse(configuration["JwtSettings:ExpirationInHours"]!)),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
    
    public Guid? GetUserIdFromToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(configuration["JwtSettings:SecretKey"]!);

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = false, 
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration["JwtSettings:Issuer"],
            ValidAudience = configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };

        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
            
            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }
            var userIdString = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value 
                               ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (Guid.TryParse(userIdString, out var userId))
            {
                return userId;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}