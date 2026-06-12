namespace QuotesApi.Queries;

public record QuoteReadModel(int Id, string AuthorName, string Text, DateTimeOffset CreatedAt);
