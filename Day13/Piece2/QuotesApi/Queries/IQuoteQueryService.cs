namespace QuotesApi.Queries;

public interface IQuoteQueryService
{
    Task<List<QuoteReadModel>> GetPagedAsync(int page, int size, CancellationToken ct);
    Task<QuoteReadModel?> GetByIdAsync(int id, CancellationToken ct);
}
