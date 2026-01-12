namespace LifecycleDashboard.Models;

/// <summary>
/// Represents an application in the portfolio.
/// </summary>
public record Application
{
    /// <summary>
    /// Unique identifier for the application.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Display name of the application.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Optional description of the application.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Short description from ServiceNow (brief summary).
    /// </summary>
    public string? ShortDescription { get; init; }

    /// <summary>
    /// Capability/business area this application belongs to.
    /// </summary>
    public required string Capability { get; init; }

    /// <summary>
    /// Application type classification (COTS, Homegrown, etc.).
    /// </summary>
    public AppType ApplicationType { get; init; } = AppType.Unknown;

    /// <summary>
    /// Architecture type (Web Based, Client Server, Desktop App, etc.).
    /// </summary>
    public ArchitectureType ArchitectureType { get; init; } = ArchitectureType.Unknown;

    /// <summary>
    /// User base estimate from ServiceNow (used as initial usage estimate).
    /// </summary>
    public string? UserBaseEstimate { get; init; }

    /// <summary>
    /// Importance value from ServiceNow.
    /// </summary>
    public string? Importance { get; init; }

    /// <summary>
    /// Link to the Azure DevOps repository.
    /// </summary>
    public string? RepositoryUrl { get; init; }

    /// <summary>
    /// Link to SharePoint documentation folder.
    /// </summary>
    public string? DocumentationUrl { get; init; }

    /// <summary>
    /// ServiceNow configuration item ID.
    /// </summary>
    public string? ServiceNowId { get; init; }

    /// <summary>
    /// Indicates if this is mock/seed data (vs real imported data).
    /// </summary>
    public bool IsMockData { get; init; }

    /// <summary>
    /// Current health score (0-100).
    /// </summary>
    public int HealthScore { get; init; }

    /// <summary>
    /// Health category based on score.
    /// </summary>
    public HealthCategory HealthCategory => HealthScore switch
    {
        >= 80 => HealthCategory.Healthy,
        >= 60 => HealthCategory.NeedsAttention,
        >= 40 => HealthCategory.AtRisk,
        _ => HealthCategory.Critical
    };

    /// <summary>
    /// Date of last commit/activity.
    /// </summary>
    public DateTimeOffset? LastActivityDate { get; init; }

    /// <summary>
    /// Date when data was last synced from sources.
    /// </summary>
    public DateTimeOffset LastSyncDate { get; init; }

    /// <summary>
    /// Technology stack (e.g., ".NET", "Node.js", "Python").
    /// </summary>
    public List<string> TechnologyStack { get; init; } = [];

    /// <summary>
    /// Tags for categorization and filtering.
    /// </summary>
    public List<string> Tags { get; init; } = [];

    /// <summary>
    /// Security findings associated with this application.
    /// </summary>
    public List<SecurityFinding> SecurityFindings { get; init; } = [];

    /// <summary>
    /// Role assignments for this application.
    /// </summary>
    public List<RoleAssignment> RoleAssignments { get; init; } = [];

    /// <summary>
    /// Usage metrics for the application.
    /// </summary>
    public UsageMetrics? Usage { get; init; }

    /// <summary>
    /// Documentation completeness status.
    /// </summary>
    public DocumentationStatus Documentation { get; init; } = new();

    /// <summary>
    /// Whether there are data conflicts requiring remediation.
    /// </summary>
    public bool HasDataConflicts { get; init; }

    /// <summary>
    /// Specific data conflict messages if any.
    /// </summary>
    public List<string> DataConflicts { get; init; } = [];

    /// <summary>
    /// Security review status and compliance information.
    /// </summary>
    public SecurityReview? SecurityReview { get; init; }

    /// <summary>
    /// History of data updates made to this application record.
    /// </summary>
    public List<DataUpdateRecord> UpdateHistory { get; init; } = [];

    /// <summary>
    /// Indicates whether usage metrics are available and meaningful for this application.
    /// </summary>
    public UsageDataAvailability UsageAvailability { get; init; } = new();

    /// <summary>
    /// Critical periods during the year when maintenance should be avoided or is preferred.
    /// </summary>
    public List<CriticalPeriod> CriticalPeriods { get; init; } = [];

    /// <summary>
    /// Key upcoming dates that are important for this application.
    /// </summary>
    public List<KeyDate> KeyDates { get; init; } = [];
}

/// <summary>
/// Health category based on health score.
/// </summary>
public enum HealthCategory
{
    Healthy,       // 80-100
    NeedsAttention, // 60-79
    AtRisk,        // 40-59
    Critical       // 0-39
}

