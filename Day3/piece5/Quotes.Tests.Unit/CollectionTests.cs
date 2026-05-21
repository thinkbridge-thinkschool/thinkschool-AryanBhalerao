using FluentAssertions;
using QuotesApi.Models;
using Xunit;

namespace Quotes.Tests.Unit;

public class CollectionTests
{
    [Fact]
    public void Create_ValidName_ReturnsCollectionWithNameAndOwnerId()
    {
        var collection = Collection.Create("My Quotes", "user-1");

        collection.Name.Should().Be("My Quotes");
        collection.OwnerId.Should().Be("user-1");
        collection.Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_NullOrWhitespaceName_ThrowsArgumentException(string? name)
    {
        var act = () => Collection.Create(name!, "user-1");

        act.Should().Throw<ArgumentException>()
           .WithMessage("Name must be between 3 and 80 characters.");
    }

    [Theory]
    [InlineData("a")]
    [InlineData("ab")]
    public void Create_NameTooShort_ThrowsArgumentException(string name)
    {
        var act = () => Collection.Create(name, "user-1");

        act.Should().Throw<ArgumentException>()
           .WithMessage("Name must be between 3 and 80 characters.");
    }

    [Fact]
    public void Create_Name81Chars_ThrowsArgumentException()
    {
        var name = new string('A', 81);

        var act = () => Collection.Create(name, "user-1");

        act.Should().Throw<ArgumentException>()
           .WithMessage("Name must be between 3 and 80 characters.");
    }

    [Fact]
    public void Create_NameExactly3Chars_ReturnsCollection()
    {
        var collection = Collection.Create("abc", "user-1");

        collection.Name.Should().Be("abc");
    }

    [Fact]
    public void Create_NameExactly80Chars_ReturnsCollection()
    {
        var name = new string('A', 80);

        var collection = Collection.Create(name, "user-1");

        collection.Name.Should().Be(name);
    }

    [Fact]
    public void AddItem_NewQuoteId_AddsToItems()
    {
        var collection = Collection.Create("My Quotes", "user-1");

        collection.AddItem(42);

        collection.Items.Should().HaveCount(1);
        collection.Items[0].QuoteId.Should().Be(42);
    }

    [Fact]
    public void AddItem_DuplicateQuoteId_ThrowsInvalidOperationException()
    {
        var collection = Collection.Create("My Quotes", "user-1");
        collection.AddItem(10);

        var act = () => collection.AddItem(10);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Quote 10 is already in this collection.");
    }

    [Fact]
    public void AddItem_WhenAt50Items_ThrowsInvalidOperationException()
    {
        var collection = Collection.Create("My Quotes", "user-1");
        for (var i = 1; i <= 50; i++)
            collection.AddItem(i);

        var act = () => collection.AddItem(51);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Collection cannot exceed 50 items.");
    }

    [Fact]
    public void RemoveItem_ExistingQuoteId_RemovesFromItems()
    {
        var collection = Collection.Create("My Quotes", "user-1");
        collection.AddItem(7);

        collection.RemoveItem(7);

        collection.Items.Should().BeEmpty();
    }

    [Fact]
    public void RemoveItem_NonExistentQuoteId_ThrowsInvalidOperationException()
    {
        var collection = Collection.Create("My Quotes", "user-1");

        var act = () => collection.RemoveItem(999);

        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Quote 999 is not in this collection.");
    }
}
