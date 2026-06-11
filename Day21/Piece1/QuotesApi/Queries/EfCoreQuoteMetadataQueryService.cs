using Microsoft.EntityFrameworkCore;
using QuotesApi.Data;

namespace QuotesApi.Queries;

public class EfCoreQuoteMetadataQueryService : IQuoteMetadataQueryService
{
    private readonly AppDbContext _db;

    public EfCoreQuoteMetadataQueryService(AppDbContext db) => _db = db;

    public async Task<List<QuoteMetadataReadModel>> GetPagedAsync(int page, int size, CancellationToken ct)
    {
        // Split query avoids cartesian product
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
                q.Author,
                q.Owner?.Email ?? "anonymous",
                q.CreatedAt,
                q.Tags.Select(t => t.Name).OrderBy(n => n).ToList(),
                q.Categories.Select(c => c.Name).OrderBy(n => n).ToList()))
            .ToList();
    }
}
