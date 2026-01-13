using LifecycleDashboard.Data.Entities;
using LifecycleDashboard.Models;
using LifecycleDashboard.Services;
using Microsoft.EntityFrameworkCore;

namespace LifecycleDashboard.Data;

/// <summary>
/// Seeds the database with mock data for development/testing.
/// </summary>
public class DatabaseSeeder
{
    private readonly IDbContextFactory<LifecycleDbContext> _contextFactory;
    private readonly MockDataService _mockDataService;
    private readonly IEolDataService? _eolDataService;
    private readonly ILogger<DatabaseSeeder>? _logger;

    public DatabaseSeeder(IDbContextFactory<LifecycleDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
        // Use MockDataService directly for seed data (not via interface to avoid circular dependency)
        _mockDataService = new MockDataService();
    }

    public DatabaseSeeder(
        IDbContextFactory<LifecycleDbContext> contextFactory,
        IEolDataService? eolDataService,
        ILogger<DatabaseSeeder>? logger) : this(contextFactory)
    {
        _eolDataService = eolDataService;
        _logger = logger;
    }

    /// <summary>
    /// Seeds the database with mock data if the tables are empty.
    /// </summary>
    public async Task SeedAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Apply any pending migrations (creates database if needed)
        await context.Database.MigrateAsync();

        // Always ensure framework versions are seeded (from endoflife.date or fallback)
        await EnsureFrameworkVersionsSeededAsync(context);

        // Only seed other mock data if database is empty
        if (await context.Applications.AnyAsync())
        {
            return;
        }

        await SeedApplicationsAsync(context);
        await SeedTasksAsync(context);
        await SeedUsersAsync(context);
        await SeedTaskDocumentationAsync(context);

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Forces a reseed, clearing existing data and repopulating.
    /// </summary>
    public async Task ReseedAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Clear existing data in reverse dependency order
        context.AuditLogs.RemoveRange(context.AuditLogs);
        context.SyncJobs.RemoveRange(context.SyncJobs);
        context.SharePointFolders.RemoveRange(context.SharePointFolders);
        context.Tasks.RemoveRange(context.Tasks);
        context.TaskDocumentation.RemoveRange(context.TaskDocumentation);
        context.Repositories.RemoveRange(context.Repositories);
        context.Users.RemoveRange(context.Users);
        context.AppNameMappings.RemoveRange(context.AppNameMappings);
        context.CapabilityMappings.RemoveRange(context.CapabilityMappings);
        context.Applications.RemoveRange(context.Applications);

        await context.SaveChangesAsync();

        await SeedApplicationsAsync(context);
        await SeedTasksAsync(context);
        await SeedUsersAsync(context);
        await SeedTaskDocumentationAsync(context);

