namespace QuotesApi.Queries;

public record QuoteMetadataReadModel(
    int QuoteId,
    string Quote,
    string Author,
    string User,
    DateTimeOffset CreatedAt,
    List<string> Tags,
    List<string> Categories);
