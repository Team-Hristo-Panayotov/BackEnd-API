using MathMaturaProject.API.DTOs.Lessons;
using MathMaturaProject.Data.Data;
using MathMaturaProject.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace MathMaturaProject.API.Services;

public class LessonService : ILessonService
{
    private readonly AppDbContext _db;

    public LessonService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<LessonDto>> GetAllAsync()
    {
        var lessons = await _db.Lessons
            .AsNoTracking()
            .OrderBy(l => l.Section)
            .ThenBy(l => l.OrderIndex)
            .ToListAsync();

        return lessons.Select(ToDto).ToList();
    }

    public async Task<LessonDto?> GetByIdAsync(Guid id)
    {
        var lesson = await _db.Lessons.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);
        return lesson == null ? null : ToDto(lesson);
    }

    public async Task<LessonDto> CreateAsync(LessonRequestDto request)
    {
        var lesson = new Lesson();
        Apply(request, lesson);

        _db.Lessons.Add(lesson);
        await _db.SaveChangesAsync();

        return ToDto(lesson);
    }

    public async Task<LessonDto?> UpdateAsync(Guid id, LessonRequestDto request)
    {
        var lesson = await _db.Lessons.FindAsync(id);
        if (lesson == null)
        {
            return null;
        }

        Apply(request, lesson);
        await _db.SaveChangesAsync();

        return ToDto(lesson);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var lesson = await _db.Lessons.FindAsync(id);
        if (lesson == null)
        {
            return false;
        }

        _db.Lessons.Remove(lesson);
        await _db.SaveChangesAsync();
        return true;
    }

    private static void Apply(LessonRequestDto request, Lesson lesson)
    {
        lesson.Title = request.Title.Trim();
        lesson.Section = request.Section;
        lesson.Content = request.Content;
        lesson.EstimatedMinutes = request.EstimatedMinutes;
        lesson.OrderIndex = request.OrderIndex;
        lesson.PrerequisiteLessonIds = request.PrerequisiteLessonIds.Distinct().ToList();
        lesson.IsPublished = request.IsPublished;
    }

    private static LessonDto ToDto(Lesson lesson) => new()
    {
        Id = lesson.Id,
        Title = lesson.Title,
        Section = lesson.Section,
        Content = lesson.Content,
        EstimatedMinutes = lesson.EstimatedMinutes,
        OrderIndex = lesson.OrderIndex,
        PrerequisiteLessonIds = lesson.PrerequisiteLessonIds,
        IsPublished = lesson.IsPublished
    };
}
