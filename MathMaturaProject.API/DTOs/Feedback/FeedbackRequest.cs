namespace MathMaturaProject.API.DTOs.Feedback;

// Fields are nullable so a missing field reaches FeedbackValidator and gets a clear error code.
// Type is "Problem" or "Suggestion".
public record FeedbackRequest(string? Type, string? Title, string? Content);
