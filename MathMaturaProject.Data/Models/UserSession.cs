namespace MathMaturaProject.Data.Models;

/// <summary>
/// A server-side login session. The browser only holds an opaque random token in an
/// HttpOnly cookie; this table stores the SHA-256 hash of that token, so a database leak
/// does not expose usable session tokens. Deleting the row revokes the session.
/// </summary>
public class UserSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;

    // Hex-encoded SHA-256 of the session token (64 characters).
    public string TokenHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }

    public UserProfile? User { get; set; }
}
