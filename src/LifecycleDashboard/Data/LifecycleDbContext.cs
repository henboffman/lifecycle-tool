using LifecycleDashboard.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LifecycleDashboard.Data;

/// <summary>
/// Entity Framework Core DbContext for the Lifecycle Dashboard.
/// </summary>
public class LifecycleDbContext : DbContext
{
    public LifecycleDbContext(DbContextOptions<LifecycleDbContext> options)
        : base(options)
    {
    }

    public DbSet<ApplicationEntity> Applications => Set<ApplicationEntity>();
    public DbSet<LifecycleTaskEntity> Tasks => Set<LifecycleTaskEntity>();
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<RepositoryInfoEntity> Repositories => Set<RepositoryInfoEntity>();
    public DbSet<AppNameMappingEntity> AppNameMappings => Set<AppNameMappingEntity>();
    public DbSet<CapabilityMappingEntity> CapabilityMappings => Set<CapabilityMappingEntity>();
    public DbSet<AuditLogEntity> AuditLogs => Set<AuditLogEntity>();
    public DbSet<SyncJobEntity> SyncJobs => Set<SyncJobEntity>();
    public DbSet<DiscoveredSharePointFolderEntity> SharePointFolders => Set<DiscoveredSharePointFolderEntity>();
    public DbSet<TaskDocumentationEntity> TaskDocumentation => Set<TaskDocumentationEntity>();
    public DbSet<SyncedRepositoryEntity> SyncedRepositories => Set<SyncedRepositoryEntity>();
    public DbSet<ImportedServiceNowApplicationEntity> ImportedServiceNowApplications => Set<ImportedServiceNowApplicationEntity>();
    public DbSet<FrameworkVersionEntity> FrameworkVersions => Set<FrameworkVersionEntity>();
    public DbSet<ServiceNowIncidentEntity> ServiceNowIncidents => Set<ServiceNowIncidentEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Application
        modelBuilder.Entity<ApplicationEntity>(entity =>
        {
            entity.ToTable("Applications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Capability).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.ShortDescription).HasMaxLength(500);
            entity.Property(e => e.RepositoryUrl).HasMaxLength(500);
            entity.Property(e => e.DocumentationUrl).HasMaxLength(500);
            entity.Property(e => e.ServiceNowId).HasMaxLength(100);
            entity.Property(e => e.ApplicationType).HasMaxLength(50);
            entity.Property(e => e.ArchitectureType).HasMaxLength(50);
            entity.Property(e => e.UserBaseEstimate).HasMaxLength(100);
            entity.Property(e => e.Importance).HasMaxLength(50);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Capability);
            entity.HasIndex(e => e.ServiceNowId);
        });

