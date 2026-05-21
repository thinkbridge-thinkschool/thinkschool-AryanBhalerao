namespace QuotesApi.Models;

public class Quote
{
    public int Id { get; private set; }
    public string Author { get; private set; } = string.Empty;
    public string Text { get; private set; } = string.Empty;
    public string OwnerId { get; private set; } = string.Empty;
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Quote() { }

    public static DomainResult<Quote> Create(string? author, string? text, string ownerId = "")
    {
        if (string.IsNullOrWhiteSpace(author) || author.Length > 200)
            return DomainResult<Quote>.Fail("Author must be between 1 and 200 characters.");

        if (string.IsNullOrWhiteSpace(text) || text.Length > 1000)
            return DomainResult<Quote>.Fail("Text must be between 1 and 1000 characters.");

        return DomainResult<Quote>.Ok(new Quote
        {
            Author = author,
            Text = text,
            OwnerId = ownerId,
            CreatedAt = DateTime.UtcNow,
        });
    }

    public void SoftDelete() => IsDeleted = true;
}

public record CreateQuoteDto(string? Author, string? Text);
