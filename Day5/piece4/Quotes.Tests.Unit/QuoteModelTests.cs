using FluentAssertions;
using QuotesApi.Models;

namespace Quotes.Tests.Unit;

public class QuoteModelTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ValidInputs_ReturnsQuote()
    {
        var (quote, errors) = Quote.Create("Aristotle", "Excellence is a habit.", 1, Now);
        quote.Should().NotBeNull();
        errors.Should().BeNull();
        quote!.Author.Should().Be("Aristotle");
        quote.Text.Should().Be("Excellence is a habit.");
        quote.OwnerId.Should().Be(1);
        quote.CreatedAt.Should().Be(Now);
    }

    [Fact]
    public void Create_EmptyAuthor_ReturnsErrors()
    {
        var (quote, errors) = Quote.Create("", "Some text", null, Now);
        quote.Should().BeNull();
        errors.Should().ContainKey("author");
    }

    [Fact]
    public void Create_WhitespaceAuthor_ReturnsErrors()
    {
        var (quote, errors) = Quote.Create("   ", "Some text", null, Now);
        quote.Should().BeNull();
        errors.Should().ContainKey("author");
    }

    [Fact]
    public void Create_AuthorTooLong_ReturnsErrors()
    {
        var longAuthor = new string('A', Quote.AuthorMaxLength + 1);
        var (quote, errors) = Quote.Create(longAuthor, "Text", null, Now);
        quote.Should().BeNull();
        errors.Should().ContainKey("author");
    }

    [Fact]
    public void Create_EmptyText_ReturnsErrors()
    {
        var (quote, errors) = Quote.Create("Author", "", null, Now);
        quote.Should().BeNull();
        errors.Should().ContainKey("text");
    }

    [Fact]
    public void Create_TextTooLong_ReturnsErrors()
    {
        var longText = new string('X', Quote.TextMaxLength + 1);
        var (quote, errors) = Quote.Create("Author", longText, null, Now);
        quote.Should().BeNull();
        errors.Should().ContainKey("text");
    }

    [Fact]
    public void Create_BothInvalid_ReturnsBothErrors()
    {
        var (quote, errors) = Quote.Create("", "", null, Now);
        quote.Should().BeNull();
        errors.Should().ContainKeys("author", "text");
    }

    [Fact]
    public void Create_NullOwnerId_IsAllowed()
    {
        var (quote, errors) = Quote.Create("Author", "Text", null, Now);
        quote.Should().NotBeNull();
        quote!.OwnerId.Should().BeNull();
    }
}
