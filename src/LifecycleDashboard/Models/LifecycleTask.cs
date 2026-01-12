namespace LifecycleDashboard.Models;

/// <summary>
/// Represents a lifecycle management task.
/// </summary>
public record LifecycleTask
{
    /// <summary>
    /// Unique identifier for the task.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Title of the task.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Detailed description of what needs to be done.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Type of lifecycle task.
    /// </summary>
    public required TaskType Type { get; init; }

    /// <summary>
    /// Priority of the task.
    /// </summary>
    public TaskPriority Priority { get; init; } = TaskPriority.Medium;

    /// <summary>
    /// Current status of the task.
    /// </summary>
    public TaskStatus Status { get; init; } = TaskStatus.Pending;

    /// <summary>
    /// Application this task is associated with.
    /// </summary>
    public required string ApplicationId { get; init; }

    /// <summary>
    /// Name of the associated application (denormalized for display).
    /// </summary>
    public required string ApplicationName { get; init; }

    /// <summary>
    /// User ID of the assigned person.
    /// </summary>
    public required string AssigneeId { get; init; }

    /// <summary>
    /// Name of the assigned person (denormalized for display).
    /// </summary>
    public required string AssigneeName { get; init; }

    /// <summary>
    /// Email of the assigned person.
    /// </summary>
    public string? AssigneeEmail { get; init; }

    /// <summary>
    /// Due date for the task.
    /// </summary>
    public DateTimeOffset DueDate { get; init; }

    /// <summary>
    /// Date when the task was created.
    /// </summary>
    public DateTimeOffset CreatedDate { get; init; }

    /// <summary>
    /// Date when the task was completed (if completed).
    /// </summary>
    public DateTimeOffset? CompletedDate { get; init; }

    /// <summary>
    /// Notes or comments on the task.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Whether the task is overdue.
    /// </summary>
    public bool IsOverdue => Status != TaskStatus.Completed && DueDate < DateTimeOffset.UtcNow;

    /// <summary>
    /// Days overdue (0 if not overdue).
    /// </summary>
    public int DaysOverdue => IsOverdue
        ? (int)(DateTimeOffset.UtcNow - DueDate).TotalDays
        : 0;

    /// <summary>
    /// Days until due (negative if overdue).
    /// </summary>
    public int DaysUntilDue => (int)(DueDate - DateTimeOffset.UtcNow).TotalDays;

    /// <summary>
    /// Whether escalation has been triggered.
    /// </summary>
    public bool IsEscalated { get; init; }

    /// <summary>
    /// Date when escalation was triggered (if applicable).
    /// </summary>
    public DateTimeOffset? EscalatedDate { get; init; }

    /// <summary>
    /// Original assignee if task was delegated.
    /// </summary>
    public string? OriginalAssigneeId { get; init; }

    /// <summary>
    /// Reason for delegation (if delegated).
    /// </summary>
    public string? DelegationReason { get; init; }

    /// <summary>
    /// History of changes to this task.
    /// </summary>
    public List<TaskHistoryEntry> History { get; init; } = [];

    /// <summary>
    /// Composite display key for user-friendly reference: ApplicationName/TaskType
    /// </summary>
    public string CompositeKey => $"{ApplicationName}/{Type}";

    /// <summary>
    /// Full composite key including assignee for unique identification
    /// </summary>
    public string FullCompositeKey => $"{AssigneeName}/{ApplicationName}/{Type}";
}

/// <summary>
/// Types of lifecycle tasks.
/// </summary>
public enum TaskType
{
    RoleValidation,
    SecurityRemediation,
    DocumentationReview,
    ArchitectureReview,
    RetirementReview,
    ComplianceCheck,
    DataConflictResolution,
    MaintenanceReview,
    Custom
}

/// <summary>
/// Task priority levels.
/// </summary>
public enum TaskPriority
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Task status values.
/// </summary>
public enum TaskStatus
{
    Pending,
    InProgress,
    Blocked,
    Completed,
    Cancelled
}

/// <summary>
/// Summary of tasks for a user or application.
/// </summary>
public record TaskSummary
{
    public int Total { get; init; }
    public int Overdue { get; init; }
    public int DueThisWeek { get; init; }
    public int DueThisMonth { get; init; }
    public int Completed { get; init; }
    public int InProgress { get; init; }
}
