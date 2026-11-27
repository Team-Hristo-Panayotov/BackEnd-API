namespace MathMaturaProject.API.DTOs.Feedback;

public record FeedbackDto(Guid Id, string Type, string Title, string Content, DateTime CreatedAt);
