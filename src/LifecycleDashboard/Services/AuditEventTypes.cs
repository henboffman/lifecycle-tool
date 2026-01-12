namespace LifecycleDashboard.Services;

/// <summary>
/// Centralized audit event type constants for consistent logging across the application.
/// Event types follow the pattern: {Category}.{Action}
/// </summary>
public static class AuditEventTypes
{
    #region Authentication Events

    /// <summary>User successfully logged in.</summary>
    public const string UserLogin = "Auth.Login";

    /// <summary>User logged out.</summary>
    public const string UserLogout = "Auth.Logout";

    /// <summary>Login attempt failed (invalid credentials, account locked, etc.).</summary>
    public const string UserLoginFailed = "Auth.LoginFailed";

    /// <summary>User session expired.</summary>
    public const string UserSessionExpired = "Auth.SessionExpired";

    #endregion

    #region User Management Events

    /// <summary>New user account created.</summary>
    public const string UserCreated = "User.Created";

    /// <summary>User account updated.</summary>
    public const string UserUpdated = "User.Updated";

    /// <summary>User account deleted.</summary>
    public const string UserDeleted = "User.Deleted";

    /// <summary>User role changed (Admin, SecurityAdmin, etc.).</summary>
    public const string UserRoleChanged = "User.RoleChanged";

    /// <summary>User profile settings updated.</summary>
    public const string UserProfileUpdated = "User.ProfileUpdated";

    #endregion

    #region Task Operation Events

    /// <summary>New lifecycle task created.</summary>
    public const string TaskCreated = "Task.Created";

    /// <summary>Task details updated (title, description, priority, due date).</summary>
    public const string TaskUpdated = "Task.Updated";

    /// <summary>Task deleted.</summary>
    public const string TaskDeleted = "Task.Deleted";

    /// <summary>Task assigned to a user.</summary>
    public const string TaskAssigned = "Task.Assigned";

    /// <summary>Task delegated from one user to another.</summary>
    public const string TaskDelegated = "Task.Delegated";

    /// <summary>Task escalated due to overdue or priority.</summary>
    public const string TaskEscalated = "Task.Escalated";

    /// <summary>Task marked as completed.</summary>
    public const string TaskCompleted = "Task.Completed";

    /// <summary>Task status changed (e.g., Pending to InProgress).</summary>
    public const string TaskStatusChanged = "Task.StatusChanged";

    /// <summary>Note added to task history.</summary>
    public const string TaskNoteAdded = "Task.NoteAdded";

    /// <summary>Task priority changed.</summary>
    public const string TaskPriorityChanged = "Task.PriorityChanged";

    /// <summary>Task due date changed.</summary>
    public const string TaskDueDateChanged = "Task.DueDateChanged";

    /// <summary>Bulk task operation performed.</summary>
    public const string TaskBulkOperation = "Task.BulkOperation";

    #endregion

    #region Application Data Events

    /// <summary>Application record updated.</summary>
    public const string ApplicationUpdated = "Application.Updated";

    /// <summary>Application security findings updated.</summary>
    public const string ApplicationSecurityUpdated = "Application.SecurityUpdated";

    /// <summary>Application role assigned to user.</summary>
    public const string ApplicationRoleAssigned = "Application.RoleAssigned";

    /// <summary>Application role removed from user.</summary>
    public const string ApplicationRoleRemoved = "Application.RoleRemoved";

    /// <summary>Data conflict detected for application.</summary>
    public const string ApplicationConflictDetected = "Application.ConflictDetected";

    /// <summary>Data conflict resolved for application.</summary>
    public const string ApplicationConflictResolved = "Application.ConflictResolved";

    /// <summary>Application health score recalculated.</summary>
    public const string ApplicationHealthRecalculated = "Application.HealthRecalculated";

    /// <summary>Application critical period added or updated.</summary>
    public const string ApplicationCriticalPeriodUpdated = "Application.CriticalPeriodUpdated";

    /// <summary>Application key date added or updated.</summary>
    public const string ApplicationKeyDateUpdated = "Application.KeyDateUpdated";

    #endregion

    #region Data Sync Events

    /// <summary>Data sync job started.</summary>
    public const string SyncStarted = "Sync.Started";

    /// <summary>Data sync job completed successfully.</summary>
    public const string SyncCompleted = "Sync.Completed";

    /// <summary>Data sync job failed.</summary>
    public const string SyncFailed = "Sync.Failed";

    /// <summary>Data conflict detected during sync.</summary>
    public const string SyncConflictDetected = "Sync.ConflictDetected";

