namespace QuotesApi.Repositories;

public record QuoteSummaryDto(int Id, string Text, DateTimeOffset CreatedAt);
public record AuthorWithQuotesDto(int AuthorId, string Name, int QuoteCount, List<QuoteSummaryDto> Quotes);

public interface IAuthorRepository
{
    Task<List<AuthorWithQuotesDto>> GetAllWithQuotesSlowAsync(CancellationToken cancellationToken);
    Task<List<AuthorWithQuotesDto>> GetAllWithQuotesAsync(CancellationToken cancellationToken);
}
