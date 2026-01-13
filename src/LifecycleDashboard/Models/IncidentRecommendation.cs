namespace LifecycleDashboard.Models;

/// <summary>
/// Represents an AI-generated recommendation based on incident analysis.
/// These recommendations are stored persistently and can be viewed on
/// both the Incidents page and Application detail pages.
/// </summary>
public record IncidentRecommendation
{
    /// <summary>
    /// Unique identifier for this recommendation.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Application this recommendation is for (null for portfolio-wide recommendations).
    /// </summary>
    public string? ApplicationId { get; init; }

    /// <summary>
    /// Application name for display purposes.
    /// </summary>
    public string? ApplicationName { get; init; }

    /// <summary>
    /// Type of recommendation.
    /// </summary>
    public IncidentRecommendationType Type { get; init; }

    /// <summary>
    /// Priority of this recommendation (1=highest, 5=lowest).
    /// </summary>
    public int Priority { get; init; } = 3;

    /// <summary>
    /// Title of the recommendation.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Detailed description of the issue identified.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// AI-generated analysis of the root cause.
    /// </summary>
    public string? RootCauseAnalysis { get; init; }

    /// <summary>
    /// Recommended action to address the issue.
    /// </summary>
    public required string RecommendedAction { get; init; }

    /// <summary>
    /// Expected impact if the recommendation is implemented.
    /// </summary>
    public string? ExpectedImpact { get; init; }

    /// <summary>
    /// Estimated effort level (Low, Medium, High).
    /// </summary>
    public string? EstimatedEffort { get; init; }

    /// <summary>
    /// Related close codes that contributed to this recommendation.
    /// </summary>
    public List<string> RelatedCloseCodes { get; init; } = [];

    /// <summary>
    /// Related incident numbers.
    /// </summary>
    public List<string> RelatedIncidentNumbers { get; init; } = [];

    /// <summary>
    /// Number of incidents that contributed to this recommendation.
    /// </summary>
    public int IncidentCount { get; init; }

    /// <summary>
    /// AI confidence score (0-100).
    /// </summary>
    public int ConfidenceScore { get; init; }

    /// <summary>
    /// Status of the recommendation.
    /// </summary>
    public RecommendationStatus Status { get; init; } = RecommendationStatus.Active;

    /// <summary>
    /// When this recommendation was generated.
    /// </summary>
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When this recommendation was last updated.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>
    /// When this recommendation was dismissed/resolved (if applicable).
    /// </summary>
    public DateTimeOffset? ResolvedAt { get; init; }

    /// <summary>
    /// User notes or resolution notes.
    /// </summary>
    public string? Notes { get; init; }
}

/// <summary>
/// Types of AI-generated incident recommendations.
/// </summary>
public enum IncidentRecommendationType
{
    /// <summary>
    /// Recommendation based on repeat patterns in close codes.
    /// </summary>
    RepeatPattern,

    /// <summary>
    /// Recommendation based on high incident volume.
    /// </summary>
    HighVolume,

    /// <summary>
    /// Recommendation based on analysis of closure notes.
    /// </summary>
    ClosureAnalysis,

    /// <summary>
    /// Recommendation based on work note patterns.
    /// </summary>
    WorkNotePattern,

    /// <summary>
    /// General improvement suggestion.
    /// </summary>
    GeneralImprovement,

    /// <summary>
    /// Suggestion for process improvement.
    /// </summary>
    ProcessImprovement,

    /// <summary>
    /// Documentation or training need identified.
    /// </summary>
    TrainingNeed,

    /// <summary>
    /// Technical debt or workaround identified.
    /// </summary>
    TechnicalDebt
}

/// <summary>
/// Status of a recommendation.
/// </summary>
public enum RecommendationStatus
{
    /// <summary>
    /// Recommendation is active and should be shown.
    /// </summary>
    Active,

    /// <summary>
    /// Recommendation is being worked on.
    /// </summary>
    InProgress,

    /// <summary>
    /// Recommendation has been implemented/resolved.
    /// </summary>
    Resolved,

    /// <summary>
    /// Recommendation was dismissed as not applicable.
    /// </summary>
    Dismissed,

    /// <summary>
    /// Recommendation has expired (incidents are too old).
    /// </summary>
    Expired
}

/// <summary>
/// Result of AI incident analysis for a batch of incidents.
/// </summary>
public record IncidentAnalysisResult
{
    /// <summary>
    /// Application ID analyzed (null for portfolio-wide analysis).
    /// </summary>
    public string? ApplicationId { get; init; }

    /// <summary>
    /// Summary of the analysis.
    /// </summary>
    public required string Summary { get; init; }

    /// <summary>
    /// Generated recommendations.
    /// </summary>
    public List<IncidentRecommendation> Recommendations { get; init; } = [];

    /// <summary>
    /// Common themes identified across incidents.
    /// </summary>
    public List<string> CommonThemes { get; init; } = [];

    /// <summary>
    /// Potential quick wins identified.
    /// </summary>
    public List<QuickWin> QuickWins { get; init; } = [];

    /// <summary>
    /// When this analysis was generated.
    /// </summary>
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// AI confidence score (0-100).
    /// </summary>
    public int ConfidenceScore { get; init; }

    /// <summary>
    /// Number of incidents analyzed.
    /// </summary>
    public int IncidentsAnalyzed { get; init; }
}

/// <summary>
/// A quick win identified from incident analysis.
/// </summary>
public record QuickWin
{
    /// <summary>
    /// Title of the quick win.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Description of what to do.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Estimated impact (incidents that could be prevented).
    /// </summary>
    public string? EstimatedImpact { get; init; }

    /// <summary>
    /// Effort level.
    /// </summary>
    public string Effort { get; init; } = "Low";
}
