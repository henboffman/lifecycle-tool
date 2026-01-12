namespace LifecycleDashboard.Services.DataIntegration;

/// <summary>
/// Result of a data synchronization operation.
/// </summary>
public record DataSyncResult
{
    /// <summary>Whether the sync completed successfully (may still have partial errors).</summary>
    public bool Success { get; init; }

    /// <summary>Overall error message if the sync failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Data source that was synced.</summary>
    public DataSourceType DataSource { get; init; }

    /// <summary>When the sync started.</summary>
    public DateTimeOffset StartTime { get; init; }

    /// <summary>When the sync completed.</summary>
    public DateTimeOffset EndTime { get; init; }

    /// <summary>Duration of the sync operation.</summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>Total records processed.</summary>
    public int RecordsProcessed { get; init; }

    /// <summary>Records created during sync.</summary>
    public int RecordsCreated { get; init; }

    /// <summary>Records updated during sync.</summary>
    public int RecordsUpdated { get; init; }

    /// <summary>Records that had no changes.</summary>
    public int RecordsUnchanged { get; init; }

    /// <summary>Individual errors encountered during sync.</summary>
    public List<SyncError> Errors { get; init; } = [];

    /// <summary>Data conflicts detected during sync.</summary>
    public List<DataConflict> ConflictsDetected { get; init; } = [];

    /// <summary>Detailed results for each sync step (for debugging/reporting).</summary>
    public List<SyncStepResult> StepResults { get; init; } = [];

    /// <summary>Creates a successful result.</summary>
    public static DataSyncResult Succeeded(DataSourceType dataSource, DateTimeOffset startTime, int processed, int created, int updated, int unchanged = 0)
    {
        return new DataSyncResult
        {
            Success = true,
            DataSource = dataSource,
            StartTime = startTime,
            EndTime = DateTimeOffset.UtcNow,
            RecordsProcessed = processed,
            RecordsCreated = created,
            RecordsUpdated = updated,
            RecordsUnchanged = unchanged
        };
    }

    /// <summary>Creates a failed result.</summary>
    public static DataSyncResult Failed(DataSourceType dataSource, DateTimeOffset startTime, string errorMessage)
    {
        return new DataSyncResult
        {
            Success = false,
            DataSource = dataSource,
            StartTime = startTime,
            EndTime = DateTimeOffset.UtcNow,
            ErrorMessage = errorMessage
        };
    }
}

/// <summary>
/// Generic result wrapper for data sync operations that return data.
/// </summary>
public record DataSyncResult<T> : DataSyncResult
{
    /// <summary>The data retrieved by the operation.</summary>
    public T? Data { get; init; }

    /// <summary>Creates a successful result with data.</summary>
    public static DataSyncResult<T> Succeeded(DataSourceType dataSource, T data)
    {
        return new DataSyncResult<T>
        {
            Success = true,
            DataSource = dataSource,
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow,
            Data = data
        };
    }

    /// <summary>Creates a failed result.</summary>
    public static new DataSyncResult<T> Failed(DataSourceType dataSource, DateTimeOffset startTime, string errorMessage)
    {
        return new DataSyncResult<T>
        {
            Success = false,
            DataSource = dataSource,
            StartTime = startTime,
            EndTime = DateTimeOffset.UtcNow,
            ErrorMessage = errorMessage
        };
    }
}

/// <summary>
/// Result of testing a data source connection.
/// </summary>
public record ConnectionTestResult
{
    /// <summary>Whether the connection test succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>Human-readable status message.</summary>
    public string? Message { get; init; }

    /// <summary>Time taken to establish the connection.</summary>
    public TimeSpan? ResponseTime { get; init; }

    /// <summary>Version information from the remote service (if available).</summary>
    public string? Version { get; init; }

    /// <summary>Additional details about the connection.</summary>
    public Dictionary<string, string> Details { get; init; } = [];