/// <summary>
/// Represents a security finding from CodeQL or other scanners.
/// </summary>
public record SecurityFinding
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required SecuritySeverity Severity { get; init; }
    public required string Description { get; init; }
    public string? FilePath { get; init; }
    public int? LineNumber { get; init; }
    public DateTimeOffset DetectedDate { get; init; }
    public DateTimeOffset? ResolvedDate { get; init; }
    public bool IsResolved => ResolvedDate.HasValue;
}

/// <summary>
/// Security finding severity levels.
/// </summary>
public enum SecuritySeverity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Represents a role assignment for an application.
/// </summary>
public record RoleAssignment
{
    /// <summary>Unique identifier for this role assignment.</summary>
    public string? Id { get; init; }

    /// <summary>Application ID this role is assigned to (used for imports).</summary>
    public string? ApplicationId { get; init; }

    public required string UserId { get; init; }
    public required string UserName { get; init; }
    public required string UserEmail { get; init; }
    public required ApplicationRole Role { get; init; }
    public DateTimeOffset AssignedDate { get; init; }
    public DateTimeOffset? LastValidatedDate { get; init; }
    public bool NeedsRevalidation { get; init; }
}

/// <summary>
/// Application role types.
/// </summary>
public enum ApplicationRole
{
    Owner,
    ProductManager,
    BusinessOwner,
    FunctionalArchitect,
    TechnicalArchitect,
    TechnicalLead,
    Developer,
    SecurityChampion,
    Support
}

/// <summary>
/// Application type classification.
/// </summary>
public enum AppType
{
    Unknown,
    COTS,       // Commercial Off-The-Shelf
    Homegrown,  // Custom developed in-house
    Hybrid,     // Mix of COTS and custom
    SaaS,       // Software as a Service
    OpenSource  // Open source solution
}

/// <summary>
/// Architecture type classification.
/// </summary>
public enum ArchitectureType
{
    Unknown,
    WebBased,
    ClientServer,
    DesktopApp,
    MobileApp,
    API,
    BatchProcess,
    Microservices,
    Monolithic,
    Other
}

/// <summary>
/// Usage metrics from IIS logs.
/// </summary>
public record UsageMetrics
{
    /// <summary>
    /// Total requests in the last month.
    /// </summary>
    public int MonthlyRequests { get; init; }

    /// <summary>
    /// Distinct users in the last month.
    /// </summary>
    public int MonthlyUsers { get; init; }

    /// <summary>
    /// Usage sessions (handles SPA scenarios).
    /// </summary>
    public int MonthlySessions { get; init; }

    /// <summary>
    /// Usage trend compared to previous month.
    /// </summary>
    public UsageTrend Trend { get; init; }

    /// <summary>
    /// Calculated usage level for health scoring.
    /// </summary>
    public UsageLevel Level => MonthlyRequests switch
    {
        0 => UsageLevel.None,
        <= 100 => UsageLevel.VeryLow,
        <= 1000 => UsageLevel.Low,
        <= 10000 => UsageLevel.Moderate,
        _ => UsageLevel.High
    };
}

public enum UsageTrend
{
    Increasing,
    Stable,
    Decreasing
}

public enum UsageLevel
{
    None,
    VeryLow,
    Low,
    Moderate,
    High
}

/// <summary>
/// Documentation completeness status.
/// </summary>
public record DocumentationStatus
{
    public bool HasArchitectureDiagram { get; init; }
    public bool HasSystemDocumentation { get; init; }
    public bool HasUserDocumentation { get; init; }
    public bool HasSupportDocumentation { get; init; }

    public bool IsComplete => HasArchitectureDiagram && HasSystemDocumentation;
    public int CompletenessScore => (HasArchitectureDiagram ? 25 : 0)
                                  + (HasSystemDocumentation ? 25 : 0)
                                  + (HasUserDocumentation ? 25 : 0)
                                  + (HasSupportDocumentation ? 25 : 0);
}

/// <summary>
/// Security review status and compliance information for an application.
/// </summary>
public record SecurityReview
{
    /// <summary>
    /// Whether the security review has been completed.
    /// </summary>
    public bool IsCompleted { get; init; }

    /// <summary>
    /// Date when the security review was completed.
    /// </summary>
    public DateTimeOffset? CompletedDate { get; init; }

    /// <summary>
    /// Date when the next review is due (typically annual).
    /// </summary>
    public DateTimeOffset? NextReviewDate { get; init; }

    /// <summary>
    /// Security designation assigned to the application.
    /// </summary>
    public SecurityDesignation Designation { get; init; }

    /// <summary>
    /// Name of the reviewer who completed the review.
    /// </summary>
    public string? ReviewerName { get; init; }

    /// <summary>
    /// Required security controls for this application.
    /// </summary>
    public List<SecurityControl> RequiredControls { get; init; } = [];

