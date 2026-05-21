# 3 Sample tests

# QuoteTests.cs
```csharp
[Theory]
[InlineData(null)]
[InlineData("")]
[InlineData("   ")]
public void Create_AuthorNullOrWhiteSpace_ReturnsFailure(string? author)
{
    var result = Quote.Create(author, "Some valid text");

    result.IsSuccess.Should().BeFalse();
    result.Error.Should().Be("Author must be between 1 and 200 characters.");
}
```

# RefreshTokenTests.cs
```csharp
[Fact]
public void Revoke_WithReplacedByToken_ExposesReuseDetectionSignal()
{
    var token = RefreshToken.Create("old-hash", 1, "family-A", DateTime.UtcNow.AddDays(7));

    token.Revoke("successor-hash");

    token.IsRevoked.Should().BeTrue();
    token.ReplacedByToken.Should().NotBeNull();
}
```

# QuoteOwnerHandlerTests.cs
```csharp
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
```