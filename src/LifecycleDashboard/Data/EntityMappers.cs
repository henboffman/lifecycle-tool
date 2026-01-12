using System.Text.Json;
using LifecycleDashboard.Data.Entities;
using LifecycleDashboard.Models;
using LifecycleDashboard.Services;

namespace LifecycleDashboard.Data;

/// <summary>
/// Maps between database entities and domain models.
/// </summary>
public static class EntityMappers
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    #region Application

    public static Application ToModel(this ApplicationEntity entity)
    {
        return new Application
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            ShortDescription = entity.ShortDescription,
            Capability = entity.Capability,
            ApplicationType = Enum.TryParse<AppType>(entity.ApplicationType, out var appType) ? appType : AppType.Unknown,
            ArchitectureType = Enum.TryParse<ArchitectureType>(entity.ArchitectureType, out var archType) ? archType : ArchitectureType.Unknown,
            UserBaseEstimate = entity.UserBaseEstimate,
            Importance = entity.Importance,
            RepositoryUrl = entity.RepositoryUrl,
            DocumentationUrl = entity.DocumentationUrl,
            ServiceNowId = entity.ServiceNowId,
            IsMockData = entity.IsMockData,
            HealthScore = entity.HealthScore,
            LastActivityDate = entity.LastActivityDate,
            LastSyncDate = entity.LastSyncDate,
            HasDataConflicts = entity.HasDataConflicts,
            TechnologyStack = JsonSerializer.Deserialize<List<string>>(entity.TechnologyStackJson, JsonOptions) ?? [],
            Tags = JsonSerializer.Deserialize<List<string>>(entity.TagsJson, JsonOptions) ?? [],
            SecurityFindings = JsonSerializer.Deserialize<List<SecurityFinding>>(entity.SecurityFindingsJson, JsonOptions) ?? [],
            RoleAssignments = JsonSerializer.Deserialize<List<RoleAssignment>>(entity.RoleAssignmentsJson, JsonOptions) ?? [],
            Usage = string.IsNullOrEmpty(entity.UsageJson) ? null : JsonSerializer.Deserialize<UsageMetrics>(entity.UsageJson, JsonOptions),
            Documentation = JsonSerializer.Deserialize<DocumentationStatus>(entity.DocumentationJson, JsonOptions) ?? new(),
            DataConflicts = JsonSerializer.Deserialize<List<string>>(entity.DataConflictsJson, JsonOptions) ?? [],
            SecurityReview = string.IsNullOrEmpty(entity.SecurityReviewJson) ? null : JsonSerializer.Deserialize<SecurityReview>(entity.SecurityReviewJson, JsonOptions),
            UpdateHistory = JsonSerializer.Deserialize<List<DataUpdateRecord>>(entity.UpdateHistoryJson, JsonOptions) ?? [],
            UsageAvailability = JsonSerializer.Deserialize<UsageDataAvailability>(entity.UsageAvailabilityJson, JsonOptions) ?? new(),
            CriticalPeriods = JsonSerializer.Deserialize<List<CriticalPeriod>>(entity.CriticalPeriodsJson, JsonOptions) ?? [],
            KeyDates = JsonSerializer.Deserialize<List<KeyDate>>(entity.KeyDatesJson, JsonOptions) ?? []
        };
    }

    public static ApplicationEntity ToEntity(this Application model, ApplicationEntity? existing = null)
    {
        var entity = existing ?? new ApplicationEntity { Id = model.Id, CreatedAt = DateTimeOffset.UtcNow };
        entity.Name = model.Name;
        entity.Description = model.Description;
        entity.ShortDescription = model.ShortDescription;
        entity.Capability = model.Capability;
        entity.ApplicationType = model.ApplicationType.ToString();
        entity.ArchitectureType = model.ArchitectureType.ToString();
        entity.UserBaseEstimate = model.UserBaseEstimate;
        entity.Importance = model.Importance;
        entity.RepositoryUrl = model.RepositoryUrl;
        entity.DocumentationUrl = model.DocumentationUrl;
        entity.ServiceNowId = model.ServiceNowId;
        entity.IsMockData = model.IsMockData;
        entity.HealthScore = model.HealthScore;
        entity.LastActivityDate = model.LastActivityDate;
        entity.LastSyncDate = model.LastSyncDate;
        entity.HasDataConflicts = model.HasDataConflicts;
        entity.TechnologyStackJson = JsonSerializer.Serialize(model.TechnologyStack, JsonOptions);
        entity.TagsJson = JsonSerializer.Serialize(model.Tags, JsonOptions);
        entity.SecurityFindingsJson = JsonSerializer.Serialize(model.SecurityFindings, JsonOptions);
        entity.RoleAssignmentsJson = JsonSerializer.Serialize(model.RoleAssignments, JsonOptions);
        entity.UsageJson = model.Usage != null ? JsonSerializer.Serialize(model.Usage, JsonOptions) : null;
        entity.DocumentationJson = JsonSerializer.Serialize(model.Documentation, JsonOptions);
        entity.DataConflictsJson = JsonSerializer.Serialize(model.DataConflicts, JsonOptions);
        entity.SecurityReviewJson = model.SecurityReview != null ? JsonSerializer.Serialize(model.SecurityReview, JsonOptions) : null;
        entity.UpdateHistoryJson = JsonSerializer.Serialize(model.UpdateHistory, JsonOptions);
        entity.UsageAvailabilityJson = JsonSerializer.Serialize(model.UsageAvailability, JsonOptions);
        entity.CriticalPeriodsJson = JsonSerializer.Serialize(model.CriticalPeriods, JsonOptions);
        entity.KeyDatesJson = JsonSerializer.Serialize(model.KeyDates, JsonOptions);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        return entity;
    }

    #endregion

    #region LifecycleTask

    public static LifecycleTask ToModel(this LifecycleTaskEntity entity)
    {
        return new LifecycleTask
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            Type = entity.Type,
            Priority = entity.Priority,
            Status = entity.Status,
            ApplicationId = entity.ApplicationId,
            ApplicationName = entity.ApplicationName,
            AssigneeId = entity.AssigneeId,
            AssigneeName = entity.AssigneeName,
            AssigneeEmail = entity.AssigneeEmail,
            DueDate = entity.DueDate,
            CreatedDate = entity.CreatedDate,
            CompletedDate = entity.CompletedDate,
            Notes = entity.Notes,
            IsEscalated = entity.IsEscalated,
            EscalatedDate = entity.EscalatedDate,
            OriginalAssigneeId = entity.OriginalAssigneeId,
            DelegationReason = entity.DelegationReason,
            History = JsonSerializer.Deserialize<List<TaskHistoryEntry>>(entity.HistoryJson, JsonOptions) ?? []
        };
    }

    public static LifecycleTaskEntity ToEntity(this LifecycleTask model, LifecycleTaskEntity? existing = null)
    {
        var entity = existing ?? new LifecycleTaskEntity { Id = model.Id, CreatedAt = DateTimeOffset.UtcNow };
        entity.Title = model.Title;
        entity.Description = model.Description;
        entity.Type = model.Type;
        entity.Priority = model.Priority;
        entity.Status = model.Status;
        entity.ApplicationId = model.ApplicationId;
        entity.ApplicationName = model.ApplicationName;
        entity.AssigneeId = model.AssigneeId;
        entity.AssigneeName = model.AssigneeName;
        entity.AssigneeEmail = model.AssigneeEmail;
        entity.DueDate = model.DueDate;
        entity.CreatedDate = model.CreatedDate;
        entity.CompletedDate = model.CompletedDate;
        entity.Notes = model.Notes;
        entity.IsEscalated = model.IsEscalated;
        entity.EscalatedDate = model.EscalatedDate;
        entity.OriginalAssigneeId = model.OriginalAssigneeId;
        entity.DelegationReason = model.DelegationReason;
        entity.HistoryJson = JsonSerializer.Serialize(model.History, JsonOptions);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        return entity;
    }

    #endregion

    #region User

    public static User ToModel(this UserEntity entity)
    {
        return new User
        {
            Id = entity.Id,
            Name = entity.Name,
            Email = entity.Email,
            Department = entity.Department,
            Title = entity.Title,
            Role = entity.Role,
            IsActive = entity.IsActive,
            LastLoginDate = entity.LastLoginDate,
            Preferences = JsonSerializer.Deserialize<UserPreferences>(entity.PreferencesJson, JsonOptions) ?? new()
        };
    }

    public static UserEntity ToEntity(this User model, UserEntity? existing = null)
    {
        var entity = existing ?? new UserEntity { Id = model.Id, CreatedAt = DateTimeOffset.UtcNow };
        entity.Name = model.Name;
        entity.Email = model.Email;
        entity.Department = model.Department;
        entity.Title = model.Title;
        entity.Role = model.Role;
        entity.IsActive = model.IsActive;
        entity.LastLoginDate = model.LastLoginDate;
        entity.PreferencesJson = JsonSerializer.Serialize(model.Preferences, JsonOptions);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        return entity;
    }

    #endregion

    #region RepositoryInfo

    public static RepositoryInfo ToModel(this RepositoryInfoEntity entity)
    {
        return new RepositoryInfo
        {
            RepositoryId = entity.RepositoryId,
            Name = entity.Name,
            DefaultBranch = entity.DefaultBranch,
            Url = entity.Url,
            Packages = JsonSerializer.Deserialize<List<Models.PackageReference>>(entity.PackagesJson, JsonOptions) ?? [],
            Stack = JsonSerializer.Deserialize<TechnologyStackInfo>(entity.StackJson, JsonOptions) ?? new TechnologyStackInfo { PrimaryStack = StackType.Unknown },
            Commits = JsonSerializer.Deserialize<Models.CommitHistory>(entity.CommitsJson, JsonOptions) ?? new Models.CommitHistory(),
            Readme = JsonSerializer.Deserialize<Models.ReadmeStatus>(entity.ReadmeJson, JsonOptions) ?? new Models.ReadmeStatus(),
            HasApplicationInsights = entity.HasApplicationInsights,
            ApplicationInsightsKey = entity.ApplicationInsightsKey,
            SystemDependencies = JsonSerializer.Deserialize<List<Models.SystemDependency>>(entity.SystemDependenciesJson, JsonOptions) ?? [],
            LastBuildDate = entity.LastBuildDate,
            LastBuildStatus = entity.LastBuildStatus,
            LastSyncDate = entity.LastSyncDate
        };
    }

    public static RepositoryInfoEntity ToEntity(this RepositoryInfo model, RepositoryInfoEntity? existing = null)
    {
        var entity = existing ?? new RepositoryInfoEntity { Id = Guid.NewGuid().ToString(), CreatedAt = DateTimeOffset.UtcNow };
        entity.RepositoryId = model.RepositoryId;
        entity.Name = model.Name;
        entity.DefaultBranch = model.DefaultBranch;
        entity.Url = model.Url;
        entity.PackagesJson = JsonSerializer.Serialize(model.Packages, JsonOptions);
        entity.StackJson = JsonSerializer.Serialize(model.Stack, JsonOptions);
        entity.CommitsJson = JsonSerializer.Serialize(model.Commits, JsonOptions);
        entity.ReadmeJson = JsonSerializer.Serialize(model.Readme, JsonOptions);
        entity.HasApplicationInsights = model.HasApplicationInsights;
        entity.ApplicationInsightsKey = model.ApplicationInsightsKey;
        entity.SystemDependenciesJson = JsonSerializer.Serialize(model.SystemDependencies, JsonOptions);
        entity.LastBuildDate = model.LastBuildDate;
        entity.LastBuildStatus = model.LastBuildStatus;
        entity.LastSyncDate = model.LastSyncDate;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        return entity;
    }

    #endregion

    #region AppNameMapping

    public static AppNameMapping ToModel(this AppNameMappingEntity entity)
    {
        return new AppNameMapping
        {
            Id = entity.Id,
            ServiceNowAppName = entity.ServiceNowAppName,
            SharePointFolderName = entity.SharePointFolderName,
            Capability = entity.Capability,
            Notes = entity.Notes,
            AzureDevOpsRepoNames = JsonSerializer.Deserialize<List<string>>(entity.AzureDevOpsRepoNamesJson, JsonOptions) ?? [],
            AlternativeNames = JsonSerializer.Deserialize<List<string>>(entity.AlternativeNamesJson, JsonOptions) ?? [],
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public static AppNameMappingEntity ToEntity(this AppNameMapping model, AppNameMappingEntity? existing = null)
    {
        var entity = existing ?? new AppNameMappingEntity { Id = model.Id, CreatedAt = DateTimeOffset.UtcNow };
        entity.ServiceNowAppName = model.ServiceNowAppName;
        entity.SharePointFolderName = model.SharePointFolderName;
        entity.Capability = model.Capability;
        entity.Notes = model.Notes;
        entity.AzureDevOpsRepoNamesJson = JsonSerializer.Serialize(model.AzureDevOpsRepoNames, JsonOptions);
        entity.AlternativeNamesJson = JsonSerializer.Serialize(model.AlternativeNames, JsonOptions);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        return entity;
    }

    #endregion

    #region CapabilityMapping

    public static CapabilityMapping ToModel(this CapabilityMappingEntity entity)
    {
        return new CapabilityMapping
        {
            Id = entity.Id,
            ApplicationName = entity.ApplicationName,
            Capability = entity.Capability,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    public static CapabilityMappingEntity ToEntity(this CapabilityMapping model, CapabilityMappingEntity? existing = null)
    {
        var entity = existing ?? new CapabilityMappingEntity { Id = model.Id, CreatedAt = DateTimeOffset.UtcNow };
        entity.ApplicationName = model.ApplicationName;
        entity.Capability = model.Capability;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        return entity;
    }

    #endregion

    #region AuditLog

    public static AuditLogEntry ToModel(this AuditLogEntity entity)
    {
        return new AuditLogEntry
        {
            Id = entity.Id,
            Timestamp = entity.Timestamp,
            EventType = entity.EventType,
            Category = entity.Category,
            Message = entity.Message,
            UserId = entity.UserId ?? "system",
            UserName = entity.UserName ?? "System",
            EntityId = entity.EntityId,
            EntityType = entity.EntityType,
            Details = string.IsNullOrEmpty(entity.DetailsJson)
                ? null
                : JsonSerializer.Deserialize<Dictionary<string, string>>(entity.DetailsJson, JsonOptions)
        };
    }

    public static AuditLogEntity ToEntity(this AuditLogEntry model)
    {
        return new AuditLogEntity
        {
            Id = model.Id,
            Timestamp = model.Timestamp,
            EventType = model.EventType,
            Category = model.Category,
            Message = model.Message,
            UserId = model.UserId,
            UserName = model.UserName,
            EntityId = model.EntityId,
            EntityType = model.EntityType,
            DetailsJson = model.Details != null
                ? JsonSerializer.Serialize(model.Details, JsonOptions)
                : null
        };
    }

    #endregion

    #region SyncJob

    public static Services.DataIntegration.SyncJobInfo ToModel(this SyncJobEntity entity)
    {
        return new Services.DataIntegration.SyncJobInfo
        {
            Id = entity.Id,
            DataSource = Enum.Parse<Services.DataIntegration.DataSourceType>(entity.DataSource),
            Status = Enum.Parse<Services.DataIntegration.SyncJobStatus>(entity.Status),
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            RecordsProcessed = entity.RecordsProcessed,
            RecordsCreated = entity.RecordsCreated,
            RecordsUpdated = entity.RecordsUpdated,
            ErrorCount = entity.ErrorCount,
            ErrorMessage = entity.ErrorMessage,
            TriggeredBy = entity.TriggeredBy
        };
    }

    public static SyncJobEntity ToEntity(this Services.DataIntegration.SyncJobInfo model, SyncJobEntity? existing = null)
    {
        var entity = existing ?? new SyncJobEntity { Id = model.Id };
        entity.DataSource = model.DataSource.ToString();
        entity.Status = model.Status.ToString();
        entity.StartTime = model.StartTime;
        entity.EndTime = model.EndTime;
        entity.RecordsProcessed = model.RecordsProcessed;
        entity.RecordsCreated = model.RecordsCreated;
        entity.RecordsUpdated = model.RecordsUpdated;
        entity.ErrorCount = model.ErrorCount;
        entity.ErrorMessage = model.ErrorMessage;
        entity.TriggeredBy = model.TriggeredBy;
        return entity;
    }

    #endregion

    #region SharePointFolder

    public static DiscoveredSharePointFolder ToModel(this DiscoveredSharePointFolderEntity entity)
    {
        return new DiscoveredSharePointFolder
        {
            Id = entity.Id,
            Name = entity.Name,
            FullPath = entity.FullPath,
            Url = entity.Url,
            Capability = entity.Capability,
            TemplateFoldersFound = JsonSerializer.Deserialize<List<string>>(entity.TemplateFoldersFoundJson, JsonOptions) ?? [],
            DocumentCounts = JsonSerializer.Deserialize<Dictionary<string, int>>(entity.DocumentCountsJson, JsonOptions) ?? [],
            LastModified = entity.LastModified,
            SyncedAt = entity.SyncedAt,
            LinkedServiceNowAppName = entity.LinkedServiceNowAppName,
            LinkedApplicationId = entity.LinkedApplicationId
        };
    }

    public static DiscoveredSharePointFolderEntity ToEntity(this DiscoveredSharePointFolder model, DiscoveredSharePointFolderEntity? existing = null)
    {
        var entity = existing ?? new DiscoveredSharePointFolderEntity { Id = model.Id };
        entity.Name = model.Name;
        entity.FullPath = model.FullPath;
        entity.Url = model.Url;
        entity.Capability = model.Capability;
        entity.TemplateFoldersFoundJson = JsonSerializer.Serialize(model.TemplateFoldersFound, JsonOptions);
        entity.DocumentCountsJson = JsonSerializer.Serialize(model.DocumentCounts, JsonOptions);
        entity.LastModified = model.LastModified;
        entity.SyncedAt = model.SyncedAt;
        entity.LinkedServiceNowAppName = model.LinkedServiceNowAppName;
        entity.LinkedApplicationId = model.LinkedApplicationId;
        return entity;
    }

    #endregion

    #region TaskDocumentation

    public static TaskDocumentation ToModel(this TaskDocumentationEntity entity)
    {
        return new TaskDocumentation
        {
            Id = entity.Id,
            TaskType = Enum.Parse<TaskType>(entity.TaskType),
            Title = entity.Title,
            Description = entity.Description,
            Instructions = JsonSerializer.Deserialize<List<TaskInstruction>>(entity.InstructionsJson, JsonOptions) ?? [],
            SystemGuidance = JsonSerializer.Deserialize<List<SystemGuidance>>(entity.SystemGuidanceJson, JsonOptions) ?? [],
            RelatedLinks = JsonSerializer.Deserialize<List<DocumentationLink>>(entity.RelatedLinksJson, JsonOptions) ?? [],
            EstimatedDuration = entity.EstimatedDurationTicks.HasValue ? TimeSpan.FromTicks(entity.EstimatedDurationTicks.Value) : null,
            Prerequisites = JsonSerializer.Deserialize<List<string>>(entity.PrerequisitesJson, JsonOptions) ?? [],
            TypicalRoles = JsonSerializer.Deserialize<List<ApplicationRole>>(entity.TypicalRolesJson, JsonOptions) ?? [],
            LastUpdated = entity.LastUpdated,
            LastUpdatedBy = entity.LastUpdatedBy
        };
    }

    public static TaskDocumentationEntity ToEntity(this TaskDocumentation model, TaskDocumentationEntity? existing = null)
    {
        var entity = existing ?? new TaskDocumentationEntity { Id = model.Id };
        entity.TaskType = model.TaskType.ToString();
        entity.Title = model.Title;
        entity.Description = model.Description;
        entity.InstructionsJson = JsonSerializer.Serialize(model.Instructions, JsonOptions);
        entity.SystemGuidanceJson = JsonSerializer.Serialize(model.SystemGuidance, JsonOptions);
        entity.RelatedLinksJson = JsonSerializer.Serialize(model.RelatedLinks, JsonOptions);
        entity.EstimatedDurationTicks = model.EstimatedDuration?.Ticks;
        entity.PrerequisitesJson = JsonSerializer.Serialize(model.Prerequisites, JsonOptions);
        entity.TypicalRolesJson = JsonSerializer.Serialize(model.TypicalRoles, JsonOptions);
        entity.LastUpdated = model.LastUpdated;
        entity.LastUpdatedBy = model.LastUpdatedBy;
        return entity;
    }

    #endregion

    #region SyncedRepository

    public static SyncedRepository ToModel(this SyncedRepositoryEntity entity)
    {
        return new SyncedRepository
        {
            Id = entity.Id,
            Name = entity.Name,
            Url = entity.Url,
            CloneUrl = entity.CloneUrl,
            DefaultBranch = entity.DefaultBranch,
            ProjectName = entity.ProjectName,
            SizeBytes = entity.SizeBytes,
            IsDisabled = entity.IsDisabled,
            SyncedAt = entity.SyncedAt,
            SyncedBy = entity.SyncedBy,
            PrimaryStack = entity.PrimaryStack,
            Frameworks = JsonSerializer.Deserialize<List<string>>(entity.FrameworksJson, JsonOptions) ?? [],
            Languages = JsonSerializer.Deserialize<List<string>>(entity.LanguagesJson, JsonOptions) ?? [],
            TargetFramework = entity.TargetFramework,
            DetectedPattern = entity.DetectedPattern,
            TotalCommits = entity.TotalCommits,
            LastCommitDate = entity.LastCommitDate,
            Contributors = JsonSerializer.Deserialize<List<string>>(entity.ContributorsJson, JsonOptions) ?? [],
            NuGetPackageCount = entity.NuGetPackageCount,
            NpmPackageCount = entity.NpmPackageCount,
            Packages = JsonSerializer.Deserialize<List<SyncedPackageReference>>(entity.PackagesJson, JsonOptions) ?? [],
            LastBuildStatus = entity.LastBuildStatus,
            LastBuildResult = entity.LastBuildResult,
            LastBuildDate = entity.LastBuildDate,
            HasReadme = entity.HasReadme,
            ReadmeQualityScore = entity.ReadmeQualityScore,
            AdvancedSecurityEnabled = entity.AdvancedSecurityEnabled,
            LastSecurityScanDate = entity.LastSecurityScanDate,
            OpenCriticalVulnerabilities = entity.OpenCriticalVulnerabilities,
            OpenHighVulnerabilities = entity.OpenHighVulnerabilities,
            OpenMediumVulnerabilities = entity.OpenMediumVulnerabilities,
            OpenLowVulnerabilities = entity.OpenLowVulnerabilities,
            ClosedCriticalVulnerabilities = entity.ClosedCriticalVulnerabilities,
            ClosedHighVulnerabilities = entity.ClosedHighVulnerabilities,
            ClosedMediumVulnerabilities = entity.ClosedMediumVulnerabilities,
            ClosedLowVulnerabilities = entity.ClosedLowVulnerabilities,
            ExposedSecretsCount = entity.ExposedSecretsCount,
            DependencyAlertCount = entity.DependencyAlertCount,
            LinkedApplicationId = entity.LinkedApplicationId,
            LinkedApplicationName = entity.LinkedApplicationName
        };
    }

    public static SyncedRepositoryEntity ToEntity(this SyncedRepository model, SyncedRepositoryEntity? existing = null)
    {
        var entity = existing ?? new SyncedRepositoryEntity { Id = model.Id, CreatedAt = DateTimeOffset.UtcNow };
        entity.Name = model.Name;
        entity.Url = model.Url;
        entity.CloneUrl = model.CloneUrl;
        entity.DefaultBranch = model.DefaultBranch;
        entity.ProjectName = model.ProjectName;
        entity.SizeBytes = model.SizeBytes;
        entity.IsDisabled = model.IsDisabled;
        entity.SyncedAt = model.SyncedAt;
        entity.SyncedBy = model.SyncedBy;
        entity.PrimaryStack = model.PrimaryStack;
        entity.FrameworksJson = JsonSerializer.Serialize(model.Frameworks, JsonOptions);
        entity.LanguagesJson = JsonSerializer.Serialize(model.Languages, JsonOptions);
        entity.TargetFramework = model.TargetFramework;
        entity.DetectedPattern = model.DetectedPattern;
        entity.TotalCommits = model.TotalCommits;
        entity.LastCommitDate = model.LastCommitDate;
        entity.ContributorsJson = JsonSerializer.Serialize(model.Contributors, JsonOptions);
        entity.NuGetPackageCount = model.NuGetPackageCount;
        entity.NpmPackageCount = model.NpmPackageCount;
        entity.PackagesJson = JsonSerializer.Serialize(model.Packages, JsonOptions);
        entity.LastBuildStatus = model.LastBuildStatus;
        entity.LastBuildResult = model.LastBuildResult;
        entity.LastBuildDate = model.LastBuildDate;
        entity.HasReadme = model.HasReadme;
        entity.ReadmeQualityScore = model.ReadmeQualityScore;
        entity.AdvancedSecurityEnabled = model.AdvancedSecurityEnabled;
        entity.LastSecurityScanDate = model.LastSecurityScanDate;
        entity.OpenCriticalVulnerabilities = model.OpenCriticalVulnerabilities;
        entity.OpenHighVulnerabilities = model.OpenHighVulnerabilities;
        entity.OpenMediumVulnerabilities = model.OpenMediumVulnerabilities;
        entity.OpenLowVulnerabilities = model.OpenLowVulnerabilities;
        entity.ClosedCriticalVulnerabilities = model.ClosedCriticalVulnerabilities;
        entity.ClosedHighVulnerabilities = model.ClosedHighVulnerabilities;
        entity.ClosedMediumVulnerabilities = model.ClosedMediumVulnerabilities;
        entity.ClosedLowVulnerabilities = model.ClosedLowVulnerabilities;
        entity.ExposedSecretsCount = model.ExposedSecretsCount;
        entity.DependencyAlertCount = model.DependencyAlertCount;
        entity.LinkedApplicationId = model.LinkedApplicationId;
        entity.LinkedApplicationName = model.LinkedApplicationName;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        return entity;
    }

    #endregion

    #region ImportedServiceNowApplication

    public static ImportedServiceNowApplication ToModel(this ImportedServiceNowApplicationEntity entity)
    {
        return new ImportedServiceNowApplication
        {
            Id = entity.Id,
            ServiceNowId = entity.ServiceNowId,
            Name = entity.Name,
            Description = entity.Description,
            ShortDescription = entity.ShortDescription,
            Capability = entity.Capability,
            Status = entity.Status,
            OwnerId = entity.OwnerId,
            OwnerName = entity.OwnerName,
            ProductManagerId = entity.ProductManagerId,
            ProductManagerName = entity.ProductManagerName,
            BusinessOwnerId = entity.BusinessOwnerId,
            BusinessOwnerName = entity.BusinessOwnerName,
            FunctionalArchitectId = entity.FunctionalArchitectId,
            FunctionalArchitectName = entity.FunctionalArchitectName,
            TechnicalArchitectId = entity.TechnicalArchitectId,
            TechnicalArchitectName = entity.TechnicalArchitectName,
            TechnicalLeadId = entity.TechnicalLeadId,
            TechnicalLeadName = entity.TechnicalLeadName,
            ApplicationType = entity.ApplicationType,
            ArchitectureType = entity.ArchitectureType,
            UserBase = entity.UserBase,
            Importance = entity.Importance,
            RepositoryUrl = entity.RepositoryUrl,
            DocumentationUrl = entity.DocumentationUrl,
            Environment = entity.Environment,
            Criticality = entity.Criticality,
            SupportGroup = entity.SupportGroup,
            ImportedAt = entity.ImportedAt,
            RawCsvValues = JsonSerializer.Deserialize<Dictionary<string, string>>(entity.RawCsvValuesJson, JsonOptions) ?? [],
            LinkedRepositoryId = entity.LinkedRepositoryId,
            LinkedRepositoryName = entity.LinkedRepositoryName
        };
    }

    public static ImportedServiceNowApplicationEntity ToEntity(this ImportedServiceNowApplication model, ImportedServiceNowApplicationEntity? existing = null)
    {
        var entity = existing ?? new ImportedServiceNowApplicationEntity { Id = model.Id, CreatedAt = DateTimeOffset.UtcNow };
        entity.ServiceNowId = model.ServiceNowId;
        entity.Name = model.Name;
        entity.Description = model.Description;
        entity.ShortDescription = model.ShortDescription;
        entity.Capability = model.Capability;
        entity.Status = model.Status;
        entity.OwnerId = model.OwnerId;
        entity.OwnerName = model.OwnerName;
        entity.ProductManagerId = model.ProductManagerId;
        entity.ProductManagerName = model.ProductManagerName;
        entity.BusinessOwnerId = model.BusinessOwnerId;
        entity.BusinessOwnerName = model.BusinessOwnerName;
        entity.FunctionalArchitectId = model.FunctionalArchitectId;
        entity.FunctionalArchitectName = model.FunctionalArchitectName;
        entity.TechnicalArchitectId = model.TechnicalArchitectId;
        entity.TechnicalArchitectName = model.TechnicalArchitectName;
        entity.TechnicalLeadId = model.TechnicalLeadId;
        entity.TechnicalLeadName = model.TechnicalLeadName;
        entity.ApplicationType = model.ApplicationType;
        entity.ArchitectureType = model.ArchitectureType;
        entity.UserBase = model.UserBase;
        entity.Importance = model.Importance;
        entity.RepositoryUrl = model.RepositoryUrl;
        entity.DocumentationUrl = model.DocumentationUrl;
        entity.Environment = model.Environment;
        entity.Criticality = model.Criticality;
        entity.SupportGroup = model.SupportGroup;
        entity.ImportedAt = model.ImportedAt;
        entity.RawCsvValuesJson = JsonSerializer.Serialize(model.RawCsvValues, JsonOptions);
        entity.LinkedRepositoryId = model.LinkedRepositoryId;
        entity.LinkedRepositoryName = model.LinkedRepositoryName;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        return entity;
    }

    #endregion
}
