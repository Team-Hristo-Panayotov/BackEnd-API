using MathMaturaProject.API.Middleware;
using MathMaturaProject.API.Services;
using MathMaturaProject.Data.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------- Users and passwords (ASP.NET Core Identity) ----------
builder.Services.AddIdentityCore<IdentityUser>(o =>
    {
        // AuthValidator holds the full rules. These only stop Identity adding rules of its own.
        o.Password.RequiredLength = AuthValidator.PasswordMin;
        o.Password.RequireDigit = true;
        o.Password.RequireUppercase = false;
        o.Password.RequireLowercase = false;
        o.Password.RequireNonAlphanumeric = false;
        o.User.RequireUniqueEmail = true;
        o.User.AllowedUserNameCharacters = ""; // The user name is the email, so allow any character.
        o.Lockout.MaxFailedAccessAttempts = 5;
        o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        o.Lockout.AllowedForNewUsers = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

// ---------- Tokens (JWT) ----------
builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .Validate(o => o.Key.Length >= JwtOptions.MinKeyLength,
        $"Jwt:Key is missing or shorter than {JwtOptions.MinKeyLength} characters. " +
        "Set it with: dotnet user-secrets set \"Jwt:Key\" \"<random text>\" --project MathMaturaProject.API " +
        "or with the environment variable Jwt__Key.")
    .Validate(o => o.Issuer.Length > 0 && o.Audience.Length > 0, "Jwt:Issuer and Jwt:Audience must be set.")
    .ValidateOnStart();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<TokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((o, jwt) =>
    {
        o.MapInboundClaims = false; // Keep "sub", "email" and "role" as they are in the token.
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Value.Issuer,
            ValidAudience = jwt.Value.Audience,
            IssuerSigningKey = jwt.Value.SigningKey(),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            NameClaimType = "sub",
            RoleClaimType = "role",
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });
builder.Services.AddAuthorization();

// ---------- Which websites may call this API (CORS) ----------
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));
// Add services to the container.
builder.Services.AddScoped<ILessonService, LessonService>();

// Return unhandled exceptions as ProblemDetails JSON
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Lets the test project start the app in memory.
public partial class Program;
