using Microsoft.Extensions.Options;

namespace MathMaturaProject.API.Services;

/// <summary>
/// The single place that reads, writes and clears the session cookie, so every
/// code path uses identical security attributes (a cookie can only be deleted
/// reliably if the delete uses the same Path/Domain it was set with).
/// </summary>
public class SessionCookieManager
{
    private readonly SessionOptions _options;
    private readonly IWebHostEnvironment _env;

    public SessionCookieManager(IOptions<SessionOptions> options, IWebHostEnvironment env)
    {
        _options = options.Value;
        _env = env;
    }

    public string CookieName => _options.CookieName;

    public string? Read(HttpContext context)
    {
        return context.Request.Cookies.TryGetValue(CookieName, out var token) && !string.IsNullOrWhiteSpace(token)
            ? token
            : null;
    }

    public void Write(HttpContext context, string token, DateTimeOffset expiresAt)
    {
        var options = BuildOptions(context);
        options.Expires = expiresAt;
        options.MaxAge = expiresAt - DateTimeOffset.UtcNow;
        context.Response.Cookies.Append(CookieName, token, options);
    }

    /// <summary>Expires the cookie in the past so the browser drops it.</summary>
    public void Clear(HttpContext context)
    {
        context.Response.Cookies.Delete(CookieName, BuildOptions(context));
    }

    private CookieOptions BuildOptions(HttpContext context) => new()
    {
        // Not readable from JavaScript (document.cookie) - protects the token from XSS.
        HttpOnly = true,

        // HTTPS only. The one exception is plain-http local development, where browsers
        // would otherwise refuse to store the cookie at all.
        Secure = !_env.IsDevelopment() || context.Request.IsHttps,

        // Lax/Strict mitigate CSRF; "None" is deliberately not offered.
        SameSite = _options.SameSite == SameSiteMode.Strict ? SameSiteMode.Strict : SameSiteMode.Lax,

        Path = "/",
        IsEssential = true
    };
}
