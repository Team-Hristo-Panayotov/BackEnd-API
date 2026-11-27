using System.Text.Json;
using MathMaturaProject.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MathMaturaProject.Data.Data;

public class AppDbContext : IdentityDbContext<IdentityUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Test> Tests => Set<Test>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<TestAttempt> TestAttempts => Set<TestAttempt>();
    public DbSet<UserProfile> Profiles => Set<UserProfile>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LessonProgress> LessonProgresses => Set<LessonProgress>();

    public DbSet<Feedback> Feedbacks => Set<Feedback>();

    // Fixed ids, so the seed data never changes between migrations.
    public const string UserRoleId = "6f1d1a52-3c55-4b8e-9c55-1f0a5d1b2c01";
    public const string AdminRoleId = "6f1d1a52-3c55-4b8e-9c55-1f0a5d1b2c02";

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Builds the Identity tables (AspNetUsers, AspNetRoles, ...). Must run first.
        base.OnModelCreating(b);

        b.Entity<IdentityRole>().HasData(
            new IdentityRole { Id = UserRoleId, Name = "User", NormalizedName = "USER", ConcurrencyStamp = UserRoleId },
            new IdentityRole { Id = AdminRoleId, Name = "Admin", NormalizedName = "ADMIN", ConcurrencyStamp = AdminRoleId });

        // Every profile belongs to exactly one login. Deleting the login deletes the profile.
        b.Entity<UserProfile>()
            .HasOne<IdentityUser>()
            .WithOne()
            .HasForeignKey<UserProfile>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

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

        // ---------- JSON columns via value converters ----------

        // Question.Options (e.g. List<string> or string[])
        var optionsComparer = new ValueComparer<List<string>>(
            (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
            c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c == null ? null! : c.ToList());

        b.Entity<Question>()
            .Property(q => q.Options)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null)!,
                optionsComparer)
            .HasColumnType("json");

        // TestAttempt.Answers -> Dictionary<Guid, int>
        var answersComparer = new ValueComparer<Dictionary<Guid, int>>(
            (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.Count == c2.Count && !c1.Except(c2).Any()),
            c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v.Key.GetHashCode(), v.Value.GetHashCode())),
            c => c == null ? null! : c.ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

        b.Entity<TestAttempt>()
            .Property(a => a.Answers)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<Guid, int>>(v, (JsonSerializerOptions?)null)!,
                answersComparer)
            .HasColumnType("json");

        // Lesson.PrerequisiteLessonIds -> List<Guid>
        var prereqComparer = new ValueComparer<List<Guid>>(
            (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
            c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c == null ? null! : c.ToList());

        b.Entity<Lesson>()
            .Property(l => l.PrerequisiteLessonIds)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null)!,
                prereqComparer)
            .HasColumnType("json");

        b.Entity<Feedback>(e =>
        {
            e.Property(f => f.Title)
                .IsRequired()
                .HasMaxLength(200);

            e.Property(f => f.Content)
                .IsRequired()
                .HasMaxLength(1000);

            // Връзка към потребител (ако решиш да я ползваш)
            //e.HasOne(f => f.User)
            //    .WithMany()
            //    .HasForeignKey(f => f.UserId)
            //    .OnDelete(DeleteBehavior.SetNull);
        });
    }
}