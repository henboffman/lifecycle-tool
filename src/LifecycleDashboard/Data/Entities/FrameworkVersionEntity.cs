namespace LifecycleDashboard.Data.Entities;

/// <summary>
/// Database entity for FrameworkVersion tracking.
/// </summary>
public class FrameworkVersionEntity
{
    public string Id { get; set; } = null!;

    /// <summary>
    /// The framework type (stored as string enum)
    /// </summary>
    public string Framework { get; set; } = null!;

    /// <summary>
    /// Version string (e.g., "8.0", "4.8", "3.12")
    /// </summary>
    public string Version { get; set; } = null!;

    /// <summary>
    /// Display name (e.g., ".NET 8", ".NET Framework 4.8")
    /// </summary>
    public string DisplayName { get; set; } = null!;

    /// <summary>
    /// Release date of this version
    /// </summary>
    public DateTimeOffset? ReleaseDate { get; set; }

    /// <summary>
    /// End of life date (null if indefinite support)
    /// </summary>
    public DateTimeOffset? EndOfLifeDate { get; set; }

    /// <summary>
    /// End of active support date
    /// </summary>
    public DateTimeOffset? EndOfActiveSupportDate { get; set; }

    /// <summary>
    /// Whether this is a Long Term Support release
    /// </summary>
    public bool IsLts { get; set; }

    /// <summary>
    /// Current support status (stored as string enum)
    /// </summary>
    public string Status { get; set; } = "Unknown";

    /// <summary>
    /// Latest patch version available
    /// </summary>
    public string? LatestPatchVersion { get; set; }

    /// <summary>
    /// Notes about this version
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Recommended upgrade path
    /// </summary>
    public string? RecommendedUpgradePath { get; set; }

    /// <summary>
    /// Whether this was auto-detected from repository scans
    /// </summary>
    public bool AutoDetected { get; set; }

    /// <summary>
    /// Target framework moniker for matching (e.g., "net8.0", "net472")
    /// </summary>
    public string? TargetFrameworkMoniker { get; set; }

    /// <summary>
    /// Whether this is system-seeded data from endoflife.date.
    /// System data can only be deleted by admin users.
    /// </summary>
    public bool IsSystemData { get; set; }

    // Audit fields
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