        // LifecycleTask
        modelBuilder.Entity<LifecycleTaskEntity>(entity =>
        {
            entity.ToTable("Tasks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(4000);
            entity.Property(e => e.ApplicationId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ApplicationName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.AssigneeId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.AssigneeName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.AssigneeEmail).HasMaxLength(200);
            entity.HasIndex(e => e.ApplicationId);
            entity.HasIndex(e => e.AssigneeId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DueDate);
        });

        // User
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Department).HasMaxLength(100);
            entity.Property(e => e.Title).HasMaxLength(200);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Name);
        });

        // Repository
        modelBuilder.Entity<RepositoryInfoEntity>(entity =>
        {
            entity.ToTable("Repositories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RepositoryId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Url).HasMaxLength(500);
            entity.Property(e => e.DefaultBranch).HasMaxLength(100);
            entity.Property(e => e.LastBuildStatus).HasMaxLength(50);
            entity.Property(e => e.ApplicationInsightsKey).HasMaxLength(100);
            entity.HasIndex(e => e.RepositoryId).IsUnique();
            entity.HasIndex(e => e.Name);
        });

        // AppNameMapping
        modelBuilder.Entity<AppNameMappingEntity>(entity =>
        {
            entity.ToTable("AppNameMappings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ServiceNowAppName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.SharePointFolderName).HasMaxLength(200);
            entity.Property(e => e.Capability).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.HasIndex(e => e.ServiceNowAppName).IsUnique();
        });

        // CapabilityMapping
        modelBuilder.Entity<CapabilityMappingEntity>(entity =>
        {
            entity.ToTable("CapabilityMappings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ApplicationName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Capability).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.ApplicationName).IsUnique();
            entity.HasIndex(e => e.Capability);
        });

        // AuditLog
        modelBuilder.Entity<AuditLogEntity>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Category).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(100);
            entity.Property(e => e.UserName).HasMaxLength(200);
            entity.Property(e => e.EntityId).HasMaxLength(100);
            entity.Property(e => e.EntityType).HasMaxLength(100);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.EntityId);
        });

        // SyncJob
        modelBuilder.Entity<SyncJobEntity>(entity =>
        {
            entity.ToTable("SyncJobs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DataSource).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.Property(e => e.TriggeredBy).HasMaxLength(200);
            entity.HasIndex(e => e.DataSource);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.StartTime);
        });

        // DiscoveredSharePointFolder
        modelBuilder.Entity<DiscoveredSharePointFolderEntity>(entity =>
        {
            entity.ToTable("SharePointFolders");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.FullPath).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.Url).HasMaxLength(500);
            entity.Property(e => e.Capability).HasMaxLength(100);
            entity.Property(e => e.LinkedServiceNowAppName).HasMaxLength(200);
            entity.Property(e => e.LinkedApplicationId).HasMaxLength(100);
            entity.HasIndex(e => e.FullPath).IsUnique();
            entity.HasIndex(e => e.Capability);
        });

        // TaskDocumentation
        modelBuilder.Entity<TaskDocumentationEntity>(entity =>
        {
            entity.ToTable("TaskDocumentation");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TaskType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(4000).IsRequired();
            entity.Property(e => e.LastUpdatedBy).HasMaxLength(200);
            entity.HasIndex(e => e.TaskType).IsUnique();
        });

        // SyncedRepository (Azure DevOps sync results)
        modelBuilder.Entity<SyncedRepositoryEntity>(entity =>
        {
            entity.ToTable("SyncedRepositories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Url).HasMaxLength(500).IsRequired();
            entity.Property(e => e.CloneUrl).HasMaxLength(500);
            entity.Property(e => e.DefaultBranch).HasMaxLength(100);
            entity.Property(e => e.ProjectName).HasMaxLength(200);
            entity.Property(e => e.SyncedBy).HasMaxLength(200);
            entity.Property(e => e.PrimaryStack).HasMaxLength(100);
            entity.Property(e => e.TargetFramework).HasMaxLength(100);
            entity.Property(e => e.DetectedPattern).HasMaxLength(500);
            entity.Property(e => e.LastBuildStatus).HasMaxLength(50);
            entity.Property(e => e.LastBuildResult).HasMaxLength(50);
            entity.Property(e => e.LinkedApplicationId).HasMaxLength(100);
            entity.Property(e => e.LinkedApplicationName).HasMaxLength(200);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.PrimaryStack);
            entity.HasIndex(e => e.LinkedApplicationId);
            entity.HasIndex(e => e.SyncedAt);
        });

        // ImportedServiceNowApplication (ServiceNow CSV import results)
        modelBuilder.Entity<ImportedServiceNowApplicationEntity>(entity =>
        {
            entity.ToTable("ImportedServiceNowApplications");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ServiceNowId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(4000);
            entity.Property(e => e.ShortDescription).HasMaxLength(500);
            entity.Property(e => e.Capability).HasMaxLength(100);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.OwnerId).HasMaxLength(100);
            entity.Property(e => e.OwnerName).HasMaxLength(200);
            entity.Property(e => e.ProductManagerId).HasMaxLength(100);
            entity.Property(e => e.ProductManagerName).HasMaxLength(200);
            entity.Property(e => e.BusinessOwnerId).HasMaxLength(100);
            entity.Property(e => e.BusinessOwnerName).HasMaxLength(200);
            entity.Property(e => e.FunctionalArchitectId).HasMaxLength(100);
            entity.Property(e => e.FunctionalArchitectName).HasMaxLength(200);
            entity.Property(e => e.TechnicalArchitectId).HasMaxLength(100);
            entity.Property(e => e.TechnicalArchitectName).HasMaxLength(200);
            entity.Property(e => e.TechnicalLeadId).HasMaxLength(100);
            entity.Property(e => e.TechnicalLeadName).HasMaxLength(200);
            entity.Property(e => e.ApplicationType).HasMaxLength(50);
            entity.Property(e => e.ArchitectureType).HasMaxLength(100);
            entity.Property(e => e.UserBase).HasMaxLength(100);
            entity.Property(e => e.Importance).HasMaxLength(50);
            entity.Property(e => e.RepositoryUrl).HasMaxLength(500);
            entity.Property(e => e.DocumentationUrl).HasMaxLength(500);
            entity.Property(e => e.Environment).HasMaxLength(100);
            entity.Property(e => e.Criticality).HasMaxLength(50);
            entity.Property(e => e.SupportGroup).HasMaxLength(200);
            entity.Property(e => e.LinkedRepositoryId).HasMaxLength(100);
            entity.Property(e => e.LinkedRepositoryName).HasMaxLength(200);
            entity.HasIndex(e => e.ServiceNowId).IsUnique();
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.Capability);
            entity.HasIndex(e => e.ImportedAt);
        });

        // FrameworkVersion
        modelBuilder.Entity<FrameworkVersionEntity>(entity =>
        {
            entity.ToTable("FrameworkVersions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Framework).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Version).HasMaxLength(50).IsRequired();
            entity.Property(e => e.DisplayName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.Property(e => e.LatestPatchVersion).HasMaxLength(50);
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.Property(e => e.RecommendedUpgradePath).HasMaxLength(100);
            entity.Property(e => e.TargetFrameworkMoniker).HasMaxLength(50);
            entity.HasIndex(e => e.Framework);
            entity.HasIndex(e => e.Version);
            entity.HasIndex(e => new { e.Framework, e.Version }).IsUnique();
            entity.HasIndex(e => e.TargetFrameworkMoniker);
        });

        // ServiceNow Incidents
        modelBuilder.Entity<ServiceNowIncidentEntity>(entity =>
        {
            entity.ToTable("ServiceNowIncidents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.IncidentNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.State).HasMaxLength(50);
            entity.Property(e => e.ConfigurationItem).HasMaxLength(200);
            entity.Property(e => e.ShortDescription).HasMaxLength(500);
            entity.Property(e => e.CloseCode).HasMaxLength(100);
            entity.Property(e => e.LinkStatus).HasMaxLength(50);
            entity.Property(e => e.LinkStatusNotes).HasMaxLength(500);
            entity.Property(e => e.LinkedApplicationId).HasMaxLength(100);
            entity.Property(e => e.LinkedApplicationName).HasMaxLength(200);
            entity.HasIndex(e => e.IncidentNumber).IsUnique();
            entity.HasIndex(e => e.ConfigurationItem);
            entity.HasIndex(e => e.LinkedApplicationId);
            entity.HasIndex(e => e.CloseCode);
            entity.HasIndex(e => e.State);
        });
    }
}
