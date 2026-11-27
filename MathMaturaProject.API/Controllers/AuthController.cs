using System.Security.Claims;
using MathMaturaProject.API.DTOs;
using MathMaturaProject.API.Services;
using MathMaturaProject.Data.Data;
using MathMaturaProject.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MathMaturaProject.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    // Verified against when the email is unknown, so "no such user" and "wrong password"
    // take about the same time and can't be told apart by timing.
    private static readonly UserProfile DummyUser = new();
    private static readonly string DummyHash =
        new PasswordHasher<UserProfile>().HashPassword(DummyUser, "not-a-real-password");

    private readonly AppDbContext _db;
    private readonly IPasswordHasher<UserProfile> _hasher;
    private readonly ISessionService _sessions;
    private readonly SessionCookieManager _cookies;

    public AuthController(
        AppDbContext db,
        IPasswordHasher<UserProfile> hasher,
        ISessionService sessions,
        SessionCookieManager cookies)
    {
        _db = db;
        _hasher = hasher;
        _sessions = sessions;
        _cookies = cookies;
    }

    /// <summary>Creates an account and signs the user in (sets the session cookie).</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<UserProfileResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);

        if (await _db.Profiles.AnyAsync(p => p.Email == email, ct))
            return EmailTaken();

        var user = new UserProfile
        {
            UserId = Guid.NewGuid().ToString(),
            Email = email,
            DisplayName = request.DisplayName.Trim()
        };
        user.PasswordHash = _hasher.HashPassword(user, request.Password);

        _db.Profiles.Add(user);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            _db.Entry(user).State = EntityState.Detached;

            // Lost a race with a concurrent registration; the unique index caught it.
            if (await _db.Profiles.AsNoTracking().AnyAsync(p => p.Email == email, ct))
                return EmailTaken();

            throw;
        }

        await SignInAsync(user.UserId, ct);

        SetNoStore();
        return CreatedAtAction(nameof(Me), UserProfileResponse.From(user));
    }

    /// <summary>Verifies credentials and sets the session cookie.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<UserProfileResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _db.Profiles.FirstOrDefaultAsync(p => p.Email == email, ct);

        if (user is null || string.IsNullOrEmpty(user.PasswordHash))
        {
            _hasher.VerifyHashedPassword(DummyUser, DummyHash, request.Password);
            return InvalidCredentials();
        }

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            return InvalidCredentials();

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _hasher.HashPassword(user, request.Password);
            await _db.SaveChangesAsync(ct);
        }

        // A fresh token on every login (no session fixation); retire any old cookie's session.
        var oldToken = _cookies.Read(HttpContext);
        if (oldToken is not null)
            await _sessions.RevokeAsync(oldToken, ct);

        await SignInAsync(user.UserId, ct);

        SetNoStore();
        return Ok(UserProfileResponse.From(user));
    }

    /// <summary>
    /// Reads the session cookie and returns the signed-in user's profile. The client calls this
    /// on startup to rehydrate its state after a reload or a return visit.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserProfileResponse>> Me(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _db.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, ct);

        // Session exists but its user is gone: treat as signed out.
        if (user is null)
        {
            _cookies.Clear(HttpContext);
            return Unauthorized();
        }

        SetNoStore();
        return Ok(UserProfileResponse.From(user));
    }

    /// <summary>
    /// Destroys the server-side session and expires the cookie. Idempotent: always 204, even if
    /// the session was already gone, so a client with a stale cookie can still clear it.
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var token = _cookies.Read(HttpContext);
        if (token is not null)
            await _sessions.RevokeAsync(token, ct);

        _cookies.Clear(HttpContext);
        return NoContent();
    }

    private async Task SignInAsync(string userId, CancellationToken ct)
    {
        var (token, expiresAt) = await _sessions.CreateAsync(userId, ct);
        _cookies.Write(HttpContext, token, expiresAt);
    }

    // Auth responses carry personal data; keep them out of shared/browser caches.
    private void SetNoStore() => Response.Headers.CacheControl = "no-store";

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private ActionResult EmailTaken() =>
        Conflict(new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Email already registered."
        });

    // Same message for unknown email and wrong password, so accounts can't be enumerated.
    private ActionResult InvalidCredentials() =>
        Unauthorized(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Invalid email or password."
        });
}
