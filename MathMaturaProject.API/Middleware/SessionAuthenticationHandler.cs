using System.Security.Claims;
using System.Text.Encodings.Web;
using MathMaturaProject.API.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace MathMaturaProject.API.Middleware;

/// <summary>
/// Authenticates each request from the session cookie. The token is looked up on the
/// server on every request, so a revoked or expired session stops working immediately.
/// Any [Authorize]d endpoint therefore answers 401 when the cookie is missing or invalid.
/// </summary>
public class SessionAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Session";
    public const string SessionIdClaim = "sid";

    private readonly ISessionService _sessions;
    private readonly SessionCookieManager _cookies;

    public SessionAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISessionService sessions,
        SessionCookieManager cookies) : base(options, logger, encoder)
    {
        _sessions = sessions;
        _cookies = cookies;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = _cookies.Read(Context);
        if (token is null)
            return AuthenticateResult.NoResult();

        var session = await _sessions.ValidateAsync(token, Context.RequestAborted);
        if (session is null)
            return AuthenticateResult.Fail("Invalid or expired session.");

        // Only opaque identifiers go into the principal - no email/name/roles.
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, session.UserId),
            new Claim(SessionIdClaim, session.Id.ToString())
        };
        var identity = new ClaimsIdentity(claims, SchemeName);

        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return AuthenticateResult.Success(ticket);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        // A cookie that was sent but failed validation is stale: tell the browser to drop it.
        if (_cookies.Read(Context) is not null)
            _cookies.Clear(Context);

        // API semantics: respond 401, never redirect to a login page.
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }
}
