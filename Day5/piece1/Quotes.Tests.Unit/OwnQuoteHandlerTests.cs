using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using QuotesApi.Authorization;
using QuotesApi.Models;

namespace Quotes.Tests.Unit;

public class OwnQuoteHandlerTests
{
    private readonly OwnQuoteHandler _handler = new();

    private static AuthorizationHandlerContext MakeContext(ClaimsPrincipal user, Quote resource)
    {
        var requirements = new[] { new OwnQuoteRequirement() };
        return new AuthorizationHandlerContext(requirements, user, resource);
    }

    private static ClaimsPrincipal UserWithSub(string sub)
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, sub)],
            authenticationType: "test");
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public async Task Owner_Succeeds()
    {
        var quote = new Quote { Id = 1, OwnerId = 42, Author = "A", Text = "T" };
        var context = MakeContext(UserWithSub("42"), quote);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task NonOwner_DoesNotSucceed()
    {
        var quote = new Quote { Id = 1, OwnerId = 42, Author = "A", Text = "T" };
        var context = MakeContext(UserWithSub("99"), quote);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task NoClaim_DoesNotSucceed()
    {
        var quote = new Quote { Id = 1, OwnerId = 42, Author = "A", Text = "T" };
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var context = MakeContext(user, quote);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task NonNumericSub_DoesNotSucceed()
    {
        var quote = new Quote { Id = 1, OwnerId = 42, Author = "A", Text = "T" };
        var context = MakeContext(UserWithSub("not-a-number"), quote);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task FallsBackToJwtSubClaim_WhenNameIdentifierMissing()
    {
        var quote = new Quote { Id = 1, OwnerId = 7, Author = "A", Text = "T" };
        // JwtRegisteredClaimNames.Sub = "sub" — handler falls back to this
        var identity = new ClaimsIdentity(
            [new Claim(JwtRegisteredClaimNames.Sub, "7")],
            authenticationType: "test");
        var user = new ClaimsPrincipal(identity);
        var context = MakeContext(user, quote);

        await _handler.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }
}
