using LifecycleDashboard.Models;

namespace LifecycleDashboard.Services;

/// <summary>
/// Service interface for mock data operations.
/// This service provides test data when running in development/mock mode.
/// </summary>
public interface IMockDataService
{
    /// <summary>
    /// Gets all applications in the portfolio.
    /// </summary>
    Task<IReadOnlyList<Application>> GetApplicationsAsync();

    /// <summary>
    /// Gets a specific application by ID.
    /// </summary>
    Task<Application?> GetApplicationAsync(string id);

    /// <summary>
    /// Gets applications filtered by health category.
    /// </summary>
    Task<IReadOnlyList<Application>> GetApplicationsByHealthAsync(HealthCategory category);

    /// <summary>
    /// Gets all tasks for a user.
    /// </summary>
    Task<IReadOnlyList<LifecycleTask>> GetTasksForUserAsync(string userId);

    /// <summary>
    /// Gets all tasks for an application.
    /// </summary>
    Task<IReadOnlyList<LifecycleTask>> GetTasksForApplicationAsync(string applicationId);

    /// <summary>
    /// Gets overdue tasks.
    /// </summary>
    Task<IReadOnlyList<LifecycleTask>> GetOverdueTasksAsync();

    /// <summary>
    /// Gets task summary for a user.
    /// </summary>
    Task<TaskSummary> GetTaskSummaryForUserAsync(string userId);

    /// <summary>
    /// Gets portfolio health summary.
    /// </summary>
    Task<PortfolioHealthSummary> GetPortfolioHealthSummaryAsync();

    /// <summary>
    /// Gets the current user.
    /// </summary>
    Task<User> GetCurrentUserAsync();

    /// <summary>
    /// Gets all users.
    /// </summary>
    Task<IReadOnlyList<User>> GetUsersAsync();

    /// <summary>
    /// Gets a specific task by ID.
    /// </summary>
    Task<LifecycleTask?> GetTaskAsync(string taskId);

    /// <summary>
    /// Gets repository information for an application.
    /// </summary>
    Task<RepositoryInfo?> GetRepositoryInfoAsync(string applicationId);

    /// <summary>
    /// Gets task documentation for a specific task type.
    /// </summary>
    Task<TaskDocumentation?> GetTaskDocumentationAsync(TaskType taskType);

    /// <summary>
    /// Gets all task documentation entries.
    /// </summary>
    Task<IReadOnlyList<TaskDocumentation>> GetAllTaskDocumentationAsync();

    /// <summary>
    /// Updates task documentation for a specific task type.
    /// </summary>
    Task<TaskDocumentation> UpdateTaskDocumentationAsync(TaskDocumentation documentation);

    /// <summary>
    /// Creates new task documentation entry.
    /// </summary>
    Task<TaskDocumentation> CreateTaskDocumentationAsync(TaskDocumentation documentation);

    /// <summary>
    /// Deletes task documentation entry by ID.
    /// </summary>
    Task DeleteTaskDocumentationAsync(string id);

    #region Task CRUD Operations

    /// <summary>
    /// Creates a new lifecycle task.
    /// </summary>
    Task<LifecycleTask> CreateTaskAsync(LifecycleTask task);

    /// <summary>
    /// Deletes a task by ID.
    /// </summary>
    Task DeleteTaskAsync(string taskId);

    /// <summary>
    /// Updates task status with history tracking.
    /// </summary>
    Task<LifecycleTask> UpdateTaskStatusAsync(string taskId, Models.TaskStatus newStatus, string performedByUserId, string performedByName, string? notes = null);

    /// <summary>
    /// Assigns a task to a user.
    /// </summary>
    Task<LifecycleTask> AssignTaskAsync(string taskId, string userId, string userName, string userEmail, string performedByUserId, string performedByName);

    /// <summary>
    /// Delegates a task from one user to another.
    /// </summary>
    Task<LifecycleTask> DelegateTaskAsync(string taskId, string fromUserId, string toUserId, string toUserName, string toUserEmail, string reason, string performedByUserId, string performedByName);

    /// <summary>
    /// Escalates a task.
    /// </summary>
    Task<LifecycleTask> EscalateTaskAsync(string taskId, string reason, string performedByUserId, string performedByName);

    /// <summary>
    /// Marks a task as completed.
    /// </summary>
    Task<LifecycleTask> CompleteTaskAsync(string taskId, string performedByUserId, string performedByName, string? notes = null);

    /// <summary>
    /// Adds a note to task history.
    /// </summary>
    Task<LifecycleTask> AddTaskNoteAsync(string taskId, string performedByUserId, string performedByName, string note);

    /// <summary>
    /// Gets all tasks (for admin purposes).
    /// </summary>
    Task<IReadOnlyList<LifecycleTask>> GetAllTasksAsync();

    #endregion

    /// <summary>
    /// Gets all framework versions with lifecycle information.
    /// </summary>
    Task<IReadOnlyList<FrameworkVersion>> GetAllFrameworkVersionsAsync();

    /// <summary>
    /// Gets a specific framework version by ID.
    /// </summary>
    Task<FrameworkVersion?> GetFrameworkVersionAsync(string id);

