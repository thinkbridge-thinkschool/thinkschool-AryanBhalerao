namespace QuotesApi.Queries;

public interface IQuoteMetadataQueryService
{
    Task<List<QuoteMetadataReadModel>> GetPagedAsync(int page, int size, CancellationToken ct);
}