    /// <summary>Creates a successful connection result.</summary>
    public static ConnectionTestResult Succeeded(string message, TimeSpan responseTime, string? version = null)
    {
        return new ConnectionTestResult
        {
            Success = true,
            Message = message,
            ResponseTime = responseTime,
            Version = version
        };
    }

    /// <summary>Creates a failed connection result.</summary>
    public static ConnectionTestResult Failed(string message)
    {
        return new ConnectionTestResult
        {
            Success = false,
            Message = message
        };
    }
}

/// <summary>
/// Individual error encountered during sync.
/// </summary>
public record SyncError
{
    /// <summary>Unique identifier for this error.</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString();

    /// <summary>When the error occurred.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Type/category of error.</summary>
    public SyncErrorType Type { get; init; }

    /// <summary>Error message.</summary>
    public required string Message { get; init; }

    /// <summary>Entity that caused the error (if applicable).</summary>
    public string? EntityId { get; init; }

    /// <summary>Entity name for display.</summary>
    public string? EntityName { get; init; }

    /// <summary>Stack trace or technical details.</summary>
    public string? TechnicalDetails { get; init; }
}

/// <summary>
/// Types of errors that can occur during sync.
/// </summary>
public enum SyncErrorType
{
    /// <summary>Could not connect to the data source.</summary>
    ConnectionError,

    /// <summary>Authentication failed.</summary>
    AuthenticationError,

    /// <summary>Access denied to resource.</summary>
    AuthorizationError,

    /// <summary>Resource not found.</summary>
    NotFound,

    /// <summary>Data validation failed.</summary>
    ValidationError,

    /// <summary>Data parsing/transformation failed.</summary>
    ParseError,

    /// <summary>Operation timed out.</summary>
    Timeout,

    /// <summary>Rate limit exceeded.</summary>
    RateLimited,

    /// <summary>General/unknown error.</summary>
    Unknown
}

/// <summary>
/// Data conflict detected between sources.
/// </summary>
public record DataConflict
{
    /// <summary>Unique identifier for this conflict.</summary>
    public required string Id { get; init; }

    /// <summary>Application this conflict relates to.</summary>
    public required string ApplicationId { get; init; }

    /// <summary>Application name for display.</summary>
    public required string ApplicationName { get; init; }

    /// <summary>Type of conflict.</summary>
    public required ConflictType Type { get; init; }

    /// <summary>Human-readable description of the conflict.</summary>
    public required string Description { get; init; }

    /// <summary>First data source in the conflict.</summary>
    public string? SourceA { get; init; }

    /// <summary>Value from first source.</summary>
    public string? ValueA { get; init; }

    /// <summary>Second data source in the conflict.</summary>
    public string? SourceB { get; init; }

    /// <summary>Value from second source.</summary>
    public string? ValueB { get; init; }

    /// <summary>When the conflict was detected.</summary>
    public DateTimeOffset DetectedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Whether the conflict has been resolved.</summary>
    public bool IsResolved { get; init; }

    /// <summary>How the conflict was resolved.</summary>
    public string? Resolution { get; init; }

    /// <summary>Who resolved the conflict.</summary>
    public string? ResolvedBy { get; init; }

    /// <summary>When the conflict was resolved.</summary>
    public DateTimeOffset? ResolvedAt { get; init; }
}

/// <summary>
/// Types of data conflicts that can be detected.
/// </summary>
public enum ConflictType
{
    /// <summary>Application name differs between sources.</summary>
    NameMismatch,

    /// <summary>Repository URL is missing or invalid.</summary>
    MissingRepository,

    /// <summary>Repository URL points to non-existent resource.</summary>
    InvalidRepository,

    /// <summary>User referenced in data doesn't exist in directory.</summary>
    UserNotFound,

    /// <summary>Same repository assigned to multiple applications.</summary>
    DuplicateRepository,

    /// <summary>Expected documentation folder not found.</summary>
    MissingDocumentation,

    /// <summary>Documentation exists for unknown application.</summary>
    OrphanedDocumentation,

    /// <summary>Application exists in ServiceNow but not SharePoint.</summary>
    MissingSharePointFolder,

