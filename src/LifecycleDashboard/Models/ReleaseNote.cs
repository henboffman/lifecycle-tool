namespace LifecycleDashboard.Models;

/// <summary>
/// Represents a single release note entry.
/// This is a reusable model that can be adapted for other projects.
/// </summary>
public record ReleaseNote
{
    /// <summary>
    /// Unique identifier for the release note.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Version string (e.g., "0.1.0", "1.0.0-beta").
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Date when this release was created.
    /// </summary>
    public required DateTimeOffset Date { get; init; }

    /// <summary>
    /// Short title describing the release or change.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Detailed description of changes in Markdown format.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Category of the change.
    /// </summary>
    public ReleaseNoteCategory Category { get; init; } = ReleaseNoteCategory.Feature;

    /// <summary>
    /// Individual items/changes within this release.
    /// </summary>
    public List<ReleaseNoteItem> Items { get; init; } = [];

    /// <summary>
    /// Tags for filtering/categorization.
    /// </summary>
    public List<string> Tags { get; init; } = [];

    /// <summary>
    /// Whether this release note should be highlighted (e.g., major release).
    /// </summary>
    public bool IsHighlighted { get; init; }

    /// <summary>
    /// Whether this is a breaking change.
    /// </summary>
    public bool IsBreakingChange { get; init; }
}

/// <summary>
/// Represents a single item within a release note.
/// </summary>
public record ReleaseNoteItem
{
    /// <summary>
    /// Type of change.
    /// </summary>
    public required ReleaseNoteItemType Type { get; init; }

    /// <summary>
    /// Description of the change.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Optional link to related issue/PR/documentation.
    /// </summary>
    public string? Link { get; init; }

    /// <summary>
    /// Component or area affected (e.g., "Dashboard", "API", "Tasks").
    /// </summary>
    public string? Component { get; init; }
}

/// <summary>
/// Category for release notes.
/// </summary>
public enum ReleaseNoteCategory
{
    Feature,
    Enhancement,
    BugFix,
    Security,
    Performance,
    Documentation,
    Infrastructure,
    Breaking
}

/// <summary>
/// Type of individual release note item.
/// </summary>
public enum ReleaseNoteItemType
{
    Added,
    Changed,
    Fixed,
    Removed,
    Deprecated,
    Security
}
