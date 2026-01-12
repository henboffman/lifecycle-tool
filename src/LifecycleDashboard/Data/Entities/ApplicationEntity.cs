using LifecycleDashboard.Models;

namespace LifecycleDashboard.Data.Entities;

/// <summary>
/// Database entity for Application.
/// </summary>
public class ApplicationEntity
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public string Capability { get; set; } = null!;
    public string? RepositoryUrl { get; set; }
    public string? DocumentationUrl { get; set; }
    public string? ServiceNowId { get; set; }
    public bool IsMockData { get; set; }
    public int HealthScore { get; set; }
    public DateTimeOffset? LastActivityDate { get; set; }
    public DateTimeOffset LastSyncDate { get; set; }
    public bool HasDataConflicts { get; set; }

    // Application Classification (from ServiceNow)
    public string ApplicationType { get; set; } = "Unknown";  // Maps to AppType enum
    public string ArchitectureType { get; set; } = "Unknown"; // Maps to ArchitectureType enum
    public string? UserBaseEstimate { get; set; }
    public string? Importance { get; set; }

    // JSON-serialized complex properties
    public string TechnologyStackJson { get; set; } = "[]";
    public string TagsJson { get; set; } = "[]";
    public string SecurityFindingsJson { get; set; } = "[]";
    public string RoleAssignmentsJson { get; set; } = "[]";
    public string? UsageJson { get; set; }
    public string DocumentationJson { get; set; } = "{}";
    public string DataConflictsJson { get; set; } = "[]";
    public string? SecurityReviewJson { get; set; }
    public string UpdateHistoryJson { get; set; } = "[]";
    public string UsageAvailabilityJson { get; set; } = "{}";
    public string CriticalPeriodsJson { get; set; } = "[]";
    public string KeyDatesJson { get; set; } = "[]";

    // Audit fields
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
