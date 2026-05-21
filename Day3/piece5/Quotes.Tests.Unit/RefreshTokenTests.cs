using FluentAssertions;
using QuotesApi.Models;
using Xunit;

namespace Quotes.Tests.Unit;

public class RefreshTokenTests
{
    [Fact]
    public void Create_ValidInputs_SetsAllProperties()
    {
        var expiresAt = DateTime.UtcNow.AddDays(7);

        var token = RefreshToken.Create("hash-abc", 42, "family-xyz", expiresAt);

        token.Token.Should().Be("hash-abc");
        token.UserId.Should().Be(42);
        token.Family.Should().Be("family-xyz");
        token.ExpiresAt.Should().Be(expiresAt);
    }

    [Fact]
    public void IsRevoked_NewToken_ReturnsFalse()
    {
        var token = RefreshToken.Create("hash", 1, "family", DateTime.UtcNow.AddDays(7));

        token.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public void Revoke_WhenCalled_SetsIsRevokedTrue()
    {
        var token = RefreshToken.Create("hash", 1, "family", DateTime.UtcNow.AddDays(7));

        token.Revoke();

        token.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public void Revoke_WithSuccessorHash_SetsReplacedByToken()
    {
        var token = RefreshToken.Create("old-hash", 1, "family", DateTime.UtcNow.AddDays(7));

        token.Revoke("new-hash");

        token.ReplacedByToken.Should().Be("new-hash");
    }

    [Fact]
    public void Revoke_WithoutSuccessorHash_ReplacedByTokenIsNull()
    {
        var token = RefreshToken.Create("hash", 1, "family", DateTime.UtcNow.AddDays(7));

        token.Revoke();

        token.ReplacedByToken.Should().BeNull();
    }

    [Fact]
    public void IsActive_NotRevokedAndFutureExpiry_ReturnsTrue()
    {
        var token = RefreshToken.Create("hash", 1, "family", DateTime.UtcNow.AddHours(1));

        token.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_ExpiredToken_ReturnsFalse()
    {
        var token = RefreshToken.Create("hash", 1, "family", DateTime.UtcNow.AddSeconds(-1));

        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_AfterRevoke_ReturnsFalse()
    {
        var token = RefreshToken.Create("hash", 1, "family", DateTime.UtcNow.AddHours(1));
        token.Revoke();

        token.IsActive.Should().BeFalse();
    }

    // Verifies the state the refresh endpoint reads to decide whether to revoke the entire family:
    // existing.IsRevoked == true && existing.ReplacedByToken != null  →  reuse detected
    [Fact]
    public void Revoke_WithReplacedByToken_ExposesReuseDetectionSignal()
    {
        var token = RefreshToken.Create("old-hash", 1, "family-A", DateTime.UtcNow.AddDays(7));

        token.Revoke("successor-hash");

        token.IsRevoked.Should().BeTrue();
        token.ReplacedByToken.Should().NotBeNull();
    }
}
