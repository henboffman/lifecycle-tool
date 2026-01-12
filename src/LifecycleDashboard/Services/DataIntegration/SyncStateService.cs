namespace LifecycleDashboard.Services.DataIntegration;

/// <summary>
/// Singleton service that tracks sync state across page navigations.
/// Allows users to navigate away and come back to see current progress.
/// </summary>
public class SyncStateService : ISyncStateService
{
    private readonly object _lock = new();

    public bool IsRunning { get; private set; }
    public string? RunningJobId { get; private set; }
    public DataSourceType? RunningSource { get; private set; }
    public SyncProgressEventArgs? CurrentProgress { get; private set; }
    public DateTimeOffset? StartTime { get; private set; }
    public DataSyncResult? LastResult { get; private set; }

    public event EventHandler<SyncProgressEventArgs>? ProgressUpdated;
    public event EventHandler<SyncJobEventArgs>? JobCompleted;

    public void StartJob(string jobId, DataSourceType source)
    {
        lock (_lock)
        {
            IsRunning = true;
            RunningJobId = jobId;
            RunningSource = source;
            CurrentProgress = null;
            StartTime = DateTimeOffset.UtcNow;
            LastResult = null;
        }
    }

    public void UpdateProgress(SyncProgressEventArgs progress)
    {
        lock (_lock)
        {
            CurrentProgress = progress;
        }
        ProgressUpdated?.Invoke(this, progress);
    }

    public void CompleteJob(DataSyncResult result)
    {
        SyncJobEventArgs? args = null;
        lock (_lock)
        {
            LastResult = result;
            IsRunning = false;

            if (RunningJobId != null)
            {
                args = new SyncJobEventArgs
                {
                    Job = new SyncJobInfo
                    {
                        Id = RunningJobId,
                        DataSource = RunningSource ?? DataSourceType.AzureDevOps,
                        Status = result.Success ? SyncJobStatus.Completed : SyncJobStatus.Failed,
                        StartTime = StartTime ?? DateTimeOffset.UtcNow,
                        EndTime = DateTimeOffset.UtcNow
                    },
                    Result = result
                };
            }

            RunningJobId = null;
            RunningSource = null;
            CurrentProgress = null;
            StartTime = null;
        }

        if (args != null)
        {
            JobCompleted?.Invoke(this, args);
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            IsRunning = false;
            RunningJobId = null;
            RunningSource = null;
            CurrentProgress = null;
            StartTime = null;
            LastResult = null;
        }
    }
}

/// <summary>
/// Interface for the sync state service.
/// </summary>
public interface ISyncStateService
{
    bool IsRunning { get; }
    string? RunningJobId { get; }
    DataSourceType? RunningSource { get; }
    SyncProgressEventArgs? CurrentProgress { get; }
    DateTimeOffset? StartTime { get; }
    DataSyncResult? LastResult { get; }

    event EventHandler<SyncProgressEventArgs>? ProgressUpdated;
    event EventHandler<SyncJobEventArgs>? JobCompleted;

    void StartJob(string jobId, DataSourceType source);
    void UpdateProgress(SyncProgressEventArgs progress);
    void CompleteJob(DataSyncResult result);
    void Reset();
}
