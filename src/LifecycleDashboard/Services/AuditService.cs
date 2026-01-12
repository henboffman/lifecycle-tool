using LifecycleDashboard.Models;

namespace LifecycleDashboard.Services;

/// <summary>
/// Implementation of IAuditService that wraps IMockDataService.RecordAuditLogAsync
/// to provide strongly-typed, consistent audit logging throughout the application.
/// </summary>
public class AuditService : IAuditService
{
    private readonly IMockDataService _dataService;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IMockDataService dataService, ILogger<AuditService> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    #region Authentication Events

    public async Task LogLoginAsync(string userId, string userName, string? ipAddress = null)
    {
        var details = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(ipAddress))
            details["IpAddress"] = ipAddress;

        await RecordAsync(
            AuditEventTypes.UserLogin,
            AuditCategories.Auth,
            $"User '{userName}' logged in",
            AuditEntityTypes.User,
            userId,
            userId,
            userName,
            details);
    }

    public async Task LogLogoutAsync(string userId, string userName)
    {
        await RecordAsync(
            AuditEventTypes.UserLogout,
            AuditCategories.Auth,
            $"User '{userName}' logged out",
            AuditEntityTypes.User,
            userId,
            userId,
            userName);
    }

    public async Task LogLoginFailedAsync(string userName, string failureReason, string? ipAddress = null)
    {
        var details = new Dictionary<string, string>
        {
            ["FailureReason"] = failureReason
        };
        if (!string.IsNullOrEmpty(ipAddress))
            details["IpAddress"] = ipAddress;

        await RecordAsync(
            AuditEventTypes.UserLoginFailed,
            AuditCategories.Auth,
            $"Login failed for '{userName}': {failureReason}",
            AuditEntityTypes.User,
            null,
            null,
            userName,
            details);
    }

    public async Task LogSessionExpiredAsync(string userId, string userName)
    {
        await RecordAsync(
            AuditEventTypes.UserSessionExpired,
            AuditCategories.Auth,
            $"Session expired for user '{userName}'",
            AuditEntityTypes.User,
            userId,
            userId,
            userName);
    }

    #endregion

    #region Task Events

    public async Task LogTaskCreatedAsync(LifecycleTask task, string performedByUserId, string performedByName)
    {
        var details = new Dictionary<string, string>
        {
            ["TaskType"] = task.Type.ToString(),
            ["Priority"] = task.Priority.ToString(),
            ["AssigneeId"] = task.AssigneeId,
            ["AssigneeName"] = task.AssigneeName,
            ["ApplicationId"] = task.ApplicationId,
            ["ApplicationName"] = task.ApplicationName,
            ["DueDate"] = task.DueDate.ToString("O")
        };

        await RecordAsync(
            AuditEventTypes.TaskCreated,
            AuditCategories.Task,
            $"Task '{task.Title}' created for application '{task.ApplicationName}'",
            AuditEntityTypes.Task,
            task.Id,
            performedByUserId,
            performedByName,
            details);
    }

    public async Task LogTaskUpdatedAsync(string taskId, string taskTitle, string performedByUserId, string performedByName, Dictionary<string, (string? OldValue, string? NewValue)>? changes = null)
    {
        var details = new Dictionary<string, string>();
        if (changes != null)
        {
            foreach (var change in changes)
            {
                details[$"{change.Key}_Old"] = change.Value.OldValue ?? "(none)";
                details[$"{change.Key}_New"] = change.Value.NewValue ?? "(none)";
            }
        }

        await RecordAsync(
            AuditEventTypes.TaskUpdated,
            AuditCategories.Task,
            $"Task '{taskTitle}' updated",
            AuditEntityTypes.Task,
            taskId,
            performedByUserId,
            performedByName,
            details);
    }

    public async Task LogTaskDeletedAsync(string taskId, string taskTitle, string performedByUserId, string performedByName)
    {
        await RecordAsync(
            AuditEventTypes.TaskDeleted,
            AuditCategories.Task,
            $"Task '{taskTitle}' deleted",
            AuditEntityTypes.Task,
            taskId,
            performedByUserId,
            performedByName);
    }

    public async Task LogTaskStatusChangedAsync(string taskId, string taskTitle, string oldStatus, string newStatus, string performedByUserId, string performedByName, string? notes = null)
    {
        var details = new Dictionary<string, string>
        {
            ["OldStatus"] = oldStatus,
            ["NewStatus"] = newStatus
        };
        if (!string.IsNullOrEmpty(notes))
            details["Notes"] = notes;

        await RecordAsync(
            AuditEventTypes.TaskStatusChanged,
            AuditCategories.Task,
            $"Task '{taskTitle}' status changed from '{oldStatus}' to '{newStatus}'",
            AuditEntityTypes.Task,
            taskId,
            performedByUserId,
            performedByName,
            details);
    }

    public async Task LogTaskAssignedAsync(string taskId, string taskTitle, string? previousAssigneeId, string? previousAssigneeName, string newAssigneeId, string newAssigneeName, string performedByUserId, string performedByName)
    {
        var details = new Dictionary<string, string>
        {
            ["NewAssigneeId"] = newAssigneeId,
            ["NewAssigneeName"] = newAssigneeName
        };
        if (!string.IsNullOrEmpty(previousAssigneeId))
        {
            details["PreviousAssigneeId"] = previousAssigneeId;
            details["PreviousAssigneeName"] = previousAssigneeName ?? "(unknown)";
        }

        var message = string.IsNullOrEmpty(previousAssigneeName)
            ? $"Task '{taskTitle}' assigned to {newAssigneeName}"
            : $"Task '{taskTitle}' reassigned from {previousAssigneeName} to {newAssigneeName}";

        await RecordAsync(
            AuditEventTypes.TaskAssigned,
            AuditCategories.Task,
            message,
            AuditEntityTypes.Task,
            taskId,
            performedByUserId,
            performedByName,
            details);
    }

    public async Task LogTaskDelegatedAsync(string taskId, string taskTitle, string fromUserId, string fromUserName, string toUserId, string toUserName, string reason, string performedByUserId, string performedByName)
    {
        var details = new Dictionary<string, string>
        {
            ["FromUserId"] = fromUserId,
            ["FromUserName"] = fromUserName,
            ["ToUserId"] = toUserId,
            ["ToUserName"] = toUserName,
            ["Reason"] = reason
        };

        await RecordAsync(
            AuditEventTypes.TaskDelegated,
            AuditCategories.Task,
            $"Task '{taskTitle}' delegated from {fromUserName} to {toUserName}: {reason}",
            AuditEntityTypes.Task,
            taskId,
            performedByUserId,
            performedByName,
            details);
    }

    public async Task LogTaskEscalatedAsync(string taskId, string taskTitle, string reason, string performedByUserId, string performedByName)
    {
        var details = new Dictionary<string, string>
        {
            ["Reason"] = reason
        };

        await RecordAsync(
            AuditEventTypes.TaskEscalated,
            AuditCategories.Task,
            $"Task '{taskTitle}' escalated: {reason}",
            AuditEntityTypes.Task,
            taskId,
            performedByUserId,
            performedByName,
            details);
    }

    public async Task LogTaskCompletedAsync(string taskId, string taskTitle, string performedByUserId, string performedByName, string? notes = null)
    {
        var details = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(notes))
            details["Notes"] = notes;

        await RecordAsync(
            AuditEventTypes.TaskCompleted,
            AuditCategories.Task,
            $"Task '{taskTitle}' completed",
            AuditEntityTypes.Task,
            taskId,
            performedByUserId,
            performedByName,
            details);
    }

    public async Task LogTaskNoteAddedAsync(string taskId, string taskTitle, string performedByUserId, string performedByName)
    {
        await RecordAsync(
            AuditEventTypes.TaskNoteAdded,
            AuditCategories.Task,
            $"Note added to task '{taskTitle}'",
            AuditEntityTypes.Task,
            taskId,
            performedByUserId,
            performedByName);
    }

    #endregion

    #region Application Events

    public async Task LogApplicationUpdatedAsync(string appId, string appName, string performedByUserId, string performedByName, Dictionary<string, (string? OldValue, string? NewValue)>? changes = null)
    {
        var details = new Dictionary<string, string>();
        if (changes != null)
        {
            foreach (var change in changes)
            {
                details[$"{change.Key}_Old"] = change.Value.OldValue ?? "(none)";
                details[$"{change.Key}_New"] = change.Value.NewValue ?? "(none)";
            }
        }

        await RecordAsync(
            AuditEventTypes.ApplicationUpdated,
            AuditCategories.Application,
            $"Application '{appName}' updated",
            AuditEntityTypes.Application,
            appId,
            performedByUserId,
            performedByName,
            details);
    }

    public async Task LogApplicationRoleChangedAsync(string appId, string appName, string roleType, string? previousUserId, string? previousUserName, string? newUserId, string? newUserName, string performedByUserId, string performedByName)
    {
        var details = new Dictionary<string, string>
        {
            ["RoleType"] = roleType
        };

        string message;
        if (!string.IsNullOrEmpty(newUserId))
        {
            details["NewUserId"] = newUserId;
            details["NewUserName"] = newUserName ?? "(unknown)";

            if (!string.IsNullOrEmpty(previousUserId))
            {
                details["PreviousUserId"] = previousUserId;
                details["PreviousUserName"] = previousUserName ?? "(unknown)";
                message = $"Application '{appName}' {roleType} role changed from {previousUserName} to {newUserName}";
            }
            else
            {
                message = $"Application '{appName}' {roleType} role assigned to {newUserName}";
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(previousUserId))
            {
                details["PreviousUserId"] = previousUserId;
                details["PreviousUserName"] = previousUserName ?? "(unknown)";
            }
            message = $"Application '{appName}' {roleType} role removed from {previousUserName}";
        }

        await RecordAsync(
            string.IsNullOrEmpty(newUserId) ? AuditEventTypes.ApplicationRoleRemoved : AuditEventTypes.ApplicationRoleAssigned,
            AuditCategories.Application,
            message,
            AuditEntityTypes.Application,
            appId,
            performedByUserId,
            performedByName,
            details);
    }

    public async Task LogApplicationConflictDetectedAsync(string appId, string appName, string conflictType, string description)
    {
        var details = new Dictionary<string, string>
        {
            ["ConflictType"] = conflictType,
            ["Description"] = description
        };

        await RecordAsync(
            AuditEventTypes.ApplicationConflictDetected,
            AuditCategories.Application,
            $"Data conflict detected for '{appName}': {conflictType}",
            AuditEntityTypes.Application,
            appId,
            null,
            "System",
            details);
    }

    public async Task LogApplicationConflictResolvedAsync(string appId, string appName, string conflictType, string resolution, string performedByUserId, string performedByName)
    {
        var details = new Dictionary<string, string>
        {
            ["ConflictType"] = conflictType,
            ["Resolution"] = resolution
        };

        await RecordAsync(
            AuditEventTypes.ApplicationConflictResolved,
            AuditCategories.Application,
            $"Data conflict resolved for '{appName}': {resolution}",
            AuditEntityTypes.Application,
            appId,
            performedByUserId,
            performedByName,
            details);
    }

    #endregion

    #region Configuration Events

    public async Task LogConfigChangedAsync(string configType, string performedByUserId, string performedByName, Dictionary<string, (string? OldValue, string? NewValue)>? changes = null)
    {
        var details = new Dictionary<string, string>
        {
            ["ConfigType"] = configType
        };
        if (changes != null)
        {
            foreach (var change in changes)
            {
                details[$"{change.Key}_Old"] = change.Value.OldValue ?? "(none)";
                details[$"{change.Key}_New"] = change.Value.NewValue ?? "(none)";
            }
        }

        var eventType = configType switch
        {
            "HealthScoring" => AuditEventTypes.ConfigHealthScoringUpdated,
            "TaskScheduling" => AuditEventTypes.ConfigTaskSchedulingUpdated,
            "DataSource" => AuditEventTypes.ConfigDataSourceUpdated,
            "AI" => AuditEventTypes.ConfigAiSettingsUpdated,
            "UI" => AuditEventTypes.ConfigUiSettingsUpdated,
            _ => AuditEventTypes.ConfigSystemSettingsUpdated
        };

        await RecordAsync(
            eventType,
            AuditCategories.Config,
            $"{configType} configuration updated",
            AuditEntityTypes.Config,
            configType,
            performedByUserId,
            performedByName,
            details);
    }

    public async Task LogDataSourceCredentialUpdatedAsync(string dataSourceId, string dataSourceName, string performedByUserId, string performedByName)
    {
        var details = new Dictionary<string, string>
        {
            ["DataSourceId"] = dataSourceId,
            ["DataSourceName"] = dataSourceName
        };

        await RecordAsync(
            AuditEventTypes.ConfigDataSourceCredentialUpdated,
            AuditCategories.Config,
            $"Credentials updated for data source '{dataSourceName}'",
            AuditEntityTypes.DataSource,
            dataSourceId,
            performedByUserId,
            performedByName,
            details);
    }

    #endregion

    #region Data Sync Events

    public async Task LogSyncStartedAsync(string dataSourceName, string jobId)
    {
        var details = new Dictionary<string, string>
        {
            ["DataSource"] = dataSourceName,
            ["JobId"] = jobId
        };

        await RecordAsync(
            AuditEventTypes.SyncStarted,
            AuditCategories.Sync,
            $"Data sync started for {dataSourceName}",
            AuditEntityTypes.SyncJob,
            jobId,
            null,
            "System",
            details);
    }

    public async Task LogSyncCompletedAsync(string dataSourceName, string jobId, int recordsProcessed, int recordsCreated, int recordsUpdated, TimeSpan duration)
    {
        var details = new Dictionary<string, string>
        {
            ["DataSource"] = dataSourceName,
            ["JobId"] = jobId,
            ["RecordsProcessed"] = recordsProcessed.ToString(),
            ["RecordsCreated"] = recordsCreated.ToString(),
            ["RecordsUpdated"] = recordsUpdated.ToString(),
            ["DurationMs"] = duration.TotalMilliseconds.ToString("F0")
        };

        await RecordAsync(
            AuditEventTypes.SyncCompleted,
            AuditCategories.Sync,
            $"Data sync completed for {dataSourceName}: {recordsProcessed} processed, {recordsCreated} created, {recordsUpdated} updated",
            AuditEntityTypes.SyncJob,
            jobId,
            null,
            "System",
            details);
    }

    public async Task LogSyncFailedAsync(string dataSourceName, string jobId, string errorMessage, TimeSpan duration)
    {
        var details = new Dictionary<string, string>
        {
            ["DataSource"] = dataSourceName,
            ["JobId"] = jobId,
            ["ErrorMessage"] = errorMessage,
            ["DurationMs"] = duration.TotalMilliseconds.ToString("F0")
        };

        await RecordAsync(
            AuditEventTypes.SyncFailed,
            AuditCategories.Sync,
            $"Data sync failed for {dataSourceName}: {errorMessage}",
            AuditEntityTypes.SyncJob,
            jobId,
            null,
            "System",
            details);
    }

    public async Task LogConnectionTestedAsync(string dataSourceName, bool success, string? message, string performedByUserId, string performedByName)
    {
        var details = new Dictionary<string, string>
        {
            ["DataSource"] = dataSourceName,
            ["Success"] = success.ToString()
        };
        if (!string.IsNullOrEmpty(message))
            details["Message"] = message;

        await RecordAsync(
            AuditEventTypes.SyncConnectionTested,
            AuditCategories.Sync,
            success ? $"Connection test succeeded for {dataSourceName}" : $"Connection test failed for {dataSourceName}: {message}",
            AuditEntityTypes.DataSource,
            dataSourceName,
            performedByUserId,
            performedByName,
            details);
    }

    #endregion

    #region Task Documentation Events

    public async Task LogTaskDocCreatedAsync(TaskType taskType, string performedByUserId, string performedByName)
    {
        await RecordAsync(
            AuditEventTypes.TaskDocCreated,
            AuditCategories.TaskDoc,
            $"Task documentation created for {taskType}",
            AuditEntityTypes.TaskDocumentation,
            taskType.ToString(),
            performedByUserId,
            performedByName);
    }

    public async Task LogTaskDocUpdatedAsync(TaskType taskType, string performedByUserId, string performedByName)
    {
        await RecordAsync(
            AuditEventTypes.TaskDocUpdated,
            AuditCategories.TaskDoc,
            $"Task documentation updated for {taskType}",
            AuditEntityTypes.TaskDocumentation,
            taskType.ToString(),
            performedByUserId,
            performedByName);
    }

    public async Task LogTaskDocDeletedAsync(TaskType taskType, string performedByUserId, string performedByName)
    {
        await RecordAsync(
            AuditEventTypes.TaskDocDeleted,
            AuditCategories.TaskDoc,
            $"Task documentation deleted for {taskType}",
            AuditEntityTypes.TaskDocumentation,
            taskType.ToString(),
            performedByUserId,
            performedByName);
    }

    #endregion

    #region Framework Events

    public async Task LogFrameworkEolRefreshedAsync(string frameworkType, int versionsAdded, int versionsUpdated, string performedByUserId, string performedByName)
    {
        var details = new Dictionary<string, string>
        {
            ["FrameworkType"] = frameworkType,
            ["VersionsAdded"] = versionsAdded.ToString(),
            ["VersionsUpdated"] = versionsUpdated.ToString()
        };

        await RecordAsync(
            AuditEventTypes.FrameworkEolRefreshed,
            AuditCategories.Framework,
            $"EOL data refreshed for {frameworkType}: {versionsAdded} added, {versionsUpdated} updated",
            AuditEntityTypes.FrameworkVersion,
            frameworkType,
            performedByUserId,
            performedByName,
            details);
    }

    #endregion

    #region Security Events

    public async Task LogSecurityFindingAcknowledgedAsync(string findingId, string appId, string appName, string performedByUserId, string performedByName)
    {
        var details = new Dictionary<string, string>
        {
            ["ApplicationId"] = appId,
            ["ApplicationName"] = appName
        };

        await RecordAsync(
            AuditEventTypes.SecurityFindingAcknowledged,
            AuditCategories.Security,
            $"Security finding acknowledged for '{appName}'",
            AuditEntityTypes.SecurityFinding,
            findingId,
            performedByUserId,
            performedByName,
            details);
    }

    public async Task LogSecurityFindingResolvedAsync(string findingId, string appId, string appName, string performedByUserId, string performedByName)
    {
        var details = new Dictionary<string, string>
        {
            ["ApplicationId"] = appId,
            ["ApplicationName"] = appName
        };

        await RecordAsync(
            AuditEventTypes.SecurityFindingResolved,
            AuditCategories.Security,
            $"Security finding resolved for '{appName}'",
            AuditEntityTypes.SecurityFinding,
            findingId,
            performedByUserId,
            performedByName,
            details);
    }

    #endregion

    #region Export Events

    public async Task LogDataExportedAsync(string exportType, int recordCount, string performedByUserId, string performedByName)
    {
        var details = new Dictionary<string, string>
        {
            ["ExportType"] = exportType,
            ["RecordCount"] = recordCount.ToString()
        };

        await RecordAsync(
            AuditEventTypes.DataExported,
            AuditCategories.Export,
            $"{exportType} export completed: {recordCount} records",
            null,
            null,
            performedByUserId,
            performedByName,
            details);
    }

    #endregion

    #region Generic Logging

    public async Task LogCustomEventAsync(string eventType, string category, string message, string? entityType = null, string? entityId = null, string? userId = null, string? userName = null, Dictionary<string, string>? details = null)
    {
        await RecordAsync(eventType, category, message, entityType, entityId, userId, userName, details);
    }

    #endregion

    #region Private Helpers

    private async Task RecordAsync(string eventType, string category, string message, string? entityType, string? entityId, string? userId, string? userName, Dictionary<string, string>? details = null)
    {
        try
        {
            var entry = new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                EventType = eventType,
                Category = category,
                Message = message,
                EntityType = entityType,
                EntityId = entityId,
                UserId = userId,
                UserName = userName ?? "System",
                Timestamp = DateTimeOffset.UtcNow,
                Details = details ?? []
            };

            await _dataService.RecordAuditLogAsync(entry);

            _logger.LogDebug("Audit event recorded: {EventType} - {Message}", eventType, message);
        }
        catch (Exception ex)
        {
            // Don't let audit logging failures break the application
            _logger.LogError(ex, "Failed to record audit event: {EventType}", eventType);
        }
    }

    #endregion
}
