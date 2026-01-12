namespace LifecycleDashboard.Models;

/// <summary>
/// Repository information from Azure DevOps including packages, stack, commits, and dependencies.
/// </summary>
public record RepositoryInfo
{
    public required string RepositoryId { get; init; }
    public required string Name { get; init; }
    public string? DefaultBranch { get; init; }
    public string? Url { get; init; }

    /// <summary>
    /// Package references found in the repository (NuGet, npm, etc.)
    /// </summary>
    public List<PackageReference> Packages { get; init; } = [];

    /// <summary>
    /// Detected technology stack information
    /// </summary>
    public required TechnologyStackInfo Stack { get; init; }

    /// <summary>
    /// Commit history and contributor information
    /// </summary>
    public required CommitHistory Commits { get; init; }

    /// <summary>
    /// README.md analysis
    /// </summary>
    public ReadmeStatus Readme { get; init; } = new();

    /// <summary>
    /// Whether Application Insights is implemented
    /// </summary>
    public bool HasApplicationInsights { get; init; }
    public string? ApplicationInsightsKey { get; init; }

    /// <summary>
    /// System dependencies detected from config files (appsettings.json, web.config)
    /// </summary>
    public List<SystemDependency> SystemDependencies { get; init; } = [];

    /// <summary>
    /// Build information
    /// </summary>
    public DateTimeOffset? LastBuildDate { get; init; }
    public string? LastBuildStatus { get; init; }

    /// <summary>
    /// Last time this data was synced from Azure DevOps
    /// </summary>
    public DateTimeOffset LastSyncDate { get; init; }
}

/// <summary>
/// A package reference (NuGet, npm, etc.) found in the repository
/// </summary>
public record PackageReference
{
    public required string Name { get; init; }
    public required string Version { get; init; }
    public string? LatestVersion { get; init; }
    public PackageType Type { get; init; }
    public bool HasKnownVulnerability { get; init; }
    public string? VulnerabilityDescription { get; init; }

    /// <summary>
    /// Whether this package is outdated (newer version available)
    /// </summary>
    public bool IsOutdated => !string.IsNullOrEmpty(LatestVersion) && Version != LatestVersion;
}

public enum PackageType
{
    NuGet,
    Npm,
    Pip,
    Maven,
    Other
}

/// <summary>
/// Technology stack detection results
/// </summary>
public record TechnologyStackInfo
{
    public required StackType PrimaryStack { get; init; }

    /// <summary>
    /// Frameworks detected (e.g., ASP.NET Core, Entity Framework, Aurelia)
    /// </summary>
    public List<string> Frameworks { get; init; } = [];

    /// <summary>
    /// Programming languages detected
    /// </summary>
    public List<string> Languages { get; init; } = [];

    /// <summary>
    /// Detected pattern from common combinations (e.g., "dotnet+aurelia", "blazor+dotnet", "full-framework")
    /// </summary>
    public string? DetectedPattern { get; init; }

    /// <summary>
    /// Target framework version (e.g., "net8.0", "net472")
    /// </summary>
    public string? TargetFramework { get; init; }
}

public enum StackType
{
    DotNetCore,
    DotNetFramework,
    Blazor,
    NodeJs,
    Python,
    Java,
    Mixed,
    Unknown
}

/// <summary>
/// Commit history and contributor information
/// </summary>
public record CommitHistory
{
    public DateTimeOffset? FirstCommitDate { get; init; }
    public DateTimeOffset? LastCommitDate { get; init; }
    public string? FirstCommitter { get; init; }
    public string? FirstCommitterEmail { get; init; }
    public string? LastCommitter { get; init; }
    public string? LastCommitterEmail { get; init; }

    /// <summary>
    /// The contributor with the most commits in the last 365 days
    /// </summary>
    public string? TopCommitter { get; init; }
    public string? TopCommitterEmail { get; init; }

    public int TotalCommitCount { get; init; }
    public int Last30DaysCommitCount { get; init; }
    public int Last90DaysCommitCount { get; init; }
    public int Last365DaysCommitCount { get; init; }

    /// <summary>
    /// Top contributors by commit count
    /// </summary>
    public List<ContributorInfo> TopContributors { get; init; } = [];

    /// <summary>
    /// Days since last commit
    /// </summary>
    public int DaysSinceLastCommit => LastCommitDate.HasValue
        ? (int)(DateTimeOffset.UtcNow - LastCommitDate.Value).TotalDays
        : -1;

    /// <summary>
    /// Whether the repository appears stale (no commits in 365+ days)
    /// </summary>
    public bool IsStale => DaysSinceLastCommit > 365;
}

/// <summary>
/// Contributor information
/// </summary>
public record ContributorInfo
{
    public required string Name { get; init; }
    public required string Email { get; init; }
    public int CommitCount { get; init; }
    public int Last365DaysCommitCount { get; init; }
    public DateTimeOffset? LastCommitDate { get; init; }
}

/// <summary>
/// README.md analysis results
/// </summary>
public record ReadmeStatus
{
    /// <summary>
    /// Whether README.md exists in the repository
    /// </summary>
    public bool Exists { get; init; }

    /// <summary>
    /// Whether the README appears to be an unedited template
    /// </summary>
    public bool IsTemplate { get; init; }

    /// <summary>
    /// Character count of the README
    /// </summary>
    public int CharacterCount { get; init; }

    /// <summary>
    /// Last modification date
    /// </summary>
    public DateTimeOffset? LastModified { get; init; }

    /// <summary>
    /// Whether the README has meaningful content (exists, not a template, > 500 chars)
    /// </summary>
    public bool HasMeaningfulContent => Exists && !IsTemplate && CharacterCount > 500;
}

/// <summary>
/// System dependency detected from configuration files
/// </summary>
public record SystemDependency
{
    public required string Name { get; init; }

    /// <summary>
    /// Type of dependency (Database, API, MessageQueue, Cache, Storage, etc.)
    /// </summary>
    public required DependencyType Type { get; init; }

    /// <summary>
    /// Sanitized/masked connection string or endpoint (sensitive parts redacted)
    /// </summary>
    public string? ConnectionInfo { get; init; }

    /// <summary>
    /// Where this dependency was detected (appsettings.json, web.config, etc.)
    /// </summary>
    public string? ConfigSource { get; init; }

    /// <summary>
    /// Whether this dependency appears to be for a production environment
    /// </summary>
    public bool IsProduction { get; init; }
}

public enum DependencyType
{
    SqlServer,
    Oracle,
    PostgreSql,
    MySql,
    CosmosDb,
    Redis,
    ServiceBus,
    EventHub,
    BlobStorage,
    KeyVault,
    ApplicationInsights,
    RestApi,
    Ldap,
    Smtp,
    Other
}
