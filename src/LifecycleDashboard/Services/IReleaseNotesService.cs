using LifecycleDashboard.Models;

namespace LifecycleDashboard.Services;

/// <summary>
/// Service for managing release notes.
/// This interface enables the release notes system to be reusable across projects.
/// </summary>
public interface IReleaseNotesService
{
    /// <summary>
    /// Gets all release notes, ordered by date descending.
    /// </summary>
    Task<IReadOnlyList<ReleaseNote>> GetAllAsync();

    /// <summary>
    /// Gets recent release notes for the dev sidebar.
    /// </summary>
    /// <param name="count">Maximum number of notes to return.</param>
    Task<IReadOnlyList<ReleaseNote>> GetRecentAsync(int count = 5);

    /// <summary>
    /// Gets release notes for a specific version.
    /// </summary>
    Task<ReleaseNote?> GetByVersionAsync(string version);

    /// <summary>
    /// Gets release notes filtered by category.
    /// </summary>
    Task<IReadOnlyList<ReleaseNote>> GetByCategoryAsync(ReleaseNoteCategory category);

    /// <summary>
    /// Gets the latest version string.
    /// </summary>
    Task<string> GetLatestVersionAsync();
}
