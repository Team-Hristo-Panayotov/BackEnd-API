namespace MathMaturaProject.API.DTOs;

// Fields are nullable so a missing field reaches AuthValidator and gets a clear error code.
public record RegisterRequest(string? Email, string? DisplayName, string? Password);

public record LoginRequest(string? Email, string? Password);

public record UserDto(string UserId, string Email, string DisplayName, string Role);

public record AuthResponse(string AccessToken, DateTime ExpiresAt, UserDto User);

public record ErrorResponse(string Code, Dictionary<string, List<string>>? Errors = null);
