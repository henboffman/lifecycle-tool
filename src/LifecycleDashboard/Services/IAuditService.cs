using LifecycleDashboard.Models;

namespace LifecycleDashboard.Services;

/// <summary>
/// Service for recording audit log entries with strongly-typed helper methods.
/// Wraps the underlying data service to provide consistent, type-safe audit logging.
/// </summary>
public interface IAuditService
{
    #region Authentication Events

    /// <summary>Records a successful user login.</summary>
    Task LogLoginAsync(string userId, string userName, string? ipAddress = null);

    /// <summary>Records a user logout.</summary>
    Task LogLogoutAsync(string userId, string userName);

    /// <summary>Records a failed login attempt.</summary>
    Task LogLoginFailedAsync(string userName, string failureReason, string? ipAddress = null);

    /// <summary>Records a session expiration.</summary>
    Task LogSessionExpiredAsync(string userId, string userName);

    #endregion

    #region Task Events

    /// <summary>Records task creation.</summary>
    Task LogTaskCreatedAsync(LifecycleTask task, string performedByUserId, string performedByName);

    /// <summary>Records task update.</summary>
    Task LogTaskUpdatedAsync(string taskId, string taskTitle, string performedByUserId, string performedByName, Dictionary<string, (string? OldValue, string? NewValue)>? changes = null);

    /// <summary>Records task deletion.</summary>
    Task LogTaskDeletedAsync(string taskId, string taskTitle, string performedByUserId, string performedByName);

    /// <summary>Records task status change.</summary>
    Task LogTaskStatusChangedAsync(string taskId, string taskTitle, string oldStatus, string newStatus, string performedByUserId, string performedByName, string? notes = null);

    /// <summary>Records task assignment.</summary>
    Task LogTaskAssignedAsync(string taskId, string taskTitle, string? previousAssigneeId, string? previousAssigneeName, string newAssigneeId, string newAssigneeName, string performedByUserId, string performedByName);

    /// <summary>Records task delegation.</summary>
    Task LogTaskDelegatedAsync(string taskId, string taskTitle, string fromUserId, string fromUserName, string toUserId, string toUserName, string reason, string performedByUserId, string performedByName);

    /// <summary>Records task escalation.</summary>
    Task LogTaskEscalatedAsync(string taskId, string taskTitle, string reason, string performedByUserId, string performedByName);

    /// <summary>Records task completion.</summary>
    Task LogTaskCompletedAsync(string taskId, string taskTitle, string performedByUserId, string performedByName, string? notes = null);

    /// <summary>Records a note added to task history.</summary>
    Task LogTaskNoteAddedAsync(string taskId, string taskTitle, string performedByUserId, string performedByName);

    #endregion

    #region Application Events

    /// <summary>Records application data update.</summary>
    Task LogApplicationUpdatedAsync(string appId, string appName, string performedByUserId, string performedByName, Dictionary<string, (string? OldValue, string? NewValue)>? changes = null);

    /// <summary>Records application role assignment.</summary>
    Task LogApplicationRoleChangedAsync(string appId, string appName, string roleType, string? previousUserId, string? previousUserName, string? newUserId, string? newUserName, string performedByUserId, string performedByName);

    /// <summary>Records data conflict detection for an application.</summary>
    Task LogApplicationConflictDetectedAsync(string appId, string appName, string conflictType, string description);

    /// <summary>Records data conflict resolution for an application.</summary>
    Task LogApplicationConflictResolvedAsync(string appId, string appName, string conflictType, string resolution, string performedByUserId, string performedByName);

    #endregion

    #region Configuration Events

    /// <summary>Records configuration change.</summary>
    Task LogConfigChangedAsync(string configType, string performedByUserId, string performedByName, Dictionary<string, (string? OldValue, string? NewValue)>? changes = null);

    /// <summary>Records data source credential update (without exposing credential values).</summary>
    Task LogDataSourceCredentialUpdatedAsync(string dataSourceId, string dataSourceName, string performedByUserId, string performedByName);

    #endregion

    #region Data Sync Events

    /// <summary>Records data sync job start.</summary>
    Task LogSyncStartedAsync(string dataSourceName, string jobId);

    /// <summary>Records successful data sync completion.</summary>
    Task LogSyncCompletedAsync(string dataSourceName, string jobId, int recordsProcessed, int recordsCreated, int recordsUpdated, TimeSpan duration);

    /// <summary>Records data sync failure.</summary>
    Task LogSyncFailedAsync(string dataSourceName, string jobId, string errorMessage, TimeSpan duration);

    /// <summary>Records connection test.</summary>
    Task LogConnectionTestedAsync(string dataSourceName, bool success, string? message, string performedByUserId, string performedByName);

    #endregion

    #region Task Documentation Events

    /// <summary>Records task documentation creation.</summary>
    Task LogTaskDocCreatedAsync(TaskType taskType, string performedByUserId, string performedByName);

    /// <summary>Records task documentation update.</summary>
    Task LogTaskDocUpdatedAsync(TaskType taskType, string performedByUserId, string performedByName);

    /// <summary>Records task documentation deletion.</summary>
    Task LogTaskDocDeletedAsync(TaskType taskType, string performedByUserId, string performedByName);

    #endregion

    #region Framework Events

    /// <summary>Records framework EOL data refresh.</summary>
    Task LogFrameworkEolRefreshedAsync(string frameworkType, int versionsAdded, int versionsUpdated, string performedByUserId, string performedByName);

    #endregion

    #region Security Events

    /// <summary>Records security finding acknowledgement.</summary>
    Task LogSecurityFindingAcknowledgedAsync(string findingId, string appId, string appName, string performedByUserId, string performedByName);

    /// <summary>Records security finding resolution.</summary>
    Task LogSecurityFindingResolvedAsync(string findingId, string appId, string appName, string performedByUserId, string performedByName);

    #endregion

    #region Export Events

    /// <summary>Records data export.</summary>
    Task LogDataExportedAsync(string exportType, int recordCount, string performedByUserId, string performedByName);

    #endregion

    #region Generic Logging

    /// <summary>Records a custom audit event for cases not covered by typed methods.</summary>
    Task LogCustomEventAsync(string eventType, string category, string message, string? entityType = null, string? entityId = null, string? userId = null, string? userName = null, Dictionary<string, string>? details = null);

    #endregion
}
