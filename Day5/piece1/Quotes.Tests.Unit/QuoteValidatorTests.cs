using FluentAssertions;
using QuotesApi.Models;
using QuotesApi.Services;

namespace Quotes.Tests.Unit;

public class QuoteValidatorTests
{
    private readonly QuoteValidator _sut = new();

    [Fact]
    public void Validate_ValidRequest_ReturnsNoErrors()
    {
        var errors = _sut.Validate(new CreateQuoteRequest { Author = "Plato", Text = "Know thyself." });
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_EmptyAuthor_ReturnsAuthorError()
    {
        var errors = _sut.Validate(new CreateQuoteRequest { Author = "", Text = "Some text" });
        errors.Should().ContainKey("author");
    }

    [Fact]
    public void Validate_WhitespaceAuthor_ReturnsAuthorError()
    {
        var errors = _sut.Validate(new CreateQuoteRequest { Author = "   ", Text = "Some text" });
        errors.Should().ContainKey("author");
    }

    [Fact]
    public void Validate_EmptyText_ReturnsTextError()
    {
        var errors = _sut.Validate(new CreateQuoteRequest { Author = "Author", Text = "" });
        errors.Should().ContainKey("text");
    }

    [Fact]
    public void Validate_WhitespaceText_ReturnsTextError()
    {
        var errors = _sut.Validate(new CreateQuoteRequest { Author = "Author", Text = "\t" });
        errors.Should().ContainKey("text");
    }

    [Fact]
    public void Validate_BothEmpty_ReturnsBothErrors()
    {
        var errors = _sut.Validate(new CreateQuoteRequest { Author = "", Text = "" });
        errors.Should().ContainKeys("author", "text");
    }
}
