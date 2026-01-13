namespace LifecycleDashboard.Data.Entities;

/// <summary>
/// Database entity for SyncedRepository - stores Azure DevOps repository sync results.
/// </summary>
public class SyncedRepositoryEntity
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Url { get; set; } = null!;
    public string? CloneUrl { get; set; }
    public string? DefaultBranch { get; set; }
    public string? ProjectName { get; set; }
    public long? SizeBytes { get; set; }
    public bool IsDisabled { get; set; }

    // Sync metadata
    public DateTimeOffset SyncedAt { get; set; }
    public string? SyncedBy { get; set; }

    // Detected tech stack
    public string? PrimaryStack { get; set; }
    public string? TargetFramework { get; set; }
    public string? DetectedPattern { get; set; }

    // Commit info
    public int? TotalCommits { get; set; }
    public DateTimeOffset? LastCommitDate { get; set; }

    // Package counts
    public int NuGetPackageCount { get; set; }
    public int NpmPackageCount { get; set; }

    // Build/Pipeline info
    public string? LastBuildStatus { get; set; }
    public string? LastBuildResult { get; set; }
    public DateTimeOffset? LastBuildDate { get; set; }

    // README status
    public bool HasReadme { get; set; }
    public int? ReadmeQualityScore { get; set; }

    // Security / CodeQL (Advanced Security)
    public bool AdvancedSecurityEnabled { get; set; }
    public DateTimeOffset? LastSecurityScanDate { get; set; }
    public int OpenCriticalVulnerabilities { get; set; }
    public int OpenHighVulnerabilities { get; set; }
    public int OpenMediumVulnerabilities { get; set; }
    public int OpenLowVulnerabilities { get; set; }
    public int ClosedCriticalVulnerabilities { get; set; }
    public int ClosedHighVulnerabilities { get; set; }
    public int ClosedMediumVulnerabilities { get; set; }
    public int ClosedLowVulnerabilities { get; set; }
    public int ExposedSecretsCount { get; set; }
    public int DependencyAlertCount { get; set; }

    // Linked application
    public string? LinkedApplicationId { get; set; }
    public string? LinkedApplicationName { get; set; }

    // JSON-serialized collections
    public string FrameworksJson { get; set; } = "[]";
    public string LanguagesJson { get; set; } = "[]";
    public string ContributorsJson { get; set; } = "[]";
    public string PackagesJson { get; set; } = "[]";
    public string SecurityAlertsJson { get; set; } = "[]";
    public string SecretAlertsJson { get; set; } = "[]";

    // Audit fields
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
