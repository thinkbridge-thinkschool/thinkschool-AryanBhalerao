using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using QuotesApi.Options;
using QuotesApi.Services;

namespace QuotesApi.Queries;

public sealed class CachedQuoteQueryService : IQuoteQueryService
{
    private readonly EfCoreQuoteQueryService _inner;
    private readonly HybridCache _cache;
    private readonly ReadStats _stats;
    private readonly QuoteCacheOptions _options;
    private readonly HybridCacheEntryOptions _entryOptions;

    public CachedQuoteQueryService(
        EfCoreQuoteQueryService inner,
        HybridCache cache,
        ReadStats stats,
        IOptions<QuoteCacheOptions> options)
    {
        _inner = inner;
        _cache = cache;
        _stats = stats;
        _options = options.Value;
        _entryOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromSeconds(_options.L2ExpirationSeconds),
            LocalCacheExpiration = TimeSpan.FromSeconds(_options.L1ExpirationSeconds)
        };
    }

    // Paged list left uncached
    public Task<List<QuoteReadModel>> GetPagedAsync(int page, int size, CancellationToken ct)
        => _inner.GetPagedAsync(page, size, ct);

    public async Task<QuoteReadModel?> GetByIdAsync(int id, CancellationToken ct)
    {
        _stats.RecordRequest();

        if (!_options.Enabled)
            return await LoadFromDbAsync(id, ct);

        return await _cache.GetOrCreateAsync(
            $"quote:{id}",
            (id, this),
            static (state, token) => state.Item2.LoadFromDbAsync(state.id, token),
            _entryOptions,
            tags: ["quotes"],
            cancellationToken: ct);
    }

    // Expensive DB read
    private async ValueTask<QuoteReadModel?> LoadFromDbAsync(int id, CancellationToken ct)
    {
        _stats.RecordDbRead();
        if (_options.SimulatedDbLatencyMs > 0)
            await Task.Delay(_options.SimulatedDbLatencyMs, ct);
        return await _inner.GetByIdAsync(id, ct);
    }
}
