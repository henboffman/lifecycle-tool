namespace LifecycleDashboard.Models;

/// <summary>
/// Represents a framework/runtime version with lifecycle information.
/// Used for tracking EOL dates and planning upgrades.
/// </summary>
public record FrameworkVersion
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The framework type (e.g., DotNet, DotNetFramework, Python, R)
    /// </summary>
    public required FrameworkType Framework { get; init; }

    /// <summary>
    /// Version string (e.g., "8.0", "4.8", "3.12")
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Display name (e.g., ".NET 8", ".NET Framework 4.8", "Python 3.12")
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Release date of this version
    /// </summary>
    public DateTimeOffset? ReleaseDate { get; init; }

    /// <summary>
    /// End of life date (null if indefinite support like .NET Framework 4.8)
    /// </summary>
    public DateTimeOffset? EndOfLifeDate { get; init; }

    /// <summary>
    /// End of active support date (after this, only security fixes)
    /// </summary>
    public DateTimeOffset? EndOfActiveSupportDate { get; init; }

    /// <summary>
    /// Whether this is a Long Term Support release
    /// </summary>
    public bool IsLts { get; init; }

    /// <summary>
    /// Current support status
    /// </summary>
    public SupportStatus Status { get; init; }

    /// <summary>
    /// Latest patch version available
    /// </summary>
    public string? LatestPatchVersion { get; init; }

    /// <summary>
    /// Notes about this version (special considerations, migration paths, etc.)
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Recommended upgrade path (which version to migrate to)
    /// </summary>
    public string? RecommendedUpgradePath { get; init; }

    /// <summary>
    /// When this record was last updated
    /// </summary>
    public DateTimeOffset LastUpdated { get; init; }

    /// <summary>
    /// Calculate days until EOL (negative if already EOL)
    /// </summary>
    public int? DaysUntilEol => EndOfLifeDate.HasValue
        ? (int)(EndOfLifeDate.Value - DateTimeOffset.UtcNow).TotalDays
        : null;

    /// <summary>
    /// Whether this version is approaching EOL (within 12 months)
    /// </summary>
    public bool IsApproachingEol => DaysUntilEol.HasValue && DaysUntilEol.Value > 0 && DaysUntilEol.Value <= 365;

    /// <summary>
    /// Whether this version is past EOL
    /// </summary>
    public bool IsPastEol => DaysUntilEol.HasValue && DaysUntilEol.Value < 0;

    /// <summary>
    /// EOL urgency level for display
    /// </summary>
    public EolUrgency EolUrgency
    {
        get
        {
            if (!DaysUntilEol.HasValue) return EolUrgency.None;
            if (DaysUntilEol.Value < 0) return EolUrgency.PastEol;
            if (DaysUntilEol.Value <= 90) return EolUrgency.Critical;
            if (DaysUntilEol.Value <= 180) return EolUrgency.High;
            if (DaysUntilEol.Value <= 365) return EolUrgency.Medium;
            return EolUrgency.Low;
        }
    }
}

/// <summary>
/// Types of frameworks/runtimes we track
/// </summary>
public enum FrameworkType
{
    /// <summary>
    /// Modern .NET (5+, including .NET Core)
    /// </summary>
    DotNet,

    /// <summary>
    /// Legacy .NET Framework (3.5 - 4.8.1)
    /// </summary>
    DotNetFramework,

    /// <summary>
    /// Python runtime
    /// </summary>
    Python,

    /// <summary>
    /// R language runtime
    /// </summary>
    R,

    /// <summary>
    /// Node.js runtime (for Aurelia apps)
    /// </summary>
    NodeJs,

    /// <summary>
    /// Java runtime
    /// </summary>
    Java,

    /// <summary>
    /// Other/custom framework
    /// </summary>
    Other
}

/// <summary>
/// Support status for a framework version
/// </summary>
public enum SupportStatus
{
    /// <summary>
    /// Actively supported with bug fixes and security updates
    /// </summary>
    Active,

    /// <summary>
    /// In maintenance mode (security fixes only)
    /// </summary>
    Maintenance,

    /// <summary>
    /// Support has ended
    /// </summary>
    EndOfLife,

    /// <summary>
    /// Preview/RC release (not for production)
    /// </summary>
    Preview,

    /// <summary>
    /// Unknown status
    /// </summary>
    Unknown
}

/// <summary>
/// EOL urgency levels for prioritization
/// </summary>
public enum EolUrgency
{
    /// <summary>
    /// No EOL date or indefinite support
    /// </summary>
    None,

    /// <summary>
    /// More than 1 year until EOL
    /// </summary>
    Low,

    /// <summary>
    /// 6-12 months until EOL
    /// </summary>
    Medium,

    /// <summary>
    /// 3-6 months until EOL
    /// </summary>
    High,

    /// <summary>
    /// Less than 3 months until EOL
    /// </summary>
    Critical,

    /// <summary>
    /// Already past EOL
    /// </summary>
    PastEol
}

/// <summary>
/// Represents the detected framework usage in a repository/application
/// </summary>
public record ApplicationFramework
{
    /// <summary>
    /// The application this framework usage belongs to
    /// </summary>
    public required string ApplicationId { get; init; }

    /// <summary>
    /// The framework type
    /// </summary>
    public required FrameworkType Framework { get; init; }

    /// <summary>
    /// The detected version string
    /// </summary>
    public required string DetectedVersion { get; init; }

    /// <summary>
    /// Reference to the FrameworkVersion record (if matched)
    /// </summary>
    public string? FrameworkVersionId { get; init; }

    /// <summary>
    /// Source of the detection (e.g., "csproj", "package.json", "requirements.txt")
    /// </summary>
    public required string DetectionSource { get; init; }

    /// <summary>
    /// File path where this was detected
    /// </summary>
    public string? DetectionPath { get; init; }

    /// <summary>
    /// When this was last scanned
    /// </summary>
    public DateTimeOffset LastScanned { get; init; }

    /// <summary>
    /// Whether an upgrade is recommended based on EOL status
    /// </summary>
    public bool UpgradeRecommended { get; init; }

    /// <summary>
    /// Notes from the scan
    /// </summary>
    public string? ScanNotes { get; init; }
}
