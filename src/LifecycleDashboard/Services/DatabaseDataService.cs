using System.Text.Json;
using LifecycleDashboard.Data;
using LifecycleDashboard.Data.Entities;
using LifecycleDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace LifecycleDashboard.Services;

/// <summary>
/// Database-backed implementation of IMockDataService using Entity Framework Core.
/// </summary>
public class DatabaseDataService : IMockDataService
{
    private readonly IDbContextFactory<LifecycleDbContext> _contextFactory;
    private readonly ILogger<DatabaseDataService> _logger;

    // In-memory caches for configuration data (not persisted to DB yet)
    private ServiceNowColumnMapping? _serviceNowColumnMapping;
    private AppNameMappingConfig? _appNameMappingConfig;
    private TaskSchedulingConfig _taskSchedulingConfig = new();
    private SystemSettings _systemSettings = new();
    private readonly List<DataSourceConfig> _dataSourceConfigs = [];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public DatabaseDataService(
        IDbContextFactory<LifecycleDbContext> contextFactory,
        ILogger<DatabaseDataService> logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;

        // Initialize default data source configs
        InitializeDataSourceConfigs();
    }

    private void InitializeDataSourceConfigs()
    {
        _dataSourceConfigs.AddRange([
            new DataSourceConfig
            {
                Id = "azure-devops",
                Name = "Azure DevOps",
                Type = DataSourceType.AzureDevOps,
                IsEnabled = false,
                IsConnected = false
            },
            new DataSourceConfig
            {
                Id = "sharepoint",
                Name = "SharePoint",
                Type = DataSourceType.SharePoint,
                IsEnabled = false,
                IsConnected = false
            },
            new DataSourceConfig
            {
                Id = "servicenow",
                Name = "ServiceNow",
                Type = DataSourceType.ServiceNow,
                IsEnabled = false,
                IsConnected = false
            },
            new DataSourceConfig
            {
                Id = "iis-database",
                Name = "IIS Database",
                Type = DataSourceType.IisDatabase,
                IsEnabled = false,
                IsConnected = false
            }
        ]);
    }

    #region IMockDataService Properties

    public bool IsMockDataEnabled => _systemSettings.MockDataEnabled;

    public event EventHandler<bool>? MockDataModeChanged;

    public Task SetMockDataEnabledAsync(bool enabled)
    {
        _systemSettings = _systemSettings with { MockDataEnabled = enabled };
        MockDataModeChanged?.Invoke(this, enabled);
        return Task.CompletedTask;
    }

    #endregion

    #region Applications

    public async Task<IReadOnlyList<Application>> GetApplicationsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Filter based on mock mode setting
        var query = context.Applications.AsNoTracking();
        if (!IsMockDataEnabled)
        {
            // Live mode: exclude mock data
            query = query.Where(a => !a.IsMockData);
        }

