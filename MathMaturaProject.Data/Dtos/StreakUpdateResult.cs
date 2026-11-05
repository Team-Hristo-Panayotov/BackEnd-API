namespace MathMaturaProject.Data.Dtos;

public record StreakUpdateResult(
    int CurrentStreak,
    int LongestStreak,
    bool StreakIncreased,
    int BonusPointsAwarded);