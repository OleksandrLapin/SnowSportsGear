namespace Core.Interfaces;

public interface ISecurityCodeService
{
    Task<string> CreateAsync(
        string userId,
        string purpose,
        string token,
        string? targetEmail,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default);

    Task<string?> RedeemTokenAsync(
        string userId,
        string purpose,
        string code,
        string? targetEmail,
        CancellationToken cancellationToken = default);
}
