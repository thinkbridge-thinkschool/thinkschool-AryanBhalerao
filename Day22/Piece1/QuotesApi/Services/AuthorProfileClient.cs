using System.Net.Http.Json;

namespace QuotesApi.Services;

public sealed record AuthorProfile(string Author, string Bio, int QuoteCount, string Source);

// Typed client for the external author-profile service
public sealed class AuthorProfileClient
{
    private readonly HttpClient _http;

    public AuthorProfileClient(HttpClient http) => _http = http;

    public async Task<AuthorProfile?> GetProfileAsync(string name, CancellationToken ct)
    {
        var response = await _http.GetAsync($"/downstream/author-profile/{Uri.EscapeDataString(name)}", ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AuthorProfile>(ct);
    }

    public async Task<bool> RefreshProfileAsync(string name, CancellationToken ct)
    {
        var response = await _http.PostAsync($"/downstream/author-profile/{Uri.EscapeDataString(name)}/refresh", null, ct);
        return response.IsSuccessStatusCode;
    }
}
