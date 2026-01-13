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
    /// Returns true if deleted, false if denied (system data and not admin).
    /// </summary>
    /// <param name="id">Framework version ID</param>
    /// <param name="allowSystemDataDeletion">Set to true if caller is admin and allowed to delete system data</param>
    Task<bool> DeleteFrameworkVersionAsync(string id, bool allowSystemDataDeletion = false);

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

    /// <summary>
    /// Refreshes application data from synced repositories.
    /// Links repositories to applications using AppNameMappings,
    /// updates tech stack, security findings, last activity date,
    /// and recalculates health scores.
    /// </summary>
    /// <returns>Tuple of (repos linked, apps updated)</returns>
    Task<(int linked, int updated)> RefreshApplicationsFromSyncedDataAsync();

    // Imported ServiceNow Applications

    /// <summary>
    /// Gets all imported ServiceNow applications.
    /// </summary>
    Task<IReadOnlyList<ImportedServiceNowApplication>> GetImportedServiceNowApplicationsAsync();

    /// <summary>
    /// Stores imported ServiceNow applications.
    /// </summary>
    Task StoreServiceNowApplicationsAsync(IEnumerable<ImportedServiceNowApplication> applications);

    /// <summary>
    /// Clears all imported ServiceNow application data.
    /// </summary>
    Task ClearServiceNowApplicationsAsync();

    /// <summary>
    /// Creates Application records from imported ServiceNow applications.
    /// Returns the number of applications created or updated.
    /// </summary>
    Task<int> CreateApplicationsFromServiceNowImportAsync();

    /// <summary>
    /// Gets the saved ServiceNow CSV column mapping.
    /// </summary>
    Task<ServiceNowColumnMapping?> GetServiceNowColumnMappingAsync();

    /// <summary>
    /// Saves the ServiceNow CSV column mapping.
    /// </summary>
    Task SaveServiceNowColumnMappingAsync(ServiceNowColumnMapping mapping);

    // Application Name Mappings (links ServiceNow apps to SharePoint folders and Azure DevOps repos)

    /// <summary>
    /// Gets all application name mappings.
    /// </summary>
    Task<IReadOnlyList<AppNameMapping>> GetAppNameMappingsAsync();

    /// <summary>
    /// Stores application name mappings from CSV import.
    /// </summary>
    Task StoreAppNameMappingsAsync(IEnumerable<AppNameMapping> mappings);

    /// <summary>
    /// Gets a specific app name mapping by ServiceNow application name.
    /// </summary>
    Task<AppNameMapping?> GetAppNameMappingByServiceNowNameAsync(string serviceNowAppName);

    /// <summary>
    /// Gets a specific app name mapping by SharePoint folder name.
    /// </summary>
    Task<AppNameMapping?> GetAppNameMappingBySharePointFolderAsync(string sharePointFolderName);

    /// <summary>
    /// Gets a specific app name mapping by Azure DevOps repository name.
    /// </summary>
    Task<AppNameMapping?> GetAppNameMappingByRepoNameAsync(string repoName);

    /// <summary>
    /// Clears all application name mappings.
    /// </summary>
    Task ClearAppNameMappingsAsync();

    /// <summary>
    /// Gets the saved app name mapping CSV column configuration.
    /// </summary>
    Task<AppNameMappingConfig?> GetAppNameMappingConfigAsync();

    /// <summary>
    /// Saves the app name mapping CSV column configuration.
    /// </summary>
    Task SaveAppNameMappingConfigAsync(AppNameMappingConfig config);

    // Capability Mappings

    /// <summary>
    /// Gets all application-to-capability mappings.
    /// </summary>
    Task<IReadOnlyList<CapabilityMapping>> GetCapabilityMappingsAsync();

    /// <summary>
    /// Stores capability mappings from CSV import.
    /// </summary>
    Task StoreCapabilityMappingsAsync(IEnumerable<CapabilityMapping> mappings);

    /// <summary>
    /// Clears all capability mappings.
    /// </summary>
    Task ClearCapabilityMappingsAsync();

    /// <summary>
    /// Gets the capability for a specific application by name.
    /// </summary>
    Task<string?> GetCapabilityForApplicationAsync(string applicationName);

    // SharePoint Discovered Folders

    /// <summary>
    /// Gets all discovered SharePoint application folders.
    /// </summary>
    Task<IReadOnlyList<DiscoveredSharePointFolder>> GetDiscoveredSharePointFoldersAsync();

    /// <summary>
    /// Stores discovered SharePoint application folders from sync.
    /// </summary>
    Task StoreDiscoveredSharePointFoldersAsync(IEnumerable<DiscoveredSharePointFolder> folders);

    /// <summary>
    /// Clears all discovered SharePoint folder data.
    /// </summary>
    Task ClearDiscoveredSharePointFoldersAsync();

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

    // ServiceNow Incidents

    /// <summary>
    /// Gets all imported ServiceNow incidents.
    /// </summary>
    Task<IReadOnlyList<ServiceNowIncident>> GetServiceNowIncidentsAsync();

    /// <summary>
    /// Gets ServiceNow incidents for a specific application.
    /// </summary>
    Task<IReadOnlyList<ServiceNowIncident>> GetIncidentsForApplicationAsync(string applicationId);

    /// <summary>
    /// Gets a specific incident by ID.
    /// </summary>
    Task<ServiceNowIncident?> GetServiceNowIncidentAsync(string incidentId);

    /// <summary>
    /// Gets a specific incident by incident number.
    /// </summary>
    Task<ServiceNowIncident?> GetServiceNowIncidentByNumberAsync(string incidentNumber);

    /// <summary>
    /// Stores imported ServiceNow incidents (upserts by incident number).
    /// </summary>
    Task<(int created, int updated, int skipped)> StoreServiceNowIncidentsAsync(IEnumerable<ServiceNowIncident> incidents);

    /// <summary>
    /// Updates an incident (for manual linking/amendments).
    /// </summary>
    Task<ServiceNowIncident> UpdateServiceNowIncidentAsync(ServiceNowIncident incident);

    /// <summary>
    /// Deletes an incident by ID.
    /// </summary>
    Task DeleteServiceNowIncidentAsync(string incidentId);

    /// <summary>
    /// Clears all imported incidents.
    /// </summary>
    Task ClearServiceNowIncidentsAsync();

    /// <summary>
    /// Gets the saved incident CSV column mapping configuration.
    /// </summary>
    Task<ServiceNowIncidentColumnMapping?> GetServiceNowIncidentColumnMappingAsync();

    /// <summary>
    /// Saves the incident CSV column mapping configuration.
    /// </summary>
    Task SaveServiceNowIncidentColumnMappingAsync(ServiceNowIncidentColumnMapping mapping);

    /// <summary>
    /// Gets incident analysis summary.
    /// </summary>
    Task<IncidentAnalysisSummary> GetIncidentAnalysisSummaryAsync();

    /// <summary>
    /// Attempts to auto-link incidents to applications based on configuration items.
    /// </summary>
    Task<int> AutoLinkIncidentsToApplicationsAsync();

    // Incident Recommendations (AI-generated)

    /// <summary>
    /// Gets all incident recommendations.
    /// </summary>
    Task<IReadOnlyList<IncidentRecommendation>> GetIncidentRecommendationsAsync();

    /// <summary>
    /// Gets incident recommendations for a specific application.
    /// </summary>
    Task<IReadOnlyList<IncidentRecommendation>> GetIncidentRecommendationsForApplicationAsync(string applicationId);

    /// <summary>
    /// Gets an incident recommendation by ID.
    /// </summary>
    Task<IncidentRecommendation?> GetIncidentRecommendationAsync(string id);

    /// <summary>
    /// Stores incident recommendations (replaces existing ones for same application).
    /// </summary>
    Task<int> StoreIncidentRecommendationsAsync(IEnumerable<IncidentRecommendation> recommendations);

    /// <summary>
    /// Updates the status of an incident recommendation.
    /// </summary>
    Task<IncidentRecommendation> UpdateIncidentRecommendationStatusAsync(string id, RecommendationStatus status, string? notes = null);

    /// <summary>
    /// Deletes old/expired recommendations.
    /// </summary>
    Task<int> CleanupExpiredRecommendationsAsync(int daysOld = 90);
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
    public bool MockDataEnabled { get; init; } = false;
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
    /// <summary>SharePoint root path (e.g., Documents/general/Offerings).</summary>
    public string? RootPath { get; init; }
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

    // Security / CodeQL (Advanced Security)
    public bool AdvancedSecurityEnabled { get; init; }
    public DateTimeOffset? LastSecurityScanDate { get; init; }
    public int OpenCriticalVulnerabilities { get; init; }
    public int OpenHighVulnerabilities { get; init; }
    public int OpenMediumVulnerabilities { get; init; }
    public int OpenLowVulnerabilities { get; init; }
    public int ClosedCriticalVulnerabilities { get; init; }
    public int ClosedHighVulnerabilities { get; init; }
    public int ClosedMediumVulnerabilities { get; init; }
    public int ClosedLowVulnerabilities { get; init; }
    public int ExposedSecretsCount { get; init; }
    public int DependencyAlertCount { get; init; }

    /// <summary>Individual security alerts (code scanning, dependency scanning).</summary>
    public List<DataIntegration.SecurityAlert> SecurityAlerts { get; init; } = [];

    /// <summary>Exposed secret alerts.</summary>
    public List<DataIntegration.SecurityAlert> SecretAlerts { get; init; } = [];

    /// <summary>Total open vulnerabilities across all severities.</summary>
    public int TotalOpenVulnerabilities => OpenCriticalVulnerabilities + OpenHighVulnerabilities + OpenMediumVulnerabilities + OpenLowVulnerabilities;

    /// <summary>Total closed vulnerabilities across all severities.</summary>
    public int TotalClosedVulnerabilities => ClosedCriticalVulnerabilities + ClosedHighVulnerabilities + ClosedMediumVulnerabilities + ClosedLowVulnerabilities;

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

