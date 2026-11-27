using MathMaturaProject.API.Middleware;
using MathMaturaProject.API.Services;
using MathMaturaProject.Data.Data;
using MathMaturaProject.Data.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
// Microsoft.AspNetCore.Builder (implicitly imported) also defines a SessionOptions type.
using SessionOptions = MathMaturaProject.API.Services.SessionOptions;

var builder = WebApplication.CreateBuilder(args);
// Add configuration for the database context
builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------- Cookie-based session auth ----------
builder.Services.Configure<SessionOptions>(builder.Configuration.GetSection(SessionOptions.SectionName));
builder.Services.AddSingleton<IPasswordHasher<UserProfile>, PasswordHasher<UserProfile>>();
builder.Services.AddSingleton<SessionCookieManager>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services
    .AddAuthentication(SessionAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(SessionAuthenticationHandler.SchemeName, null);
builder.Services.AddAuthorization();

// A browser frontend on another origin can only send/receive the cookie if CORS allows
// credentials for that exact origin. Configure origins under "Cors:AllowedOrigins".
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (allowedOrigins.Length > 0)
{
    builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));
}

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    // Tell browsers to use HTTPS only, so the Secure session cookie is never
    // sent over a plain-http request in the first place.
    app.UseHsts();
}

app.UseHttpsRedirection();

if (allowedOrigins.Length > 0)
{
    app.UseCors();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
