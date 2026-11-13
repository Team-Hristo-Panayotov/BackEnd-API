using MathMaturaProject.API.Services;

namespace MathMaturaProject.Tests;

public class AuthValidatorTests
{
    private static List<string> Codes(string? email = "maria@example.com", string? name = "Maria", string? password = "parola123")
        => AuthValidator.ValidateRegister(email, name, password).SelectMany(e => e.Value).ToList();

    [Fact]
    public void Valid_input_has_no_errors() => Assert.Empty(Codes());

    [Fact]
    public void Cyrillic_letters_count_as_letters() => Assert.Empty(Codes(password: "парола12"));

    [Theory]
    [InlineData("12345678", "PASSWORD_NEEDS_LETTER")]
    [InlineData("abcdefgh", "PASSWORD_NEEDS_DIGIT")]
    [InlineData("abc123", "PASSWORD_TOO_SHORT")]
    [InlineData("", "PASSWORD_TOO_SHORT")]
    [InlineData(null, "PASSWORD_TOO_SHORT")]
    public void Weak_passwords_are_refused(string? password, string code) => Assert.Contains(code, Codes(password: password));

    [Fact]
    public void Password_length_limits_are_inclusive()
    {
        Assert.Empty(Codes(password: "a1234567"));                     // 8
        Assert.Empty(Codes(password: "a" + new string('1', 127)));    // 128
        Assert.Contains("PASSWORD_TOO_LONG", Codes(password: "a" + new string('1', 128))); // 129
    }

    [Fact]
    public void Password_cannot_be_the_email_in_any_case()
        => Assert.Contains("PASSWORD_SAME_AS_EMAIL", Codes(email: "ivan123@abv.bg", password: "IVAN123@ABV.BG"));

    [Theory]
    [InlineData(null, "EMAIL_REQUIRED")]
    [InlineData("   ", "EMAIL_REQUIRED")]
    [InlineData("not-an-email", "EMAIL_INVALID")]
    [InlineData("a b@example.com", "EMAIL_INVALID")]
    [InlineData("a@b@example.com", "EMAIL_INVALID")]
    public void Bad_emails_are_refused(string? email, string code) => Assert.Contains(code, Codes(email: email));

    [Fact]
    public void Email_over_254_characters_is_refused()
        => Assert.Contains("EMAIL_TOO_LONG", Codes(email: new string('a', 243) + "@example.com")); // 255

    [Fact]
    public void Email_spaces_at_the_ends_are_ignored() => Assert.Empty(Codes(email: "  maria@example.com  "));

    [Theory]
    [InlineData(null)]
    [InlineData("M")]
    [InlineData("   M   ")]
    public void Display_name_too_short_is_refused(string? name) => Assert.Contains("DISPLAY_NAME_INVALID", Codes(name: name));

    [Fact]
    public void Display_name_length_limits_are_inclusive()
    {
        Assert.Empty(Codes(name: "Al"));
        Assert.Empty(Codes(name: new string('я', 50)));
        Assert.Contains("DISPLAY_NAME_INVALID", Codes(name: new string('я', 51)));
    }

    [Fact]
    public void Errors_are_grouped_by_field()
    {
        var errors = AuthValidator.ValidateRegister("bad", "", "short");
        Assert.Equal(["displayName", "email", "password"], errors.Keys.Order());
    }

    [Fact]
    public void Login_only_checks_that_fields_are_filled()
    {
        Assert.Empty(AuthValidator.ValidateLogin("x@y.bg", "short"));
        Assert.Equal(["email", "password"], AuthValidator.ValidateLogin(" ", "").Keys.Order());
    }
}
