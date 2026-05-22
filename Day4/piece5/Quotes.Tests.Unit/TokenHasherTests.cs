using FluentAssertions;
using QuotesApi.Services;

namespace Quotes.Tests.Unit;

public class TokenHasherTests
{
    [Fact]
    public void Hash_SameInput_ReturnsSameHash()
    {
        var hash1 = TokenHasher.Hash("my-token");
        var hash2 = TokenHasher.Hash("my-token");
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void Hash_DifferentInputs_ReturnDifferentHashes()
    {
        var h1 = TokenHasher.Hash("token-a");
        var h2 = TokenHasher.Hash("token-b");
        h1.Should().NotBe(h2);
    }

    [Fact]
    public void Hash_ReturnsLowercaseHex()
    {
        var hash = TokenHasher.Hash("anything");
        hash.Should().MatchRegex("^[0-9a-f]+$");
    }

    [Fact]
    public void Hash_Returns64CharSha256Hex()
    {
        // SHA-256 produces 32 bytes → 64 hex chars
        var hash = TokenHasher.Hash("some input");
        hash.Should().HaveLength(64);
    }
}
