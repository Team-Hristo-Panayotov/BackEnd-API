using MathMaturaProject.Data.Models;
using MathMaturaProject.Data.Enums;

namespace MathMaturaProject.Data.Models;

public class Lesson
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public TestSection Section { get; set; }
    public string Content { get; set; } = string.Empty;
    public int EstimatedMinutes { get; set; }
    public int OrderIndex { get; set; }
    public List<Guid> PrerequisiteLessonIds { get; set; } = new();
    public bool IsPublished { get; set; } = true;

    public List<LessonProgress> ProgressEntries { get; set; } = new();
}