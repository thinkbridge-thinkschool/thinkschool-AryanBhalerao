using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Quotes.Tests.Integration.Tests;

// ── GET /api/collections/{id} ─────────────────────────────────────────────────

public class GetCollection_WhenNotFound : IntegrationTestBase
{
    [Fact]
    public async Task Returns404()
    {
        var response = await Client.GetAsync("/api/collections/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}

// ── POST /api/collections ─────────────────────────────────────────────────────

public class PostCollection_WithValidData : IntegrationTestBase
{
    [Fact]
    public async Task Returns201WithCreatedCollection()
    {
        var response = await Client.PostAsJsonAsync("/api/collections",
            new { Name = "My Stoic Reads", OwnerId = "user-1" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("name").GetString().Should().Be("My Stoic Reads");
        response.Headers.Location.Should().NotBeNull();
    }
}

public class PostCollection_WithMissingName : IntegrationTestBase
{
    [Fact]
    public async Task ReturnsValidationProblem()
    {
        var response = await Client.PostAsJsonAsync("/api/collections",
            new { Name = "", OwnerId = "user-1" });

        // Results.ValidationProblem returns 400 with application/problem+json
        ((int)response.StatusCode).Should().BeInRange(400, 422);
        response.Content.Headers.ContentType?.MediaType.Should().Contain("problem+json");
    }
}

// ── POST /api/collections/{id}/items ─────────────────────────────────────────

public class AddItem_ToExistingCollection : IntegrationTestBase
{
    [Fact]
    public async Task Returns204()
    {
        // Create a collection
        var collResp = await Client.PostAsJsonAsync("/api/collections",
            new { Name = "Test Coll", OwnerId = "u1" });
        var coll = await collResp.Content.ReadFromJsonAsync<JsonElement>();
        int collId = coll.GetProperty("id").GetInt32();

        // Create a quote to add
        var qReq = new HttpRequestMessage(HttpMethod.Post, "/api/quotes");
        qReq.Headers.Add("X-Test-Claims", "sub=1,scope=quotes.write");
        qReq.Content = JsonContent.Create(new { Author = "Zeno", Text = "Well-being is attained by little and little." });
        var qResp  = await Client.SendAsync(qReq);
        var qBody  = await qResp.Content.ReadFromJsonAsync<JsonElement>();
        int quoteId = qBody.GetProperty("id").GetInt32();

        // Add quote to collection
        var response = await Client.PostAsJsonAsync(
            $"/api/collections/{collId}/items",
            new { QuoteId = quoteId });

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}

// ── DELETE /api/collections/{id} ─────────────────────────────────────────────

public class DeleteCollection_WhenExists : IntegrationTestBase
{
    [Fact]
    public async Task Returns204()
    {
        var collResp = await Client.PostAsJsonAsync("/api/collections",
            new { Name = "Temporary", OwnerId = "u99" });
        var coll = await collResp.Content.ReadFromJsonAsync<JsonElement>();
        int id = coll.GetProperty("id").GetInt32();

        var response = await Client.DeleteAsync($"/api/collections/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}

public class DeleteCollection_WhenNotFound : IntegrationTestBase
{
    [Fact]
    public async Task Returns404()
    {
        var response = await Client.DeleteAsync("/api/collections/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
