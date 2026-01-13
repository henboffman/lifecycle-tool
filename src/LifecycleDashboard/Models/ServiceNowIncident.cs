namespace LifecycleDashboard.Models;

/// <summary>
/// Represents a ServiceNow incident imported for analysis.
/// </summary>
public record ServiceNowIncident
{
    /// <summary>
    /// Internal unique identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// ServiceNow incident number (e.g., INC0012345).
    /// </summary>
    public required string IncidentNumber { get; init; }

    /// <summary>
    /// Current state of the incident (e.g., Resolved, Closed).
    /// </summary>
    public string? State { get; init; }

    /// <summary>
    /// Configuration item from ServiceNow (cmdb_ci).
    /// This should match a ServiceNow application name.
    /// </summary>
    public string? ConfigurationItem { get; init; }

    /// <summary>
    /// Brief description of the incident.
    /// </summary>
    public string? ShortDescription { get; init; }

    /// <summary>
    /// Full description of the incident.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Close code indicating resolution type.
    /// </summary>
    public string? CloseCode { get; init; }

    /// <summary>
    /// Notes entered when closing the incident.
    /// </summary>
    public string? CloseNotes { get; init; }

    /// <summary>
    /// Raw comments and work notes field from CSV.
    /// </summary>
    public string? CommentsAndWorkNotesRaw { get; init; }

    /// <summary>
    /// Parsed individual comment/work note entries.
    /// </summary>
    public List<IncidentEntry> Entries { get; init; } = [];

    /// <summary>
    /// Linked application ID in our system (if matched).
    /// </summary>
    public string? LinkedApplicationId { get; init; }

    /// <summary>
    /// Linked application name (for display).
    /// </summary>
    public string? LinkedApplicationName { get; init; }

    /// <summary>
    /// Status of the configuration item linking.
    /// </summary>
    public ConfigItemLinkStatus LinkStatus { get; init; } = ConfigItemLinkStatus.Unknown;

    /// <summary>
    /// Notes about the link status (e.g., why it couldn't be matched).
    /// </summary>
    public string? LinkStatusNotes { get; init; }

    /// <summary>
    /// Whether this incident has been manually reviewed/amended.
    /// </summary>
    public bool ManuallyReviewed { get; init; }

    /// <summary>
    /// When this incident was imported.
    /// </summary>
    public DateTimeOffset ImportedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When this record was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>
    /// Raw CSV values for reference.
    /// </summary>
    public Dictionary<string, string>? RawCsvValues { get; init; }
}

/// <summary>
/// A single comment or work note entry parsed from the combined field.
/// </summary>
public record IncidentEntry
{
    /// <summary>
    /// Type of entry (Comment or WorkNote).
    /// </summary>
    public IncidentEntryType EntryType { get; init; }

    /// <summary>
    /// Timestamp when this entry was created.
    /// </summary>
    public DateTimeOffset? Timestamp { get; init; }

    /// <summary>
    /// User who created this entry.
    /// </summary>
    public string? Author { get; init; }

    /// <summary>
    /// The actual content of the comment/work note.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Raw text before parsing (for debugging).
    /// </summary>
    public string? RawText { get; init; }
}

/// <summary>
/// Type of incident entry.
/// </summary>
public enum IncidentEntryType
{
    Comment,
    WorkNote,
    Unknown
}

/// <summary>
/// Status of configuration item linking.
/// </summary>
public enum ConfigItemLinkStatus
{
    /// <summary>
    /// Link status not yet determined.
    /// </summary>
    Unknown,

    /// <summary>
    /// No configuration item was provided in the import.
    /// </summary>
    MissingConfigItem,

    /// <summary>
    /// Configuration item doesn't match any application in our system.
    /// May be hardware, infrastructure, or non-application item.
    /// </summary>
    NoMatchingApplication,

    /// <summary>
    /// Successfully linked to an application.
    /// </summary>
    Linked,

    /// <summary>
    /// Manually linked by a user after import.
    /// </summary>
    ManuallyLinked
}

/// <summary>
/// Column mapping configuration for ServiceNow incident CSV imports.
/// </summary>
public record ServiceNowIncidentColumnMapping
{
    public string? IncidentNumberColumn { get; init; }
    public string? StateColumn { get; init; }
    public string? ConfigurationItemColumn { get; init; }
    public string? ShortDescriptionColumn { get; init; }
    public string? DescriptionColumn { get; init; }
    public string? CloseCodeColumn { get; init; }
    public string? CloseNotesColumn { get; init; }
    public string? CommentsAndWorkNotesColumn { get; init; }
}

/// <summary>
/// Summary statistics for incident analysis.
/// </summary>
public record IncidentAnalysisSummary
{
    public int TotalIncidents { get; init; }
    public int LinkedIncidents { get; init; }
    public int UnlinkedIncidents { get; init; }
    public int MissingConfigItem { get; init; }
    public int NoMatchingApplication { get; init; }

    /// <summary>
    /// Applications with most incidents.
    /// </summary>
    public List<ApplicationIncidentCount> TopApplications { get; init; } = [];

    /// <summary>
    /// Most common close codes.
    /// </summary>
    public List<CloseCodeCount> TopCloseCodes { get; init; } = [];

    /// <summary>
    /// Incidents by state.
    /// </summary>
    public Dictionary<string, int> ByState { get; init; } = [];
}

/// <summary>
/// Incident count for an application.
/// </summary>
public record ApplicationIncidentCount
{
    public required string ApplicationId { get; init; }
    public required string ApplicationName { get; init; }
    public int IncidentCount { get; init; }
    public int BandaidFixCount { get; init; }
    public List<string> CommonIssues { get; init; } = [];
}

/// <summary>
/// Count of incidents by close code.
/// </summary>
public record CloseCodeCount
{
    public required string CloseCode { get; init; }
    public int Count { get; init; }
    public string? Description { get; init; }
}
