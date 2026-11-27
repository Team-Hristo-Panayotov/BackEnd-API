using MathMaturaProject.API.Services;

namespace MathMaturaProject.Tests;

public class FeedbackValidatorTests
{
    private static List<string> Codes(string? type = "Problem", string? title = "Грешка в урок 3", string? content = "Отговорът на задача 5 е грешен.")
        => FeedbackValidator.Validate(type, title, content).SelectMany(e => e.Value).ToList();

    [Theory]
    [InlineData("Problem")]
    [InlineData("Suggestion")]
    [InlineData("suggestion")]
    [InlineData("  PROBLEM ")]
    public void Valid_input_has_no_errors(string type) => Assert.Empty(Codes(type: type));

    [Theory]
    [InlineData(null, "TYPE_REQUIRED")]
    [InlineData("   ", "TYPE_REQUIRED")]
    [InlineData("Question", "TYPE_INVALID")]
    [InlineData("Проблем", "TYPE_INVALID")]
    public void Type_must_be_problem_or_suggestion(string? type, string code) => Assert.Contains(code, Codes(type: type));

    [Theory]
    [InlineData(null, "TITLE_REQUIRED")]
    [InlineData("    ", "TITLE_REQUIRED")]
    [InlineData("Кратко", "TITLE_TOO_SHORT")]
    [InlineData("   123456789   ", "TITLE_TOO_SHORT")]
    public void Bad_titles_are_refused(string? title, string code) => Assert.Contains(code, Codes(title: title));

    [Fact]
    public void Title_length_limits_are_inclusive()
    {
        Assert.Empty(Codes(title: new string('я', 10)));
        Assert.Empty(Codes(title: new string('я', 200)));
        Assert.Contains("TITLE_TOO_SHORT", Codes(title: new string('я', 9)));
        Assert.Contains("TITLE_TOO_LONG", Codes(title: new string('я', 201)));
    }

    [Theory]
    [InlineData(null, "CONTENT_REQUIRED")]
    [InlineData("", "CONTENT_REQUIRED")]
    [InlineData("Грешка", "CONTENT_TOO_SHORT")]
    public void Bad_content_is_refused(string? content, string code) => Assert.Contains(code, Codes(content: content));

    [Fact]
    public void Content_length_limits_are_inclusive()
    {
        Assert.Empty(Codes(content: new string('a', 10)));
        Assert.Empty(Codes(content: new string('a', 1000)));
        Assert.Contains("CONTENT_TOO_SHORT", Codes(content: new string('a', 9)));
        Assert.Contains("CONTENT_TOO_LONG", Codes(content: new string('a', 1001)));
    }

    [Fact]
    public void Spaces_at_the_ends_are_not_counted()
        => Assert.Empty(Codes(title: "   " + new string('a', 200) + "   ", content: "  " + new string('a', 1000) + "  "));

    [Fact]
    public void Errors_are_grouped_by_field()
    {
        var errors = FeedbackValidator.Validate("x", "", "short");
        Assert.Equal(["content", "title", "type"], errors.Keys.Order());
    }

    [Theory]
    [InlineData(" problem ", "Problem")]
    [InlineData("SUGGESTION", "Suggestion")]
    [InlineData("other", null)]
    [InlineData(null, null)]
    public void Type_is_normalized(string? input, string? expected) => Assert.Equal(expected, FeedbackValidator.NormalizeType(input));
}