    /// <summary>
    /// Compliance expectations for this application.
    /// </summary>
    public List<ComplianceExpectation> Expectations { get; init; } = [];

    /// <summary>
    /// Additional notes or guidance for compliance.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// History of security review updates.
    /// </summary>
    public List<SecurityReviewUpdate> ReviewHistory { get; init; } = [];
}

/// <summary>
/// Security designation levels.
/// </summary>
public enum SecurityDesignation
{
    NotReviewed,
    Public,
    Internal,
    Confidential,
    Restricted,
    HighlyRestricted
}

/// <summary>
/// Required security control for an application.
/// </summary>
public record SecurityControl
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required ControlCategory Category { get; init; }
    public bool IsImplemented { get; init; }
    public string? ImplementationNotes { get; init; }
    public DateTimeOffset? VerifiedDate { get; init; }
}

/// <summary>
/// Security control categories.
/// </summary>
public enum ControlCategory
{
    Authentication,
    Authorization,
    DataProtection,
    Encryption,
    Logging,
    NetworkSecurity,
    VulnerabilityManagement,
    AccessControl,
    IncidentResponse,
    BusinessContinuity
}

/// <summary>
/// Compliance expectation for an application.
/// </summary>
public record ComplianceExpectation
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required ComplianceStatus Status { get; init; }
    public string? ActionRequired { get; init; }
    public DateTimeOffset? DueDate { get; init; }
}

/// <summary>
/// Compliance expectation status.
/// </summary>
public enum ComplianceStatus
{
    NotApplicable,
    Pending,
    InProgress,
    Compliant,
    NonCompliant,
    ExceptionGranted
}

/// <summary>
/// Record of a security review update.
/// </summary>
public record SecurityReviewUpdate
{
    public required string Id { get; init; }
    public required DateTimeOffset UpdateDate { get; init; }
    public required string UpdatedBy { get; init; }
    public required string ChangeDescription { get; init; }
    public string? FieldChanged { get; init; }
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
}

/// <summary>
/// Record of a data update made to an application.
/// </summary>
public record DataUpdateRecord
{
    public required string Id { get; init; }
    public required DateTimeOffset UpdateDate { get; init; }
    public required string UpdatedBy { get; init; }
    public required DataUpdateSource Source { get; init; }
    public required string ChangeDescription { get; init; }
    public List<FieldChange> FieldChanges { get; init; } = [];
}

/// <summary>
/// Source of a data update.
/// </summary>
public enum DataUpdateSource
{
    Manual,
    AzureDevOps,
    SharePoint,
    ServiceNow,
    IISDatabase,
    System
}

/// <summary>
/// Record of a specific field change.
/// </summary>
public record FieldChange
{
    public required string FieldName { get; init; }
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
}

/// <summary>
/// Indicates whether usage metrics are available and meaningful for this application.
/// </summary>
public record UsageDataAvailability
{
    /// <summary>
    /// Whether usage data is available from data sources.
    /// </summary>
    public bool IsAvailable { get; init; } = true;

    /// <summary>
    /// Reason why usage data is not available or not meaningful.
    /// </summary>
    public UsageDataReason Reason { get; init; } = UsageDataReason.Available;

    /// <summary>
    /// Additional notes explaining the usage data situation.
    /// </summary>
    public string? Notes { get; init; }

    /// <summary>
    /// Whether this application has seasonal usage patterns.
    /// </summary>
    public bool IsSeasonal { get; init; }

    /// <summary>
    /// Description of seasonal pattern if applicable (e.g., "High in July, low in December").
    /// </summary>
    public string? SeasonalPattern { get; init; }

    /// <summary>
    /// Date when this was last reviewed/confirmed by data owner.
    /// </summary>
    public DateTimeOffset? LastReviewedDate { get; init; }

    /// <summary>
    /// User who last reviewed this setting.
    /// </summary>
    public string? ReviewedBy { get; init; }
}

/// <summary>
/// Reasons why usage data may not be available or meaningful.
/// </summary>
public enum UsageDataReason
{
    /// <summary>Usage data is available and meaningful.</summary>
    Available,

    /// <summary>Data source doesn't support user metrics.</summary>
    DataSourceUnsupported,

    /// <summary>Application is internal/backend with no direct user interaction.</summary>
    BackendService,

    /// <summary>Application has seasonal usage (e.g., recruiting only in certain months).</summary>
    SeasonalUsage,

    /// <summary>Application is batch/scheduled process with no interactive users.</summary>
    BatchProcess,

    /// <summary>Application is new and doesn't have enough usage history.</summary>
    NewApplication,

    /// <summary>Application is being retired and usage is intentionally declining.</summary>
    BeingRetired,

    /// <summary>Usage tracking is not enabled or configured.</summary>
    TrackingNotConfigured,

