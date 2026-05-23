using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace QuotesApi.Tests;

/// <summary>
/// Fake authentication handler for integration tests.
/// Unauthenticated by default (no header → NoResult).
/// Set "X-Test-Claims" to a JSON array of [{type,value}] to get an authenticated principal.
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "TestAuth";

    private static readonly JsonSerializerOptions _jsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // No X-Test-Claims header → treat as anonymous
        if (!Request.Headers.TryGetValue("X-Test-Claims", out var claimsHeader))
            return Task.FromResult(AuthenticateResult.NoResult());

        var raw = claimsHeader.ToString();
        var dtos = JsonSerializer.Deserialize<ClaimDto[]>(raw, _jsonOpts) ?? [];

        var claims = dtos
            .Where(d => d.Type is not null && d.Value is not null)
            .Select(d => new Claim(d.Type!, d.Value!))
            .ToList();

        var identity = new ClaimsIdentity(claims, SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private record ClaimDto(string? Type, string? Value);
}
