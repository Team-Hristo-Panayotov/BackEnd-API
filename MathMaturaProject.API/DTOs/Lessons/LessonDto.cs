using MathMaturaProject.Data.Enums;

namespace MathMaturaProject.API.DTOs.Lessons;

public class LessonDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public TestSection Section { get; set; }
    public string Content { get; set; } = string.Empty;
    public int EstimatedMinutes { get; set; }
    public int OrderIndex { get; set; }
    public List<Guid> PrerequisiteLessonIds { get; set; } = new();
    public bool IsPublished { get; set; }
}
