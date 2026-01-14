using System.ComponentModel.DataAnnotations;

namespace LifecycleDashboard.Data.Entities;

/// <summary>
/// Tracks imports to prevent duplicate data imports.
/// Stores file hashes and metadata about each import.
/// </summary>
public class ImportTrackingEntity
{
    [Key]
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The data source type (e.g., "ServiceNow", "ServiceNowIncidents").
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string DataSource { get; set; } = null!;

    /// <summary>
    /// SHA256 hash of the imported file content.
    /// Used to detect duplicate imports.
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string FileHash { get; set; } = null!;

    /// <summary>
    /// Original filename that was imported.
    /// </summary>
    [MaxLength(256)]
    public string? FileName { get; set; }

    /// <summary>
    /// Size of the imported file in bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Number of records in the import.
    /// </summary>
    public int RecordCount { get; set; }

    /// <summary>
    /// Number of new records created.
    /// </summary>
    public int NewRecords { get; set; }

    /// <summary>
    /// Number of existing records updated.
    /// </summary>
    public int UpdatedRecords { get; set; }

    /// <summary>
    /// Number of records skipped (already existed unchanged).
    /// </summary>
    public int SkippedRecords { get; set; }

    /// <summary>
    /// Number of records with errors.
    /// </summary>
    public int ErrorRecords { get; set; }

    /// <summary>
    /// When the import was performed.
    /// </summary>
    public DateTimeOffset ImportedAt { get; set; }

    /// <summary>
    /// Entra ID of the user who performed the import.
    /// </summary>
    [MaxLength(36)]
    public string? ImportedByUserId { get; set; }

    /// <summary>
    /// Display name of the user who performed the import.
    /// </summary>
    [MaxLength(256)]
    public string? ImportedByName { get; set; }

    /// <summary>
    /// Any notes or messages from the import process.
    /// </summary>
    [MaxLength(2000)]
    public string? Notes { get; set; }

    /// <summary>
    /// JSON string containing any additional metadata about the import.
    /// </summary>
    public string? MetadataJson { get; set; }
}