    /// <summary>
    /// Gets framework versions filtered by type.
    /// </summary>
    Task<IReadOnlyList<FrameworkVersion>> GetFrameworkVersionsByTypeAsync(FrameworkType type);

    /// <summary>
    /// Updates a framework version record.
    /// </summary>
    Task<FrameworkVersion> UpdateFrameworkVersionAsync(FrameworkVersion version);

    /// <summary>
    /// Creates a new framework version record.
    /// </summary>
    Task<FrameworkVersion> CreateFrameworkVersionAsync(FrameworkVersion version);

    /// <summary>
    /// Deletes a framework version record.
    /// </summary>
    Task DeleteFrameworkVersionAsync(string id);

    /// <summary>
    /// Gets applications using a specific framework version.
    /// </summary>
    Task<IReadOnlyList<Application>> GetApplicationsByFrameworkAsync(string frameworkVersionId);

    /// <summary>
    /// Gets framework EOL summary for the portfolio.
    /// </summary>
    Task<FrameworkEolSummary> GetFrameworkEolSummaryAsync();

    /// <summary>
    /// Indicates whether mock data mode is enabled.
    /// </summary>
    bool IsMockDataEnabled { get; }

    /// <summary>
    /// Toggles mock data mode on or off.
    /// </summary>
    Task SetMockDataEnabledAsync(bool enabled);

    /// <summary>
    /// Event raised when mock data mode changes.
    /// </summary>
    event EventHandler<bool>? MockDataModeChanged;

    // Synced Repository Data (from Azure DevOps)

    /// <summary>
    /// Gets all synced repositories from Azure DevOps.
    /// </summary>
    Task<IReadOnlyList<SyncedRepository>> GetSyncedRepositoriesAsync();

    /// <summary>
    /// Stores synced repositories from Azure DevOps.
    /// </summary>
    Task StoreSyncedRepositoriesAsync(IEnumerable<SyncedRepository> repositories);

    /// <summary>
    /// Gets a synced repository by ID.
    /// </summary>
    Task<SyncedRepository?> GetSyncedRepositoryAsync(string repositoryId);

    /// <summary>
    /// Clears all synced repository data.
    /// </summary>
    Task ClearSyncedRepositoriesAsync();

    // User Management

    /// <summary>
    /// Gets a specific user by ID.
    /// </summary>
    Task<User?> GetUserAsync(string userId);

    /// <summary>
    /// Creates a new user.
    /// </summary>
    Task<User> CreateUserAsync(User user);

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    Task<User> UpdateUserAsync(User user);

    /// <summary>
    /// Deletes a user by ID.
    /// </summary>
    Task DeleteUserAsync(string userId);

    /// <summary>
    /// Gets applications assigned to a user.
    /// </summary>
    Task<IReadOnlyList<Application>> GetApplicationsForUserAsync(string userId);

    // Task Settings

    /// <summary>
    /// Gets task scheduling configuration.
    /// </summary>
    Task<TaskSchedulingConfig> GetTaskSchedulingConfigAsync();

    /// <summary>
    /// Updates task scheduling configuration.
    /// </summary>
    Task<TaskSchedulingConfig> UpdateTaskSchedulingConfigAsync(TaskSchedulingConfig config);

    // Audit Log

    /// <summary>
    /// Gets audit log entries with optional filtering.
    /// </summary>
    Task<IReadOnlyList<AuditLogEntry>> GetAuditLogAsync(AuditLogFilter? filter = null);

    /// <summary>
    /// Records an audit log entry.
    /// </summary>
    Task RecordAuditLogAsync(AuditLogEntry entry);

    // System Settings

    /// <summary>
    /// Gets system settings.
    /// </summary>
    Task<SystemSettings> GetSystemSettingsAsync();

    /// <summary>
    /// Updates system settings.
    /// </summary>
    Task<SystemSettings> UpdateSystemSettingsAsync(SystemSettings settings);

    /// <summary>
    /// Gets data source connection configurations.
    /// </summary>
    Task<IReadOnlyList<DataSourceConfig>> GetDataSourceConfigsAsync();

    /// <summary>
    /// Updates a data source connection configuration.
    /// </summary>
    Task<DataSourceConfig> UpdateDataSourceConfigAsync(DataSourceConfig config);

    /// <summary>
    /// Tests a data source connection.
    /// </summary>
    Task<DataSourceTestResult> TestDataSourceConnectionAsync(string dataSourceId);
}

/// <summary>
/// Task scheduling configuration for the system.
/// </summary>
public record TaskSchedulingConfig
{
    public int RoleValidationFrequencyDays { get; init; } = 90;
    public int ReminderDaysBeforeDue { get; init; } = 7;
    public int EscalationDaysAfterOverdue { get; init; } = 14;
    public int CriticalTaskEscalationDays { get; init; } = 0;
    public int MaxActiveTasksPerUser { get; init; } = 10;
    public bool SmartSchedulingEnabled { get; init; } = true;
    public DateTimeOffset LastUpdated { get; init; } = DateTimeOffset.UtcNow;
    public string? UpdatedBy { get; init; }
}

