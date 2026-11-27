namespace MathMaturaProject.API.Services;

/// <summary>Bound from the "Session" section of appsettings.</summary>
public class SessionOptions
{
    public const string SectionName = "Session";

    /// <summary>Name of the session cookie.</summary>
    public string CookieName { get; set; } = "mm_session";

    /// <summary>Session lifespan in days. Clamped to 1-30.</summary>
    public int LifetimeDays { get; set; } = 14;

    /// <summary>Only Lax or Strict are accepted; anything else falls back to Lax.</summary>
    public SameSiteMode SameSite { get; set; } = SameSiteMode.Lax;

    public TimeSpan Lifetime => TimeSpan.FromDays(Math.Clamp(LifetimeDays, 1, 30));
}
