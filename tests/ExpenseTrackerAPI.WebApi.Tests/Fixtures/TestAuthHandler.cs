using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExpenseTrackerAPI.WebApi.Tests.Fixtures;

public static class TestAuthDefaults
{
    public const string AuthenticationScheme = "Test";
}

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const int DefaultUserId = 1;
    public const string DefaultUserEmail = "test.user@expense-tracker.local";
    public const string DefaultUserName = "Test User";

#pragma warning disable CS0618
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }
#pragma warning restore CS0618

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, DefaultUserId.ToString()),
            new Claim(ClaimTypes.Email, DefaultUserEmail),
            new Claim(ClaimTypes.Name, DefaultUserName)
        };

        var identity = new ClaimsIdentity(claims, TestAuthDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, TestAuthDefaults.AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