/// <summary>
/// Application imported from ServiceNow CSV.
/// </summary>
public record ImportedServiceNowApplication
{
    public required string Id { get; init; }
    public required string ServiceNowId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? ShortDescription { get; init; }
    public string? Capability { get; init; }
    public string? Status { get; init; }

    // Key Roles
    public string? OwnerId { get; init; }
    public string? OwnerName { get; init; }
    public string? ProductManagerId { get; init; }
    public string? ProductManagerName { get; init; }
    public string? BusinessOwnerId { get; init; }
    public string? BusinessOwnerName { get; init; }
    public string? FunctionalArchitectId { get; init; }
    public string? FunctionalArchitectName { get; init; }
    public string? TechnicalArchitectId { get; init; }
    public string? TechnicalArchitectName { get; init; }

    // Legacy - kept for backwards compatibility
    public string? TechnicalLeadId { get; init; }
    public string? TechnicalLeadName { get; init; }

    // Application Classification
    public string? ApplicationType { get; init; }  // COTS, Homegrown
    public string? ArchitectureType { get; init; } // Web Based, Client Server, Desktop App, Other
    public string? UserBase { get; init; }         // User base range estimate
    public string? Importance { get; init; }       // Importance value from ServiceNow

    // Other fields
    public string? RepositoryUrl { get; init; }
    public string? DocumentationUrl { get; init; }
    public string? Environment { get; init; }
    public string? Criticality { get; init; }
    public string? SupportGroup { get; init; }
    public DateTimeOffset ImportedAt { get; init; } = DateTimeOffset.UtcNow;

