namespace QuotesApi.Options;

public record AzureAdOptions
{
    public string TenantId { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
}
