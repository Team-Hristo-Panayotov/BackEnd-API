using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using MathMaturaProject.API.DTOs;
using MathMaturaProject.Data.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace MathMaturaProject.Tests;

public class AuthEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string Password = "parola123";
    private readonly HttpClient _client = factory.CreateClient();

    // Every test uses its own email, so tests never see each other's users.
    private static string NewEmail() => $"student-{Guid.NewGuid():N}@example.com";

    private Task<HttpResponseMessage> Register(object body) => _client.PostAsJsonAsync("/api/auth/register", body);
    private Task<HttpResponseMessage> Login(string email, string password)
        => _client.PostAsJsonAsync("/api/auth/login", new { email, password });

    private async Task<AuthResponse> RegisterOk(string email, string displayName = "Мария")
    {
        var res = await Register(new { email, displayName, password = Password });
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    private Task<HttpResponseMessage> Me(string? token)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        if (token is not null) req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return _client.SendAsync(req);
    }

    private static async Task<string> Code(HttpResponseMessage res)
        => (await res.Content.ReadFromJsonAsync<ErrorResponse>())!.Code;

    [Fact]
    public async Task Register_creates_user_profile_and_token()
    {
        var email = NewEmail();
        var body = await RegisterOk("  " + email + "  ", "  Мария  ");

        Assert.False(string.IsNullOrEmpty(body.AccessToken));
        Assert.Equal(email, body.User.Email);
        Assert.Equal("Мария", body.User.DisplayName);
        Assert.Equal("User", body.User.Role);
        Assert.InRange(body.ExpiresAt, DateTime.UtcNow.AddMinutes(59), DateTime.UtcNow.AddMinutes(61));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var profile = await db.Profiles.SingleAsync(p => p.UserId == body.User.UserId);
        Assert.Equal("Мария", profile.DisplayName);
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = await users.FindByIdAsync(body.User.UserId);
        Assert.Equal(["User"], await users.GetRolesAsync(user!));
        Assert.NotEqual(Password, user!.PasswordHash); // Stored as a hash, never as typed.
    }

    [Fact]
    public async Task Token_holds_the_agreed_claims()
    {
        var body = await RegisterOk(NewEmail());
        var jwt = new JsonWebToken(body.AccessToken);

        Assert.Equal("HS256", jwt.Alg);
        Assert.Equal(body.User.UserId, jwt.Subject);
        Assert.Equal(body.User.Email, jwt.GetClaim("email").Value);
        Assert.Equal("User", jwt.GetClaim("role").Value);
        Assert.False(string.IsNullOrEmpty(jwt.Id));
        Assert.Equal("MathMatura.API", jwt.Issuer);
        Assert.Equal(["MathMatura.Client"], jwt.Audiences);
    }

    [Fact]
    public async Task Request_cannot_choose_its_role()
    {
        var res = await Register(new { email = NewEmail(), displayName = "Hacker", password = Password, role = "Admin" });
        var body = await res.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        Assert.Equal("User", body!.User.Role);
    }

    [Fact]
    public async Task Same_email_twice_is_409_in_any_case()
    {
        var email = NewEmail();
        await RegisterOk(email);
        var res = await Register(new { email = email.ToUpperInvariant(), displayName = "Other", password = Password });
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("EMAIL_TAKEN", await Code(res));
    }

    [Fact]
    public async Task Invalid_register_is_400_and_saves_nothing()
    {
        var email = NewEmail();
        var res = await Register(new { email, displayName = "M", password = "12345678" });
        var body = await res.Content.ReadFromJsonAsync<ErrorResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("VALIDATION_FAILED", body!.Code);
        Assert.Equal(["DISPLAY_NAME_INVALID"], body.Errors!["displayName"]);
        Assert.Equal(["PASSWORD_NEEDS_LETTER"], body.Errors["password"]);
        await RegisterOk(email); // The failed try left no user behind.
    }

    [Fact]
    public async Task Empty_body_fields_are_400()
    {
        var res = await Register(new { });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("VALIDATION_FAILED", await Code(res));
    }

    [Fact]
    public async Task Login_works_and_ignores_email_case_and_spaces()
    {
        var email = NewEmail();
        var registered = await RegisterOk(email);
        var res = await Login("  " + email.ToUpperInvariant() + " ", Password);
        var body = await res.Content.ReadFromJsonAsync<AuthResponse>();

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal(registered.User, body!.User);
        Assert.Equal(HttpStatusCode.OK, (await Me(body.AccessToken)).StatusCode);
    }

    [Fact]
    public async Task Wrong_password_and_unknown_email_get_the_same_reply()
    {
        var email = NewEmail();
        await RegisterOk(email);
        var wrong = await Login(email, "wrong-password1");
        var unknown = await Login(NewEmail(), Password);

        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknown.StatusCode);
        Assert.Equal(await wrong.Content.ReadAsStringAsync(), await unknown.Content.ReadAsStringAsync());
        Assert.Equal("INVALID_LOGIN", await Code(wrong));
    }

    [Fact]
    public async Task Empty_login_is_400()
    {
        var res = await Login("", "");
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Equal("VALIDATION_FAILED", await Code(res));
    }

    [Fact]
    public async Task Five_wrong_passwords_lock_the_account()
    {
        var email = NewEmail();
        await RegisterOk(email);
        for (var i = 0; i < 4; i++) Assert.Equal("INVALID_LOGIN", await Code(await Login(email, "wrong-password1")));

        // A right password before the 5th failure resets the count.
        Assert.Equal(HttpStatusCode.OK, (await Login(email, Password)).StatusCode);
        for (var i = 0; i < 5; i++) await Login(email, "wrong-password1");

        var res = await Login(email, Password);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        Assert.Equal("ACCOUNT_LOCKED", await Code(res));
    }

    [Fact]
    public async Task Me_returns_the_user_for_a_valid_token()
    {
        var registered = await RegisterOk(NewEmail());
        var res = await Me(registered.AccessToken);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal(registered.User, await res.Content.ReadFromJsonAsync<UserDto>());
    }

    [Fact]
    public async Task Me_refuses_missing_broken_and_tampered_tokens()
    {
        var token = (await RegisterOk(NewEmail())).AccessToken;
        var parts = token.Split('.');
        var tampered = parts[0] + "." + Base64UrlEncoder.Encode("{\"sub\":\"x\",\"role\":\"Admin\"}") + "." + parts[2];

        Assert.Equal(HttpStatusCode.Unauthorized, (await Me(null)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Me("not-a-token")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Me(tampered)).StatusCode);
    }

    [Fact]
    public async Task Me_refuses_expired_and_foreign_tokens()
    {
        var userId = (await RegisterOk(NewEmail())).User.UserId;
        string Make(string key, DateTime expires) => new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "MathMatura.API",
            Audience = "MathMatura.Client",
            NotBefore = expires.AddHours(-2),
            IssuedAt = expires.AddHours(-2),
            Expires = expires,
            Subject = new ClaimsIdentity([new Claim("sub", userId), new Claim("role", "User")]),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256),
        });

        Assert.Equal(HttpStatusCode.OK, (await Me(Make(ApiFactory.TestKey, DateTime.UtcNow.AddMinutes(5)))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Me(Make(ApiFactory.TestKey, DateTime.UtcNow.AddMinutes(-5)))).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await Me(Make("some-other-key-that-is-long-enough-123", DateTime.UtcNow.AddMinutes(5)))).StatusCode);
    }

    [Fact]
    public async Task Cors_allows_only_the_configured_frontend()
    {
        async Task<HttpResponseMessage> Preflight(string origin)
        {
            var req = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
            req.Headers.Add("Origin", origin);
            req.Headers.Add("Access-Control-Request-Method", "POST");
            req.Headers.Add("Access-Control-Request-Headers", "content-type");
            return await _client.SendAsync(req);
        }

        var ok = await Preflight("http://localhost:5173");
        var evil = await Preflight("https://evil.example");
        Assert.Equal(["http://localhost:5173"], ok.Headers.GetValues("Access-Control-Allow-Origin"));
        Assert.False(evil.Headers.Contains("Access-Control-Allow-Origin"));
    }
}

public class StartupTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("too-short")]
    public void App_refuses_to_start_without_a_strong_key(string? key)
    {
        using var factory = new ApiFactory { JwtKey = key };
        var ex = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());
        Assert.Contains("Jwt:Key", ex.Message);
    }
}