        await context.SaveChangesAsync();
    }

    private async Task SeedApplicationsAsync(LifecycleDbContext context)
    {
        var applications = await _mockDataService.GetApplicationsAsync();

        foreach (var app in applications)
        {
            context.Applications.Add(app.ToEntity());
        }
    }

    private async Task SeedTasksAsync(LifecycleDbContext context)
    {
        var tasks = await _mockDataService.GetAllTasksAsync();

        foreach (var task in tasks)
        {
            context.Tasks.Add(task.ToEntity());
        }
    }

    private async Task SeedUsersAsync(LifecycleDbContext context)
    {
        var users = await _mockDataService.GetUsersAsync();

        foreach (var user in users)
        {
            context.Users.Add(user.ToEntity());
        }
    }

    private async Task SeedTaskDocumentationAsync(LifecycleDbContext context)
    {
        var docs = await _mockDataService.GetAllTaskDocumentationAsync();

        foreach (var doc in docs)
        {
            context.TaskDocumentation.Add(doc.ToEntity());
        }
    }

    /// <summary>
    /// Ensures framework versions are seeded from endoflife.date API.
    /// If API is unavailable, falls back to hardcoded data.
    /// </summary>
    private async Task EnsureFrameworkVersionsSeededAsync(LifecycleDbContext context)
    {
        // Check if we already have framework versions
        if (await context.FrameworkVersions.AnyAsync())
        {
            _logger?.LogInformation("Framework versions already exist, skipping seed");
            return;
        }

        _logger?.LogInformation("Seeding framework versions...");

        // Try to fetch from endoflife.date via EolDataService
        if (_eolDataService != null)
        {
            try
            {
                var result = await _eolDataService.RefreshEolDataAsync();
                if (result.Success && result.Added.Count > 0)
                {
                    _logger?.LogInformation("Seeded {Count} framework versions from endoflife.date", result.Added.Count);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to fetch from endoflife.date, using fallback data");
            }
        }

        // Fallback: Seed with hardcoded data
        _logger?.LogInformation("Using fallback framework data");
        await SeedFallbackFrameworkVersionsAsync(context);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds fallback framework version data when endoflife.date is unavailable.
    /// </summary>
    private static Task SeedFallbackFrameworkVersionsAsync(LifecycleDbContext context)
    {
        var fallbackVersions = new List<FrameworkVersionEntity>
        {
            // .NET versions
            new()
            {
                Id = "dotnet-9",
                Framework = FrameworkType.DotNet.ToString(),
                Version = "9.0",
                DisplayName = ".NET 9",
                ReleaseDate = new DateTimeOffset(2024, 11, 12, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = new DateTimeOffset(2026, 5, 12, 0, 0, 0, TimeSpan.Zero),
                Status = SupportStatus.Active.ToString(),
                IsLts = false,
                IsSystemData = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = "dotnet-8",
                Framework = FrameworkType.DotNet.ToString(),
                Version = "8.0",
                DisplayName = ".NET 8",
                ReleaseDate = new DateTimeOffset(2023, 11, 14, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = new DateTimeOffset(2026, 11, 10, 0, 0, 0, TimeSpan.Zero),
                Status = SupportStatus.Active.ToString(),
                IsLts = true,
                IsSystemData = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = "dotnet-7",
                Framework = FrameworkType.DotNet.ToString(),
                Version = "7.0",
                DisplayName = ".NET 7",
                ReleaseDate = new DateTimeOffset(2022, 11, 8, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = new DateTimeOffset(2024, 5, 14, 0, 0, 0, TimeSpan.Zero),
                Status = SupportStatus.EndOfLife.ToString(),
                IsLts = false,
                IsSystemData = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = "dotnet-6",
                Framework = FrameworkType.DotNet.ToString(),
                Version = "6.0",
                DisplayName = ".NET 6",
                ReleaseDate = new DateTimeOffset(2021, 11, 8, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = new DateTimeOffset(2024, 11, 12, 0, 0, 0, TimeSpan.Zero),
                Status = SupportStatus.EndOfLife.ToString(),
                IsLts = true,
                IsSystemData = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            // .NET Framework versions
            new()
            {
                Id = "dotnetframework-4.8.1",
                Framework = FrameworkType.DotNetFramework.ToString(),
                Version = "4.8.1",
                DisplayName = ".NET Framework 4.8.1",
                ReleaseDate = new DateTimeOffset(2022, 8, 9, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = null, // Indefinite support
                Status = SupportStatus.Active.ToString(),
                IsLts = true,
                Notes = "Supported as part of Windows lifecycle",
                IsSystemData = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = "dotnetframework-4.8",
                Framework = FrameworkType.DotNetFramework.ToString(),
                Version = "4.8",
                DisplayName = ".NET Framework 4.8",
                ReleaseDate = new DateTimeOffset(2019, 4, 18, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = null, // Indefinite support
                Status = SupportStatus.Active.ToString(),
                IsLts = true,
                Notes = "Supported as part of Windows lifecycle",
                RecommendedUpgradePath = ".NET 8 or .NET Framework 4.8.1",
                IsSystemData = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = "dotnetframework-4.7.2",
                Framework = FrameworkType.DotNetFramework.ToString(),
                Version = "4.7.2",
                DisplayName = ".NET Framework 4.7.2",
                ReleaseDate = new DateTimeOffset(2018, 4, 30, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = null,
                Status = SupportStatus.Maintenance.ToString(),
                IsLts = false,
                RecommendedUpgradePath = ".NET Framework 4.8",
                IsSystemData = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            // Python versions
            new()
            {
                Id = "python-3.12",
                Framework = FrameworkType.Python.ToString(),
                Version = "3.12",
                DisplayName = "Python 3.12",
                ReleaseDate = new DateTimeOffset(2023, 10, 2, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = new DateTimeOffset(2028, 10, 1, 0, 0, 0, TimeSpan.Zero),
                Status = SupportStatus.Active.ToString(),
                IsLts = false,
                IsSystemData = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = "python-3.11",
                Framework = FrameworkType.Python.ToString(),
                Version = "3.11",
                DisplayName = "Python 3.11",
                ReleaseDate = new DateTimeOffset(2022, 10, 24, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = new DateTimeOffset(2027, 10, 1, 0, 0, 0, TimeSpan.Zero),
                Status = SupportStatus.Active.ToString(),
                IsLts = false,
                IsSystemData = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            // Node.js versions
            new()
            {
                Id = "nodejs-22",
                Framework = FrameworkType.NodeJs.ToString(),
                Version = "22",
                DisplayName = "Node.js 22",
                ReleaseDate = new DateTimeOffset(2024, 4, 24, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = new DateTimeOffset(2027, 4, 30, 0, 0, 0, TimeSpan.Zero),
                Status = SupportStatus.Active.ToString(),
                IsLts = true,
                IsSystemData = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = "nodejs-20",
                Framework = FrameworkType.NodeJs.ToString(),
                Version = "20",
                DisplayName = "Node.js 20",
                ReleaseDate = new DateTimeOffset(2023, 4, 18, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = new DateTimeOffset(2026, 4, 30, 0, 0, 0, TimeSpan.Zero),
                Status = SupportStatus.Active.ToString(),
                IsLts = true,
                IsSystemData = true,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = "nodejs-18",
                Framework = FrameworkType.NodeJs.ToString(),
                Version = "18",
                DisplayName = "Node.js 18",
                ReleaseDate = new DateTimeOffset(2022, 4, 19, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = new DateTimeOffset(2025, 4, 30, 0, 0, 0, TimeSpan.Zero),
                Status = SupportStatus.Maintenance.ToString(),
                IsLts = true,
                IsSystemData = true,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        context.FrameworkVersions.AddRange(fallbackVersions);
        return Task.CompletedTask;
    }
}
