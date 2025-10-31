using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace BookApi.NET.Tests;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationScheme = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder)
    {
    }
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, TestUsers.Admin.ExternalId),
            new Claim(ClaimTypes.Email, TestUsers.Admin.Email),
            new Claim(ClaimsIdentity.DefaultNameClaimType, TestUsers.Admin.FullName),
            new Claim("internal_user_id", TestUsers.Admin.Id.ToString())
        };
        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public static class TestUsers
{
    public static readonly Models.User Admin = new("google-admin", "admin@test.com", "Test Admin");
}