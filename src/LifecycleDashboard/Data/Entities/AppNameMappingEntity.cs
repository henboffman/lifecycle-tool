namespace LifecycleDashboard.Data.Entities;

/// <summary>
/// Database entity for AppNameMapping.
/// </summary>
public class AppNameMappingEntity
{
    public string Id { get; set; } = null!;
    public string ServiceNowAppName { get; set; } = null!;
    public string? SharePointFolderName { get; set; }
    public string? Capability { get; set; }
    public string? Notes { get; set; }

    // JSON-serialized arrays
    public string AzureDevOpsRepoNamesJson { get; set; } = "[]";
    public string AlternativeNamesJson { get; set; } = "[]";

    // Audit fields
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}

/// <summary>
/// Database entity for CapabilityMapping.
/// </summary>
public class CapabilityMappingEntity
{
    public string Id { get; set; } = null!;
    public string ApplicationName { get; set; } = null!;
    public string Capability { get; set; } = null!;

    // Audit fields
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}

/// <summary>
/// Database entity for AuditLog.
/// </summary>
public class AuditLogEntity
{
    public string Id { get; set; } = null!;
    public DateTimeOffset Timestamp { get; set; }
    public string EventType { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string Message { get; set; } = null!;
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? EntityId { get; set; }
    public string? EntityType { get; set; }

    // JSON-serialized (maps to Details in model)
    public string? DetailsJson { get; set; }
}

/// <summary>
/// Database entity for SyncJobInfo.
/// </summary>
public class SyncJobEntity
{
    public string Id { get; set; } = null!;
    public string DataSource { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public int RecordsProcessed { get; set; }
    public int RecordsCreated { get; set; }
    public int RecordsUpdated { get; set; }
    public int ErrorCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string? TriggeredBy { get; set; }
}

/// <summary>
/// Database entity for SharePoint folder discovery.
/// </summary>
public class DiscoveredSharePointFolderEntity
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string FullPath { get; set; } = null!;
    public string? Url { get; set; }
    public string? Capability { get; set; }
    public DateTimeOffset? LastModified { get; set; }
    public DateTimeOffset SyncedAt { get; set; }
    public string? LinkedServiceNowAppName { get; set; }
    public string? LinkedApplicationId { get; set; }

    // JSON-serialized
    public string TemplateFoldersFoundJson { get; set; } = "[]";
    public string DocumentCountsJson { get; set; } = "{}";
}

/// <summary>
/// Database entity for TaskDocumentation.
/// </summary>
public class TaskDocumentationEntity
{
    public string Id { get; set; } = null!;
    public string TaskType { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public long? EstimatedDurationTicks { get; set; }
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
    public string? LastUpdatedBy { get; set; }

    // JSON-serialized complex properties
    public string InstructionsJson { get; set; } = "[]";
    public string SystemGuidanceJson { get; set; } = "[]";
    public string RelatedLinksJson { get; set; } = "[]";
    public string PrerequisitesJson { get; set; } = "[]";
    public string TypicalRolesJson { get; set; } = "[]";
}
