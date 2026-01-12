using LifecycleDashboard.Models;
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
    private readonly ILogger<DataSyncOrchestrator> _logger;

    private SyncConfiguration _configuration = new();
    private readonly List<SyncJobInfo> _syncJobHistory = [];
    private readonly List<DataConflict> _unresolvedConflicts = [];
    private readonly Dictionary<string, CancellationTokenSource> _runningJobs = [];

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
        ILogger<DataSyncOrchestrator> logger)
    {
        _azureDevOpsService = azureDevOpsService;
        _sharePointService = sharePointService;
        _serviceNowService = serviceNowService;
        _iisDatabaseService = iisDatabaseService;
        _mockDataService = mockDataService;
        _secureStorage = secureStorage;
        _auditService = auditService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<DataSourceStatus>> GetDataSourceStatusesAsync()
    {
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
    public Task<List<SyncJobInfo>> GetSyncJobHistoryAsync(int limit = 50)
    {
        var jobs = _syncJobHistory
            .OrderByDescending(j => j.StartTime)
            .Take(limit)
            .ToList();

        return Task.FromResult(jobs);
    }

    /// <inheritdoc />
    public Task<SyncJobInfo?> GetSyncJobAsync(string jobId)
    {
        var job = _syncJobHistory.FirstOrDefault(j => j.Id == jobId);
        return Task.FromResult(job);
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

        // Raise started event
        SyncJobStarted?.Invoke(this, new SyncJobEventArgs { Job = job });

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

            // Update in history
            var index = _syncJobHistory.FindIndex(j => j.Id == jobId);
            if (index >= 0)
            {
                _syncJobHistory[index] = job;
            }

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

            // Raise completed/failed event
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

            await _auditService.LogSyncFailedAsync(dataSource.ToString(), jobId,
                "Operation was cancelled", endTime - startTime);

            return DataSyncResult.Failed(dataSource, startTime, "Sync operation was cancelled");
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

            await _auditService.LogSyncFailedAsync(dataSource.ToString(), jobId, ex.Message, endTime - startTime);
            SyncJobFailed?.Invoke(this, new SyncJobEventArgs { Job = job });

            return DataSyncResult.Failed(dataSource, startTime, ex.Message);
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

        _logger.LogInformation("Starting Azure DevOps sync...");

        // Helper to report progress
        void ReportProgress(string phase, int processed, int total, string? currentItem = null, string? message = null)
        {
            SyncProgressUpdated?.Invoke(this, new SyncProgressEventArgs
            {
                JobId = jobId,
                DataSource = DataSourceType.AzureDevOps,
                Phase = phase,
                ProcessedItems = processed,
                TotalItems = total,
                CurrentItem = currentItem,
                Message = message
            });
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Step 1: Get all repositories from Azure DevOps
            _logger.LogInformation("Fetching repositories from Azure DevOps...");
            ReportProgress("Fetching repositories", 0, 0, message: "Retrieving repository list from Azure DevOps...");

            var reposResult = await _azureDevOpsService.GetRepositoriesAsync();

            if (!reposResult.Success)
            {
                _logger.LogError("Failed to get repositories: {Error}", reposResult.ErrorMessage);
                return DataSyncResult.Failed(DataSourceType.AzureDevOps, startTime,
                    reposResult.ErrorMessage ?? "Failed to get repositories");
            }

            var repos = reposResult.Data ?? [];
            _logger.LogInformation("Found {Count} repositories in Azure DevOps", repos.Count);
            ReportProgress("Processing repositories", 0, repos.Count, message: $"Found {repos.Count} repositories");

            // Step 2: Convert each repo to a SyncedRepository and store it
            var processedCount = 0;
            foreach (var repo in repos)
            {
                cancellationToken.ThrowIfCancellationRequested();
                processedCount++;
                _logger.LogInformation("Processing repository: {RepoName} ({Current}/{Total})", repo.Name, processedCount, repos.Count);
                ReportProgress("Processing repositories", processedCount, repos.Count, repo.Name);

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

                // Try to get additional details (tech stack, commits, etc.) - but don't fail the whole sync if one repo fails
                try
                {
                    // Get tech stack
                    var techStackResult = await _azureDevOpsService.DetectTechStackAsync(repo.Id);
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
                    }

                    // Get commit history
                    var commitsResult = await _azureDevOpsService.GetCommitHistoryAsync(repo.Id, 365);
                    if (commitsResult.Success && commitsResult.Data != null)
                    {
                        syncedRepo = syncedRepo with
                        {
                            TotalCommits = commitsResult.Data.TotalCommits,
                            LastCommitDate = commitsResult.Data.LastCommitDate,
                            Contributors = commitsResult.Data.Contributors
                        };
                    }

                    // Get packages
                    var packagesResult = await _azureDevOpsService.GetPackagesAsync(repo.Id);
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
                    }

                    // Get README status
                    var readmeResult = await _azureDevOpsService.GetReadmeStatusAsync(repo.Id);
                    if (readmeResult.Success && readmeResult.Data != null)
                    {
                        syncedRepo = syncedRepo with
                        {
                            HasReadme = readmeResult.Data.Exists,
                            ReadmeQualityScore = readmeResult.Data.QualityScore
                        };
                    }

                    // Get pipeline/build status
                    var pipelineResult = await _azureDevOpsService.GetPipelineStatusAsync(repo.Id);
                    if (pipelineResult.Success && pipelineResult.Data != null)
                    {
                        syncedRepo = syncedRepo with
                        {
                            LastBuildStatus = pipelineResult.Data.Status.ToString(),
                            LastBuildResult = pipelineResult.Data.Result.ToString(),
                            LastBuildDate = pipelineResult.Data.FinishTime ?? pipelineResult.Data.StartTime
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get additional details for repo {RepoName}, continuing with basic info", repo.Name);
                    errors.Add(new SyncError { Message = $"Partial sync for {repo.Name}: {ex.Message}" });
                }

                syncedRepos.Add(syncedRepo);
            }

            // Step 3: Store all synced repositories
            _logger.LogInformation("Storing {Count} synced repositories...", syncedRepos.Count);
            await _mockDataService.StoreSyncedRepositoriesAsync(syncedRepos);

            _logger.LogInformation("Azure DevOps sync completed. Processed {Count} repositories.", syncedRepos.Count);

            return new DataSyncResult
            {
                Success = true,
                DataSource = DataSourceType.AzureDevOps,
                RecordsProcessed = syncedRepos.Count,
                RecordsCreated = syncedRepos.Count,
                Errors = errors,
                StartTime = startTime,
                EndTime = DateTimeOffset.UtcNow
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Azure DevOps sync was cancelled");
            return DataSyncResult.Failed(DataSourceType.AzureDevOps, startTime, "Sync was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure DevOps sync failed");
            return DataSyncResult.Failed(DataSourceType.AzureDevOps, startTime, ex.Message);
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
