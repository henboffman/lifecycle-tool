using LifecycleDashboard.Models;

namespace LifecycleDashboard.Data.Entities;

/// <summary>
/// Database entity for LifecycleTask.
/// </summary>
public class LifecycleTaskEntity
{
    public string Id { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public TaskType Type { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public Models.TaskStatus Status { get; set; } = Models.TaskStatus.Pending;
    public string ApplicationId { get; set; } = null!;
    public string ApplicationName { get; set; } = null!;
    public string AssigneeId { get; set; } = null!;
    public string AssigneeName { get; set; } = null!;
    public string? AssigneeEmail { get; set; }
    public DateTimeOffset DueDate { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public DateTimeOffset? CompletedDate { get; set; }
    public string? Notes { get; set; }
    public bool IsEscalated { get; set; }
    public DateTimeOffset? EscalatedDate { get; set; }
    public string? OriginalAssigneeId { get; set; }
    public string? DelegationReason { get; set; }

    // JSON-serialized complex properties
    public string HistoryJson { get; set; } = "[]";

    // Audit fields
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