    // Raw values from CSV (for debugging/verification)
    public Dictionary<string, string> RawCsvValues { get; init; } = [];

    // Linked repository (if mapped)
    public string? LinkedRepositoryId { get; init; }
    public string? LinkedRepositoryName { get; init; }
}

/// <summary>
/// Saved column mapping for ServiceNow CSV import.
/// Maps CSV column headers to application properties.
/// </summary>
public record ServiceNowColumnMapping
{
    public string? ServiceNowIdColumn { get; init; }
    public string? NameColumn { get; init; }
    public string? DescriptionColumn { get; init; }
    public string? ShortDescriptionColumn { get; init; }
    public string? CapabilityColumn { get; init; }
    public string? StatusColumn { get; init; }

    // Key Roles
    public string? OwnerIdColumn { get; init; }
    public string? OwnerNameColumn { get; init; }
    public string? ProductManagerIdColumn { get; init; }
    public string? ProductManagerNameColumn { get; init; }
    public string? BusinessOwnerIdColumn { get; init; }
    public string? BusinessOwnerNameColumn { get; init; }
    public string? FunctionalArchitectIdColumn { get; init; }
    public string? FunctionalArchitectNameColumn { get; init; }
    public string? TechnicalArchitectIdColumn { get; init; }
    public string? TechnicalArchitectNameColumn { get; init; }

    // Legacy - kept for backwards compatibility
    public string? TechnicalLeadIdColumn { get; init; }
    public string? TechnicalLeadNameColumn { get; init; }

    // Application Classification
    public string? ApplicationTypeColumn { get; init; }
    public string? ArchitectureTypeColumn { get; init; }
    public string? UserBaseColumn { get; init; }
    public string? ImportanceColumn { get; init; }

    // Other fields
    public string? RepositoryUrlColumn { get; init; }
    public string? DocumentationUrlColumn { get; init; }
    public string? EnvironmentColumn { get; init; }
    public string? CriticalityColumn { get; init; }
    public string? SupportGroupColumn { get; init; }

    public DateTimeOffset SavedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Mapping between application names across different data sources.
/// ServiceNow application name is treated as the canonical/correct name.
/// </summary>
public record AppNameMapping
{
    public required string Id { get; init; }

    /// <summary>The canonical application name from ServiceNow.</summary>
    public required string ServiceNowAppName { get; init; }

    /// <summary>The folder name in SharePoint (may differ from ServiceNow name).</summary>
    public string? SharePointFolderName { get; init; }