    /// <summary>Data conflict resolved during sync.</summary>
    public const string SyncConflictResolved = "Sync.ConflictResolved";

    /// <summary>Connection test performed.</summary>
    public const string SyncConnectionTested = "Sync.ConnectionTested";

    #endregion

    #region Configuration Events

    /// <summary>Health scoring configuration updated.</summary>
    public const string ConfigHealthScoringUpdated = "Config.HealthScoring.Updated";

    /// <summary>Task scheduling configuration updated.</summary>
    public const string ConfigTaskSchedulingUpdated = "Config.TaskScheduling.Updated";

    /// <summary>Data source configuration updated.</summary>
    public const string ConfigDataSourceUpdated = "Config.DataSource.Updated";

    /// <summary>Data source credentials updated (credential itself not logged).</summary>
    public const string ConfigDataSourceCredentialUpdated = "Config.DataSource.CredentialUpdated";

    /// <summary>AI/ML settings updated.</summary>
    public const string ConfigAiSettingsUpdated = "Config.AI.Updated";

    /// <summary>System settings updated.</summary>
    public const string ConfigSystemSettingsUpdated = "Config.System.Updated";

    /// <summary>Theme or UI settings updated.</summary>
    public const string ConfigUiSettingsUpdated = "Config.UI.Updated";

    #endregion

    #region Task Documentation Events

    /// <summary>Task documentation created.</summary>
    public const string TaskDocCreated = "TaskDoc.Created";

    /// <summary>Task documentation updated.</summary>
    public const string TaskDocUpdated = "TaskDoc.Updated";

    /// <summary>Task documentation deleted.</summary>
    public const string TaskDocDeleted = "TaskDoc.Deleted";

    #endregion

    #region Framework Version Events

    /// <summary>Framework version record created.</summary>
    public const string FrameworkCreated = "Framework.Created";

    /// <summary>Framework version record updated.</summary>
    public const string FrameworkUpdated = "Framework.Updated";

    /// <summary>Framework version record deleted.</summary>
    public const string FrameworkDeleted = "Framework.Deleted";

    /// <summary>Framework EOL data refreshed from external source.</summary>
    public const string FrameworkEolRefreshed = "Framework.EolRefreshed";

    #endregion

    #region Security Events

    /// <summary>Security review completed.</summary>
    public const string SecurityReviewCompleted = "Security.ReviewCompleted";

    /// <summary>Security finding acknowledged.</summary>
    public const string SecurityFindingAcknowledged = "Security.FindingAcknowledged";

    /// <summary>Security finding resolved.</summary>
    public const string SecurityFindingResolved = "Security.FindingResolved";

    #endregion

    #region Export Events

    /// <summary>Data export performed.</summary>
    public const string DataExported = "Export.Data";

    /// <summary>Audit log export performed.</summary>
    public const string AuditLogExported = "Export.AuditLog";

    /// <summary>Report generated.</summary>
    public const string ReportGenerated = "Export.Report";

    #endregion
}

/// <summary>
/// Centralized audit category constants for grouping audit events.
/// </summary>
public static class AuditCategories
{
    /// <summary>Authentication-related events (login, logout, session).</summary>
    public const string Auth = "Auth";

    /// <summary>User management events (create, update, delete users).</summary>
    public const string User = "User";

    /// <summary>Task operation events (lifecycle task CRUD).</summary>
    public const string Task = "Task";

    /// <summary>Application data events.</summary>
    public const string Application = "Application";

    /// <summary>Data synchronization events.</summary>
    public const string Sync = "Sync";

    /// <summary>Configuration change events.</summary>
    public const string Config = "Config";

    /// <summary>Task documentation events.</summary>
    public const string TaskDoc = "TaskDoc";

    /// <summary>Framework version tracking events.</summary>
    public const string Framework = "Framework";

    /// <summary>Security-related events.</summary>
    public const string Security = "Security";

    /// <summary>Data export events.</summary>
    public const string Export = "Export";
}

/// <summary>
/// Entity type constants for audit log EntityType field.
/// </summary>
public static class AuditEntityTypes
{
    public const string User = "User";
    public const string Task = "LifecycleTask";
    public const string Application = "Application";
    public const string TaskDocumentation = "TaskDocumentation";
    public const string FrameworkVersion = "FrameworkVersion";
    public const string DataSource = "DataSource";
    public const string SecurityFinding = "SecurityFinding";
    public const string Config = "Config";
    public const string SyncJob = "SyncJob";
}