        var entities = await query.ToListAsync();
        return entities.Select(e => e.ToModel()).ToList();
    }

    public async Task<Application?> GetApplicationAsync(string id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.Applications.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
        return entity?.ToModel();
    }

    public async Task<IReadOnlyList<Application>> GetApplicationsByHealthAsync(HealthCategory category)
    {
        var apps = await GetApplicationsAsync();
        return apps.Where(a => a.HealthCategory == category).ToList();
    }

    public async Task<IReadOnlyList<Application>> GetApplicationsByFrameworkAsync(string frameworkVersionId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Get the framework version to find its TFM
        var frameworkVersion = await context.FrameworkVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == frameworkVersionId);

        if (frameworkVersion == null)
            return [];

        // Find repositories that use this framework
        var matchingRepos = await context.SyncedRepositories
            .AsNoTracking()
            .Where(r => r.TargetFramework == frameworkVersion.TargetFrameworkMoniker)
            .ToListAsync();

        if (matchingRepos.Count == 0)
            return [];

        // Get applications that are linked to these repositories
        var linkedAppIds = matchingRepos
            .Where(r => !string.IsNullOrEmpty(r.LinkedApplicationId))
            .Select(r => r.LinkedApplicationId!)
            .Distinct()
            .ToList();

        // Also get applications by matching repo names to ServiceNow apps
        var repoNames = matchingRepos.Select(r => r.Name).ToList();

        var matchingApps = await context.ImportedServiceNowApplications
            .AsNoTracking()
            .Where(app => linkedAppIds.Contains(app.Id) ||
                          repoNames.Contains(app.Name) ||
                          repoNames.Contains(app.LinkedRepositoryName!))
            .ToListAsync();

        // Convert to Application models (basic conversion for framework page display)
        return matchingApps.Select(app => new Application
        {
            Id = app.Id,
            Name = app.Name,
            Capability = app.Capability ?? "Unknown",
            Description = app.Description,
            ShortDescription = app.ShortDescription,
            HealthScore = 50 // Default - HealthCategory is computed from this
        }).ToList();
    }

    public async Task<IReadOnlyList<Application>> GetApplicationsForUserAsync(string userId)
    {
        var apps = await GetApplicationsAsync();
        return apps.Where(a => a.RoleAssignments.Any(r => r.UserId == userId)).ToList();
    }

    #endregion

    #region Tasks

    public async Task<IReadOnlyList<LifecycleTask>> GetTasksForUserAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entities = await context.Tasks.AsNoTracking()
            .Where(t => t.AssigneeId == userId)
            .ToListAsync();
        return entities.Select(e => e.ToModel()).ToList();
    }

    public async Task<IReadOnlyList<LifecycleTask>> GetTasksForApplicationAsync(string applicationId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entities = await context.Tasks.AsNoTracking()
            .Where(t => t.ApplicationId == applicationId)
            .ToListAsync();
        return entities.Select(e => e.ToModel()).ToList();
    }

    public async Task<IReadOnlyList<LifecycleTask>> GetOverdueTasksAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var now = DateTimeOffset.UtcNow;
        var entities = await context.Tasks.AsNoTracking()
            .Where(t => t.DueDate < now && t.Status != Models.TaskStatus.Completed)
            .ToListAsync();
        return entities.Select(e => e.ToModel()).ToList();
    }

    public async Task<IReadOnlyList<LifecycleTask>> GetAllTasksAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entities = await context.Tasks.AsNoTracking().ToListAsync();
        return entities.Select(e => e.ToModel()).ToList();
    }

    public async Task<LifecycleTask?> GetTaskAsync(string taskId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == taskId);
        return entity?.ToModel();
    }

    public async Task<LifecycleTask> CreateTaskAsync(LifecycleTask task)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = task.ToEntity();
        context.Tasks.Add(entity);
        await context.SaveChangesAsync();
        return entity.ToModel();
    }

    public async Task DeleteTaskAsync(string taskId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.Tasks.FindAsync(taskId);
        if (entity != null)
        {
            context.Tasks.Remove(entity);
            await context.SaveChangesAsync();
        }
    }

    public async Task<LifecycleTask> UpdateTaskStatusAsync(string taskId, Models.TaskStatus newStatus, string performedByUserId, string performedByName, string? notes = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.Tasks.FindAsync(taskId)
            ?? throw new InvalidOperationException($"Task {taskId} not found");

        var task = entity.ToModel();
        var updatedHistory = task.History.ToList();
        updatedHistory.Add(new TaskHistoryEntry
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            Action = $"Status changed to {newStatus}",
            PerformedBy = performedByName,
            PerformedById = performedByUserId,
            Notes = notes
        });

        var updatedTask = task with
        {
            Status = newStatus,
            CompletedDate = newStatus == Models.TaskStatus.Completed ? DateTimeOffset.UtcNow : task.CompletedDate,
            History = updatedHistory
        };

        updatedTask.ToEntity(entity);
        await context.SaveChangesAsync();
        return entity.ToModel();
    }

    public async Task<LifecycleTask> AssignTaskAsync(string taskId, string userId, string userName, string userEmail, string performedByUserId, string performedByName)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.Tasks.FindAsync(taskId)
            ?? throw new InvalidOperationException($"Task {taskId} not found");

        var task = entity.ToModel();
        var updatedHistory = task.History.ToList();
        updatedHistory.Add(new TaskHistoryEntry
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            Action = $"Assigned to {userName}",
            PerformedBy = performedByName,
            PerformedById = performedByUserId
        });

        var updatedTask = task with
        {
            AssigneeId = userId,
            AssigneeName = userName,
            AssigneeEmail = userEmail,
            History = updatedHistory
        };

        updatedTask.ToEntity(entity);
        await context.SaveChangesAsync();
        return entity.ToModel();
    }

    public async Task<LifecycleTask> DelegateTaskAsync(string taskId, string fromUserId, string toUserId, string toUserName, string toUserEmail, string reason, string performedByUserId, string performedByName)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.Tasks.FindAsync(taskId)
            ?? throw new InvalidOperationException($"Task {taskId} not found");

        var task = entity.ToModel();
        var updatedHistory = task.History.ToList();
        updatedHistory.Add(new TaskHistoryEntry
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            Action = $"Delegated to {toUserName}",
            PerformedBy = performedByName,
            PerformedById = performedByUserId,
            Notes = reason
        });

        var updatedTask = task with
        {
            AssigneeId = toUserId,
            AssigneeName = toUserName,
            AssigneeEmail = toUserEmail,
            OriginalAssigneeId = task.OriginalAssigneeId ?? fromUserId,
            DelegationReason = reason,
            History = updatedHistory
        };

        updatedTask.ToEntity(entity);
        await context.SaveChangesAsync();
        return entity.ToModel();
    }

    public async Task<LifecycleTask> EscalateTaskAsync(string taskId, string reason, string performedByUserId, string performedByName)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.Tasks.FindAsync(taskId)
            ?? throw new InvalidOperationException($"Task {taskId} not found");

        var task = entity.ToModel();
        var updatedHistory = task.History.ToList();
        updatedHistory.Add(new TaskHistoryEntry
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            Action = "Task escalated",
            PerformedBy = performedByName,
            PerformedById = performedByUserId,
            Notes = reason
        });

        var updatedTask = task with
        {
            IsEscalated = true,
            EscalatedDate = DateTimeOffset.UtcNow,
            History = updatedHistory
        };

        updatedTask.ToEntity(entity);
        await context.SaveChangesAsync();
        return entity.ToModel();
    }

    public async Task<LifecycleTask> CompleteTaskAsync(string taskId, string performedByUserId, string performedByName, string? notes = null)
    {
        return await UpdateTaskStatusAsync(taskId, Models.TaskStatus.Completed, performedByUserId, performedByName, notes);
    }

    public async Task<LifecycleTask> AddTaskNoteAsync(string taskId, string performedByUserId, string performedByName, string note)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.Tasks.FindAsync(taskId)
            ?? throw new InvalidOperationException($"Task {taskId} not found");

        var task = entity.ToModel();
        var updatedHistory = task.History.ToList();
        updatedHistory.Add(new TaskHistoryEntry
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            Action = "Note added",
            PerformedBy = performedByName,
            PerformedById = performedByUserId,
            Notes = note
        });

        var updatedTask = task with { History = updatedHistory };
        updatedTask.ToEntity(entity);
        await context.SaveChangesAsync();
        return entity.ToModel();
    }

    public async Task<TaskSummary> GetTaskSummaryForUserAsync(string userId)
    {
        var tasks = await GetTasksForUserAsync(userId);
        var now = DateTimeOffset.UtcNow;

        return new TaskSummary
        {
            Total = tasks.Count,
            Overdue = tasks.Count(t => t.DueDate < now && t.Status != Models.TaskStatus.Completed),
            DueThisWeek = tasks.Count(t => t.DueDate >= now && t.DueDate <= now.AddDays(7) && t.Status != Models.TaskStatus.Completed),
            DueThisMonth = tasks.Count(t => t.DueDate >= now && t.DueDate <= now.AddDays(30) && t.Status != Models.TaskStatus.Completed),
            Completed = tasks.Count(t => t.Status == Models.TaskStatus.Completed),
            InProgress = tasks.Count(t => t.Status == Models.TaskStatus.InProgress)
        };
    }

    #endregion

    #region Users

    public async Task<User> GetCurrentUserAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.Users.AsNoTracking().FirstOrDefaultAsync();

        if (entity != null)
            return entity.ToModel();

        // Return default user if none exists
        return new User
        {
            Id = "current-user",
            Name = "Current User",
            Email = "user@example.com",
            Role = Models.SystemRole.StandardUser,
            IsActive = true
        };
    }

    public async Task<IReadOnlyList<User>> GetUsersAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entities = await context.Users.AsNoTracking().ToListAsync();
        return entities.Select(e => e.ToModel()).ToList();
    }

    public async Task<User?> GetUserAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
        return entity?.ToModel();
    }

    public async Task<User> CreateUserAsync(User user)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = user.ToEntity();
        context.Users.Add(entity);
        await context.SaveChangesAsync();
        return entity.ToModel();
    }

    public async Task<User> UpdateUserAsync(User user)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.Users.FindAsync(user.Id)
            ?? throw new InvalidOperationException($"User {user.Id} not found");

        user.ToEntity(entity);
        await context.SaveChangesAsync();
        return entity.ToModel();
    }

    public async Task DeleteUserAsync(string userId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.Users.FindAsync(userId);
        if (entity != null)
        {
            context.Users.Remove(entity);
            await context.SaveChangesAsync();
        }
    }

    #endregion

    #region Portfolio Health

    public async Task<PortfolioHealthSummary> GetPortfolioHealthSummaryAsync()
    {
        var apps = await GetApplicationsAsync();

        return new PortfolioHealthSummary
        {
            TotalApplications = apps.Count,
            HealthyCount = apps.Count(a => a.HealthCategory == HealthCategory.Healthy),
            NeedsAttentionCount = apps.Count(a => a.HealthCategory == HealthCategory.NeedsAttention),
            AtRiskCount = apps.Count(a => a.HealthCategory == HealthCategory.AtRisk),
            CriticalCount = apps.Count(a => a.HealthCategory == HealthCategory.Critical),
            AverageScore = apps.Count > 0 ? apps.Average(a => a.HealthScore) : 0,
            LastUpdated = DateTimeOffset.UtcNow
        };
    }

    #endregion

    #region Repository Info

    public async Task<RepositoryInfo?> GetRepositoryInfoAsync(string applicationId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.Repositories.AsNoTracking()
            .FirstOrDefaultAsync(r => r.RepositoryId == applicationId);
        return entity?.ToModel();
    }

    #endregion

    #region Task Documentation

    public async Task<TaskDocumentation?> GetTaskDocumentationAsync(TaskType taskType)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.TaskDocumentation.AsNoTracking()
            .FirstOrDefaultAsync(d => d.TaskType == taskType.ToString());
        return entity?.ToModel();
    }

    public async Task<IReadOnlyList<TaskDocumentation>> GetAllTaskDocumentationAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entities = await context.TaskDocumentation.AsNoTracking().ToListAsync();
        return entities.Select(e => e.ToModel()).ToList();
    }

    public async Task<TaskDocumentation> UpdateTaskDocumentationAsync(TaskDocumentation documentation)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.TaskDocumentation.FindAsync(documentation.Id)
            ?? throw new InvalidOperationException($"Task documentation {documentation.Id} not found");

        documentation.ToEntity(entity);
        await context.SaveChangesAsync();
        return entity.ToModel();
    }

    public async Task<TaskDocumentation> CreateTaskDocumentationAsync(TaskDocumentation documentation)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = documentation.ToEntity();
        context.TaskDocumentation.Add(entity);
        await context.SaveChangesAsync();
        return entity.ToModel();
    }

    public async Task DeleteTaskDocumentationAsync(string id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.TaskDocumentation.FindAsync(id);
        if (entity != null)
        {
            context.TaskDocumentation.Remove(entity);
            await context.SaveChangesAsync();
        }
    }

    #endregion

    #region Framework Versions

    public async Task<IReadOnlyList<FrameworkVersion>> GetAllFrameworkVersionsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // First, auto-detect frameworks from synced repositories and ensure they exist
        await EnsureAutoDetectedFrameworkVersionsAsync(context);

        // Query all framework versions from the database
        var entities = await context.FrameworkVersions
            .AsNoTracking()
            .OrderBy(f => f.Framework)
            .ThenByDescending(f => f.Version)
            .ToListAsync();

        return entities.Select(EntityToFrameworkVersion).ToList();
    }

    public async Task<FrameworkVersion?> GetFrameworkVersionAsync(string id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.FrameworkVersions.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
        return entity != null ? EntityToFrameworkVersion(entity) : null;
    }

    public async Task<IReadOnlyList<FrameworkVersion>> GetFrameworkVersionsByTypeAsync(FrameworkType type)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var typeString = type.ToString();
        var entities = await context.FrameworkVersions
            .AsNoTracking()
            .Where(f => f.Framework == typeString)
            .OrderByDescending(f => f.Version)
            .ToListAsync();

        return entities.Select(EntityToFrameworkVersion).ToList();
    }

    public async Task<FrameworkVersion> UpdateFrameworkVersionAsync(FrameworkVersion version)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.FrameworkVersions.FirstOrDefaultAsync(f => f.Id == version.Id);
        if (entity == null)
        {
            throw new InvalidOperationException($"Framework version with ID {version.Id} not found");
        }

        FrameworkVersionToEntity(version, entity);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await context.SaveChangesAsync();

        return EntityToFrameworkVersion(entity);
    }

    public async Task<FrameworkVersion> CreateFrameworkVersionAsync(FrameworkVersion version)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var entity = new FrameworkVersionEntity { Id = string.IsNullOrEmpty(version.Id) ? Guid.NewGuid().ToString() : version.Id };
        FrameworkVersionToEntity(version, entity);

        context.FrameworkVersions.Add(entity);
        await context.SaveChangesAsync();

        return EntityToFrameworkVersion(entity);
    }

    public async Task DeleteFrameworkVersionAsync(string id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.FrameworkVersions.FirstOrDefaultAsync(f => f.Id == id);
        if (entity != null)
        {
            context.FrameworkVersions.Remove(entity);
            await context.SaveChangesAsync();
        }
    }

    public async Task<FrameworkEolSummary> GetFrameworkEolSummaryAsync()
    {
        var frameworks = await GetAllFrameworkVersionsAsync();
        var applications = await GetApplicationsAsync();

        var eolFrameworks = frameworks.Where(f => f.IsPastEol || f.IsApproachingEol).ToList();
        var details = new List<FrameworkEolDetail>();

        foreach (var framework in eolFrameworks)
        {
            var matchingApps = await GetApplicationsByFrameworkAsync(framework.Id);
            if (matchingApps.Count > 0)
            {
                details.Add(new FrameworkEolDetail
                {
                    Framework = framework,
                    ApplicationCount = matchingApps.Count,
                    ApplicationNames = matchingApps.Select(a => a.Name).ToList()
                });
            }
        }

        return new FrameworkEolSummary
        {
            TotalApplications = applications.Count,
            ApplicationsWithEolFrameworks = details.Where(d => d.Framework.IsPastEol).Sum(d => d.ApplicationCount),
            ApplicationsApproachingEol = details.Where(d => d.Framework.IsApproachingEol && !d.Framework.IsPastEol).Sum(d => d.ApplicationCount),
            CriticalEolCount = details.Count(d => d.Framework.EolUrgency == EolUrgency.Critical),
            HighEolCount = details.Count(d => d.Framework.EolUrgency == EolUrgency.High),
            MediumEolCount = details.Count(d => d.Framework.EolUrgency == EolUrgency.Medium),
            Details = details.OrderBy(d => d.Framework.DaysUntilEol ?? int.MaxValue).ToList()
        };
    }

    /// <summary>
    /// Ensures framework versions are auto-created from detected target frameworks in repositories.
    /// </summary>
    private async Task EnsureAutoDetectedFrameworkVersionsAsync(LifecycleDbContext context)
    {
        // Get distinct target frameworks from synced repositories
        var detectedFrameworks = await context.SyncedRepositories
            .Where(r => !string.IsNullOrEmpty(r.TargetFramework))
            .Select(r => r.TargetFramework!)
            .Distinct()
            .ToListAsync();

        foreach (var tfm in detectedFrameworks)
        {
            // Check if we already have this TFM
            var existing = await context.FrameworkVersions
                .FirstOrDefaultAsync(f => f.TargetFrameworkMoniker == tfm);

            if (existing == null)
            {
                // Parse the TFM and create a framework version entry
                var (frameworkType, version, displayName) = ParseTargetFrameworkMoniker(tfm);

                var entity = new FrameworkVersionEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    Framework = frameworkType.ToString(),
                    Version = version,
                    DisplayName = displayName,
                    Status = "Unknown",
                    TargetFrameworkMoniker = tfm,
                    AutoDetected = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                context.FrameworkVersions.Add(entity);
                _logger.LogInformation("Auto-detected framework version: {DisplayName} (TFM: {TFM})", displayName, tfm);
            }
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Parses a Target Framework Moniker (TFM) into framework type, version, and display name.
    /// </summary>
    private static (FrameworkType Type, string Version, string DisplayName) ParseTargetFrameworkMoniker(string tfm)
    {
        // Common TFM patterns:
        // net8.0, net7.0, net6.0 → .NET 8, .NET 7, .NET 6
        // net472, net48, net461 → .NET Framework 4.7.2, 4.8, 4.6.1
        // netcoreapp3.1, netcoreapp2.1 → .NET Core 3.1, 2.1
        // netstandard2.0, netstandard2.1 → .NET Standard 2.0, 2.1

        var lower = tfm.ToLowerInvariant();

        // Modern .NET (net5.0+)
        if (lower.StartsWith("net") && lower.Contains('.'))
        {
            var versionPart = lower[3..]; // Remove "net"
            if (Version.TryParse(versionPart, out var parsed))
            {
                return (FrameworkType.DotNet, versionPart, $".NET {parsed.Major}");
            }
        }

        // .NET Framework (net472, net48, etc)
        if (lower.StartsWith("net") && !lower.Contains('.'))
        {
            var versionDigits = lower[3..];
            if (versionDigits.Length >= 2)
            {
                var major = versionDigits[0];
                var minor = versionDigits.Length > 1 ? versionDigits[1] : '0';
                var patch = versionDigits.Length > 2 ? versionDigits[2..] : "";
                var displayVersion = patch.Length > 0 ? $"{major}.{minor}.{patch}" : $"{major}.{minor}";
                return (FrameworkType.DotNetFramework, displayVersion, $".NET Framework {displayVersion}");
            }
        }

        // .NET Core
        if (lower.StartsWith("netcoreapp"))
        {
            var versionPart = lower[10..];
            return (FrameworkType.DotNet, versionPart, $".NET Core {versionPart}");
        }

        // .NET Standard (treat as .NET)
        if (lower.StartsWith("netstandard"))
        {
            var versionPart = lower[11..];
            return (FrameworkType.DotNet, $"standard{versionPart}", $".NET Standard {versionPart}");
        }

        // Unknown - return as-is
        return (FrameworkType.Other, tfm, tfm);
    }

    private static FrameworkVersion EntityToFrameworkVersion(FrameworkVersionEntity entity)
    {
        return new FrameworkVersion
        {
            Id = entity.Id,
            Framework = Enum.TryParse<FrameworkType>(entity.Framework, out var ft) ? ft : FrameworkType.Other,
            Version = entity.Version,
            DisplayName = entity.DisplayName,
            ReleaseDate = entity.ReleaseDate,
            EndOfLifeDate = entity.EndOfLifeDate,
            EndOfActiveSupportDate = entity.EndOfActiveSupportDate,
            IsLts = entity.IsLts,
            Status = Enum.TryParse<SupportStatus>(entity.Status, out var ss) ? ss : SupportStatus.Unknown,
            LatestPatchVersion = entity.LatestPatchVersion,
            Notes = entity.Notes,
            RecommendedUpgradePath = entity.RecommendedUpgradePath,
            LastUpdated = entity.UpdatedAt ?? entity.CreatedAt
        };
    }

    private static void FrameworkVersionToEntity(FrameworkVersion version, FrameworkVersionEntity entity)
    {
        entity.Framework = version.Framework.ToString();
        entity.Version = version.Version;
        entity.DisplayName = version.DisplayName;
        entity.ReleaseDate = version.ReleaseDate;
        entity.EndOfLifeDate = version.EndOfLifeDate;
        entity.EndOfActiveSupportDate = version.EndOfActiveSupportDate;
        entity.IsLts = version.IsLts;
        entity.Status = version.Status.ToString();
        entity.LatestPatchVersion = version.LatestPatchVersion;
        entity.Notes = version.Notes;
        entity.RecommendedUpgradePath = version.RecommendedUpgradePath;
    }

    #endregion

    #region Synced Repositories

    public async Task<IReadOnlyList<SyncedRepository>> GetSyncedRepositoriesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entities = await context.SyncedRepositories.AsNoTracking().ToListAsync();
        return entities.Select(e => e.ToModel()).ToList();
    }

    public async Task StoreSyncedRepositoriesAsync(IEnumerable<SyncedRepository> repositories)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        foreach (var repo in repositories)
        {
            var existing = await context.SyncedRepositories.FirstOrDefaultAsync(r => r.Id == repo.Id);
            if (existing != null)
            {
                repo.ToEntity(existing);
            }
            else
            {
                context.SyncedRepositories.Add(repo.ToEntity());
            }
        }

        await context.SaveChangesAsync();
        _logger.LogInformation("Stored {Count} synced repositories to database", repositories.Count());
    }

    public async Task<SyncedRepository?> GetSyncedRepositoryAsync(string repositoryId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.SyncedRepositories.AsNoTracking().FirstOrDefaultAsync(r => r.Id == repositoryId);
        return entity?.ToModel();
    }

    public async Task ClearSyncedRepositoriesAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.SyncedRepositories.RemoveRange(context.SyncedRepositories);
        await context.SaveChangesAsync();
    }

    #endregion

    #region ServiceNow Applications

    public async Task<IReadOnlyList<ImportedServiceNowApplication>> GetImportedServiceNowApplicationsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entities = await context.ImportedServiceNowApplications.AsNoTracking().ToListAsync();
        return entities.Select(e => e.ToModel()).ToList();
    }

    public async Task StoreServiceNowApplicationsAsync(IEnumerable<ImportedServiceNowApplication> applications)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Track added entities by ServiceNowId to handle duplicates in the same batch
        var addedEntities = new Dictionary<string, ImportedServiceNowApplicationEntity>(StringComparer.OrdinalIgnoreCase);

        foreach (var app in applications)
        {
            // Truncate strings to their max lengths to prevent database errors
            var truncatedApp = TruncateServiceNowAppStrings(app);

            // Check if we already added this ServiceNowId in this batch
            if (addedEntities.TryGetValue(truncatedApp.ServiceNowId, out var alreadyAdded))
            {
                // Update the already-added entity instead of adding a duplicate
                truncatedApp.ToEntity(alreadyAdded);
                continue;
            }

            // Check if entity exists in database
            var existing = await context.ImportedServiceNowApplications
                .FirstOrDefaultAsync(a => a.ServiceNowId == truncatedApp.ServiceNowId);

            if (existing != null)
            {
                truncatedApp.ToEntity(existing);
            }
            else
            {
                var newEntity = truncatedApp.ToEntity();
                context.ImportedServiceNowApplications.Add(newEntity);
                addedEntities[truncatedApp.ServiceNowId] = newEntity;
            }
        }

        await context.SaveChangesAsync();
        _logger.LogInformation("Stored {Count} imported ServiceNow applications to database", applications.Count());
    }

    /// <summary>
    /// Truncates string fields to their maximum database lengths to prevent EF Core save errors.
    /// </summary>
    private static ImportedServiceNowApplication TruncateServiceNowAppStrings(ImportedServiceNowApplication app)
    {
        return app with
        {
            ServiceNowId = Truncate(app.ServiceNowId, 100),
            Name = Truncate(app.Name, 200),
            Description = Truncate(app.Description, 4000),
            ShortDescription = Truncate(app.ShortDescription, 500),
            Capability = Truncate(app.Capability, 100),
            Status = Truncate(app.Status, 50),
            OwnerId = Truncate(app.OwnerId, 100),
            OwnerName = Truncate(app.OwnerName, 200),
            ProductManagerId = Truncate(app.ProductManagerId, 100),
            ProductManagerName = Truncate(app.ProductManagerName, 200),
            BusinessOwnerId = Truncate(app.BusinessOwnerId, 100),
            BusinessOwnerName = Truncate(app.BusinessOwnerName, 200),
            FunctionalArchitectId = Truncate(app.FunctionalArchitectId, 100),
            FunctionalArchitectName = Truncate(app.FunctionalArchitectName, 200),
            TechnicalArchitectId = Truncate(app.TechnicalArchitectId, 100),
            TechnicalArchitectName = Truncate(app.TechnicalArchitectName, 200),
            TechnicalLeadId = Truncate(app.TechnicalLeadId, 100),
            TechnicalLeadName = Truncate(app.TechnicalLeadName, 200),
            ApplicationType = Truncate(app.ApplicationType, 50),
            ArchitectureType = Truncate(app.ArchitectureType, 100),
            UserBase = Truncate(app.UserBase, 100),
            Importance = Truncate(app.Importance, 50),
            RepositoryUrl = Truncate(app.RepositoryUrl, 500),
            DocumentationUrl = Truncate(app.DocumentationUrl, 500),
            Environment = Truncate(app.Environment, 100),
            Criticality = Truncate(app.Criticality, 50),
            SupportGroup = Truncate(app.SupportGroup, 200)
        };
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    public async Task ClearServiceNowApplicationsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        await context.ImportedServiceNowApplications.ExecuteDeleteAsync();
        _logger.LogInformation("Cleared all imported ServiceNow applications from database");
    }

    public async Task<int> CreateApplicationsFromServiceNowImportAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var importedApps = await context.ImportedServiceNowApplications.AsNoTracking().ToListAsync();

        if (importedApps.Count == 0)
            return 0;

        int count = 0;
        var processedServiceNowIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var importedEntity in importedApps)
        {
            var imported = importedEntity.ToModel();

            // Skip duplicates in the imported data
            if (!processedServiceNowIds.Add(imported.ServiceNowId))
            {
                _logger.LogWarning("Skipping duplicate ServiceNowId: {ServiceNowId}", imported.ServiceNowId);
                continue;
            }

            // Check if app already exists by ServiceNow ID
            var existing = await context.Applications
                .FirstOrDefaultAsync(a => a.ServiceNowId == imported.ServiceNowId);

            // Truncate fields to match database column limits
            var app = new Application
            {
                Id = existing?.Id ?? Guid.NewGuid().ToString(),
                Name = Truncate(imported.Name, 200),
                Description = Truncate(imported.Description, 2000),
                ShortDescription = Truncate(imported.ShortDescription, 500),
                Capability = Truncate(imported.Capability, 100) ?? "Uncategorized",
                ApplicationType = ParseAppType(imported.ApplicationType),
                ArchitectureType = ParseArchitectureType(imported.ArchitectureType),
                UserBaseEstimate = Truncate(imported.UserBase, 100),
                Importance = Truncate(imported.Importance, 50),
                ServiceNowId = Truncate(imported.ServiceNowId, 100),
                RepositoryUrl = Truncate(imported.RepositoryUrl, 500),
                DocumentationUrl = Truncate(imported.DocumentationUrl, 500),
                IsMockData = false, // Imported data is real, not mock
                HealthScore = existing?.HealthScore ?? 70,
                LastSyncDate = DateTimeOffset.UtcNow,
                RoleAssignments = BuildRoleAssignments(imported)
            };

            if (existing != null)
            {
                app.ToEntity(existing);
            }
            else
            {
                context.Applications.Add(app.ToEntity());
            }

            count++;
        }

        await context.SaveChangesAsync();
        _logger.LogInformation("Created/updated {Count} applications from ServiceNow import", count);
        return count;
    }

    private static AppType ParseAppType(string? value)
    {
        if (string.IsNullOrEmpty(value)) return AppType.Unknown;
        return value.ToUpperInvariant() switch
        {
            "COTS" => AppType.COTS,
            "HOMEGROWN" or "CUSTOM" or "IN-HOUSE" => AppType.Homegrown,
            "HYBRID" => AppType.Hybrid,
            "SAAS" => AppType.SaaS,
            "OPEN SOURCE" or "OPENSOURCE" => AppType.OpenSource,
            _ => AppType.Unknown
        };
    }

    private static ArchitectureType ParseArchitectureType(string? value)
    {
        if (string.IsNullOrEmpty(value)) return ArchitectureType.Unknown;
        return value.ToUpperInvariant() switch
        {
            "WEB BASED" or "WEB-BASED" or "WEB" => ArchitectureType.WebBased,
            "CLIENT SERVER" or "CLIENT-SERVER" or "CLIENT/SERVER" => ArchitectureType.ClientServer,
            "DESKTOP APP" or "DESKTOP" => ArchitectureType.DesktopApp,
            "MOBILE APP" or "MOBILE" => ArchitectureType.MobileApp,
            "API" => ArchitectureType.API,
            "BATCH PROCESS" or "BATCH" => ArchitectureType.BatchProcess,
            "MICROSERVICES" => ArchitectureType.Microservices,
            "MONOLITHIC" or "MONOLITH" => ArchitectureType.Monolithic,
            "OTHER" => ArchitectureType.Other,
            _ => ArchitectureType.Unknown
        };
    }

    private static List<RoleAssignment> BuildRoleAssignments(ImportedServiceNowApplication imported)
    {
        var assignments = new List<RoleAssignment>();

        if (!string.IsNullOrEmpty(imported.ProductManagerName))
        {
            assignments.Add(new RoleAssignment
            {
                UserId = imported.ProductManagerId ?? Guid.NewGuid().ToString(),
                UserName = imported.ProductManagerName,
                UserEmail = GenerateEmailFromName(imported.ProductManagerName),
                Role = ApplicationRole.ProductManager
            });
        }

        if (!string.IsNullOrEmpty(imported.BusinessOwnerName))
        {
            assignments.Add(new RoleAssignment
            {
                UserId = imported.BusinessOwnerId ?? Guid.NewGuid().ToString(),
                UserName = imported.BusinessOwnerName,
                UserEmail = GenerateEmailFromName(imported.BusinessOwnerName),
                Role = ApplicationRole.BusinessOwner
            });
        }

        if (!string.IsNullOrEmpty(imported.FunctionalArchitectName))
        {
            assignments.Add(new RoleAssignment
            {
                UserId = imported.FunctionalArchitectId ?? Guid.NewGuid().ToString(),
                UserName = imported.FunctionalArchitectName,
                UserEmail = GenerateEmailFromName(imported.FunctionalArchitectName),
                Role = ApplicationRole.FunctionalArchitect
            });
        }

        if (!string.IsNullOrEmpty(imported.TechnicalArchitectName))
        {
            assignments.Add(new RoleAssignment
            {
                UserId = imported.TechnicalArchitectId ?? Guid.NewGuid().ToString(),
                UserName = imported.TechnicalArchitectName,
                UserEmail = GenerateEmailFromName(imported.TechnicalArchitectName),
                Role = ApplicationRole.TechnicalArchitect
            });
        }

        if (!string.IsNullOrEmpty(imported.OwnerName))
        {
            assignments.Add(new RoleAssignment
            {
                UserId = imported.OwnerId ?? Guid.NewGuid().ToString(),
                UserName = imported.OwnerName,
                UserEmail = GenerateEmailFromName(imported.OwnerName),
                Role = ApplicationRole.Owner
            });
        }

        if (!string.IsNullOrEmpty(imported.TechnicalLeadName))
        {
            assignments.Add(new RoleAssignment
            {
                UserId = imported.TechnicalLeadId ?? Guid.NewGuid().ToString(),
                UserName = imported.TechnicalLeadName,
                UserEmail = GenerateEmailFromName(imported.TechnicalLeadName),
                Role = ApplicationRole.TechnicalLead
            });
        }

        return assignments;
    }

    private static string GenerateEmailFromName(string name)
    {
        // Generate a placeholder email from the name (will be updated when synced with Entra ID)
        var normalized = name.ToLowerInvariant().Replace(" ", ".").Replace(",", "");
        return $"{normalized}@example.com";
    }

    public Task<ServiceNowColumnMapping?> GetServiceNowColumnMappingAsync()
    {
        return Task.FromResult(_serviceNowColumnMapping);
    }

    public Task SaveServiceNowColumnMappingAsync(ServiceNowColumnMapping mapping)
    {
        _serviceNowColumnMapping = mapping;
        return Task.CompletedTask;
    }

    #endregion

    #region App Name Mappings

    public async Task<IReadOnlyList<AppNameMapping>> GetAppNameMappingsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entities = await context.AppNameMappings.AsNoTracking().ToListAsync();
        return entities.Select(e => e.ToModel()).ToList();
    }

    public async Task StoreAppNameMappingsAsync(IEnumerable<AppNameMapping> mappings)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        foreach (var mapping in mappings)
        {
            var existing = await context.AppNameMappings
                .FirstOrDefaultAsync(m => m.ServiceNowAppName == mapping.ServiceNowAppName);

            if (existing != null)
            {
                mapping.ToEntity(existing);
            }
            else
            {
                context.AppNameMappings.Add(mapping.ToEntity());
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task<AppNameMapping?> GetAppNameMappingByServiceNowNameAsync(string serviceNowAppName)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.AppNameMappings.AsNoTracking()
            .FirstOrDefaultAsync(m => m.ServiceNowAppName == serviceNowAppName);
        return entity?.ToModel();
    }

    public async Task<AppNameMapping?> GetAppNameMappingBySharePointFolderAsync(string sharePointFolderName)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.AppNameMappings.AsNoTracking()
            .FirstOrDefaultAsync(m => m.SharePointFolderName == sharePointFolderName);
        return entity?.ToModel();
    }

    public async Task<AppNameMapping?> GetAppNameMappingByRepoNameAsync(string repoName)
    {
        var mappings = await GetAppNameMappingsAsync();
        return mappings.FirstOrDefault(m => m.AzureDevOpsRepoNames.Contains(repoName, StringComparer.OrdinalIgnoreCase));
    }

    public async Task ClearAppNameMappingsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.AppNameMappings.RemoveRange(context.AppNameMappings);
        await context.SaveChangesAsync();
    }

    public Task<AppNameMappingConfig?> GetAppNameMappingConfigAsync()
    {
        return Task.FromResult(_appNameMappingConfig);
    }

    public Task SaveAppNameMappingConfigAsync(AppNameMappingConfig config)
    {
        _appNameMappingConfig = config;
        return Task.CompletedTask;
    }

    #endregion

    #region Capability Mappings

    public async Task<IReadOnlyList<CapabilityMapping>> GetCapabilityMappingsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entities = await context.CapabilityMappings.AsNoTracking().ToListAsync();
        return entities.Select(e => e.ToModel()).ToList();
    }

    public async Task StoreCapabilityMappingsAsync(IEnumerable<CapabilityMapping> mappings)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        foreach (var mapping in mappings)
        {
            var existing = await context.CapabilityMappings
                .FirstOrDefaultAsync(m => m.ApplicationName == mapping.ApplicationName);

            if (existing != null)
            {
                mapping.ToEntity(existing);
            }
            else
            {
                context.CapabilityMappings.Add(mapping.ToEntity());
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task ClearCapabilityMappingsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.CapabilityMappings.RemoveRange(context.CapabilityMappings);
        await context.SaveChangesAsync();
    }

    public async Task<string?> GetCapabilityForApplicationAsync(string applicationName)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entity = await context.CapabilityMappings.AsNoTracking()
            .FirstOrDefaultAsync(m => m.ApplicationName == applicationName);
        return entity?.Capability;
    }

    #endregion

    #region SharePoint Folders

    public async Task<IReadOnlyList<DiscoveredSharePointFolder>> GetDiscoveredSharePointFoldersAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var entities = await context.SharePointFolders.AsNoTracking().ToListAsync();
        return entities.Select(e => e.ToModel()).ToList();
    }

    public async Task StoreDiscoveredSharePointFoldersAsync(IEnumerable<DiscoveredSharePointFolder> folders)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        foreach (var folder in folders)
        {
            var existing = await context.SharePointFolders
                .FirstOrDefaultAsync(f => f.FullPath == folder.FullPath);

            if (existing != null)
            {
                folder.ToEntity(existing);
            }
            else
            {
                context.SharePointFolders.Add(folder.ToEntity());
            }
        }

        await context.SaveChangesAsync();
    }

    public async Task ClearDiscoveredSharePointFoldersAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.SharePointFolders.RemoveRange(context.SharePointFolders);
        await context.SaveChangesAsync();
    }

    #endregion

    #region Task Settings

    public Task<TaskSchedulingConfig> GetTaskSchedulingConfigAsync()
    {
        return Task.FromResult(_taskSchedulingConfig);
    }

    public Task<TaskSchedulingConfig> UpdateTaskSchedulingConfigAsync(TaskSchedulingConfig config)
    {
        _taskSchedulingConfig = config;
        return Task.FromResult(_taskSchedulingConfig);
    }

    #endregion

    #region Audit Log

    public async Task<IReadOnlyList<AuditLogEntry>> GetAuditLogAsync(AuditLogFilter? filter = null)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.AuditLogs.AsNoTracking().AsQueryable();

        if (filter != null)
        {
            if (!string.IsNullOrEmpty(filter.Category))
                query = query.Where(e => e.Category == filter.Category);
            if (!string.IsNullOrEmpty(filter.EventType))
                query = query.Where(e => e.EventType == filter.EventType);
            if (!string.IsNullOrEmpty(filter.UserId))
                query = query.Where(e => e.UserId == filter.UserId);
            if (filter.StartDate.HasValue)
                query = query.Where(e => e.Timestamp >= filter.StartDate.Value);
            if (filter.EndDate.HasValue)
                query = query.Where(e => e.Timestamp <= filter.EndDate.Value);
        }

        query = query.OrderByDescending(e => e.Timestamp);

        if (filter?.Limit.HasValue == true)
            query = query.Take(filter.Limit.Value);

        var entities = await query.ToListAsync();
        return entities.Select(e => e.ToModel()).ToList();
    }

    public async Task RecordAuditLogAsync(AuditLogEntry entry)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.AuditLogs.Add(entry.ToEntity());
        await context.SaveChangesAsync();
    }

    #endregion

    #region System Settings

    public Task<SystemSettings> GetSystemSettingsAsync()
    {
        return Task.FromResult(_systemSettings);
    }

    public Task<SystemSettings> UpdateSystemSettingsAsync(SystemSettings settings)
    {
        _systemSettings = settings;
        return Task.FromResult(_systemSettings);
    }

    public Task<IReadOnlyList<DataSourceConfig>> GetDataSourceConfigsAsync()
    {
        return Task.FromResult<IReadOnlyList<DataSourceConfig>>(_dataSourceConfigs.ToList());
    }

    public Task<DataSourceConfig> UpdateDataSourceConfigAsync(DataSourceConfig config)
    {
        var existing = _dataSourceConfigs.FirstOrDefault(c => c.Id == config.Id);
        if (existing != null)
        {
            _dataSourceConfigs.Remove(existing);
        }
        _dataSourceConfigs.Add(config);
        return Task.FromResult(config);
    }

    public Task<DataSourceTestResult> TestDataSourceConnectionAsync(string dataSourceId)
    {
        return Task.FromResult(new DataSourceTestResult
        {
            Success = false,
            Message = "Connection testing not implemented for database service"
        });
    }

    #endregion
}
