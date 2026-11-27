using MathMaturaProject.API.DTOs;
using MathMaturaProject.API.DTOs.Feedback;
using MathMaturaProject.API.Services;
using MathMaturaProject.Data.Data;
using MathMaturaProject.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace MathMaturaProject.API.Controllers;

[ApiController]
[Route("api/feedback")]
public class FeedbackController(AppDbContext db) : ControllerBase
{
    /// <summary>
    /// Saves a message from the feedback box. Anyone may send one.
    /// When the request carries a valid token, the message is linked to that user.
    /// </summary>
    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Create(FeedbackRequest req)
    {
        var errors = FeedbackValidator.Validate(req.Type, req.Title, req.Content);
        if (errors.Count > 0) return BadRequest(new ErrorResponse("VALIDATION_FAILED", errors));

        var feedback = new Feedback
        {
            Type = FeedbackValidator.ParseType(req.Type),
            Title = req.Title!.Trim(),
            Content = req.Content!.Trim(),
            UserId = await CurrentProfileId(),
        };
        db.Feedbacks.Add(feedback);
        await db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new FeedbackDto(
            feedback.Id, feedback.Type.ToString(), feedback.Title, feedback.Content, feedback.CreatedAt));
    }

    // The logged-in user's id, or null for a guest or for an account deleted after its token was issued.
    private async Task<string?> CurrentProfileId()
    {
        var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (userId is null) return null;
        return await db.Profiles.FindAsync(userId) is null ? null : userId;
    }
}
