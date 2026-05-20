namespace QuotesApi.Models;

public class RefreshToken
{
    public int Id { get; private set; }
    public string Token { get; private set; } = null!;      // SHA-256 hash of the raw token
    public int UserId { get; private set; }
    public User User { get; private set; } = null!;
    public string Family { get; private set; } = null!;     // groups the rotation chain
    public DateTime ExpiresAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public string? ReplacedByToken { get; private set; }    // hash of successor token

    private RefreshToken() { }

    public static RefreshToken Create(string tokenHash, int userId, string family, DateTime expiresAt) =>
        new() { Token = tokenHash, UserId = userId, Family = family, ExpiresAt = expiresAt };

    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive => !IsRevoked && DateTime.UtcNow < ExpiresAt;

    public void Revoke(string? replacedByTokenHash = null)
    {
        RevokedAt = DateTime.UtcNow;
        ReplacedByToken = replacedByTokenHash;
    }
}
