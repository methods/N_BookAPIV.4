using System.Text.Encodings.Web;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace BookApi.NET.Tests;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AuthenticationScheme = "Test";
    public const string TestUserHeader = "X-Test-User-Id";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,   
        ILoggerFactory logger,
        UrlEncoder encoder)
         : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(TestUserHeader, out var userIdValues))
        {
            return CreateTicketFor(TestUsers.Admin);
        }

        var externalId = userIdValues.FirstOrDefault();
        var user = TestUsers.FindByExternalId(externalId);

        if (user is null)
        {
            return Task.FromResult(AuthenticateResult.Fail($"Test user with external ID '{externalId}' not found."));
        }

        // 3. Create a success ticket for the found user.
        return CreateTicketFor(user);
    }

        private Task<AuthenticateResult> CreateTicketFor(Models.User user)
    {
        var claims = TestUsers.CreateClaimsFor(user);
        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

public static class TestUsers
{
    // Make IDs stable and predictable
    public static readonly Models.User Admin = new("google-admin", "admin@test.com", "Test Admin")
    { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Role = "Admin" };

    public static readonly Models.User User1 = new("google-user1", "user1@test.com", "User One")
    { Id = Guid.Parse("22222222-2222-2222-2222-222222222222") };

    public static readonly Models.User User2 = new("google-user2", "user2@test.com", "User Two")
    { Id = Guid.Parse("33333333-3333-3333-3333-333333333333") };

    private static readonly List<Models.User> AllUsers = new() { Admin, User1, User2 };

    // New lookup method
    public static Models.User? FindByExternalId(string? externalId) => AllUsers.FirstOrDefault(u => u.ExternalId == externalId);

    // Renamed for clarity
    public static List<Claim> CreateClaimsFor(Models.User user) => new()
    {
        new Claim(ClaimTypes.NameIdentifier, user.ExternalId),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimsIdentity.DefaultNameClaimType, user.FullName),
        new Claim("internal_user_id", user.Id.ToString()),
        new Claim(ClaimTypes.Role, user.Role)
    };
}