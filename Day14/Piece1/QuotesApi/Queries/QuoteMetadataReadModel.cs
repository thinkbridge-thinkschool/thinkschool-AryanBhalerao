namespace QuotesApi.Queries;

public record QuoteMetadataReadModel(
    int QuoteId,
    string Quote,
    string User,
    List<string> Tags,
    List<string> Categories);
