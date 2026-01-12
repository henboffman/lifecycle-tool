using LifecycleDashboard.Data.Entities;
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

    public DatabaseSeeder(IDbContextFactory<LifecycleDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
        // Use MockDataService directly for seed data (not via interface to avoid circular dependency)
        _mockDataService = new MockDataService();
    }

    /// <summary>
    /// Seeds the database with mock data if the tables are empty.
    /// </summary>
    public async Task SeedAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Apply any pending migrations (creates database if needed)
        await context.Database.MigrateAsync();

        // Only seed if database is empty
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
}
