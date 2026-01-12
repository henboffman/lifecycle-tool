namespace LifecycleDashboard.Models;

/// <summary>
/// Represents a user in the system.
/// </summary>
public record User
{
    /// <summary>
    /// Unique identifier (typically from Entra ID).
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Display name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Email address.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Department or team.
    /// </summary>
    public string? Department { get; init; }

    /// <summary>
    /// Job title.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// User's role in the system.
    /// </summary>
    public SystemRole Role { get; init; } = SystemRole.StandardUser;

    /// <summary>
    /// Whether the user is active in Entra ID.
    /// </summary>
    public bool IsActive { get; init; } = true;

    /// <summary>
    /// Last login date.
    /// </summary>
    public DateTimeOffset? LastLoginDate { get; init; }

    /// <summary>
    /// User preferences.
    /// </summary>
    public UserPreferences Preferences { get; init; } = new();
}

/// <summary>
/// System-level user roles.
/// </summary>
public enum SystemRole
{
    ReadOnly,
    StandardUser,
    PowerUser,
    Administrator,
    SecurityAdministrator
}

/// <summary>
/// User preferences stored in IndexedDB.
/// </summary>
public record UserPreferences
{
    /// <summary>
    /// Light or dark theme preference.
    /// </summary>
    public ThemePreference Theme { get; init; } = ThemePreference.System;

    /// <summary>
    /// Whether to show the dev mode sidebar.
    /// </summary>
    public bool ShowDevSidebar { get; init; } = true;

    /// <summary>
    /// Default heatmap view type.
    /// </summary>
    public HeatmapViewType DefaultHeatmapView { get; init; } = HeatmapViewType.Grid;

    /// <summary>
    /// Number of items to show per page in lists.
    /// </summary>
    public int ItemsPerPage { get; init; } = 25;

    /// <summary>
    /// Notification preferences.
    /// </summary>
    public NotificationPreferences Notifications { get; init; } = new();

    /// <summary>
    /// Saved filter configurations.
    /// </summary>
    public List<SavedFilter> SavedFilters { get; init; } = [];

    /// <summary>
    /// Custom dashboard layout configuration.
    /// </summary>
    public DashboardLayout? CustomDashboard { get; init; }
}

public enum ThemePreference
{
    Light,
    Dark,
    System
}

public enum HeatmapViewType
{
    Grid,
    Treemap
}

/// <summary>
/// Notification preferences.
/// </summary>
public record NotificationPreferences
{
    public bool EnableInApp { get; init; } = true;
    public bool EnableEmail { get; init; } = false;
    public bool NotifyOnTaskDue { get; init; } = true;
    public bool NotifyOnHealthChange { get; init; } = true;
    public bool NotifyOnDataSync { get; init; } = false;
    public int TaskWarningDays { get; init; } = 14;
}

/// <summary>
/// Saved filter configuration.
/// </summary>
public record SavedFilter
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string FilterType { get; init; } // "applications", "tasks", "heatmap"
    public required Dictionary<string, string> FilterValues { get; init; }
    public bool IsDefault { get; init; }
    public DateTimeOffset CreatedDate { get; init; }
}

/// <summary>
/// Custom dashboard layout configuration.
/// </summary>
public record DashboardLayout
{
    public List<DashboardWidget> Widgets { get; init; } = [];
}

/// <summary>
/// Dashboard widget configuration.
/// </summary>
public record DashboardWidget
{
    public required string Id { get; init; }
    public required string Type { get; init; }
    public int Column { get; init; }
    public int Row { get; init; }
    public int Width { get; init; } = 1;
    public int Height { get; init; } = 1;
    public Dictionary<string, string> Settings { get; init; } = [];
}
