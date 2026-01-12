namespace LifecycleDashboard.Models;

/// <summary>
/// Admin-editable documentation for a specific task type.
/// Provides step-by-step instructions and system-specific guidance.
/// </summary>
public record TaskDocumentation
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The task type this documentation applies to
    /// </summary>
    public required TaskType TaskType { get; init; }

    /// <summary>
    /// Display title for this documentation
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Overview description of what this task type involves
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Step-by-step instructions for completing this type of task
    /// </summary>
    public List<TaskInstruction> Instructions { get; init; } = [];

    /// <summary>
    /// System-specific guidance (e.g., "how to revalidate roles in ServiceNow")
    /// </summary>
    public List<SystemGuidance> SystemGuidance { get; init; } = [];

    /// <summary>
    /// Links to external documentation or resources
    /// </summary>
    public List<DocumentationLink> RelatedLinks { get; init; } = [];

    /// <summary>
    /// Estimated time to complete this type of task
    /// </summary>
    public TimeSpan? EstimatedDuration { get; init; }

    /// <summary>
    /// Prerequisites that must be met before starting
    /// </summary>
    public List<string> Prerequisites { get; init; } = [];

    /// <summary>
    /// Who typically performs this type of task (for reference)
    /// </summary>
    public List<ApplicationRole> TypicalRoles { get; init; } = [];

    /// <summary>
    /// When this documentation was last updated
    /// </summary>
    public DateTimeOffset LastUpdated { get; init; }

    /// <summary>
    /// Who last updated this documentation
    /// </summary>
    public string? LastUpdatedBy { get; init; }
}

/// <summary>
/// A single instruction step within task documentation
/// </summary>
public record TaskInstruction
{
    /// <summary>
    /// Step number (1-based)
    /// </summary>
    public int StepNumber { get; init; }

    /// <summary>
    /// Brief title for this step
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Detailed description of what to do
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Which system this step involves (ServiceNow, SharePoint, Azure DevOps, etc.)
    /// </summary>
    public string? SystemReference { get; init; }

    /// <summary>
    /// Direct URL to perform this action (if applicable)
    /// </summary>
    public string? ActionUrl { get; init; }

    /// <summary>
    /// Whether this step is optional
    /// </summary>
    public bool IsOptional { get; init; }

    /// <summary>
    /// Tips or warnings for this step
    /// </summary>
    public string? Notes { get; init; }
}

/// <summary>
/// System-specific guidance for completing tasks
/// </summary>
public record SystemGuidance
{
    /// <summary>
    /// The system this guidance applies to (ServiceNow, SharePoint, Azure DevOps, IIS)
    /// </summary>
    public required string SystemName { get; init; }

    /// <summary>
    /// The action type this guidance covers (e.g., "Revalidate Roles", "Update Documentation")
    /// </summary>
    public required string ActionType { get; init; }

    /// <summary>
    /// Detailed instructions for this system-specific action
    /// </summary>
    public required string Instructions { get; init; }

    /// <summary>
    /// Direct link to the system or specific page
    /// </summary>
    public string? DirectLink { get; init; }

    /// <summary>
    /// Required permissions or access level
    /// </summary>
    public string? RequiredPermissions { get; init; }

    /// <summary>
    /// Common issues and how to resolve them
    /// </summary>
    public List<TroubleshootingTip> TroubleshootingTips { get; init; } = [];
}

/// <summary>
/// Troubleshooting tip for common issues
/// </summary>
public record TroubleshootingTip
{
    public required string Issue { get; init; }
    public required string Resolution { get; init; }
}

/// <summary>
/// Link to external documentation or resources
/// </summary>
public record DocumentationLink
{
    public required string Title { get; init; }
    public required string Url { get; init; }
    public string? Description { get; init; }
    public DocumentationLinkType Type { get; init; }
}

public enum DocumentationLinkType
{
    InternalDocs,
    SharePoint,
    Confluence,
    ExternalReference,
    Video,
    Template
}

/// <summary>
/// History entry for tracking changes to a task
/// </summary>
public record TaskHistoryEntry
{
    public required string Id { get; init; }
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Action performed: Created, StatusChanged, Assigned, Delegated, NoteAdded, Escalated, Completed
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// Who performed this action
    /// </summary>
    public required string PerformedBy { get; init; }

    /// <summary>
    /// User ID of performer
    /// </summary>
    public string? PerformedById { get; init; }

    /// <summary>
    /// Previous value (for status changes, assignments, etc.)
    /// </summary>
    public string? OldValue { get; init; }

    /// <summary>
    /// New value
    /// </summary>
    public string? NewValue { get; init; }

    /// <summary>
    /// Additional notes or context
    /// </summary>
    public string? Notes { get; init; }
}
