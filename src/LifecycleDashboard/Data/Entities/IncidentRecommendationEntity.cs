namespace LifecycleDashboard.Data.Entities;

/// <summary>
/// Database entity for AI-generated incident recommendations.
/// </summary>
public class IncidentRecommendationEntity
{
    public string Id { get; set; } = null!;

    /// <summary>
    /// Application this recommendation is for (null for portfolio-wide).
    /// </summary>
    public string? ApplicationId { get; set; }

    /// <summary>
    /// Application name for display.
    /// </summary>
    public string? ApplicationName { get; set; }

    /// <summary>
    /// Type of recommendation (stored as string enum).
    /// </summary>
    public string Type { get; set; } = "GeneralImprovement";

    /// <summary>
    /// Priority (1=highest, 5=lowest).
    /// </summary>
    public int Priority { get; set; } = 3;

    /// <summary>
    /// Recommendation title.
    /// </summary>
    public string Title { get; set; } = null!;

    /// <summary>
    /// Detailed description.
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// AI-generated root cause analysis.
    /// </summary>
    public string? RootCauseAnalysis { get; set; }

    /// <summary>
    /// Recommended action.
    /// </summary>
    public string RecommendedAction { get; set; } = null!;

    /// <summary>
    /// Expected impact.
    /// </summary>
    public string? ExpectedImpact { get; set; }

    /// <summary>
    /// Estimated effort (Low, Medium, High).
    /// </summary>
    public string? EstimatedEffort { get; set; }

    /// <summary>
    /// Related close codes (JSON array).
    /// </summary>
    public string RelatedCloseCodesJson { get; set; } = "[]";

    /// <summary>
    /// Related incident numbers (JSON array).
    /// </summary>
    public string RelatedIncidentNumbersJson { get; set; } = "[]";

    /// <summary>
    /// Number of incidents that contributed to this recommendation.
    /// </summary>
    public int IncidentCount { get; set; }

    /// <summary>
    /// AI confidence score (0-100).
    /// </summary>
    public int ConfidenceScore { get; set; }

    /// <summary>
    /// Status (stored as string enum).
    /// </summary>
    public string Status { get; set; } = "Active";

    /// <summary>
    /// When this recommendation was generated.
    /// </summary>
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When this recommendation was resolved/dismissed.
    /// </summary>
    public DateTimeOffset? ResolvedAt { get; set; }

    /// <summary>
    /// User notes or resolution notes.
    /// </summary>
    public string? Notes { get; set; }

    // Audit fields
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
