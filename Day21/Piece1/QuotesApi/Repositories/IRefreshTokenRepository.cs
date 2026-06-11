using QuotesApi.Models;

namespace QuotesApi.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken ct = default);
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    Task RevokeTokenAsync(RefreshToken token, string? replacedByHash, CancellationToken ct = default);

    // Atomic rotate
    Task RotateAsync(RefreshToken oldToken, RefreshToken newToken, CancellationToken ct = default);

    // Revoke token family
    Task RevokeFamilyAsync(string familyId, CancellationToken ct = default);
}
