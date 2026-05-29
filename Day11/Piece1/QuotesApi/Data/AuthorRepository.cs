using Microsoft.EntityFrameworkCore;
using QuotesApi.Repositories;

namespace QuotesApi.Data;

public class AuthorRepository : IAuthorRepository
{
    private readonly AppDbContext _db;

    public AuthorRepository(AppDbContext db) => _db = db;

    // N+1: fetches all authors in one query, then issues a separate SELECT per author
    // to load their quotes. With no index on Quotes.AuthorId each per-author query
    // full-scans the entire Quotes table.
    public async Task<List<AuthorWithQuotesDto>> GetAllWithQuotesSlowAsync(CancellationToken ct)
    {
        // Query 1 — load all authors
        var authors = await _db.Authors.ToListAsync(ct);

        var result = new List<AuthorWithQuotesDto>(authors.Count);

        foreach (var author in authors)
        {
            // Query N — one full-table-scan of Quotes per author (no index on AuthorId)
            var quotes = await _db.Quotes
                .Where(q => q.AuthorId == author.Id)
                .ToListAsync(ct);

            result.Add(new AuthorWithQuotesDto(
                author.Id,
                author.Name,
                quotes.Count,
                quotes.Select(q => new QuoteSummaryDto(q.Id, q.Text, q.CreatedAt)).ToList()
            ));
        }

        return result;
    }
}
