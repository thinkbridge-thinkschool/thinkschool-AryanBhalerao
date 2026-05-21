namespace QuotesApi.Models;

public sealed class CollectionItem
{
    public int QuoteId { get; private set; }
    public DateTime AddedAt { get; private set; }

    private CollectionItem() { }

    public static CollectionItem Create(int quoteId) =>
        new() { QuoteId = quoteId, AddedAt = DateTime.UtcNow };
}
