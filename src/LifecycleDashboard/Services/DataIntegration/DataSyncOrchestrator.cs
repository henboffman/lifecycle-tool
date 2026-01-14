using LifecycleDashboard.Data;
using LifecycleDashboard.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LifecycleDashboard.Services.DataIntegration;

/// <summary>
/// Orchestrator service that coordinates data synchronization across all data sources,
/// tracks sync job status, and detects/resolves data conflicts.
/// </summary>
public class DataSyncOrchestrator : IDataSyncOrchestrator
{
    private readonly IAzureDevOpsService _azureDevOpsService;
    private readonly ISharePointService _sharePointService;
    private readonly IServiceNowService _serviceNowService;
    private readonly IIisDatabaseService _iisDatabaseService;
    private readonly IMockDataService _mockDataService;
    private readonly ISecureStorageService _secureStorage;
    private readonly IAuditService _auditService;
    private readonly IDbContextFactory<LifecycleDbContext> _contextFactory;
    private readonly ISyncStateService _syncStateService;
    private readonly ILogger<DataSyncOrchestrator> _logger;

    private SyncConfiguration _configuration = new();
    private readonly List<SyncJobInfo> _syncJobHistory = [];
    private readonly List<DataConflict> _unresolvedConflicts = [];
    private readonly Dictionary<string, CancellationTokenSource> _runningJobs = [];
    private bool _jobHistoryLoaded;

    public event EventHandler<SyncJobEventArgs>? SyncJobStarted;
    public event EventHandler<SyncJobEventArgs>? SyncJobCompleted;
    public event EventHandler<SyncJobEventArgs>? SyncJobFailed;
    public event EventHandler<DataConflictEventArgs>? ConflictDetected;
    public event EventHandler<SyncProgressEventArgs>? SyncProgressUpdated;

    public DataSyncOrchestrator(
        IAzureDevOpsService azureDevOpsService,
        ISharePointService sharePointService,
        IServiceNowService serviceNowService,
        IIisDatabaseService iisDatabaseService,
        IMockDataService mockDataService,
        ISecureStorageService secureStorage,
        IAuditService auditService,
        IDbContextFactory<LifecycleDbContext> contextFactory,
        ISyncStateService syncStateService,
        ILogger<DataSyncOrchestrator> logger)
    {
        _azureDevOpsService = azureDevOpsService;
        _sharePointService = sharePointService;
        _serviceNowService = serviceNowService;
        _iisDatabaseService = iisDatabaseService;
        _mockDataService = mockDataService;
        _secureStorage = secureStorage;
        _auditService = auditService;
        _contextFactory = contextFactory;
        _syncStateService = syncStateService;
        _logger = logger;
    }

    /// <summary>
    /// Ensures job history is loaded from the database.
    /// </summary>
    private async Task EnsureJobHistoryLoadedAsync()
    {
        if (_jobHistoryLoaded) return;

        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var entities = await context.SyncJobs
                .OrderByDescending(j => j.StartTime)
                .Take(100) // Keep last 100 jobs
                .ToListAsync();

            _syncJobHistory.Clear();
            _syncJobHistory.AddRange(entities.Select(e => e.ToModel()));
            _jobHistoryLoaded = true;
            _logger.LogInformation("Loaded {Count} sync jobs from database", _syncJobHistory.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load sync job history from database");
            _jobHistoryLoaded = true; // Don't keep retrying
        }
    }

    /// <summary>
    /// Persists a sync job to the database.
    /// </summary>
    private async Task PersistJobAsync(SyncJobInfo job)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var existing = await context.SyncJobs.FirstOrDefaultAsync(j => j.Id == job.Id);

