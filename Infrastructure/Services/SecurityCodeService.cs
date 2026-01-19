using System.Security.Cryptography;
using System.Text;
using Core.Entities;
using Core.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class SecurityCodeService : ISecurityCodeService
{
    private const int DefaultCodeLength = 6;
    private const int CodeMax = 1_000_000;
    private readonly StoreContext context;
    private readonly ILogger<SecurityCodeService> logger;

    public SecurityCodeService(StoreContext context, ILogger<SecurityCodeService> logger)
    {
        this.context = context;
        this.logger = logger;
    }

    public async Task<string> CreateAsync(
        string userId,
        string purpose,
        string token,
        string? targetEmail,
        TimeSpan expiresIn,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var code = GenerateCode();
        var salt = GenerateSalt();
        var hash = HashCode(code, salt);

        await ExpireActiveCodesAsync(userId, purpose, now, cancellationToken);

        context.SecurityCodes.Add(new SecurityCode
        {
            UserId = userId,
            Purpose = purpose,
            CodeHash = hash,
            CodeSalt = salt,
            Token = token,
            TargetEmail = targetEmail,
            CreatedAt = now,
            ExpiresAt = now.Add(expiresIn)
        });

        await context.SaveChangesAsync(cancellationToken);
        return code;
    }

    public async Task<string?> RedeemTokenAsync(
        string userId,
        string purpose,
        string code,
        string? targetEmail,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var candidates = await context.SecurityCodes
            .Where(x => x.UserId == userId
                && x.Purpose == purpose
                && x.UsedAt == null
                && x.ExpiresAt > now)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        foreach (var candidate in candidates)
        {
            if (!TargetEmailMatches(candidate.TargetEmail, targetEmail))
            {
                continue;
            }

            if (!Verify(code, candidate))
            {
                continue;
            }

            candidate.UsedAt = now;
            await context.SaveChangesAsync(cancellationToken);
            return candidate.Token;
        }

        logger.LogWarning("Security code redemption failed for user {UserId} purpose {Purpose}", userId, purpose);
        return null;
    }

    private async Task ExpireActiveCodesAsync(string userId, string purpose, DateTime now, CancellationToken cancellationToken)
    {
        var active = await context.SecurityCodes
            .Where(x => x.UserId == userId
                && x.Purpose == purpose
                && x.UsedAt == null
                && x.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        if (active.Count == 0) return;

        foreach (var item in active)
        {
            item.ExpiresAt = now;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static string GenerateCode()
    {
        var value = RandomNumberGenerator.GetInt32(0, CodeMax);
        return value.ToString($"D{DefaultCodeLength}");
    }

    private static string GenerateSalt()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
    }

    private static string HashCode(string code, string salt)
    {
        var data = Encoding.UTF8.GetBytes($"{code}:{salt}");
        return Convert.ToBase64String(SHA256.HashData(data));
    }

    private static bool Verify(string code, SecurityCode candidate)
    {
        var hash = HashCode(code, candidate.CodeSalt);
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(hash),
            Convert.FromBase64String(candidate.CodeHash));
    }

    private static bool TargetEmailMatches(string? storedEmail, string? targetEmail)
    {
        if (string.IsNullOrWhiteSpace(storedEmail)) return true;
        return string.Equals(storedEmail, targetEmail, StringComparison.OrdinalIgnoreCase);
    }
}
