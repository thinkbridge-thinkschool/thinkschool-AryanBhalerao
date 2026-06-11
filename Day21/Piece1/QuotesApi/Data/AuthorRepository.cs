using Microsoft.EntityFrameworkCore;
using QuotesApi.Repositories;

namespace QuotesApi.Data;

public class AuthorRepository : IAuthorRepository
{
    private readonly AppDbContext _db;

    public AuthorRepository(AppDbContext db) => _db = db;

    // Group by author, project read-model
    public async Task<List<AuthorWithQuotesDto>> GetAllWithQuotesAsync(CancellationToken ct)
    {
        var rows = await _db.Quotes
            .AsNoTracking()
            .GroupBy(q => q.Author)
            .Select(g => new
            {
                AuthorName = g.Key,
                Quotes = g
                    .OrderByDescending(q => q.CreatedAt)
                    .Select(q => new QuoteSummaryDto(q.Id, q.Text, q.CreatedAt))
                    .ToList()
            })
            .OrderBy(x => x.AuthorName)
            .ToListAsync(ct);

        return rows
            .Select(a => new AuthorWithQuotesDto(a.AuthorName, a.Quotes.Count, a.Quotes))
            .ToList();
    }
}
