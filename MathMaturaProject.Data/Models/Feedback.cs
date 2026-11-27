using MathMaturaProject.Data.Enums;

namespace MathMaturaProject.Data.Models;

public class Feedback
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public FeedbackType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? UserId { get; set; }
    public UserProfile? User { get; set; }
}