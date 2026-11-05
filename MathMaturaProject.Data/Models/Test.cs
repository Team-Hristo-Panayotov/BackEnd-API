using MathMaturaProject.Data.Models;
using MathMaturaProject.Data.Enums;

namespace MathMaturaProject.Data.Models;

public class Test
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TestSection Section { get; set; }
    public Difficulty Difficulty { get; set; }
    public int DurationMinutes { get; set; }
    public int TotalMarks => Questions.Sum(q => q.Marks);
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Question> Questions { get; set; } = new();
    public List<TestAttempt> Attempts { get; set; } = new();
}