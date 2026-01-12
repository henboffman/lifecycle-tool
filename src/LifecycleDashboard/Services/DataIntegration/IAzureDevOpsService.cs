using LifecycleDashboard.Models;

namespace LifecycleDashboard.Services.DataIntegration;

/// <summary>
/// Service for integrating with Azure DevOps to retrieve repository data,
/// detect technology stacks, analyze packages, and track commit history.
/// </summary>
public interface IAzureDevOpsService
{
    /// <summary>
    /// Tests the connection to Azure DevOps using configured credentials.
    /// </summary>
    Task<ConnectionTestResult> TestConnectionAsync();

    /// <summary>
    /// Gets all repositories from the configured Azure DevOps project.
    /// </summary>
    Task<DataSyncResult<List<AzureDevOpsRepository>>> GetRepositoriesAsync();

    /// <summary>
    /// Detects the technology stack for a repository by analyzing project files.
    /// Parses .csproj, package.json, requirements.txt, etc.
    /// </summary>
    Task<DataSyncResult<TechStackDetectionResult>> DetectTechStackAsync(string repositoryId);

    /// <summary>
    /// Gets package references from a repository (NuGet, npm, pip, etc.).
    /// </summary>
    Task<DataSyncResult<List<PackageReference>>> GetPackagesAsync(string repositoryId);

    /// <summary>
    /// Gets commit history for a repository.
    /// </summary>
    Task<DataSyncResult<CommitHistory>> GetCommitHistoryAsync(string repositoryId, int daysBefore = 365);

    /// <summary>
    /// Gets README status for a repository.
    /// </summary>
    Task<DataSyncResult<ReadmeStatus>> GetReadmeStatusAsync(string repositoryId);

    /// <summary>
    /// Gets the latest pipeline/build status for a repository.
    /// </summary>
    Task<DataSyncResult<PipelineStatus>> GetPipelineStatusAsync(string repositoryId);

    /// <summary>
    /// Detects system dependencies by scanning configuration files
    /// (appsettings.json, web.config, connection strings, etc.).
    /// </summary>
    Task<DataSyncResult<List<SystemDependency>>> DetectSystemDependenciesAsync(string repositoryId);

    /// <summary>
    /// Syncs all repository data for a specific application.
    /// </summary>
    Task<DataSyncResult> SyncRepositoryDataAsync(string applicationId, string repositoryUrl);

    /// <summary>
    /// Syncs all repository data across all configured applications.
    /// </summary>
    Task<DataSyncResult> SyncAllRepositoriesAsync();
}

/// <summary>
/// Azure DevOps repository information.
/// </summary>
public record AzureDevOpsRepository
{
    /// <summary>Repository ID in Azure DevOps.</summary>
    public required string Id { get; init; }

    /// <summary>Repository name.</summary>
    public required string Name { get; init; }

    /// <summary>Full URL to the repository.</summary>
    public required string Url { get; init; }

    /// <summary>Clone URL for the repository.</summary>
    public string? CloneUrl { get; init; }

    /// <summary>Default branch (usually main or master).</summary>
    public string? DefaultBranch { get; init; }

    /// <summary>Project this repository belongs to.</summary>
    public string? ProjectName { get; init; }

    /// <summary>Size in bytes.</summary>
    public long? SizeBytes { get; init; }

    /// <summary>Date of last commit.</summary>
    public DateTimeOffset? LastCommitDate { get; init; }

    /// <summary>Whether the repository is disabled.</summary>
    public bool IsDisabled { get; init; }
}

/// <summary>
/// Result of technology stack detection.
/// </summary>
public record TechStackDetectionResult
{
    /// <summary>Primary detected stack.</summary>
    public PrimaryStack PrimaryStack { get; init; }

    /// <summary>Frameworks detected in the repository.</summary>
    public List<string> Frameworks { get; init; } = [];

    /// <summary>Programming languages detected.</summary>
    public List<string> Languages { get; init; } = [];

    /// <summary>Detected pattern description (e.g., "dotnet+aurelia", "blazor").</summary>
    public string? DetectedPattern { get; init; }

    /// <summary>Target framework (e.g., "net8.0", "net472").</summary>
    public string? TargetFramework { get; init; }

    /// <summary>Project files found in the repository.</summary>
    public List<string> ProjectFiles { get; init; } = [];
}

/// <summary>
/// Primary technology stack types.
/// </summary>
public enum PrimaryStack
{
    Unknown,
    DotNetCore,
    DotNetFramework,
    Blazor,
    NodeJs,
    Python,
    Java,
    Mixed
}

