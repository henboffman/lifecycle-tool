using LifecycleDashboard.Models;

namespace LifecycleDashboard.Data.Entities;

/// <summary>
/// Database entity for User.
/// </summary>
public class UserEntity
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Department { get; set; }
    public string? Title { get; set; }
    public SystemRole Role { get; set; } = SystemRole.StandardUser;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastLoginDate { get; set; }

    // JSON-serialized complex properties
    public string PreferencesJson { get; set; } = "{}";

    // Audit fields
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
