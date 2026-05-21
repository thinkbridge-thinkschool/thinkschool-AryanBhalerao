using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Quotes.Tests.Integration.Infrastructure;

/// <summary>
/// Reads "X-Test-Claims: sub=42,scope=quotes.write" and builds a ClaimsPrincipal.
/// Returning NoResult() when the header is absent leaves the user unauthenticated,
/// which triggers the real challenge path (→ 401).
/// </summary>
public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Test-Claims", out var raw))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = raw.ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .Where(kv => kv.Length == 2)
            .Select(kv => kv[0].Trim() == "sub"
                ? new Claim(ClaimTypes.NameIdentifier, kv[1].Trim())
                : new Claim(kv[0].Trim(), kv[1].Trim()))
            .ToList();

        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket   = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
