using System.ComponentModel.DataAnnotations;

namespace LifecycleDashboard.Data.Entities;

/// <summary>
/// Tracks users found in ServiceNow or other sources but not in Entra ID.
/// These represent potential departed employees that need attention.
/// </summary>
public class DepartedUserAlertEntity
{
    [Key]
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The name/identifier that couldn't be matched to Entra.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string UnmatchedValue { get; set; } = null!;

    /// <summary>
    /// Type of the unmatched value (Name, Email, etc.).
    /// </summary>
    public AliasType ValueType { get; set; }

    /// <summary>
    /// ID of the application where this user was referenced.
    /// </summary>
    [Required]
    [MaxLength(36)]
    public string ApplicationId { get; set; } = null!;

    /// <summary>
    /// Name of the application for display purposes.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string ApplicationName { get; set; } = null!;

    /// <summary>
    /// The role type where the user was assigned (FunctionalArchitect, Owner, etc.).
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string RoleType { get; set; } = null!;

    /// <summary>
    /// Source system where this user reference was found.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string DataSource { get; set; } = null!;

    /// <summary>
    /// Current status of this alert.
    /// </summary>
    public DepartedUserAlertStatus Status { get; set; } = DepartedUserAlertStatus.Open;

    /// <summary>
    /// Entra ID of the user who resolved this alert.
    /// </summary>
    [MaxLength(36)]
    public string? ResolvedByUserId { get; set; }

    /// <summary>
    /// Display name of the user who resolved this alert.
    /// </summary>
    [MaxLength(256)]
    public string? ResolvedByName { get; set; }

    /// <summary>
    /// Notes about how this alert was resolved.
    /// </summary>
    [MaxLength(1000)]
    public string? ResolutionNotes { get; set; }

    /// <summary>
    /// Entra ID of the replacement user, if one was assigned.
    /// </summary>
    [MaxLength(36)]
    public string? ReplacementUserId { get; set; }

    /// <summary>
    /// Display name of the replacement user.
    /// </summary>
    [MaxLength(256)]
    public string? ReplacementUserName { get; set; }

    /// <summary>
    /// When this departed user was detected.
    /// </summary>
    public DateTimeOffset DetectedAt { get; set; }

    /// <summary>
    /// When this alert was resolved.
    /// </summary>
    public DateTimeOffset? ResolvedAt { get; set; }

    /// <summary>
    /// ID of the task created for this alert, if any.
    /// </summary>
    [MaxLength(36)]
    public string? LinkedTaskId { get; set; }
}

/// <summary>
/// Status of a departed user alert.
/// </summary>
public enum DepartedUserAlertStatus
{
    /// <summary>
    /// Alert is open and needs attention.
    /// </summary>
    Open,

    /// <summary>
    /// Alert has been acknowledged but not yet resolved.
    /// </summary>
    Acknowledged,

    /// <summary>
    /// Alert has been resolved (user replaced or confirmed departed).
    /// </summary>
    Resolved,

    /// <summary>
    /// Alert was a false positive (user found or name was incorrect).
    /// </summary>
    FalsePositive
}
