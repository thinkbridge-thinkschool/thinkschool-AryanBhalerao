using FluentAssertions;
using QuotesApi.Models;
using Xunit;

namespace Tests.Domain;

public class CollectionTests
{
    [Fact]
    public void Create_WithEmptyName_ThrowsArgumentException()
    {
        var act = () => Collection.Create("", "owner1");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithNameOver80Chars_ThrowsArgumentException()
    {
        var act = () => Collection.Create(new string('a', 81), "owner1");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AddItem_51st_ThrowsInvalidOperationException()
    {
        var collection = Collection.Create("My Collection", "owner1");
        for (var i = 1; i <= 50; i++) collection.AddItem(i);

        var act = () => collection.AddItem(51);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddItem_DuplicateQuoteId_ThrowsInvalidOperationException()
    {
        var collection = Collection.Create("My Collection", "owner1");
        collection.AddItem(1);

        var act = () => collection.AddItem(1);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RemoveItem_NonExistent_ThrowsInvalidOperationException()
    {
        var collection = Collection.Create("My Collection", "owner1");

        var act = () => collection.RemoveItem(99);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AddThenRemoveItem_LeavesZeroItems()
    {
        var collection = Collection.Create("My Collection", "owner1");
        collection.AddItem(1);
        collection.RemoveItem(1);

        collection.Items.Should().BeEmpty();
    }
}
