using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using NSubstitute;
using QuotesApi.Authorization;
using QuotesApi.Models;
using Xunit;

namespace Quotes.Tests.Unit;

public class QuoteOwnerHandlerTests
{
    private static QuoteOwnerHandler BuildHandler() =>
        new(Substitute.For<ILogger<QuoteOwnerHandler>>());

    [Fact]
    public async Task HandleRequirement_UserOwnsQuote_ContextSucceeds()
    {
        var handler = BuildHandler();
        var quote = Quote.Create("Author", "Text", "user-123").Value!;
        var requirement = new QuoteOwnerRequirement();
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user-123")], "test");
        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(identity), quote);

        await ((IAuthorizationHandler)handler).HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task HandleRequirement_UserDoesNotOwnQuote_ContextDoesNotSucceed()
    {
        var handler = BuildHandler();
        var quote = Quote.Create("Author", "Text", "owner-456").Value!;
        var requirement = new QuoteOwnerRequirement();
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "different-user")], "test");
        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(identity), quote);

        await ((IAuthorizationHandler)handler).HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task HandleRequirement_MissingNameIdentifierClaim_ContextDoesNotSucceed()
    {
        var handler = BuildHandler();
        var quote = Quote.Create("Author", "Text", "owner-456").Value!;
        var requirement = new QuoteOwnerRequirement();
        var identity = new ClaimsIdentity([], "test");
        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(identity), quote);

        await ((IAuthorizationHandler)handler).HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }
}
