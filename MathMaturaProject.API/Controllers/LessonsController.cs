using MathMaturaProject.API.DTOs.Lessons;
using MathMaturaProject.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace MathMaturaProject.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LessonsController : ControllerBase
{
    private readonly ILessonService _lessons;

    public LessonsController(ILessonService lessons)
    {
        _lessons = lessons;
    }

    [HttpGet]
    public async Task<ActionResult<List<LessonDto>>> GetAll()
    {
        return Ok(await _lessons.GetAllAsync());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LessonDto>> GetById(Guid id)
    {
        var lesson = await _lessons.GetByIdAsync(id);
        return lesson == null ? NotFound() : Ok(lesson);
    }

    [HttpPost]
    public async Task<ActionResult<LessonDto>> Create(LessonRequestDto request)
    {
        var lesson = await _lessons.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = lesson.Id }, lesson);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, LessonRequestDto request)
    {
        if (request.PrerequisiteLessonIds.Contains(id))
        {
            ModelState.AddModelError(nameof(request.PrerequisiteLessonIds), "A lesson cannot be its own prerequisite.");
            return ValidationProblem(ModelState);
        }

        var lesson = await _lessons.UpdateAsync(id, request);
        return lesson == null ? NotFound() : NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        return await _lessons.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
