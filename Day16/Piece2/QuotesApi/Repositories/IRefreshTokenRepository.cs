using QuotesApi.Models;

namespace QuotesApi.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken ct = default);
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    Task RevokeTokenAsync(RefreshToken token, string? replacedByHash, CancellationToken ct = default);

    // Revokes the old token and persists its replacement in a single SaveChanges,
    // so rotation can never leave the user with a revoked token and no replacement.
    Task RotateAsync(RefreshToken oldToken, RefreshToken newToken, CancellationToken ct = default);

    // Revokes every token in the family — called on reuse detection.
    Task RevokeFamilyAsync(string familyId, CancellationToken ct = default);
}
