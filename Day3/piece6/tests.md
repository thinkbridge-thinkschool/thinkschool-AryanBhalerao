# Integration Tests — representative samples

## Happy path — `PostQuote_WithScopeClaim.Returns201WithCreatedQuote`

```csharp
public class PostQuote_WithScopeClaim : IntegrationTestBase
{
    [Fact]
    public async Task Returns201WithCreatedQuote()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/quotes");
        request.Headers.Add("X-Test-Claims", "sub=42,scope=quotes.write");
        request.Content = JsonContent.Create(new
        {
            Author = "Marcus Aurelius",
            Text   = "The impediment to action advances action."
        });

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("author").GetString().Should().Be("Marcus Aurelius");
        body.GetProperty("ownerId").GetString().Should().Be("42");
        response.Headers.Location.Should().NotBeNull();
    }
}
```

## Error path — `PostQuote_WithEmptyAuthor.ReturnsProblemDetails`

```csharp
public class PostQuote_WithEmptyAuthor : IntegrationTestBase
{
    [Fact]
    public async Task ReturnsProblemDetails()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/quotes");
        request.Headers.Add("X-Test-Claims", "sub=1,scope=quotes.write");
        request.Content = JsonContent.Create(new { Author = "", Text = "Some text here." });

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Contain("problem+json");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("detail").GetString().Should()
            .Contain("Author must be between 1 and 200 characters");
    }
}
```