    /// <summary>The repository name(s) in Azure DevOps (may differ from ServiceNow name).</summary>
    public List<string> AzureDevOpsRepoNames { get; init; } = [];

    /// <summary>Alternative names or aliases for this application.</summary>
    public List<string> AlternativeNames { get; init; } = [];

    /// <summary>The capability/business area this application belongs to.</summary>
    public string? Capability { get; init; }

    /// <summary>Notes about the mapping (e.g., why names differ).</summary>
    public string? Notes { get; init; }

    /// <summary>When this mapping was created/imported.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When this mapping was last updated.</summary>
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>Raw CSV values for debugging.</summary>
    public Dictionary<string, string> RawCsvValues { get; init; } = [];
}

/// <summary>
/// Configuration for app name mapping CSV import.
/// Maps CSV column headers to mapping properties.
/// </summary>
public record AppNameMappingConfig
{
    /// <summary>Column containing the ServiceNow/canonical application name.</summary>
    public string? ServiceNowAppNameColumn { get; init; }

    /// <summary>Column containing the SharePoint folder name.</summary>
    public string? SharePointFolderNameColumn { get; init; }

    /// <summary>Column containing Azure DevOps repository name(s). May contain multiple comma-separated values.</summary>
    public string? AzureDevOpsRepoNameColumn { get; init; }

    /// <summary>Column containing alternative names/aliases.</summary>
    public string? AlternativeNamesColumn { get; init; }

    /// <summary>Column containing the capability/business area.</summary>
    public string? CapabilityColumn { get; init; }

    /// <summary>Column containing notes about the mapping.</summary>
    public string? NotesColumn { get; init; }

    /// <summary>When this configuration was saved.</summary>
    public DateTimeOffset SavedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Mapping of an application to a business capability.
/// </summary>
public record CapabilityMapping
{
    /// <summary>Unique identifier for this mapping.</summary>
    public required string Id { get; init; }

    /// <summary>The ServiceNow/canonical application name.</summary>
    public required string ApplicationName { get; init; }

    /// <summary>The business capability this application belongs to.</summary>
    public required string Capability { get; init; }

    /// <summary>When this mapping was created/imported.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>When this mapping was last updated.</summary>
    public DateTimeOffset? UpdatedAt { get; init; }
}

/// <summary>
/// A SharePoint folder discovered during sync.
/// </summary>
public record DiscoveredSharePointFolder
{
    public required string Id { get; init; }

    /// <summary>Folder name (potential application name).</summary>
    public required string Name { get; init; }

    /// <summary>Full path in SharePoint (e.g., "Documents/general/Offerings/Finance/BudgetApp").</summary>
    public required string FullPath { get; init; }

    /// <summary>URL to the folder in SharePoint.</summary>
    public string? Url { get; init; }

    /// <summary>Parent capability folder name.</summary>
    public string? Capability { get; init; }

    /// <summary>Template subfolders found (Project Documents, Technical Documentation, etc.).</summary>
    public List<string> TemplateFoldersFound { get; init; } = [];

    /// <summary>Whether this folder has all 4 expected template subfolders.</summary>
    public bool HasAllTemplateFolders => TemplateFoldersFound.Count >= 4;

    /// <summary>Document counts per template folder.</summary>
    public Dictionary<string, int> DocumentCounts { get; init; } = [];

    /// <summary>Total document count across all subfolders.</summary>
    public int TotalDocumentCount => DocumentCounts.Values.Sum();

    /// <summary>When the folder was last modified in SharePoint.</summary>
    public DateTimeOffset? LastModified { get; init; }

    /// <summary>When this folder was discovered/synced.</summary>
    public DateTimeOffset SyncedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Linked ServiceNow application name (if mapped).</summary>
    public string? LinkedServiceNowAppName { get; init; }

    /// <summary>Linked application ID (if resolved).</summary>
    public string? LinkedApplicationId { get; init; }
}

/// <summary>
/// SharePoint configuration settings.
/// </summary>
public record SharePointConfig
{
    /// <summary>Root path for offerings documentation (e.g., "Documents/general/Offerings").</summary>
    public string RootPath { get; init; } = "Documents/general/Offerings";

    /// <summary>Expected template subfolder names.</summary>
    public List<string> TemplateFolderNames { get; init; } =
    [
        "Project Documents",
        "Promotional Content",
        "Technical Documentation",
        "User Documentation"
    ];

    /// <summary>Whether to recursively search for application folders.</summary>
    public bool RecursiveSearch { get; init; } = true;

    /// <summary>Maximum folder depth to search (0 = unlimited).</summary>
    public int MaxSearchDepth { get; init; } = 3;

    /// <summary>When this configuration was last updated.</summary>
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}
