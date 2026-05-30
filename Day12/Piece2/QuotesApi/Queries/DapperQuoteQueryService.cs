using System.Data;
using Dapper;
using Microsoft.EntityFrameworkCore;
using QuotesApi.Data;

namespace QuotesApi.Queries;

public class DapperQuoteQueryService : IQuoteQueryService
{
    private readonly AppDbContext _db;

    public DapperQuoteQueryService(AppDbContext db) => _db = db;

    public async Task<List<QuoteReadModel>> GetPagedAsync(int page, int size, CancellationToken ct)
    {
        var conn = _db.Database.GetDbConnection();
        var results = await conn.QueryAsync<QuoteReadModel>(
            new CommandDefinition(
                """
                SELECT Id, Author AS AuthorName, Text, CreatedAt
                FROM   Quotes
                ORDER  BY CreatedAt DESC
                OFFSET @Offset ROWS FETCH NEXT @Size ROWS ONLY
                """,
                new { Offset = (page - 1) * size, Size = size },
                cancellationToken: ct));

        return results.ToList();
    }

    public async Task<QuoteReadModel?> GetByIdAsync(int id, CancellationToken ct)
    {
        var conn = _db.Database.GetDbConnection();
        return await conn.QueryFirstOrDefaultAsync<QuoteReadModel>(
            new CommandDefinition(
                "SELECT Id, Author AS AuthorName, Text, CreatedAt FROM Quotes WHERE Id = @Id",
                new { Id = id },
                cancellationToken: ct));
    }
}
