using Microsoft.EntityFrameworkCore;
using QuotesApi.Data;

namespace QuotesApi.Queries;

public class QuoteQueryService : IQuoteQueryService
{
    private readonly AppDbContext _db;

    public QuoteQueryService(AppDbContext db) => _db = db;

    public async Task<List<QuoteReadModel>> GetPagedAsync(int page, int size, CancellationToken ct)
    {
        return await _db.Quotes
            .OrderByDescending(q => q.CreatedAt)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(q => new QuoteReadModel(q.Id, q.Author, q.Text, q.CreatedAt))
            .ToListAsync(ct);
    }

    public async Task<QuoteReadModel?> GetByIdAsync(int id, CancellationToken ct)
    {
        return await _db.Quotes
            .Where(q => q.Id == id)
            .Select(q => new QuoteReadModel(q.Id, q.Author, q.Text, q.CreatedAt))
            .FirstOrDefaultAsync(ct);
    }
}
