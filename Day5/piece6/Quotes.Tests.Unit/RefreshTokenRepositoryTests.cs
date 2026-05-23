using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using QuotesApi.Data;
using QuotesApi.Models;
using QuotesApi.Services;

namespace Quotes.Tests.Unit;

public class RefreshTokenRepositoryTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly IClock _clock;
    private readonly RefreshTokenRepository _sut;
    private readonly DateTimeOffset _now = new(2026, 1, 15, 0, 0, 0, TimeSpan.Zero);

    public RefreshTokenRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new AppDbContext(options);
        _clock = Substitute.For<IClock>();
        _clock.UtcNow.Returns(_now);
        _sut = new RefreshTokenRepository(_db, _clock);
    }

    public void Dispose() => _db.Dispose();

    private async Task<RefreshToken> AddToken(string hash, string family, bool revoked = false)
    {
        var t = new RefreshToken
        {
            TokenHash = hash,
            UserId = 1,
            FamilyId = family,
            ExpiresAt = _now.AddDays(7),
            RevokedAt = revoked ? _now.AddHours(-1) : null,
        };
        _db.RefreshTokens.Add(t);
        await _db.SaveChangesAsync();
        return t;
    }

    [Fact]
    public async Task FindByHash_ReturnsToken_WhenExists()
    {
        await AddToken("abc123", "family-1");
        var result = await _sut.FindByHashAsync("abc123");
        result.Should().NotBeNull();
        result!.TokenHash.Should().Be("abc123");
    }

    [Fact]
    public async Task FindByHash_ReturnsNull_WhenNotFound()
    {
        var result = await _sut.FindByHashAsync("no-such-hash");
        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_PersistsToken()
    {
        var token = new RefreshToken
        {
            TokenHash = "newhash",
            UserId = 1,
            FamilyId = "fam",
            ExpiresAt = _now.AddDays(7),
        };
        await _sut.AddAsync(token);

        var saved = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == "newhash");
        saved.Should().NotBeNull();
    }

    [Fact]
    public async Task RevokeToken_SetsRevokedAtAndReplacedBy()
    {
        var token = await AddToken("tok-a", "fam-a");
        await _sut.RevokeTokenAsync(token, "tok-b-hash");

        token.RevokedAt.Should().Be(_now);
        token.ReplacedByToken.Should().Be("tok-b-hash");
    }

    [Fact]
    public async Task RevokeToken_WithNullReplacement_SetsRevokedAtOnly()
    {
        var token = await AddToken("tok-logout", "fam-logout");
        await _sut.RevokeTokenAsync(token, replacedByHash: null);

        token.RevokedAt.Should().Be(_now);
        token.ReplacedByToken.Should().BeNull();
    }

    [Fact]
    public async Task RevokeFamily_RevokesAllActiveTokensInFamily()
    {
        const string family = "shared-family";
        await AddToken("t1", family);
        await AddToken("t2", family);
        await AddToken("t3-already-revoked", family, revoked: true);

        await _sut.RevokeFamilyAsync(family);

        var active = await _db.RefreshTokens
            .Where(t => t.FamilyId == family && t.RevokedAt == null)
            .CountAsync();
        active.Should().Be(0);
    }

    [Fact]
    public async Task RevokeFamily_DoesNotAffectOtherFamilies()
    {
        await AddToken("other-tok", "other-family");
        await AddToken("target-tok", "target-family");

        await _sut.RevokeFamilyAsync("target-family");

        var otherToken = await _db.RefreshTokens.FirstAsync(t => t.TokenHash == "other-tok");
        otherToken.RevokedAt.Should().BeNull();
    }
}
