namespace LifecycleDashboard.Data.Entities;

/// <summary>
/// Database entity for RepositoryInfo.
/// </summary>
public class RepositoryInfoEntity
{
    public string Id { get; set; } = null!;
    public string RepositoryId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? DefaultBranch { get; set; }
    public string? Url { get; set; }

    // Build information
    public DateTimeOffset? LastBuildDate { get; set; }
    public string? LastBuildStatus { get; set; }

    // Application Insights
    public bool HasApplicationInsights { get; set; }
    public string? ApplicationInsightsKey { get; set; }

    // Sync info
    public DateTimeOffset LastSyncDate { get; set; }

    // JSON-serialized complex properties (nested objects)
    public string PackagesJson { get; set; } = "[]";
    public string StackJson { get; set; } = "{}";
    public string CommitsJson { get; set; } = "{}";
    public string ReadmeJson { get; set; } = "{}";
    public string SystemDependenciesJson { get; set; } = "[]";

    // Audit fields
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
