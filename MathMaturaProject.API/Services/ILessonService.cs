using MathMaturaProject.API.DTOs.Lessons;

namespace MathMaturaProject.API.Services;

public interface ILessonService
{
    Task<List<LessonDto>> GetAllAsync();
    Task<LessonDto?> GetByIdAsync(Guid id);
    Task<LessonDto> CreateAsync(LessonRequestDto request);

    // Returns null when the lesson does not exist.
    Task<LessonDto?> UpdateAsync(Guid id, LessonRequestDto request);

    // Returns false when the lesson does not exist.
    Task<bool> DeleteAsync(Guid id);
}
