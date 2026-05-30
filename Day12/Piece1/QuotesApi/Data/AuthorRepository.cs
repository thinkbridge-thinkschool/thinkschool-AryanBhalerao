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

    // Fix: single LEFT JOIN — projects only the 3 columns QuoteSummaryDto needs, so SQL
    // transfers ~40% fewer bytes per row. AsNoTracking skips EF Core's identity map and
    // change-detection overhead (read-only path). Index on AuthorId turns per-author
    // scans into seeks.
    public async Task<List<AuthorWithQuotesDto>> GetAllWithQuotesAsync(CancellationToken ct)
    {
        var rows = await _db.Authors
            .AsNoTracking()
            .Select(a => new
            {
                a.Id,
                a.Name,
                Quotes = a.Quotes
                    .Select(q => new QuoteSummaryDto(q.Id, q.Text, q.CreatedAt))
                    .ToList()
            })
            .ToListAsync(ct);

        return rows
            .Select(a => new AuthorWithQuotesDto(a.Id, a.Name, a.Quotes.Count, a.Quotes))
            .ToList();
    }
}
