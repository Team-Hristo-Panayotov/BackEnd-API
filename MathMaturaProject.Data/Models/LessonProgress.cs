using MathMaturaProject.Data.Enums;

namespace MathMaturaProject.Data.Models;

public class LessonProgress
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public Guid LessonId { get; set; }
    public ProgressStatus Status { get; set; } = ProgressStatus.NotStarted;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;

    public Lesson? Lesson { get; set; }
    public UserProfile? User { get; set; }
}