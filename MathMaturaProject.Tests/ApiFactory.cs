using MathMaturaProject.Data.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace MathMaturaProject.Tests;

/// <summary>
/// Runs the real API in memory, with SQLite in place of SQL Server.
/// The schema uses SQL Server 2025 "json" columns, which SQLite simply stores as text.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public const string TestKey = "test-signing-key-only-for-tests-0123456789";
    private readonly SqliteConnection _db = new("DataSource=:memory:");

    // Null means "leave the key unset", for the startup check test.
    public string? JwtKey { get; init; } = TestKey;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Jwt:Key", JwtKey ?? "");
        builder.ConfigureServices(services =>
        {
            // EF 9+ keeps the provider in two places. Both must go, or EF sees two providers.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            _db.Open();
            services.AddDbContext<AppDbContext>(o => o.UseSqlite(_db));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _db.Dispose();
    }
}
