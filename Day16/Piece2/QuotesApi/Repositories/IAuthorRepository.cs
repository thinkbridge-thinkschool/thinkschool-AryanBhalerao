namespace QuotesApi.Repositories;

public record QuoteSummaryDto(int Id, string Text, DateTimeOffset CreatedAt);
public record AuthorWithQuotesDto(string Name, int QuoteCount, List<QuoteSummaryDto> Quotes);

public interface IAuthorRepository
{
    Task<List<AuthorWithQuotesDto>> GetAllWithQuotesAsync(CancellationToken cancellationToken);
}
