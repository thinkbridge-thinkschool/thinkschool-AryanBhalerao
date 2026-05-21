using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using QuotesApi.Models;

namespace QuotesApi.Authorization;

public sealed class QuoteOwnerHandler : AuthorizationHandler<QuoteOwnerRequirement, Quote>
{
    private readonly ILogger<QuoteOwnerHandler> _logger;

    public QuoteOwnerHandler(ILogger<QuoteOwnerHandler> logger) => _logger = logger;

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        QuoteOwnerRequirement requirement,
        Quote resource)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId is not null && userId == resource.OwnerId)
        {
            _logger.LogInformation("User {UserId} authorized to modify quote {QuoteId}", userId, resource.Id);
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "User {UserId} denied: quote {QuoteId} is owned by {OwnerId}",
                userId, resource.Id, resource.OwnerId);
        }

        return Task.CompletedTask;
    }
}
