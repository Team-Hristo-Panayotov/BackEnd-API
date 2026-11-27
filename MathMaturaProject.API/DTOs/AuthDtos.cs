using System.ComponentModel.DataAnnotations;
using MathMaturaProject.Data.Models;

namespace MathMaturaProject.API.DTOs;

public class RegisterRequest
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string DisplayName { get; init; } = string.Empty;

    // Upper bound stops absurdly long inputs being fed to the (deliberately slow) password hasher.
    [Required, StringLength(128, MinimumLength = 8)]
    public string Password { get; init; } = string.Empty;
}

public class LoginRequest
{
    [Required, StringLength(256)]
    public string Email { get; init; } = string.Empty;

    [Required, StringLength(128)]
    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// Profile state returned in the response body (never stored in the cookie).
/// Deliberately has no PasswordHash.
/// </summary>
public record UserProfileResponse(
    string UserId,
    string DisplayName,
    string Email,
    string? AvatarUrl,
    int CurrentStreak,
    int LongestStreak,
    int TotalPoints,
    int TotalTestsCompleted,
    int TotalLessonsCompleted)
{
    public static UserProfileResponse From(UserProfile p) => new(
        p.UserId,
        p.DisplayName,
        p.Email,
        p.AvatarUrl,
        p.CurrentStreak,
        p.LongestStreak,
        p.TotalPoints,
        p.TotalTestsCompleted,
        p.TotalLessonsCompleted);
}
