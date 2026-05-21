namespace QuotesApi.Models;

public class Collection
{
    private readonly List<CollectionItem> _items = new();

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string OwnerId { get; private set; } = string.Empty;
    public IReadOnlyList<CollectionItem> Items => _items.AsReadOnly();

    private Collection() { }

    public static Collection Create(string name, string ownerId)
    {
        ValidateName(name);
        return new Collection { Name = name, OwnerId = ownerId };
    }

    public void AddItem(int quoteId)
    {
        if (_items.Count >= 50)
            throw new InvalidOperationException("Collection cannot exceed 50 items.");
        if (_items.Any(i => i.QuoteId == quoteId))
            throw new InvalidOperationException($"Quote {quoteId} is already in this collection.");
        _items.Add(CollectionItem.Create(quoteId));
    }

    public void RemoveItem(int quoteId)
    {
        var item = _items.FirstOrDefault(i => i.QuoteId == quoteId);
        if (item is null)
            throw new InvalidOperationException($"Quote {quoteId} is not in this collection.");
        _items.Remove(item);
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 3 || name.Length > 80)
            throw new ArgumentException("Name must be between 3 and 80 characters.");
    }
}

public record CreateCollectionDto(string Name, string OwnerId);
public record AddItemDto(int QuoteId);
