using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using QuotesApi.Data;
using QuotesApi.Models;
using QuotesApi.Services;

namespace Quotes.Tests.Unit;

public class QuoteRepositoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly IClock _clock;
    private readonly QuoteRepository _sut;
    private readonly DateTimeOffset _now = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    public QuoteRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _clock = Substitute.For<IClock>();
        _clock.UtcNow.Returns(_now);
        _sut = new QuoteRepository(_db, _clock);
    }

    public void Dispose() => _db.Dispose();

    private async Task<Quote> AddQuote(int? ownerId = null)
    {
        var q = new Quote
        {
            Author = "Test Author",
            Text = "Test text",
            OwnerId = ownerId,
            CreatedAt = _now.AddDays(-1),
        };
        _db.Quotes.Add(q);
        await _db.SaveChangesAsync();
        return q;
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsPage()
    {
        await AddQuote();
        await AddQuote();
        await AddQuote();

        var result = await _sut.GetPagedAsync(page: 1, size: 2, CancellationToken.None);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPagedAsync_Page2_SkipsFirst()
    {
        await AddQuote();
        await AddQuote();
        await AddQuote();

        var page1 = await _sut.GetPagedAsync(page: 1, size: 2, CancellationToken.None);
        var page2 = await _sut.GetPagedAsync(page: 2, size: 2, CancellationToken.None);

        page1.Select(q => q.Id).Intersect(page2.Select(q => q.Id)).Should().BeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsQuote_WhenExists()
    {
        var q = await AddQuote();
        var result = await _sut.GetByIdAsync(q.Id, CancellationToken.None);
        result.Should().NotBeNull();
        result!.Id.Should().Be(q.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        var result = await _sut.GetByIdAsync(99999, CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_PersistsAndSetsCreatedAt()
    {
        var input = new Quote { Author = "Author", Text = "Text", OwnerId = 1 };
        var created = await _sut.CreateAsync(input, CancellationToken.None);

        created.Id.Should().BeGreaterThan(0);
        created.CreatedAt.Should().Be(_now);
        (await _db.Quotes.FindAsync(created.Id)).Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteAsync_RemovesQuote_ReturnsTrue()
    {
        var q = await AddQuote();
        var result = await _sut.DeleteAsync(q.Id, CancellationToken.None);
        result.Should().BeTrue();
        (await _db.Quotes.FindAsync(q.Id)).Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenNotFound()
    {
        var result = await _sut.DeleteAsync(99999, CancellationToken.None);
        result.Should().BeFalse();
    }
}
