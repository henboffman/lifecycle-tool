using LifecycleDashboard.Models;

namespace LifecycleDashboard.Services;

/// <summary>
/// Service for fetching end-of-life data from external sources like endoflife.date
/// </summary>
public interface IEolDataService
{
    /// <summary>
    /// Fetch EOL data for all supported frameworks from endoflife.date
    /// </summary>
    /// <returns>Result containing new/updated framework versions</returns>
    Task<EolRefreshResult> RefreshEolDataAsync();

    /// <summary>
    /// Fetch EOL data for a specific framework type
    /// </summary>
    Task<EolRefreshResult> RefreshFrameworkEolDataAsync(FrameworkType frameworkType);
}

/// <summary>
/// Result of an EOL data refresh operation
/// </summary>
public record EolRefreshResult
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Framework versions that were added
    /// </summary>
    public List<FrameworkVersion> Added { get; init; } = [];

    /// <summary>
    /// Framework versions that were updated (with old values for comparison)
    /// </summary>
    public List<EolUpdateInfo> Updated { get; init; } = [];

    /// <summary>
    /// Frameworks that were checked but had no changes
    /// </summary>
    public List<string> Unchanged { get; init; } = [];

    /// <summary>
    /// Frameworks that failed to fetch
    /// </summary>
    public List<EolFetchError> Errors { get; init; } = [];

    /// <summary>
    /// When the refresh was performed
    /// </summary>
    public DateTimeOffset RefreshedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Summary count of changes
    /// </summary>
    public int TotalChanges => Added.Count + Updated.Count;
}

/// <summary>
/// Information about an updated framework version
/// </summary>
public record EolUpdateInfo
{
    public required FrameworkVersion Version { get; init; }
    public DateTimeOffset? PreviousEolDate { get; init; }
    public DateTimeOffset? NewEolDate { get; init; }
    public SupportStatus? PreviousStatus { get; init; }
    public SupportStatus? NewStatus { get; init; }
    public string? ChangeDescription { get; init; }
}

/// <summary>
/// Error information for a failed fetch
/// </summary>
public record EolFetchError
{
    public required FrameworkType Framework { get; init; }
    public required string ErrorMessage { get; init; }
}
