using MathMaturaProject.Data.Enums;

namespace MathMaturaProject.API.Services;

/// <summary>
/// The rules for the feedback box: a type (Problem or Suggestion),
/// a title of 10 to 200 characters and a description of 10 to 1000 characters.
/// The frontend checks the same rules, but this is the one that decides.
/// </summary>
public static class FeedbackValidator
{
    public const int TitleMin = 10;
    public const int TitleMax = 200;
    public const int ContentMin = 10;
    public const int ContentMax = 1000;

    // Taken from the FeedbackType enum, so the allowed values always match the database.
    public static readonly IReadOnlyList<string> Types = Enum.GetNames<FeedbackType>();

    public static Dictionary<string, List<string>> Validate(string? type, string? title, string? content)
    {
        var errors = new Dictionary<string, List<string>>();

        // 1. Type: required and one of the fixed values.
        type = type?.Trim();
        if (string.IsNullOrEmpty(type))
            Add(errors, "type", "TYPE_REQUIRED");
        else if (!Types.Contains(type, StringComparer.OrdinalIgnoreCase))
            Add(errors, "type", "TYPE_INVALID");

        // 2. Title: required and 10-200 characters, spaces at the ends not counted.
        CheckText(title?.Trim(), "title", "TITLE", TitleMin, TitleMax, errors);

        // 3. Content: required and 10-1000 characters, spaces at the ends not counted.
        CheckText(content?.Trim(), "content", "CONTENT", ContentMin, ContentMax, errors);

        return errors;
    }

    /// <summary>Returns the type spelled as stored ("Problem" or "Suggestion"), or null when it is not valid.</summary>
    public static string? NormalizeType(string? type)
        => Types.FirstOrDefault(t => string.Equals(t, type?.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>The type as the enum. Call only after Validate found no errors.</summary>
    public static FeedbackType ParseType(string? type)
        => Enum.Parse<FeedbackType>(NormalizeType(type) ?? throw new ArgumentException("Not a feedback type.", nameof(type)));

    private static void CheckText(string? value, string field, string prefix, int min, int max, Dictionary<string, List<string>> errors)
    {
        if (string.IsNullOrEmpty(value))
            Add(errors, field, $"{prefix}_REQUIRED");
        else if (value.Length < min)
            Add(errors, field, $"{prefix}_TOO_SHORT");
        else if (value.Length > max)
            Add(errors, field, $"{prefix}_TOO_LONG");
    }

    private static void Add(Dictionary<string, List<string>> errors, string field, string code)
    {
        if (!errors.TryGetValue(field, out var list)) errors[field] = list = [];
        list.Add(code);
    }
}
