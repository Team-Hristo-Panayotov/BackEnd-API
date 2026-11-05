namespace MathMaturaProject.Data.Models;

public class UserProfile
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Streak tracking
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public DateOnly? LastActivityDate { get; set; }

    // Aggregated stats
    public int TotalTestsCompleted { get; set; }
    public int TotalLessonsCompleted { get; set; }
    public int TotalPoints { get; set; }
}