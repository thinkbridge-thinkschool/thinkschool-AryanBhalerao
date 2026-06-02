using Microsoft.EntityFrameworkCore;
using QuotesApi.Data;

namespace QuotesApi.Queries;

public class EfCoreQuoteMetadataQueryService : IQuoteMetadataQueryService
{
    private readonly AppDbContext _db;

    public EfCoreQuoteMetadataQueryService(AppDbContext db) => _db = db;

    public async Task<List<QuoteMetadataReadModel>> GetPagedAsync(int page, int size, CancellationToken ct)
    {
        // AsSplitQuery avoids the cartesian product that arises when including
        // two independent collections (Tags and Categories) in a single JOIN.
        var quotes = await _db.Quotes
            .Include(q => q.Tags)
            .Include(q => q.Categories)
            .Include(q => q.Owner)
            .OrderByDescending(q => q.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .AsSplitQuery()
            .ToListAsync(ct);

        return quotes
            .Select(q => new QuoteMetadataReadModel(
                q.Id,
                q.Text,
                q.Owner?.Email ?? "anonymous",
                q.Tags.Select(t => t.Name).OrderBy(n => n).ToList(),
                q.Categories.Select(c => c.Name).OrderBy(n => n).ToList()))
            .ToList();
    }
}