/// <summary>
/// Audit log entry for tracking system changes.
/// </summary>
public record AuditLogEntry
{
    public required string Id { get; init; }
    public required string EventType { get; init; }
    public required string Category { get; init; }
    public required string Message { get; init; }
    public required string UserId { get; init; }
    public required string UserName { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string? EntityType { get; init; }
    public string? EntityId { get; init; }
    public Dictionary<string, string>? Details { get; init; }
}

/// <summary>
/// Filter for querying audit log entries.
/// </summary>
public record AuditLogFilter
{
    public string? Category { get; init; }
    public string? EventType { get; init; }
    public string? UserId { get; init; }
    public DateTimeOffset? StartDate { get; init; }
    public DateTimeOffset? EndDate { get; init; }
    public int? Limit { get; init; }
}

/// <summary>
/// System-wide settings.
/// </summary>
public record SystemSettings
{
    public bool MockDataEnabled { get; init; } = true;
    public bool DevModeSidebarEnabled { get; init; } = true;
    public bool AiRecommendationsEnabled { get; init; } = false;
    public string ApplicationVersion { get; init; } = "1.0.0-dev";
    public string EnvironmentMode { get; init; } = "Development";
    public DateTimeOffset LastUpdated { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Configuration for a data source connection.
/// </summary>
public record DataSourceConfig
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required DataSourceType Type { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsConnected { get; init; }
    public DataSourceConnectionSettings? ConnectionSettings { get; init; }
    public DateTimeOffset? LastSyncTime { get; init; }
    public string? LastSyncStatus { get; init; }
    public int? LastSyncRecordCount { get; init; }
}

/// <summary>
/// Types of data sources.
/// </summary>
public enum DataSourceType
{
    AzureDevOps,
    SharePoint,
    ServiceNow,
    IisDatabase
}

/// <summary>
/// Connection settings for a data source.
/// </summary>
public record DataSourceConnectionSettings
{
    public string? Organization { get; init; }
    public string? Project { get; init; }
    public string? BaseUrl { get; init; }
    public string? ApiKey { get; init; }
    public string? ConnectionString { get; init; }
    public string? SiteUrl { get; init; }
    public string? ListName { get; init; }
    public string? Instance { get; init; }
    public string? Table { get; init; }
}

/// <summary>
/// Result of testing a data source connection.
/// </summary>
public record DataSourceTestResult
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public TimeSpan? ResponseTime { get; init; }
    public string? Version { get; init; }
}

/// <summary>
/// Summary of framework EOL status across the portfolio.
/// </summary>
public record FrameworkEolSummary
{
    public int TotalApplications { get; init; }
    public int ApplicationsWithEolFrameworks { get; init; }
    public int ApplicationsApproachingEol { get; init; }
    public int CriticalEolCount { get; init; }
    public int HighEolCount { get; init; }
    public int MediumEolCount { get; init; }
    public List<FrameworkEolDetail> Details { get; init; } = [];
}

/// <summary>
/// Detail of applications using a specific framework version.
/// </summary>
public record FrameworkEolDetail
{
    public required FrameworkVersion Framework { get; init; }
    public int ApplicationCount { get; init; }
    public List<string> ApplicationNames { get; init; } = [];
}

/// <summary>
/// Repository synced from Azure DevOps with detected tech stack and metadata.
/// </summary>
public record SyncedRepository
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Url { get; init; }
    public string? CloneUrl { get; init; }
    public string? DefaultBranch { get; init; }
    public string? ProjectName { get; init; }
    public long? SizeBytes { get; init; }
    public bool IsDisabled { get; init; }

    // Sync metadata
    public DateTimeOffset SyncedAt { get; init; } = DateTimeOffset.UtcNow;
    public string? SyncedBy { get; init; }

    // Detected tech stack
    public string? PrimaryStack { get; init; }
    public List<string> Frameworks { get; init; } = [];
    public List<string> Languages { get; init; } = [];
    public string? TargetFramework { get; init; }
    public string? DetectedPattern { get; init; }

    // Commit info
    public int? TotalCommits { get; init; }
    public DateTimeOffset? LastCommitDate { get; init; }
    public List<string> Contributors { get; init; } = [];

    // Package info
    public int NuGetPackageCount { get; init; }
    public int NpmPackageCount { get; init; }
    public List<SyncedPackageReference> Packages { get; init; } = [];

    // Build/Pipeline info
    public string? LastBuildStatus { get; init; }
    public string? LastBuildResult { get; init; }
    public DateTimeOffset? LastBuildDate { get; init; }

    // README status
    public bool HasReadme { get; init; }
    public int? ReadmeQualityScore { get; init; }

    // Linked application (if mapped)
    public string? LinkedApplicationId { get; init; }
    public string? LinkedApplicationName { get; init; }
}

/// <summary>
/// Package reference from a synced repository.
/// </summary>
public record SyncedPackageReference
{
    public required string Name { get; init; }
    public required string Version { get; init; }
    public required string PackageManager { get; init; }
    public string? SourceFile { get; init; }
    public bool IsDevelopmentDependency { get; init; }
}