/// <summary>
/// Pipeline/build status information.
/// </summary>
public record PipelineStatus
{
    /// <summary>Latest build ID.</summary>
    public string? LastBuildId { get; init; }

    /// <summary>Pipeline/build definition name.</summary>
    public string? PipelineName { get; init; }

    /// <summary>Build status.</summary>
    public BuildStatus Status { get; init; }

    /// <summary>Build result.</summary>
    public BuildResult Result { get; init; }

    /// <summary>When the build started.</summary>
    public DateTimeOffset? StartTime { get; init; }

    /// <summary>When the build finished.</summary>
    public DateTimeOffset? FinishTime { get; init; }

    /// <summary>Who triggered the build.</summary>
    public string? RequestedBy { get; init; }

    /// <summary>Source branch.</summary>
    public string? SourceBranch { get; init; }
}

/// <summary>
/// Build status values.
/// </summary>
public enum BuildStatus
{
    Unknown,
    NotStarted,
    InProgress,
    Completed,
    Cancelling,
    Postponed
}

/// <summary>
/// Build result values.
/// </summary>
public enum BuildResult
{
    Unknown,
    Succeeded,
    PartiallySucceeded,
    Failed,
    Cancelled
}

/// <summary>
/// Package reference from a project file.
/// </summary>
public record PackageReference
{
    /// <summary>Package name.</summary>
    public required string Name { get; init; }

    /// <summary>Package version.</summary>
    public string? Version { get; init; }

    /// <summary>Package manager (NuGet, npm, pip, etc.).</summary>
    public required string PackageManager { get; init; }

    /// <summary>Source file where the package was found.</summary>
    public string? SourceFile { get; init; }

    /// <summary>Whether this is a development-only dependency.</summary>
    public bool IsDevelopmentDependency { get; init; }

    /// <summary>Whether the package is vulnerable.</summary>
    public bool? IsVulnerable { get; init; }

    /// <summary>Known vulnerabilities for this package version.</summary>
    public List<string> Vulnerabilities { get; init; } = [];
}

/// <summary>
/// Commit history information for a repository.
/// </summary>
public record CommitHistory
{
    /// <summary>Repository ID.</summary>
    public string? RepositoryId { get; init; }

    /// <summary>Total commits in the period.</summary>
    public int TotalCommits { get; init; }

    /// <summary>Date of the last commit.</summary>
    public DateTimeOffset? LastCommitDate { get; init; }

    /// <summary>Unique contributors.</summary>
    public List<string> Contributors { get; init; } = [];

    /// <summary>Commits per month.</summary>
    public Dictionary<DateOnly, int> CommitsPerMonth { get; init; } = [];

    /// <summary>Period start date.</summary>
    public DateTimeOffset PeriodStart { get; init; }

    /// <summary>Period end date.</summary>
    public DateTimeOffset PeriodEnd { get; init; }

    /// <summary>Average commits per month.</summary>
    public double AverageCommitsPerMonth => CommitsPerMonth.Count > 0
        ? CommitsPerMonth.Values.Average()
        : 0;

    /// <summary>Whether the repository is stale (no commits in 365+ days).</summary>
    public bool IsStale => LastCommitDate.HasValue &&
        DateTimeOffset.UtcNow.Subtract(LastCommitDate.Value).TotalDays > 365;
}

/// <summary>
/// README file status for a repository.
/// </summary>
public record ReadmeStatus
{
    /// <summary>Repository ID.</summary>
    public string? RepositoryId { get; init; }

    /// <summary>Whether a README exists.</summary>
    public bool Exists { get; init; }

    /// <summary>File size in bytes.</summary>
    public long? SizeBytes { get; init; }

    /// <summary>Number of lines.</summary>
    public int? LineCount { get; init; }

    /// <summary>Quality score (0-100).</summary>
    public int? QualityScore { get; init; }

    /// <summary>When the README was last updated.</summary>
    public DateTimeOffset? LastModified { get; init; }
}

/// <summary>
/// System dependency detected from configuration files.
/// </summary>
public record SystemDependency
{
    /// <summary>Dependency name/identifier.</summary>
    public required string Name { get; init; }

    /// <summary>Type of dependency (SQL Server, Redis, etc.).</summary>
    public required string Type { get; init; }

    /// <summary>Masked connection string.</summary>
    public string? ConnectionString { get; init; }

    /// <summary>Source file where detected.</summary>
    public string? SourceFile { get; init; }

    /// <summary>Environment (if determinable).</summary>
    public string? Environment { get; init; }

    /// <summary>Server/host name (if parseable).</summary>
    public string? Server { get; init; }

    /// <summary>Database/resource name (if parseable).</summary>
    public string? Database { get; init; }
}
