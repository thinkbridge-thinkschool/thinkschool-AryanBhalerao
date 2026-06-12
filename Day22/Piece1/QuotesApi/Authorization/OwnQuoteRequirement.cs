using Microsoft.AspNetCore.Authorization;

namespace QuotesApi.Authorization;

// Caller must own the quote
public class OwnQuoteRequirement : IAuthorizationRequirement { }
