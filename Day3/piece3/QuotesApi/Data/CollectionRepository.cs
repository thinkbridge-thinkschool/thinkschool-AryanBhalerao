using Microsoft.EntityFrameworkCore;
using QuotesApi.Models;

namespace QuotesApi.Data;

public class CollectionRepository : ICollectionRepository
{
    private readonly QuoteDbContext _dbContext;
    private readonly ILogger<CollectionRepository> _logger;

    public CollectionRepository(QuoteDbContext dbContext, ILogger<CollectionRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Collection?> GetByIdAsync(int id, CancellationToken ct)
    {
        _logger.LogInformation("Fetching collection {Id}", id);
        return await _dbContext.Collections
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Collection> AddAsync(Collection collection, CancellationToken ct)
    {
        _logger.LogInformation("Adding collection '{Name}'", collection.Name);
        _dbContext.Collections.Add(collection);
        await _dbContext.SaveChangesAsync(ct);
        return collection;
    }

    public async Task UpdateAsync(Collection collection, CancellationToken ct)
    {
        _logger.LogInformation("Updating collection {Id}", collection.Id);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        _logger.LogInformation("Deleting collection {Id}", id);
        var collection = await _dbContext.Collections.FindAsync(new object[] { id }, ct);
        if (collection is null) return false;
        _dbContext.Collections.Remove(collection);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }
}
