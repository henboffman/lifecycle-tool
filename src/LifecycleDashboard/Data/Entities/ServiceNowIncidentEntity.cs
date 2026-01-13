namespace LifecycleDashboard.Data.Entities;

/// <summary>
/// Database entity for ServiceNow incidents imported for analysis.
/// </summary>
public class ServiceNowIncidentEntity
{
    public string Id { get; set; } = null!;

    /// <summary>
    /// ServiceNow incident number (e.g., INC0012345).
    /// </summary>
    public string IncidentNumber { get; set; } = null!;

    /// <summary>
    /// Current state of the incident.
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Configuration item from ServiceNow (cmdb_ci).
    /// </summary>
    public string? ConfigurationItem { get; set; }

    /// <summary>
    /// Brief description of the incident.
    /// </summary>
    public string? ShortDescription { get; set; }

    /// <summary>
    /// Full description of the incident.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Close code indicating resolution type.
    /// </summary>
    public string? CloseCode { get; set; }

    /// <summary>
    /// Notes entered when closing the incident.
    /// </summary>
    public string? CloseNotes { get; set; }

    /// <summary>
    /// Raw comments and work notes field from CSV.
    /// </summary>
    public string? CommentsAndWorkNotesRaw { get; set; }

    /// <summary>
    /// Parsed entries stored as JSON array.
    /// </summary>
    public string EntriesJson { get; set; } = "[]";

    /// <summary>
    /// Linked application ID in our system.
    /// </summary>
    public string? LinkedApplicationId { get; set; }

    /// <summary>
    /// Linked application name.
    /// </summary>
    public string? LinkedApplicationName { get; set; }

    /// <summary>
    /// Status of the configuration item linking (stored as string enum).
    /// </summary>
    public string LinkStatus { get; set; } = "Unknown";

    /// <summary>
    /// Notes about the link status.
    /// </summary>
    public string? LinkStatusNotes { get; set; }

    /// <summary>
    /// Whether this incident has been manually reviewed.
    /// </summary>
    public bool ManuallyReviewed { get; set; }

    /// <summary>
    /// When this incident was imported.
    /// </summary>
    public DateTimeOffset ImportedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Raw CSV values stored as JSON.
    /// </summary>
    public string RawCsvValuesJson { get; set; } = "{}";

    // Audit fields
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
