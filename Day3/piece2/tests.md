# Tests

## QuotesApi.Tests/PolicyTests.cs

```csharp
[Fact]
public async Task PostQuote_AuthenticatedWithoutScopeClaim_Returns403()
{
    var request = new HttpRequestMessage(HttpMethod.Post, "/api/quotes");
    request.Headers.Add("X-Test-Claims", "sub=1");
    request.Content = JsonContent.Create(new { Author = "Seneca", Text = "Per aspera ad astra." });

    var response = await _client.SendAsync(request);

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}

[Fact]
public async Task DeleteQuote_ByNonOwner_Returns403()
{
    var create = new HttpRequestMessage(HttpMethod.Post, "/api/quotes");
    create.Headers.Add("X-Test-Claims", "sub=1,scope=quotes.write");
    create.Content = JsonContent.Create(new { Author = "Twain", Text = "Classic quote." });
    var createResp = await _client.SendAsync(create);
    createResp.EnsureSuccessStatusCode();
    var body = await createResp.Content.ReadFromJsonAsync<JsonElement>();
    int id = body.GetProperty("id").GetInt32();

    var delete = new HttpRequestMessage(HttpMethod.Delete, $"/api/quotes/{id}");
    delete.Headers.Add("X-Test-Claims", "sub=2,scope=quotes.write");
    var response = await _client.SendAsync(delete);

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
}
```

## Test output

```
dotnet test QuotesApi.Tests/QuotesApi.Tests.csproj
```
```
Passed  PostQuote_AuthenticatedWithoutScopeClaim_Returns403
Passed  DeleteQuote_ByNonOwner_Returns403

Test Run Successful.
Total tests: 2
     Passed: 2
 Total time: 1.234 s
```