    /// <summary>SharePoint folder exists but no ServiceNow CI.</summary>
    OrphanedSharePointFolder,

    /// <summary>Role assignment conflict.</summary>
    RoleConflict,

    /// <summary>Usage data mismatch.</summary>
    UsageDataMismatch
}

/// <summary>
/// Information about a sync job.
/// </summary>
public record SyncJobInfo
{
    /// <summary>Unique identifier for this job.</summary>
    public required string Id { get; init; }

    /// <summary>Data source being synced.</summary>
    public required DataSourceType DataSource { get; init; }

    /// <summary>When the job started.</summary>
    public required DateTimeOffset StartTime { get; init; }

    /// <summary>When the job ended (null if still running).</summary>
    public DateTimeOffset? EndTime { get; init; }

    /// <summary>Current status of the job.</summary>
    public SyncJobStatus Status { get; init; }

    /// <summary>Records processed so far.</summary>
    public int RecordsProcessed { get; init; }

    /// <summary>Records created.</summary>
    public int RecordsCreated { get; init; }

    /// <summary>Records updated.</summary>
    public int RecordsUpdated { get; init; }

    /// <summary>Number of errors encountered.</summary>
    public int ErrorCount { get; init; }

    /// <summary>Error message if failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Who triggered the sync (null for scheduled).</summary>
    public string? TriggeredBy { get; init; }

    /// <summary>Duration of the job.</summary>
    public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : null;
}

/// <summary>
/// Status of a sync job.
/// </summary>
public enum SyncJobStatus
{
    /// <summary>Job is queued but not started.</summary>
    Pending,

    /// <summary>Job is currently running.</summary>
    Running,

    /// <summary>Job completed successfully.</summary>
    Completed,

    /// <summary>Job completed with some errors.</summary>
    CompletedWithErrors,

    /// <summary>Job failed.</summary>
    Failed,

    /// <summary>Job was cancelled.</summary>
    Cancelled
}

/// <summary>
/// Status of a data source.
/// </summary>
public record DataSourceStatus
{
    /// <summary>Data source type.</summary>
    public required DataSourceType DataSource { get; init; }

    /// <summary>Display name.</summary>
    public required string Name { get; init; }

    /// <summary>Whether credentials are configured.</summary>
    public bool IsConfigured { get; init; }

    /// <summary>Whether connection test passed.</summary>
    public bool IsConnected { get; init; }

    /// <summary>Last successful sync time.</summary>
    public DateTimeOffset? LastSyncTime { get; init; }

    /// <summary>Last sync result status.</summary>
    public SyncJobStatus? LastSyncStatus { get; init; }

    /// <summary>Number of records from this source.</summary>
    public int RecordCount { get; init; }

    /// <summary>Connection error message if not connected.</summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Data source types for synchronization.
/// </summary>
public enum DataSourceType
{
    /// <summary>Azure DevOps for repository data.</summary>
    AzureDevOps,

    /// <summary>SharePoint for documentation.</summary>
    SharePoint,

    /// <summary>ServiceNow for CMDB data.</summary>
    ServiceNow,

    /// <summary>IIS database for usage metrics.</summary>
    IisDatabase
}

/// <summary>
/// Result of an individual sync step (e.g., tech stack detection, commit history, etc.)
/// </summary>
public record SyncStepResult
{
    /// <summary>Name of the sync step.</summary>
    public required string StepName { get; init; }

    /// <summary>Whether this step succeeded.</summary>
    public bool Success { get; init; }

    /// <summary>Number of items successfully processed.</summary>
    public int SuccessCount { get; init; }

    /// <summary>Number of items that failed.</summary>
    public int FailCount { get; init; }

    /// <summary>Number of items skipped (e.g., no data available).</summary>
    public int SkipCount { get; init; }

    /// <summary>Error message if the step failed.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Duration of this step.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Additional details about this step.</summary>
    public Dictionary<string, object> Details { get; init; } = [];
}
