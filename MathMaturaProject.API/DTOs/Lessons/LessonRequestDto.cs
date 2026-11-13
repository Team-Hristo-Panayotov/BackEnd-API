using System.ComponentModel.DataAnnotations;
using MathMaturaProject.Data.Enums;

namespace MathMaturaProject.API.DTOs.Lessons;

// Used for both create (POST) and full update (PUT).
public class LessonRequestDto
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [EnumDataType(typeof(TestSection))]
    public TestSection Section { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    [Range(1, 600)]
    public int EstimatedMinutes { get; set; }

    [Range(0, int.MaxValue)]
    public int OrderIndex { get; set; }

    public List<Guid> PrerequisiteLessonIds { get; set; } = new();

    public bool IsPublished { get; set; } = true;
}
