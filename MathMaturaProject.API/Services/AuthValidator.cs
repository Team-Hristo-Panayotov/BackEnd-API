using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace MathMaturaProject.API.Services;

/// <summary>
/// The email, password and display name rules from issue #5.
/// The frontend checks the same rules, but this is the one that decides.
/// </summary>
public static partial class AuthValidator
{
    public const int PasswordMin = 8;
    public const int PasswordMax = 128;
    public const int EmailMax = 254;

    private static readonly EmailAddressAttribute EmailFormat = new();

    // \p{L} is any letter in any alphabet, so Cyrillic counts too.
    [GeneratedRegex(@"\p{L}")]
    private static partial Regex Letter();

    [GeneratedRegex(@"\d")]
    private static partial Regex Digit();

    public static Dictionary<string, List<string>> ValidateRegister(string? email, string? displayName, string? password)
    {
        var errors = new Dictionary<string, List<string>>();
        email = email?.Trim();
        CheckEmail(email, errors);
        CheckPassword(password, email, errors);

        var name = displayName?.Trim() ?? "";
        if (name.Length is < 2 or > 50)
            Add(errors, "displayName", "DISPLAY_NAME_INVALID");

        return errors;
    }

    /// <summary>Login only checks that both fields are filled, so it never hints at the password rules.</summary>
    public static Dictionary<string, List<string>> ValidateLogin(string? email, string? password)
    {
        var errors = new Dictionary<string, List<string>>();
        if (string.IsNullOrWhiteSpace(email)) Add(errors, "email", "EMAIL_REQUIRED");
        if (string.IsNullOrEmpty(password)) Add(errors, "password", "PASSWORD_REQUIRED");
        return errors;
    }

    private static void CheckEmail(string? email, Dictionary<string, List<string>> errors)
    {
        if (string.IsNullOrEmpty(email))
            Add(errors, "email", "EMAIL_REQUIRED");
        else if (email.Length > EmailMax)
            Add(errors, "email", "EMAIL_TOO_LONG");
        else if (email.Any(char.IsWhiteSpace) || !EmailFormat.IsValid(email))
            Add(errors, "email", "EMAIL_INVALID");
    }

    private static void CheckPassword(string? password, string? email, Dictionary<string, List<string>> errors)
    {
        password ??= "";
        if (password.Length < PasswordMin) Add(errors, "password", "PASSWORD_TOO_SHORT");
        if (password.Length > PasswordMax) Add(errors, "password", "PASSWORD_TOO_LONG");
        if (!Letter().IsMatch(password)) Add(errors, "password", "PASSWORD_NEEDS_LETTER");
        if (!Digit().IsMatch(password)) Add(errors, "password", "PASSWORD_NEEDS_DIGIT");
        if (!string.IsNullOrEmpty(email) && string.Equals(password, email, StringComparison.OrdinalIgnoreCase))
            Add(errors, "password", "PASSWORD_SAME_AS_EMAIL");
    }

    private static void Add(Dictionary<string, List<string>> errors, string field, string code)
    {
        if (!errors.TryGetValue(field, out var list)) errors[field] = list = [];
        list.Add(code);
    }
}
