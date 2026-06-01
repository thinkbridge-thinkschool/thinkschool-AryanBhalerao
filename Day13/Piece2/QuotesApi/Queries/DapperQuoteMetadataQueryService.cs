using Dapper;
using Microsoft.EntityFrameworkCore;
using QuotesApi.Data;

namespace QuotesApi.Queries;

public class DapperQuoteMetadataQueryService : IQuoteMetadataQueryService
{
    private readonly AppDbContext _db;

    public DapperQuoteMetadataQueryService(AppDbContext db) => _db = db;

    public async Task<List<QuoteMetadataReadModel>> GetPagedAsync(int page, int size, CancellationToken ct)
    {
        var conn = _db.Database.GetDbConnection();
        var rows = await conn.QueryAsync<FlatQuoteMetadataRow>(
            new CommandDefinition(
                """
                WITH PagedQuotes AS (
                    SELECT Id, [Text], OwnerId, CreatedAt
                    FROM Quotes
                    ORDER BY CreatedAt DESC
                    OFFSET @Offset ROWS FETCH NEXT @Size ROWS ONLY
                )
                SELECT
                    q.Id AS QuoteId,
                    q.[Text] AS Quote,
                    COALESCE(u.Email, 'anonymous') AS [User],
                    t.Name AS Tag,
                    c.Name AS Category
                FROM PagedQuotes q
                LEFT JOIN Users u ON u.Id = q.OwnerId
                LEFT JOIN QuoteTags qt ON qt.QuoteId = q.Id
                LEFT JOIN Tags t ON t.Id = qt.TagId
                LEFT JOIN QuoteCategories qc ON qc.QuoteId = q.Id
                LEFT JOIN Categories c ON c.Id = qc.CategoryId
                ORDER BY q.CreatedAt DESC, q.Id
                """,
                new { Offset = (page - 1) * size, Size = size },
                cancellationToken: ct));

        return rows
            .GroupBy(row => new { row.QuoteId, row.Quote, row.User })
            .Select(group => new QuoteMetadataReadModel(
                group.Key.QuoteId,
                group.Key.Quote,
                group.Key.User,
                group.Where(r => !string.IsNullOrWhiteSpace(r.Tag)).Select(r => r.Tag!).Distinct().Order().ToList(),
                group.Where(r => !string.IsNullOrWhiteSpace(r.Category)).Select(r => r.Category!).Distinct().Order().ToList()))
            .ToList();
    }

    private sealed class FlatQuoteMetadataRow
    {
        public int QuoteId { get; init; }
        public string Quote { get; init; } = string.Empty;
        public string User { get; init; } = string.Empty;
        public string? Tag { get; init; }
        public string? Category { get; init; }
    }
}
