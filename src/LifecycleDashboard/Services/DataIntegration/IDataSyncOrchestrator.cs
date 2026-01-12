namespace LifecycleDashboard.Services.DataIntegration;

/// <summary>
/// Orchestrator service that coordinates data synchronization across all data sources,
/// tracks sync job status, and detects/resolves data conflicts.
/// </summary>
public interface IDataSyncOrchestrator
{
    /// <summary>
    /// Gets the status of all configured data sources.
    /// </summary>
    Task<List<DataSourceStatus>> GetDataSourceStatusesAsync();

    /// <summary>
    /// Gets the history of sync jobs.
    /// </summary>
    Task<List<SyncJobInfo>> GetSyncJobHistoryAsync(int limit = 50);

    /// <summary>
    /// Gets a specific sync job by ID.
    /// </summary>
    Task<SyncJobInfo?> GetSyncJobAsync(string jobId);

    /// <summary>
    /// Runs sync for a specific data source.
    /// </summary>
    Task<DataSyncResult> SyncDataSourceAsync(DataSourceType dataSource, string? triggeredBy = null);

    /// <summary>
    /// Runs sync for all configured data sources.
    /// </summary>
    Task<DataSyncResult> SyncAllAsync(string? triggeredBy = null);

    /// <summary>
    /// Gets the next scheduled sync time.
    /// </summary>
    Task<DateTimeOffset?> GetNextScheduledSyncAsync();

    /// <summary>
    /// Detects data conflicts across all sources.
    /// </summary>
    Task<List<DataConflict>> DetectConflictsAsync();

    /// <summary>
    /// Gets all unresolved data conflicts.
    /// </summary>
    Task<List<DataConflict>> GetUnresolvedConflictsAsync();

    /// <summary>
    /// Resolves a data conflict.
    /// </summary>
    Task ResolveConflictAsync(string conflictId, string resolution, string resolvedBy, string resolvedByName);

    /// <summary>
    /// Cancels a running sync job.
    /// </summary>
    Task<bool> CancelSyncJobAsync(string jobId);

    /// <summary>
    /// Gets sync configuration.
    /// </summary>
    SyncConfiguration GetConfiguration();

    /// <summary>
    /// Updates sync configuration.
    /// </summary>
    Task SetConfigurationAsync(SyncConfiguration configuration);

    /// <summary>
    /// Event raised when a sync job starts.
    /// </summary>
    event EventHandler<SyncJobEventArgs>? SyncJobStarted;

    /// <summary>
    /// Event raised when a sync job completes.
    /// </summary>
    event EventHandler<SyncJobEventArgs>? SyncJobCompleted;

    /// <summary>
    /// Event raised when a sync job fails.
    /// </summary>
    event EventHandler<SyncJobEventArgs>? SyncJobFailed;

    /// <summary>
    /// Event raised when a conflict is detected.
    /// </summary>
    event EventHandler<DataConflictEventArgs>? ConflictDetected;

    /// <summary>
    /// Event raised when sync progress is updated.
    /// </summary>
    event EventHandler<SyncProgressEventArgs>? SyncProgressUpdated;
}

/// <summary>
/// Configuration for data synchronization.
/// </summary>
public record SyncConfiguration
{
    /// <summary>Whether automatic scheduled sync is enabled.</summary>
    public bool AutoSyncEnabled { get; init; } = true;

    /// <summary>Day of week for weekly sync (0 = Sunday).</summary>
    public DayOfWeek SyncDayOfWeek { get; init; } = DayOfWeek.Sunday;

    /// <summary>Hour of day for sync (0-23, in server local time).</summary>
    public int SyncHour { get; init; } = 2;

    /// <summary>Minutes past the hour for sync.</summary>
    public int SyncMinute { get; init; } = 0;

    /// <summary>Timeout for sync operations in minutes.</summary>
    public int SyncTimeoutMinutes { get; init; } = 60;

    /// <summary>Number of retries for failed sync operations.</summary>
    public int MaxRetries { get; init; } = 3;

