namespace LifecycleDashboard.Data.Entities;

/// <summary>
/// Database entity for ImportedServiceNowApplication - stores ServiceNow CSV import results.
/// </summary>
public class ImportedServiceNowApplicationEntity
{
    public string Id { get; set; } = null!;
    public string ServiceNowId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }
    public string? Capability { get; set; }
    public string? Status { get; set; }

    // Key Roles
    public string? OwnerId { get; set; }
    public string? OwnerName { get; set; }
    public string? ProductManagerId { get; set; }
    public string? ProductManagerName { get; set; }
    public string? BusinessOwnerId { get; set; }
    public string? BusinessOwnerName { get; set; }
    public string? FunctionalArchitectId { get; set; }
    public string? FunctionalArchitectName { get; set; }
    public string? TechnicalArchitectId { get; set; }
    public string? TechnicalArchitectName { get; set; }

    // Legacy - kept for backwards compatibility
    public string? TechnicalLeadId { get; set; }
    public string? TechnicalLeadName { get; set; }

    // Application Classification
    public string? ApplicationType { get; set; }  // COTS, Homegrown
    public string? ArchitectureType { get; set; } // Web Based, Client Server, Desktop App, Other
    public string? UserBase { get; set; }         // User base range estimate
    public string? Importance { get; set; }       // Importance value from ServiceNow

    // Other fields
    public string? RepositoryUrl { get; set; }
    public string? DocumentationUrl { get; set; }
    public string? Environment { get; set; }
    public string? Criticality { get; set; }
    public string? SupportGroup { get; set; }
    public DateTimeOffset ImportedAt { get; set; } = DateTimeOffset.UtcNow;

    // Raw values from CSV stored as JSON
    public string RawCsvValuesJson { get; set; } = "{}";

    // Linked repository
    public string? LinkedRepositoryId { get; set; }
    public string? LinkedRepositoryName { get; set; }

    // Audit fields
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
