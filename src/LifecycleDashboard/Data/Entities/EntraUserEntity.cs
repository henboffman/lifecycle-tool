using System.ComponentModel.DataAnnotations;

namespace LifecycleDashboard.Data.Entities;

/// <summary>
/// Represents a user from Microsoft Entra ID (Azure AD).
/// This is the source of truth for user identity in the system.
/// </summary>
public class EntraUserEntity
{
    /// <summary>
    /// Primary key - Entra Object ID (GUID).
    /// </summary>
    [Key]
    [MaxLength(36)]
    public string Id { get; set; } = null!;

    /// <summary>
    /// User Principal Name (e.g., user@domain.com).
    /// This is the primary login identifier.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string UserPrincipalName { get; set; } = null!;

    /// <summary>
    /// Display name as shown in Entra.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string DisplayName { get; set; } = null!;

    /// <summary>
    /// First name / given name.
    /// </summary>
    [MaxLength(100)]
    public string? GivenName { get; set; }

    /// <summary>
    /// Last name / surname.
    /// </summary>
    [MaxLength(100)]
    public string? Surname { get; set; }

    /// <summary>
    /// Email address (may differ from UPN).
    /// </summary>
    [MaxLength(256)]
    public string? Mail { get; set; }

    /// <summary>
    /// HR employee number if available.
    /// </summary>
    [MaxLength(50)]
    public string? EmployeeId { get; set; }

    /// <summary>
    /// Department from Entra.
    /// </summary>
    [MaxLength(200)]
    public string? Department { get; set; }

    /// <summary>
    /// Job title from Entra.
    /// </summary>
    [MaxLength(200)]
    public string? JobTitle { get; set; }

    /// <summary>
    /// Office location from Entra.
    /// </summary>
    [MaxLength(200)]
    public string? OfficeLocation { get; set; }

    /// <summary>
    /// Entra ObjectId of the user's manager.
    /// </summary>
    [MaxLength(36)]
    public string? ManagerId { get; set; }

    /// <summary>
    /// Whether the account is enabled in Entra.
    /// </summary>
    public bool AccountEnabled { get; set; } = true;

    /// <summary>
    /// User's profile photo data (binary).
    /// </summary>
    public byte[]? PhotoData { get; set; }

    /// <summary>
    /// Content type of the photo (e.g., "image/jpeg").
    /// </summary>
    [MaxLength(50)]
    public string? PhotoContentType { get; set; }

    /// <summary>
    /// When the photo was last updated from Entra.
    /// </summary>
    public DateTimeOffset? PhotoLastUpdated { get; set; }

    /// <summary>
    /// When this user was last synced from Entra.
    /// </summary>
    public DateTimeOffset EntraLastSyncedAt { get; set; }

    /// <summary>
    /// When this record was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When this record was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property to aliases discovered for this user.
    /// </summary>
    public ICollection<UserAliasEntity> Aliases { get; set; } = new List<UserAliasEntity>();
}
