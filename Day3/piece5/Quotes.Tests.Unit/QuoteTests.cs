using FluentAssertions;
using QuotesApi.Models;
using Xunit;

namespace Quotes.Tests.Unit;

public class QuoteTests
{
    [Fact]
    public void Create_ValidAuthorAndText_ReturnsSuccessWithValues()
    {
        var result = Quote.Create("Mark Twain", "The secret of getting ahead is getting started.");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Author.Should().Be("Mark Twain");
        result.Value.Text.Should().Be("The secret of getting ahead is getting started.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_AuthorNullOrWhiteSpace_ReturnsFailure(string? author)
    {
        var result = Quote.Create(author, "Some valid text");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Author must be between 1 and 200 characters.");
    }

    [Fact]
    public void Create_Author201Chars_ReturnsFailure()
    {
        var author = new string('A', 201);

        var result = Quote.Create(author, "Some valid text");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Author must be between 1 and 200 characters.");
    }

    [Fact]
    public void Create_Author200Chars_ReturnsSuccess()
    {
        var author = new string('A', 200);

        var result = Quote.Create(author, "Some valid text");

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_TextNullOrWhiteSpace_ReturnsFailure(string? text)
    {
        var result = Quote.Create("Valid Author", text);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Text must be between 1 and 1000 characters.");
    }

    [Fact]
    public void Create_Text1001Chars_ReturnsFailure()
    {
        var text = new string('T', 1001);

        var result = Quote.Create("Valid Author", text);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Text must be between 1 and 1000 characters.");
    }

    [Fact]
    public void Create_Text1000Chars_ReturnsSuccess()
    {
        var text = new string('T', 1000);

        var result = Quote.Create("Valid Author", text);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_WithOwnerId_SetsOwnerIdOnQuote()
    {
        var result = Quote.Create("Author", "Text", "owner-42");

        result.Value!.OwnerId.Should().Be("owner-42");
    }

    [Fact]
    public void Create_WithoutOwnerId_DefaultsToEmptyString()
    {
        var result = Quote.Create("Author", "Text");

        result.Value!.OwnerId.Should().Be(string.Empty);
    }

    [Fact]
    public void SoftDelete_OnActiveQuote_SetsIsDeletedToTrue()
    {
        var quote = Quote.Create("Author", "Text").Value!;

        quote.SoftDelete();

        quote.IsDeleted.Should().BeTrue();
    }
}
