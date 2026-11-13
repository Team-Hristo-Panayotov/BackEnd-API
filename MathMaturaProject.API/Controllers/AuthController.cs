using MathMaturaProject.API.DTOs;
using MathMaturaProject.API.Services;
using MathMaturaProject.Data.Data;
using MathMaturaProject.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;

namespace MathMaturaProject.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(UserManager<IdentityUser> users, AppDbContext db, TokenService tokens) : ControllerBase
{
    private const string DefaultRole = "User";

    // A real hash of a throwaway password. Checking against it when the email is unknown
    // makes that reply take as long as a wrong password, so timing does not reveal accounts.
    private static string? _dummyHash;

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        var errors = AuthValidator.ValidateRegister(req.Email, req.DisplayName, req.Password);
        if (errors.Count > 0) return BadRequest(new ErrorResponse("VALIDATION_FAILED", errors));

        var email = req.Email!.Trim();
        var displayName = req.DisplayName!.Trim();
        if (await users.FindByEmailAsync(email) is not null) return EmailTaken();

        // The login, its role and its profile are saved together or not at all.
        await using var tx = await db.Database.BeginTransactionAsync();
        var user = new IdentityUser { UserName = email, Email = email };
        var created = await users.CreateAsync(user, req.Password!);
        if (!created.Succeeded)
        {
            // Two sign-ups with the same email at once: the second one lands here.
            if (created.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName")) return EmailTaken();
            return BadRequest(new ErrorResponse("VALIDATION_FAILED",
                new() { ["password"] = created.Errors.Select(e => e.Code).ToList() }));
        }

        // The role is always "User". The request has no way to choose it.
        var roled = await users.AddToRoleAsync(user, DefaultRole);
        if (!roled.Succeeded) throw new InvalidOperationException($"Role '{DefaultRole}' is missing. Run the migrations.");

        var profile = new UserProfile { UserId = user.Id, Email = email, DisplayName = displayName };
        db.Profiles.Add(profile);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        return StatusCode(StatusCodes.Status201Created, Issue(user.Id, profile, DefaultRole));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var errors = AuthValidator.ValidateLogin(req.Email, req.Password);
        if (errors.Count > 0) return BadRequest(new ErrorResponse("VALIDATION_FAILED", errors));

        var user = await users.FindByEmailAsync(req.Email!.Trim());
        if (user is null)
        {
            _dummyHash ??= users.PasswordHasher.HashPassword(new IdentityUser(), Guid.NewGuid().ToString());
            users.PasswordHasher.VerifyHashedPassword(new IdentityUser(), _dummyHash, req.Password!);
            return InvalidLogin();
        }

        if (await users.IsLockedOutAsync(user)) return AccountLocked();
        if (!await users.CheckPasswordAsync(user, req.Password!))
        {
            // Counts the failure. The 5th one in a row locks the account for 15 minutes.
            await users.AccessFailedAsync(user);
            return InvalidLogin();
        }
        await users.ResetAccessFailedCountAsync(user);

        var profile = await db.Profiles.FindAsync(user.Id);
        if (profile is null) return InvalidLogin();
        return Ok(Issue(user.Id, profile, await RoleOf(user)));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var user = userId is null ? null : await users.FindByIdAsync(userId);
        var profile = user is null ? null : await db.Profiles.FindAsync(user.Id);
        // The token is valid but the account was deleted after it was issued.
        if (user is null || profile is null) return Unauthorized(new ErrorResponse("UNAUTHORIZED"));

        return Ok(new UserDto(user.Id, profile.Email, profile.DisplayName, await RoleOf(user)));
    }

    private AuthResponse Issue(string userId, UserProfile profile, string role)
    {
        var (token, expiresAt) = tokens.Create(userId, profile.Email, role);
        return new AuthResponse(token, expiresAt, new UserDto(userId, profile.Email, profile.DisplayName, role));
    }

    // Admin wins if a user somehow has both roles.
    private async Task<string> RoleOf(IdentityUser user)
    {
        var roles = await users.GetRolesAsync(user);
        return roles.Contains("Admin") ? "Admin" : roles.FirstOrDefault() ?? DefaultRole;
    }

    private ConflictObjectResult EmailTaken() => Conflict(new ErrorResponse("EMAIL_TAKEN"));
    private UnauthorizedObjectResult InvalidLogin() => Unauthorized(new ErrorResponse("INVALID_LOGIN"));
    private UnauthorizedObjectResult AccountLocked() => Unauthorized(new ErrorResponse("ACCOUNT_LOCKED"));
}