            if (existing != null)
            {
                job.ToEntity(existing);
            }
            else
            {
                context.SyncJobs.Add(job.ToEntity());
            }

            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist sync job {JobId} to database", job.Id);
        }
    }

    /// <inheritdoc />
    public async Task<List<DataSourceStatus>> GetDataSourceStatusesAsync()
    {
        await EnsureJobHistoryLoadedAsync();

        var statuses = new List<DataSourceStatus>();

        // Test each data source in parallel
        var tasks = new List<Task<(DataSourceType Type, ConnectionTestResult Result)>>
        {
            TestDataSourceAsync(DataSourceType.AzureDevOps, _azureDevOpsService.TestConnectionAsync),
            TestDataSourceAsync(DataSourceType.SharePoint, _sharePointService.TestConnectionAsync),
            TestDataSourceAsync(DataSourceType.ServiceNow, _serviceNowService.TestConnectionAsync),
            TestDataSourceAsync(DataSourceType.IisDatabase, _iisDatabaseService.TestConnectionAsync)
        };

        var results = await Task.WhenAll(tasks);

        foreach (var (type, result) in results)
        {
            var lastJob = _syncJobHistory
                .Where(j => j.DataSource == type)
                .OrderByDescending(j => j.EndTime)
                .FirstOrDefault();

            statuses.Add(new DataSourceStatus
            {
                DataSource = type,
                Name = GetDataSourceDisplayName(type),
                IsConfigured = _configuration.EnabledSources.Contains(type),
                IsConnected = result.Success,
                LastSyncTime = lastJob?.EndTime,
                LastSyncStatus = lastJob?.Status,
                ErrorMessage = result.Success ? null : result.Message
            });
        }

        return statuses;
    }

    private static string GetDataSourceDisplayName(DataSourceType type) => type switch
    {
        DataSourceType.AzureDevOps => "Azure DevOps",
        DataSourceType.SharePoint => "SharePoint",
        DataSourceType.ServiceNow => "ServiceNow",
        DataSourceType.IisDatabase => "IIS Database",
        _ => type.ToString()
    };

    /// <inheritdoc />
    public async Task<List<SyncJobInfo>> GetSyncJobHistoryAsync(int limit = 50)
    {
        await EnsureJobHistoryLoadedAsync();

        var jobs = _syncJobHistory
            .OrderByDescending(j => j.StartTime)
            .Take(limit)
            .ToList();

        return jobs;
    }

    /// <inheritdoc />
    public async Task<SyncJobInfo?> GetSyncJobAsync(string jobId)
    {
        await EnsureJobHistoryLoadedAsync();
        return _syncJobHistory.FirstOrDefault(j => j.Id == jobId);
    }

    /// <inheritdoc />
    public async Task<DataSyncResult> SyncDataSourceAsync(DataSourceType dataSource, string? triggeredBy = null)
    {
        var startTime = DateTimeOffset.UtcNow;
        var jobId = Guid.NewGuid().ToString();
        var cts = new CancellationTokenSource(TimeSpan.FromMinutes(_configuration.SyncTimeoutMinutes));

        var job = new SyncJobInfo
        {
            Id = jobId,
            DataSource = dataSource,
            StartTime = startTime,
            Status = SyncJobStatus.Running,
            TriggeredBy = triggeredBy ?? "System"
        };

        _syncJobHistory.Add(job);
        _runningJobs[jobId] = cts;
        await PersistJobAsync(job);

        // Raise started event and update shared state
        SyncJobStarted?.Invoke(this, new SyncJobEventArgs { Job = job });
        _syncStateService.StartJob(jobId, dataSource);

        // Log audit event
        await _auditService.LogSyncStartedAsync(dataSource.ToString(), jobId);

        try
        {
            var result = dataSource switch
            {
                DataSourceType.AzureDevOps => await SyncAzureDevOpsAsync(jobId, cts.Token),
                DataSourceType.SharePoint => await SyncSharePointAsync(jobId, cts.Token),
                DataSourceType.ServiceNow => await SyncServiceNowAsync(jobId, cts.Token),
                DataSourceType.IisDatabase => await SyncIisDatabaseAsync(jobId, cts.Token),
                _ => DataSyncResult.Failed(dataSource, startTime, $"Unknown data source: {dataSource}")
            };

            // Update job status
            job = job with
            {
                EndTime = DateTimeOffset.UtcNow,
                Status = result.Success ? SyncJobStatus.Completed : SyncJobStatus.Failed,
                RecordsProcessed = result.RecordsProcessed,
                RecordsCreated = result.RecordsCreated,
                RecordsUpdated = result.RecordsUpdated,
                ErrorCount = result.Errors.Count,
                ErrorMessage = result.ErrorMessage ?? result.Errors.FirstOrDefault()?.Message
            };

            // Update in history and persist
            var index = _syncJobHistory.FindIndex(j => j.Id == jobId);
            if (index >= 0)
            {
                _syncJobHistory[index] = job;
            }
            await PersistJobAsync(job);

            // Run conflict detection if enabled
            if (result.Success && _configuration.RunConflictDetection)
            {
                var conflicts = await DetectConflictsForSourceAsync(dataSource);
                foreach (var conflict in conflicts)
                {
                    _unresolvedConflicts.Add(conflict);
                    ConflictDetected?.Invoke(this, new DataConflictEventArgs { Conflict = conflict });
                }
            }

            // Raise completed/failed event and update shared state
            _syncStateService.CompleteJob(result);
            if (result.Success)
            {
                await _auditService.LogSyncCompletedAsync(dataSource.ToString(), jobId, result.RecordsProcessed,
                    result.RecordsCreated, result.RecordsUpdated, result.Duration);
                SyncJobCompleted?.Invoke(this, new SyncJobEventArgs { Job = job, Result = result });
            }
            else
            {
                await _auditService.LogSyncFailedAsync(dataSource.ToString(), jobId,
                    result.ErrorMessage ?? "Unknown error", result.Duration);
                SyncJobFailed?.Invoke(this, new SyncJobEventArgs { Job = job, Result = result });
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            var endTime = DateTimeOffset.UtcNow;
            job = job with
            {
                EndTime = endTime,
                Status = SyncJobStatus.Cancelled,
                ErrorMessage = "Operation was cancelled"
            };

            var index = _syncJobHistory.FindIndex(j => j.Id == jobId);
            if (index >= 0)
            {
                _syncJobHistory[index] = job;
            }
            await PersistJobAsync(job);

            var cancelResult = DataSyncResult.Failed(dataSource, startTime, "Sync operation was cancelled");
            _syncStateService.CompleteJob(cancelResult);

            await _auditService.LogSyncFailedAsync(dataSource.ToString(), jobId,
                "Operation was cancelled", endTime - startTime);

            return cancelResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing data source {DataSource}", dataSource);

            var endTime = DateTimeOffset.UtcNow;
            job = job with
            {
                EndTime = endTime,
                Status = SyncJobStatus.Failed,
                ErrorMessage = ex.Message
            };

            var index = _syncJobHistory.FindIndex(j => j.Id == jobId);
            if (index >= 0)
            {
                _syncJobHistory[index] = job;
            }
            await PersistJobAsync(job);

            var errorResult = DataSyncResult.Failed(dataSource, startTime, ex.Message);
            _syncStateService.CompleteJob(errorResult);

            await _auditService.LogSyncFailedAsync(dataSource.ToString(), jobId, ex.Message, endTime - startTime);
            SyncJobFailed?.Invoke(this, new SyncJobEventArgs { Job = job });

            return errorResult;
        }
        finally
        {
            _runningJobs.Remove(jobId);
            cts.Dispose();
        }
    }

    /// <inheritdoc />
    public async Task<DataSyncResult> SyncAllAsync(string? triggeredBy = null)
    {
        var startTime = DateTimeOffset.UtcNow;
        var allErrors = new List<SyncError>();
        var totalProcessed = 0;
        var totalCreated = 0;
        var totalUpdated = 0;

        foreach (var source in _configuration.SyncOrder.Where(s => _configuration.EnabledSources.Contains(s)))
        {
            var result = await SyncDataSourceAsync(source, triggeredBy);

            totalProcessed += result.RecordsProcessed;
            totalCreated += result.RecordsCreated;
            totalUpdated += result.RecordsUpdated;
            allErrors.AddRange(result.Errors);

            // If a sync fails and we have retries configured, retry
            if (!result.Success && _configuration.MaxRetries > 0)
            {
                for (var retry = 0; retry < _configuration.MaxRetries; retry++)
                {
                    _logger.LogWarning("Retrying {Source} sync (attempt {Attempt}/{MaxRetries})",
                        source, retry + 1, _configuration.MaxRetries);

                    await Task.Delay(TimeSpan.FromSeconds(_configuration.RetryDelaySeconds));

                    result = await SyncDataSourceAsync(source, triggeredBy);
                    if (result.Success)
                        break;
                }
            }
        }

        return new DataSyncResult
        {
            Success = allErrors.Count == 0,
            RecordsProcessed = totalProcessed,
            RecordsCreated = totalCreated,
            RecordsUpdated = totalUpdated,
            Errors = allErrors,
            StartTime = startTime,
            EndTime = DateTimeOffset.UtcNow
        };
    }

    /// <inheritdoc />
    public Task<DateTimeOffset?> GetNextScheduledSyncAsync()
    {
        if (!_configuration.AutoSyncEnabled)
        {
            return Task.FromResult<DateTimeOffset?>(null);
        }

        var now = DateTimeOffset.Now;
        var nextSync = now.Date;

        // Find next occurrence of the configured day
        while (nextSync.DayOfWeek != _configuration.SyncDayOfWeek || nextSync <= now.Date)
        {
            nextSync = nextSync.AddDays(1);
        }

        // Add configured time
        nextSync = nextSync.AddHours(_configuration.SyncHour).AddMinutes(_configuration.SyncMinute);

        // If we've passed today's sync time, move to next week
        if (nextSync <= now && nextSync.DayOfWeek == _configuration.SyncDayOfWeek)
        {
            nextSync = nextSync.AddDays(7);
        }

        return Task.FromResult<DateTimeOffset?>(new DateTimeOffset(nextSync, now.Offset));
    }

    /// <inheritdoc />
    public async Task<List<DataConflict>> DetectConflictsAsync()
    {
        var allConflicts = new List<DataConflict>();

        foreach (var source in _configuration.EnabledSources)
        {
            var conflicts = await DetectConflictsForSourceAsync(source);
            allConflicts.AddRange(conflicts);
        }

        // Cross-source conflicts
        allConflicts.AddRange(await DetectCrossSourceConflictsAsync());

        // Update unresolved conflicts list
        foreach (var conflict in allConflicts.Where(c => !c.IsResolved))
        {
            if (!_unresolvedConflicts.Any(uc => uc.Id == conflict.Id))
            {
                _unresolvedConflicts.Add(conflict);
                ConflictDetected?.Invoke(this, new DataConflictEventArgs { Conflict = conflict });
            }
        }

        return allConflicts;
    }

    /// <inheritdoc />
    public Task<List<DataConflict>> GetUnresolvedConflictsAsync()
    {
        return Task.FromResult(_unresolvedConflicts.Where(c => !c.IsResolved).ToList());
    }

    /// <inheritdoc />
    public async Task ResolveConflictAsync(string conflictId, string resolution, string resolvedBy, string resolvedByName)
    {
        var conflict = _unresolvedConflicts.FirstOrDefault(c => c.Id == conflictId);
        if (conflict == null)
        {
            _logger.LogWarning("Conflict {ConflictId} not found", conflictId);
            return;
        }

        // Update conflict
        var index = _unresolvedConflicts.FindIndex(c => c.Id == conflictId);
        if (index >= 0)
        {
            _unresolvedConflicts[index] = conflict with
            {
                IsResolved = true,
                Resolution = resolution,
                ResolvedBy = resolvedBy,
                ResolvedAt = DateTimeOffset.UtcNow
            };
        }

        await _auditService.LogApplicationConflictResolvedAsync(
            conflict.ApplicationId, conflict.ApplicationName,
            conflict.Type.ToString(), resolution, resolvedBy, resolvedByName);
    }

    /// <inheritdoc />
    public Task<bool> CancelSyncJobAsync(string jobId)
    {
        if (_runningJobs.TryGetValue(jobId, out var cts))
        {
            cts.Cancel();
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public async Task RecordManualImportAsync(
        DataSourceType dataSource,
        int recordsProcessed,
        int recordsCreated,
        int recordsUpdated,
        bool success,
        string? errorMessage = null,
        string? triggeredBy = null,
        string? description = null)
    {
        var startTime = DateTimeOffset.UtcNow;
        var jobId = Guid.NewGuid().ToString();

        var job = new SyncJobInfo
        {
            Id = jobId,
            DataSource = dataSource,
            StartTime = startTime,
            EndTime = startTime, // Manual imports are instant from tracking perspective
            Status = success ? SyncJobStatus.Completed : SyncJobStatus.Failed,
            RecordsProcessed = recordsProcessed,
            RecordsCreated = recordsCreated,
            RecordsUpdated = recordsUpdated,
            ErrorCount = success ? 0 : 1,
            ErrorMessage = errorMessage,
            TriggeredBy = triggeredBy ?? "Manual Import"
        };

        // Add to in-memory history
        await EnsureJobHistoryLoadedAsync();
        _syncJobHistory.Insert(0, job);

        // Persist to database
        await PersistJobAsync(job);

        // Log audit event
        if (success)
        {
            _logger.LogInformation(
                "Manual import recorded for {DataSource}: {RecordsProcessed} processed, {RecordsCreated} created, {RecordsUpdated} updated. {Description}",
                dataSource, recordsProcessed, recordsCreated, recordsUpdated, description ?? "");
            await _auditService.LogSyncCompletedAsync(
                $"{dataSource} (Manual Import)",
                jobId,
                recordsProcessed,
                recordsCreated,
                recordsUpdated,
                TimeSpan.Zero);
        }
        else
        {
            _logger.LogWarning(
                "Manual import failed for {DataSource}: {ErrorMessage}. {Description}",
                dataSource, errorMessage, description ?? "");
            await _auditService.LogSyncFailedAsync(
                $"{dataSource} (Manual Import)",
                jobId,
                errorMessage ?? "Unknown error",
                TimeSpan.Zero);
        }
    }

    /// <inheritdoc />
    public SyncConfiguration GetConfiguration() => _configuration;

    /// <inheritdoc />
    public Task SetConfigurationAsync(SyncConfiguration configuration)
    {
        _configuration = configuration;
        return Task.CompletedTask;
    }

    #region Private Helpers

    private static async Task<(DataSourceType Type, ConnectionTestResult Result)> TestDataSourceAsync(
        DataSourceType type, Func<Task<ConnectionTestResult>> testFunc)
    {
        try
        {
            var result = await testFunc();
            return (type, result);
        }
        catch (Exception ex)
        {
            return (type, ConnectionTestResult.Failed($"Error: {ex.Message}"));
        }
    }

    private async Task<DataSyncResult> SyncAzureDevOpsAsync(string jobId, CancellationToken cancellationToken)
    {
        var startTime = DateTimeOffset.UtcNow;
        var errors = new List<SyncError>();
        var syncedRepos = new List<SyncedRepository>();
        var stepResults = new List<SyncStepResult>();

        // Step counters for detailed reporting
        int techStackSuccess = 0, techStackFail = 0, techStackSkip = 0;
        int commitsSuccess = 0, commitsFail = 0, commitsSkip = 0;
        int packagesSuccess = 0, packagesFail = 0, packagesSkip = 0;
        int readmeSuccess = 0, readmeFail = 0, readmeSkip = 0;
        int pipelineSuccess = 0, pipelineFail = 0, pipelineSkip = 0;
        int securitySuccess = 0, securityFail = 0, securitySkip = 0;
        int incrementalSkip = 0;

        _logger.LogInformation("Starting Azure DevOps sync...");

        // Helper to report progress
        void ReportProgress(string phase, int processed, int total, string? currentItem = null, string? message = null)
        {
            var progressArgs = new SyncProgressEventArgs
            {
                JobId = jobId,
                DataSource = DataSourceType.AzureDevOps,
                Phase = phase,
                ProcessedItems = processed,
                TotalItems = total,
                CurrentItem = currentItem,
                Message = message
            };
            SyncProgressUpdated?.Invoke(this, progressArgs);
            _syncStateService.UpdateProgress(progressArgs);
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Step 1: Get all repositories from Azure DevOps
            var fetchStart = DateTimeOffset.UtcNow;
            _logger.LogInformation("Fetching repositories from Azure DevOps...");
            ReportProgress("Fetching repositories", 0, 0, message: "Retrieving repository list from Azure DevOps...");

            var reposResult = await _azureDevOpsService.GetRepositoriesAsync();

            stepResults.Add(new SyncStepResult
            {
                StepName = "Fetch Repository List",
                Success = reposResult.Success,
                SuccessCount = reposResult.Success ? (reposResult.Data?.Count ?? 0) : 0,
                FailCount = reposResult.Success ? 0 : 1,
                ErrorMessage = reposResult.ErrorMessage,
                Duration = DateTimeOffset.UtcNow - fetchStart
            });

            if (!reposResult.Success)
            {
                _logger.LogError("Failed to get repositories: {Error}", reposResult.ErrorMessage);
                return new DataSyncResult
                {
                    Success = false,
                    DataSource = DataSourceType.AzureDevOps,
                    StartTime = startTime,
                    EndTime = DateTimeOffset.UtcNow,
                    ErrorMessage = reposResult.ErrorMessage ?? "Failed to get repositories",
                    StepResults = stepResults
                };
            }

            var repos = reposResult.Data ?? [];
            _logger.LogInformation("Found {Count} repositories in Azure DevOps", repos.Count);

            // Load existing synced repositories for incremental sync
            var existingRepos = await _mockDataService.GetSyncedRepositoriesAsync();
            var existingRepoDict = existingRepos.ToDictionary(r => r.Id, r => r);
            var today = DateTimeOffset.UtcNow.Date;

            _logger.LogInformation("Loaded {Count} existing synced repositories for incremental comparison", existingRepos.Count);

            // Apply dev mode repo limit if configured
            var reposToProcess = repos;
            if (_configuration.DevModeRepoLimit > 0 && repos.Count > _configuration.DevModeRepoLimit)
            {
                reposToProcess = repos.Take(_configuration.DevModeRepoLimit).ToList();
                _logger.LogWarning("Dev mode: limiting sync to {Limit} of {Total} repositories", _configuration.DevModeRepoLimit, repos.Count);
            }

            ReportProgress("Processing repositories", 0, reposToProcess.Count, message: $"Processing {reposToProcess.Count} of {repos.Count} repositories");

            // Step 2: Process each repository
            var processStart = DateTimeOffset.UtcNow;
            var processedCount = 0;
            foreach (var repo in reposToProcess)
            {
                cancellationToken.ThrowIfCancellationRequested();
                processedCount++;
                _logger.LogInformation("Processing repository: {RepoName} ({Current}/{Total})", repo.Name, processedCount, reposToProcess.Count);
                ReportProgress("Processing repositories", processedCount, repos.Count, repo.Name);

                // Check for incremental sync - if we have recent data, only fetch security data
                var existingRepo = existingRepoDict.GetValueOrDefault(repo.Id);
                var hasRecentData = existingRepo != null && existingRepo.SyncedAt.Date == today;
                var needsSecurityOnly = hasRecentData && !existingRepo!.AdvancedSecurityEnabled && existingRepo.LastSecurityScanDate == null;

                var syncedRepo = new SyncedRepository
                {
                    Id = repo.Id,
                    Name = repo.Name,
                    Url = repo.Url,
                    CloneUrl = repo.CloneUrl,
                    DefaultBranch = repo.DefaultBranch,
                    ProjectName = repo.ProjectName,
                    SizeBytes = repo.SizeBytes,
                    IsDisabled = repo.IsDisabled,
                    SyncedAt = DateTimeOffset.UtcNow,
                    SyncedBy = "DataSyncOrchestrator"
                };

                // If we have recent data, carry it forward and only fetch missing parts (security)
                if (hasRecentData)
                {
                    syncedRepo = existingRepo! with
                    {
                        SyncedAt = DateTimeOffset.UtcNow,
                        SyncedBy = "DataSyncOrchestrator"
                    };

                    if (!needsSecurityOnly)
                    {
                        // All data is recent including security, just update timestamps
                        _logger.LogDebug("Skipping full sync for {RepoName} - synced today, security data present", repo.Name);
                        incrementalSkip++;
                        techStackSkip++;
                        commitsSkip++;
                        packagesSkip++;
                        readmeSkip++;
                        pipelineSkip++;
                        securitySkip++;
                        syncedRepos.Add(syncedRepo);
                        continue;
                    }

                    _logger.LogInformation("Incremental sync for {RepoName} - only fetching security data (other data synced today)", repo.Name);
                    incrementalSkip++;
                    techStackSkip++;
                    commitsSkip++;
                    packagesSkip++;
                    readmeSkip++;
                    pipelineSkip++;
                    // Don't skip security - fetch it below
                }

                // Skip disabled repos - they may not be accessible
                if (repo.IsDisabled)
                {
                    _logger.LogInformation("Skipping disabled repo {RepoName} for tech stack/commits/packages/security detection", repo.Name);
                    techStackSkip++;
                    commitsSkip++;
                    packagesSkip++;
                    readmeSkip++;
                    pipelineSkip++;
                    securitySkip++;
                    syncedRepos.Add(syncedRepo);
                    continue;
                }

                // If incremental sync (security only), skip to security section
                var skipToSecurity = hasRecentData && needsSecurityOnly;

                // Tech stack detection
                if (!skipToSecurity)
                try
                {
                    _logger.LogDebug("Detecting tech stack for {RepoName} (DefaultBranch: {DefaultBranch})", repo.Name, repo.DefaultBranch ?? "(null)");
                    var techStackResult = await _azureDevOpsService.DetectTechStackAsync(repo.Id, repo.DefaultBranch);
                    if (techStackResult.Success && techStackResult.Data != null)
                    {
                        syncedRepo = syncedRepo with
                        {
                            PrimaryStack = techStackResult.Data.PrimaryStack.ToString(),
                            Frameworks = techStackResult.Data.Frameworks,
                            Languages = techStackResult.Data.Languages,
                            TargetFramework = techStackResult.Data.TargetFramework,
                            DetectedPattern = techStackResult.Data.DetectedPattern
                        };
                        techStackSuccess++;
                        _logger.LogDebug("Tech stack for {RepoName}: {Stack}, Frameworks: {Frameworks}",
                            repo.Name, techStackResult.Data.PrimaryStack, string.Join(", ", techStackResult.Data.Frameworks));
                    }
                    else
                    {
                        techStackSkip++;
                        _logger.LogInformation("No tech stack detected for {RepoName}: {Error}", repo.Name, techStackResult.ErrorMessage ?? "Unknown error");
                    }
                }
                catch (Exception ex)
                {
                    techStackFail++;
                    _logger.LogWarning(ex, "Tech stack detection failed for {RepoName}", repo.Name);
                }

                // Commit history
                if (!skipToSecurity)
                try
                {
                    var commitsResult = await _azureDevOpsService.GetCommitHistoryAsync(repo.Id, repo.DefaultBranch, 365);
                    if (commitsResult.Success && commitsResult.Data != null)
                    {
                        syncedRepo = syncedRepo with
                        {
                            TotalCommits = commitsResult.Data.TotalCommits,
                            LastCommitDate = commitsResult.Data.LastCommitDate,
                            Contributors = commitsResult.Data.Contributors
                        };
                        commitsSuccess++;
                    }
                    else
                    {
                        commitsSkip++;
                    }
                }
                catch (Exception ex)
                {
                    commitsFail++;
                    _logger.LogWarning(ex, "Commit history failed for {RepoName}", repo.Name);
                }

                // Packages
                if (!skipToSecurity)
                try
                {
                    var packagesResult = await _azureDevOpsService.GetPackagesAsync(repo.Id, repo.DefaultBranch);
                    if (packagesResult.Success && packagesResult.Data != null)
                    {
                        syncedRepo = syncedRepo with
                        {
                            NuGetPackageCount = packagesResult.Data.Count(p => p.PackageManager == "NuGet"),
                            NpmPackageCount = packagesResult.Data.Count(p => p.PackageManager == "npm"),
                            Packages = packagesResult.Data.Select(p => new SyncedPackageReference
                            {
                                Name = p.Name,
                                Version = p.Version,
                                PackageManager = p.PackageManager,
                                SourceFile = p.SourceFile,
                                IsDevelopmentDependency = p.IsDevelopmentDependency
                            }).ToList()
                        };
                        packagesSuccess++;
                    }
                    else
                    {
                        packagesSkip++;
                    }
                }
                catch (Exception ex)
                {
                    packagesFail++;
                    _logger.LogWarning(ex, "Package detection failed for {RepoName}", repo.Name);
                }

                // README status
                if (!skipToSecurity)
                try
                {
                    var readmeResult = await _azureDevOpsService.GetReadmeStatusAsync(repo.Id, repo.DefaultBranch);
                    if (readmeResult.Success && readmeResult.Data != null)
                    {
                        syncedRepo = syncedRepo with
                        {
                            HasReadme = readmeResult.Data.Exists,
                            ReadmeQualityScore = readmeResult.Data.QualityScore
                        };
                        readmeSuccess++;
                    }
                    else
                    {
                        readmeSkip++;
                    }
                }
                catch (Exception ex)
                {
                    readmeFail++;
                    _logger.LogWarning(ex, "README check failed for {RepoName}", repo.Name);
                }

                // Pipeline/build status
                if (!skipToSecurity)
                try
                {
                    var pipelineResult = await _azureDevOpsService.GetPipelineStatusAsync(repo.Id);
                    if (pipelineResult.Success && pipelineResult.Data != null)
                    {
                        syncedRepo = syncedRepo with
                        {
                            LastBuildStatus = pipelineResult.Data.Status.ToString(),
                            LastBuildResult = pipelineResult.Data.Result.ToString(),
                            LastBuildDate = pipelineResult.Data.FinishTime ?? pipelineResult.Data.StartTime
                        };
                        pipelineSuccess++;
                    }
                    else
                    {
                        pipelineSkip++;
                    }
                }
                catch (Exception ex)
                {
                    pipelineFail++;
                    _logger.LogWarning(ex, "Pipeline check failed for {RepoName}", repo.Name);
                }

                // Security alerts (CodeQL/Advanced Security)
                try
                {
                    _logger.LogDebug("Fetching security alerts for {RepoName} (Project: {Project})", repo.Name, repo.ProjectName ?? "(null)");
                    var securityResult = await _azureDevOpsService.GetSecurityAlertsAsync(repo.Name, repo.ProjectName ?? "");

                    if (securityResult.Success && securityResult.Data != null)
                    {
                        syncedRepo = syncedRepo with
                        {
                            AdvancedSecurityEnabled = securityResult.Data.AdvancedSecurityEnabled,
                            LastSecurityScanDate = securityResult.Data.LastScanDate ?? DateTimeOffset.UtcNow,
                            OpenCriticalVulnerabilities = securityResult.Data.OpenCritical,
                            OpenHighVulnerabilities = securityResult.Data.OpenHigh,
                            OpenMediumVulnerabilities = securityResult.Data.OpenMedium,
                            OpenLowVulnerabilities = securityResult.Data.OpenLow,
                            ClosedCriticalVulnerabilities = securityResult.Data.ClosedCritical,
                            ClosedHighVulnerabilities = securityResult.Data.ClosedHigh,
                            ClosedMediumVulnerabilities = securityResult.Data.ClosedMedium,
                            ClosedLowVulnerabilities = securityResult.Data.ClosedLow,
                            ExposedSecretsCount = securityResult.Data.ExposedSecrets,
                            DependencyAlertCount = securityResult.Data.DependencyAlerts,
                            SecurityAlerts = securityResult.Data.Alerts,
                            SecretAlerts = securityResult.Data.SecretAlerts
                        };
                        securitySuccess++;
                        _logger.LogDebug("Security data for {RepoName}: AdvSec={Enabled}, {AlertCount} alerts ({Critical} crit, {High} high), {SecretCount} secrets",
                            repo.Name, securityResult.Data.AdvancedSecurityEnabled,
                            securityResult.Data.Alerts.Count, securityResult.Data.OpenCritical, securityResult.Data.OpenHigh,
                            securityResult.Data.SecretAlerts.Count);
                    }
                    else
                    {
                        securityFail++;
                        _logger.LogWarning("Security alerts failed for {RepoName}: {Error}",
                            repo.Name, securityResult.ErrorMessage ?? "Unknown error (Success=false)");
                    }
                }
                catch (Exception ex)
                {
                    securityFail++;
                    _logger.LogWarning(ex, "Security alert fetch exception for {RepoName}", repo.Name);
                }

                syncedRepos.Add(syncedRepo);
            }

            var processEnd = DateTimeOffset.UtcNow;

            // Add step results for each sync step
            stepResults.Add(new SyncStepResult
            {
                StepName = "Tech Stack Detection",
                Success = techStackFail == 0,
                SuccessCount = techStackSuccess,
                FailCount = techStackFail,
                SkipCount = techStackSkip,
                Duration = processEnd - processStart,
                Details = new Dictionary<string, object>
                {
                    ["DetectionRate"] = repos.Count > 0 ? $"{(techStackSuccess * 100.0 / repos.Count):F1}%" : "N/A"
                }
            });

            stepResults.Add(new SyncStepResult
            {
                StepName = "Commit History",
                Success = commitsFail == 0,
                SuccessCount = commitsSuccess,
                FailCount = commitsFail,
                SkipCount = commitsSkip,
                Duration = processEnd - processStart
            });

            stepResults.Add(new SyncStepResult
            {
                StepName = "Package Detection",
                Success = packagesFail == 0,
                SuccessCount = packagesSuccess,
                FailCount = packagesFail,
                SkipCount = packagesSkip,
                Duration = processEnd - processStart
            });

            stepResults.Add(new SyncStepResult
            {
                StepName = "README Check",
                Success = readmeFail == 0,
                SuccessCount = readmeSuccess,
                FailCount = readmeFail,
                SkipCount = readmeSkip,
                Duration = processEnd - processStart
            });

            stepResults.Add(new SyncStepResult
            {
                StepName = "Pipeline Status",
                Success = pipelineFail == 0,
                SuccessCount = pipelineSuccess,
                FailCount = pipelineFail,
                SkipCount = pipelineSkip,
                Duration = processEnd - processStart
            });

            stepResults.Add(new SyncStepResult
            {
                StepName = "Security Alerts (CodeQL)",
                Success = securityFail == 0,
                SuccessCount = securitySuccess,
                FailCount = securityFail,
                SkipCount = 0, // Security is always attempted for non-disabled repos
                Duration = processEnd - processStart
            });

            // Step 3: Store all synced repositories
            var storeStart = DateTimeOffset.UtcNow;
            _logger.LogInformation("Storing {Count} synced repositories...", syncedRepos.Count);
            await _mockDataService.StoreSyncedRepositoriesAsync(syncedRepos);

            stepResults.Add(new SyncStepResult
            {
                StepName = "Store to Database",
                Success = true,
                SuccessCount = syncedRepos.Count,
                Duration = DateTimeOffset.UtcNow - storeStart
            });

            // Step 4: Refresh application data from synced repositories
            var refreshStart = DateTimeOffset.UtcNow;
            _logger.LogInformation("Refreshing application data from synced repositories...");
            ReportProgress("Refreshing applications", syncedRepos.Count, syncedRepos.Count, message: "Updating applications with synced data...");

            var (linkedCount, updatedCount) = await _mockDataService.RefreshApplicationsFromSyncedDataAsync();

            stepResults.Add(new SyncStepResult
            {
                StepName = "Refresh Application Data",
                Success = true,
                SuccessCount = updatedCount,
                Duration = DateTimeOffset.UtcNow - refreshStart,
                Details = new Dictionary<string, object>
                {
                    ["RepositoriesLinked"] = linkedCount,
                    ["ApplicationsUpdated"] = updatedCount
                }
            });

            _logger.LogInformation("Application refresh complete: {LinkedCount} repos linked, {UpdatedCount} apps updated",
                linkedCount, updatedCount);

            _logger.LogInformation("Azure DevOps sync completed. Processed {Count} repositories (incremental skipped: {IncrementalSkip}). " +
                "Tech Stack: {TechStackSuccess}/{Total}, Commits: {CommitsSuccess}/{Total}, " +
                "Packages: {PackagesSuccess}/{Total}, README: {ReadmeSuccess}/{Total}, Pipeline: {PipelineSuccess}/{Total}, " +
                "Security: {SecuritySuccess}/{Total}",
                syncedRepos.Count, incrementalSkip, techStackSuccess, repos.Count, commitsSuccess, repos.Count,
                packagesSuccess, repos.Count, readmeSuccess, repos.Count, pipelineSuccess, repos.Count,
                securitySuccess, repos.Count);

            return new DataSyncResult
            {
                Success = true,
                DataSource = DataSourceType.AzureDevOps,
                RecordsProcessed = syncedRepos.Count,
                RecordsCreated = syncedRepos.Count,
                Errors = errors,
                StartTime = startTime,
                EndTime = DateTimeOffset.UtcNow,
                StepResults = stepResults
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Azure DevOps sync was cancelled");
            return new DataSyncResult
            {
                Success = false,
                DataSource = DataSourceType.AzureDevOps,
                StartTime = startTime,
                EndTime = DateTimeOffset.UtcNow,
                ErrorMessage = "Sync was cancelled",
                StepResults = stepResults
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure DevOps sync failed");
            return new DataSyncResult
            {
                Success = false,
                DataSource = DataSourceType.AzureDevOps,
                StartTime = startTime,
                EndTime = DateTimeOffset.UtcNow,
                ErrorMessage = ex.Message,
                StepResults = stepResults
            };
        }
    }

    private async Task<DataSyncResult> SyncSharePointAsync(string jobId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SyncProgressUpdated?.Invoke(this, new SyncProgressEventArgs
        {
            JobId = jobId,
            DataSource = DataSourceType.SharePoint,
            Phase = "Syncing documentation",
            ProcessedItems = 0,
            TotalItems = 0,
            Message = "Synchronizing documentation status..."
        });
        return await _sharePointService.SyncDocumentationStatusAsync();
    }

    private async Task<DataSyncResult> SyncServiceNowAsync(string jobId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var startTime = DateTimeOffset.UtcNow;

        SyncProgressUpdated?.Invoke(this, new SyncProgressEventArgs
        {
            JobId = jobId,
            DataSource = DataSourceType.ServiceNow,
            Phase = "Loading CSV",
            ProcessedItems = 0,
            TotalItems = 0,
            Message = "Loading ServiceNow CSV files..."
        });

        // Get CSV directory from ServiceNow instance configuration
        var csvDir = await _secureStorage.GetSecretAsync(SecretKeys.ServiceNowInstance);

        if (string.IsNullOrEmpty(csvDir))
        {
            return DataSyncResult.Failed(
                DataSourceType.ServiceNow, startTime, "ServiceNow CSV directory not configured");
        }

        // Default filenames for CSV exports
        const string appsCsv = "applications.csv";
        const string rolesCsv = "role_assignments.csv";

        var appsPath = Path.Combine(csvDir, appsCsv);
        var rolesPath = Path.Combine(csvDir, rolesCsv);

        // Only include roles path if file exists
        var actualRolesPath = File.Exists(rolesPath) ? rolesPath : null;

        return await _serviceNowService.SyncFromCsvAsync(appsPath, actualRolesPath);
    }

    private async Task<DataSyncResult> SyncIisDatabaseAsync(string jobId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        SyncProgressUpdated?.Invoke(this, new SyncProgressEventArgs
        {
            JobId = jobId,
            DataSource = DataSourceType.IisDatabase,
            Phase = "Querying database",
            ProcessedItems = 0,
            TotalItems = 0,
            Message = "Querying IIS database for usage data..."
        });
        return await _iisDatabaseService.SyncUsageDataAsync();
    }

    private async Task<List<DataConflict>> DetectConflictsForSourceAsync(DataSourceType source)
    {
        var conflicts = new List<DataConflict>();

        switch (source)
        {
            case DataSourceType.ServiceNow:
                // Validate role assignments
                var apps = await _mockDataService.GetApplicationsAsync();
                foreach (var app in apps)
                {
                    // Check for missing owners
                    var hasOwner = app.RoleAssignments.Any(r => r.Role == ApplicationRole.Owner);
                    if (!hasOwner)
                    {
                        conflicts.Add(new DataConflict
                        {
                            Id = Guid.NewGuid().ToString(),
                            ApplicationId = app.Id,
                            ApplicationName = app.Name,
                            Type = ConflictType.RoleConflict,
                            Description = $"Application '{app.Name}' has no owner assigned",
                            SourceA = "ServiceNow",
                            DetectedAt = DateTimeOffset.UtcNow
                        });
                    }
                }
                break;

            case DataSourceType.AzureDevOps:
                // Check for invalid repository URLs
                foreach (var app in await _mockDataService.GetApplicationsAsync())
                {
                    if (!string.IsNullOrEmpty(app.RepositoryUrl) &&
                        !Uri.TryCreate(app.RepositoryUrl, UriKind.Absolute, out _))
                    {
                        conflicts.Add(new DataConflict
                        {
                            Id = Guid.NewGuid().ToString(),
                            ApplicationId = app.Id,
                            ApplicationName = app.Name,
                            Type = ConflictType.InvalidRepository,
                            Description = $"Application '{app.Name}' has an invalid repository URL",
                            SourceA = "ServiceNow",
                            SourceB = "Azure DevOps",
                            ValueA = app.RepositoryUrl,
                            DetectedAt = DateTimeOffset.UtcNow
                        });
                    }
                }
                break;
        }

        return conflicts;
    }

    private async Task<List<DataConflict>> DetectCrossSourceConflictsAsync()
    {
        var conflicts = new List<DataConflict>();

        var apps = await _mockDataService.GetApplicationsAsync();

        // Check ServiceNow name vs SharePoint folder name mismatches
        // This would require SharePoint folder data to be available

        // Check for duplicate repository assignments
        var repoUrls = apps
            .Where(a => !string.IsNullOrEmpty(a.RepositoryUrl))
            .GroupBy(a => a.RepositoryUrl, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1);

        foreach (var group in repoUrls)
        {
            var appNames = string.Join(", ", group.Select(a => a.Name));
            var firstApp = group.First();
            conflicts.Add(new DataConflict
            {
                Id = Guid.NewGuid().ToString(),
                ApplicationId = firstApp.Id,
                ApplicationName = firstApp.Name,
                Type = ConflictType.DuplicateRepository,
                Description = $"Repository URL is assigned to multiple applications: {appNames}",
                SourceA = "ServiceNow",
                SourceB = "Azure DevOps",
                ValueA = group.Key!,
                DetectedAt = DateTimeOffset.UtcNow
            });
        }

        return conflicts;
    }

    #endregion
}
