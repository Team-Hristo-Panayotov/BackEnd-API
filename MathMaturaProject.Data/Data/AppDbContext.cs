using MathMaturaProject.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace MathMaturaProject.Data.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Test> Tests => Set<Test>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<TestAttempt> TestAttempts => Set<TestAttempt>();
    public DbSet<UserProfile> Profiles => Set<UserProfile>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LessonProgress> LessonProgresses => Set<LessonProgress>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Test>(e =>
        {
            e.HasMany(t => t.Questions)
             .WithOne(q => q.Test!)
             .HasForeignKey(q => q.TestId);
            e.HasMany(t => t.Attempts)
             .WithOne(a => a.Test!)
             .HasForeignKey(a => a.TestId);
        });

        b.Entity<TestAttempt>().HasKey(a => a.Id);
        b.Entity<UserProfile>().HasKey(p => p.UserId);

        b.Entity<LessonProgress>()
            .HasIndex(p => new { p.UserId, p.LessonId })
            .IsUnique();

        // JSON columns (Requires EF Core 8+ / SQLite supports this)
        b.Entity<Question>().Property(q => q.Options).HasColumnType("json");
        b.Entity<TestAttempt>().Property(a => a.Answers).HasColumnType("json");
        b.Entity<Lesson>().Property(l => l.PrerequisiteLessonIds).HasColumnType("json");
    }
}