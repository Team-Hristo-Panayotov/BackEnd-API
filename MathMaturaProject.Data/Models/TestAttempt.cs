using MathMaturaProject.Data.Models;

namespace MathMaturaProject.Data.Models;

public class TestAttempt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TestId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Dictionary<Guid, int> Answers { get; set; } = new();
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Test? Test { get; set; }
    public UserProfile? User { get; set; }
}