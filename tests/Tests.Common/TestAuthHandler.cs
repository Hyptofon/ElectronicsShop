using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Tests.Common;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly TestAuthData _authData;

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        TestAuthData authData) 
        : base(options, logger, encoder)
    {
        _authData = authData;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // ВИПРАВЛЕННЯ: Перевіряємо наявність Authorization header
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new[] 
        { 
            new Claim(ClaimTypes.NameIdentifier, _authData.UserId),
            new Claim(ClaimTypes.Email, $"test-{_authData.Role.ToLower()}@test.com"),
            new Claim(ClaimTypes.Role, _authData.Role),
            new Claim(ClaimTypes.Name, $"Test {_authData.Role}")
        };
        
        var identity = new ClaimsIdentity(claims, "TestScheme");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "TestScheme");

        var result = AuthenticateResult.Success(ticket);

        return Task.FromResult(result);
    }
}