    /// <summary>Delay between retries in seconds.</summary>
    public int RetryDelaySeconds { get; init; } = 30;

    /// <summary>Whether to run conflict detection after sync.</summary>
    public bool RunConflictDetection { get; init; } = true;

    /// <summary>Which data sources to sync.</summary>
    public List<DataSourceType> EnabledSources { get; init; } =
    [
        DataSourceType.ServiceNow,
        DataSourceType.AzureDevOps,
        DataSourceType.SharePoint,
        DataSourceType.IisDatabase
    ];

    /// <summary>Order in which to sync sources (for dependency resolution).</summary>
    public List<DataSourceType> SyncOrder { get; init; } =
    [
        DataSourceType.ServiceNow,    // Primary source of truth for apps
        DataSourceType.SharePoint,     // Documentation depends on app names
        DataSourceType.AzureDevOps,    // Repository data
        DataSourceType.IisDatabase     // Usage data
    ];

    /// <summary>Email addresses to notify on sync completion.</summary>
    public List<string> NotificationEmails { get; init; } = [];

    /// <summary>Whether to notify only on failures.</summary>
    public bool NotifyOnFailureOnly { get; init; } = false;
}

/// <summary>
/// Event arguments for sync job events.
/// </summary>
public class SyncJobEventArgs : EventArgs
{
    /// <summary>The sync job information.</summary>
    public required SyncJobInfo Job { get; init; }

    /// <summary>Sync result (for completed/failed events).</summary>
    public DataSyncResult? Result { get; init; }
}

/// <summary>
/// Event arguments for conflict detection events.
/// </summary>
public class DataConflictEventArgs : EventArgs
{
    /// <summary>The detected conflict.</summary>
    public required DataConflict Conflict { get; init; }
}

/// <summary>
/// Statistics about sync operations.
/// </summary>
public record SyncStatistics
{
    /// <summary>Total sync jobs run.</summary>
    public int TotalSyncJobs { get; init; }

    /// <summary>Successful sync jobs.</summary>
    public int SuccessfulJobs { get; init; }

    /// <summary>Failed sync jobs.</summary>
    public int FailedJobs { get; init; }

    /// <summary>Average sync duration.</summary>
    public TimeSpan AverageDuration { get; init; }

    /// <summary>Total records synced.</summary>
    public long TotalRecordsSynced { get; init; }

    /// <summary>Total conflicts detected.</summary>
    public int TotalConflicts { get; init; }

    /// <summary>Unresolved conflicts.</summary>
    public int UnresolvedConflicts { get; init; }

    /// <summary>Last successful sync per data source.</summary>
    public Dictionary<DataSourceType, DateTimeOffset?> LastSuccessfulSync { get; init; } = [];

    /// <summary>Statistics period start.</summary>
    public DateTimeOffset PeriodStart { get; init; }

    /// <summary>Statistics period end.</summary>
    public DateTimeOffset PeriodEnd { get; init; }
}

/// <summary>
/// Event arguments for sync progress updates.
/// </summary>
public class SyncProgressEventArgs : EventArgs
{
    /// <summary>The job ID for this progress update.</summary>
    public required string JobId { get; init; }

    /// <summary>The data source being synced.</summary>
    public required DataSourceType DataSource { get; init; }

    /// <summary>Current phase of the sync operation.</summary>
    public required string Phase { get; init; }

    /// <summary>Total number of items to process.</summary>
    public int TotalItems { get; init; }

    /// <summary>Number of items processed so far.</summary>
    public int ProcessedItems { get; init; }

    /// <summary>Current item being processed (if applicable).</summary>
    public string? CurrentItem { get; init; }

    /// <summary>Progress percentage (0-100).</summary>
    public int ProgressPercent => TotalItems > 0 ? (int)Math.Round((double)ProcessedItems / TotalItems * 100) : 0;

    /// <summary>Optional status message.</summary>
    public string? Message { get; init; }
}