    /// <summary>Other reason - see notes for details.</summary>
    Other
}

/// <summary>
/// Represents a critical period during the year for an application.
/// Used to indicate when maintenance should be avoided or when downtime is acceptable.
/// </summary>
public record CriticalPeriod
{
    /// <summary>
    /// Unique identifier for this period.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Name/label for this period (e.g., "Recruiting Season", "Year-End Close").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Description of why this period is critical.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The type/criticality of this period.
    /// </summary>
    public PeriodCriticality Criticality { get; init; }

    /// <summary>
    /// Start month (1-12).
    /// </summary>
    public int StartMonth { get; init; }

    /// <summary>
    /// Start day of month (1-31, optional for month-level periods).
    /// </summary>
    public int? StartDay { get; init; }

    /// <summary>
    /// End month (1-12).
    /// </summary>
    public int EndMonth { get; init; }

    /// <summary>
    /// End day of month (1-31, optional for month-level periods).
    /// </summary>
    public int? EndDay { get; init; }

    /// <summary>
    /// Whether this period recurs annually.
    /// </summary>
    public bool IsRecurring { get; init; } = true;

    /// <summary>
    /// Specific year if not recurring (for one-time events).
    /// </summary>
    public int? Year { get; init; }

    /// <summary>
    /// Formatted display of the period (computed).
    /// </summary>
    public string DisplayPeriod => FormatPeriod();

    private string FormatPeriod()
    {
        var months = new[] { "", "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
        var start = StartDay.HasValue ? $"{months[StartMonth]} {StartDay}" : months[StartMonth];
        var end = EndDay.HasValue ? $"{months[EndMonth]} {EndDay}" : months[EndMonth];
        return StartMonth == EndMonth && !StartDay.HasValue ? start : $"{start} - {end}";
    }
}

/// <summary>
/// Criticality level for a time period.
/// </summary>
public enum PeriodCriticality
{
    /// <summary>No maintenance or changes allowed - maximum uptime required.</summary>
    Blackout,

    /// <summary>Critical period - avoid maintenance if possible.</summary>
    Critical,

    /// <summary>Elevated importance - extra caution required.</summary>
    Elevated,

    /// <summary>Normal operations.</summary>
    Normal,

    /// <summary>Reduced activity - good time for maintenance.</summary>
    LowActivity,

    /// <summary>Downtime acceptable - ideal maintenance window.</summary>
    MaintenanceWindow
}

/// <summary>
/// Represents a key upcoming date for an application.
/// </summary>
public record KeyDate
{
    /// <summary>
    /// Unique identifier for this date entry.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Title/name of the event or milestone.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Description of what happens on this date.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The actual date.
    /// </summary>
    public required DateTimeOffset Date { get; init; }

    /// <summary>
    /// Type of key date.
    /// </summary>
    public KeyDateType Type { get; init; }

    /// <summary>
    /// Whether this blocks maintenance activities.
    /// </summary>
    public bool BlocksMaintenance { get; init; }

    /// <summary>
    /// Number of days before the date to start warning about maintenance.
    /// </summary>
    public int WarningDaysBefore { get; init; } = 7;

    /// <summary>
    /// Number of days after the date to continue blocking/warning.
    /// </summary>
    public int WarningDaysAfter { get; init; } = 0;

    /// <summary>
    /// User who added this key date.
    /// </summary>
    public string? AddedBy { get; init; }

    /// <summary>
    /// Date when this was added.
    /// </summary>
    public DateTimeOffset? AddedDate { get; init; }

    /// <summary>
    /// Whether this date is in the warning window (computed).
    /// </summary>
    public bool IsInWarningWindow => Date > DateTimeOffset.UtcNow.AddDays(-WarningDaysAfter)
                                     && Date < DateTimeOffset.UtcNow.AddDays(WarningDaysBefore);

    /// <summary>
    /// Days until this date (negative if passed).
    /// </summary>
    public int DaysUntil => (int)(Date - DateTimeOffset.UtcNow).TotalDays;
}

/// <summary>
/// Types of key dates.
/// </summary>
public enum KeyDateType
{
    /// <summary>Major release or deployment.</summary>
    Release,

    /// <summary>Deadline for a deliverable.</summary>
    Deadline,

    /// <summary>Audit or compliance review.</summary>
    Audit,

    /// <summary>Scheduled maintenance window.</summary>
    ScheduledMaintenance,

    /// <summary>Major business event (e.g., year-end close).</summary>
    BusinessEvent,

    /// <summary>Training or go-live date.</summary>
    GoLive,

    /// <summary>License or certificate expiration.</summary>
    Expiration,

    /// <summary>External dependency date (e.g., vendor deadline).</summary>
    ExternalDependency,

    /// <summary>Other important date.</summary>
    Other
}
