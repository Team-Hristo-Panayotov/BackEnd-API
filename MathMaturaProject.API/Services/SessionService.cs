using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using MathMaturaProject.Data.Data;
using MathMaturaProject.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace MathMaturaProject.API.Services;

public interface ISessionService
{
    /// <summary>Creates a session for the user and returns the raw token (only ever sent to the client).</summary>
    Task<(string Token, DateTimeOffset ExpiresAt)> CreateAsync(string userId, CancellationToken ct = default);

    /// <summary>Returns the live session for a token, or null if unknown or expired.</summary>
    Task<UserSession?> ValidateAsync(string token, CancellationToken ct = default);

    /// <summary>Destroys the session. Safe to call for tokens that no longer exist.</summary>
    Task RevokeAsync(string token, CancellationToken ct = default);
}

public class SessionService : ISessionService
{
    // Real tokens are 43 characters; anything wildly longer is junk we don't need to hash.
    private const int MaxTokenLength = 128;

    private readonly AppDbContext _db;
    private readonly SessionOptions _options;

    public SessionService(AppDbContext db, IOptions<SessionOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<(string Token, DateTimeOffset ExpiresAt)> CreateAsync(string userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        // Housekeeping: drop this user's expired sessions so the table doesn't grow forever.
        await _db.UserSessions
            .Where(s => s.UserId == userId && s.ExpiresAt <= now)
            .ExecuteDeleteAsync(ct);

        // 256 bits from the OS CSPRNG, URL-safe. The token carries no data about the user.
        var token = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        var expiresAt = now.Add(_options.Lifetime);

        _db.UserSessions.Add(new UserSession
        {
            UserId = userId,
            TokenHash = Hash(token),
            CreatedAt = now,
            ExpiresAt = expiresAt
        });
        await _db.SaveChangesAsync(ct);

        return (token, new DateTimeOffset(expiresAt, TimeSpan.Zero));
    }

    public async Task<UserSession?> ValidateAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > MaxTokenLength)
            return null;

        var hash = Hash(token);
        var now = DateTime.UtcNow;

        return await _db.UserSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.TokenHash == hash && s.ExpiresAt > now, ct);
    }

    public async Task RevokeAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > MaxTokenLength)
            return;

        var hash = Hash(token);
        await _db.UserSessions.Where(s => s.TokenHash == hash).ExecuteDeleteAsync(ct);
    }

    // Only the hash is stored, so a leaked database cannot be used to impersonate users.
    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
