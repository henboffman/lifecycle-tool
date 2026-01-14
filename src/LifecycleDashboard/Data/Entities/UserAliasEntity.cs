using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LifecycleDashboard.Data.Entities;

/// <summary>
/// Stores discovered name/email variations for a user to improve matching.
/// These are discovered during imports from ServiceNow, Azure DevOps, etc.
/// </summary>
public class UserAliasEntity
{
    [Key]
    [MaxLength(36)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Foreign key to the Entra user this alias belongs to.
    /// </summary>
    [Required]
    [MaxLength(36)]
    public string EntraUserId { get; set; } = null!;

    /// <summary>
    /// Type of alias (Email, Name, EmployeeId, Username).
    /// </summary>
    public AliasType Type { get; set; }

    /// <summary>
    /// Normalized value (lowercase, trimmed) for matching.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string Value { get; set; } = null!;

    /// <summary>
    /// Original value with original casing for display.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string OriginalValue { get; set; } = null!;

    /// <summary>
    /// Source where this alias was discovered.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string DiscoveredFrom { get; set; } = null!;

    /// <summary>
    /// When this alias was discovered.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Navigation property to the Entra user.
    /// </summary>
    [ForeignKey(nameof(EntraUserId))]
    public EntraUserEntity EntraUser { get; set; } = null!;
}

/// <summary>
/// Types of user aliases that can be stored.
/// </summary>
public enum AliasType
{
    /// <summary>
    /// Email address variation (e.g., user@dow.com, dowid@dow.com).
    /// </summary>
    Email,

    /// <summary>
    /// Name variation (e.g., "John Smith", "Smith, John").
    /// </summary>
    Name,

    /// <summary>
    /// Employee ID from HR systems.
    /// </summary>
    EmployeeId,

    /// <summary>
    /// Short username (e.g., "jsmith").
    /// </summary>
    Username
}
