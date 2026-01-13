namespace LifecycleDashboard.Models;

/// <summary>
/// Represents a health score snapshot for historical tracking.
/// </summary>
public record HealthScoreSnapshot
{
    /// <summary>
    /// Application this score belongs to.
    /// </summary>
    public required string ApplicationId { get; init; }

    /// <summary>
    /// Date of this snapshot.
    /// </summary>
    public required DateTimeOffset Date { get; init; }

    /// <summary>
    /// Overall health score (0-100).
    /// </summary>
    public required int Score { get; init; }

    /// <summary>
    /// Breakdown of score components.
    /// </summary>
    public required HealthScoreBreakdown Breakdown { get; init; }
}

/// <summary>
/// Breakdown of health score components.
/// </summary>
public record HealthScoreBreakdown
{
    /// <summary>
    /// Base score before adjustments (starts at 100).
    /// </summary>
    public int BaseScore { get; init; } = 100;

    /// <summary>
    /// Security vulnerability penalty.
    /// </summary>
    public int SecurityPenalty { get; init; }

    /// <summary>
    /// Usage metrics adjustment (can be positive or negative).
    /// </summary>
    public int UsageAdjustment { get; init; }

    /// <summary>
    /// Maintenance activity adjustment.
    /// </summary>
    public int MaintenanceAdjustment { get; init; }

    /// <summary>
    /// Documentation completeness adjustment.
    /// </summary>
    public int DocumentationAdjustment { get; init; }

    /// <summary>
    /// Overdue task penalty.
    /// </summary>
    public int OverdueTaskPenalty { get; init; }

    /// <summary>
    /// Data conflict penalty.
    /// </summary>
    public int DataConflictPenalty { get; init; }

    /// <summary>
    /// Incident history penalty (high incident count indicates instability).
    /// </summary>
    public int IncidentPenalty { get; init; }

    /// <summary>
    /// Final calculated score.
    /// </summary>
    public int FinalScore => Math.Max(0, Math.Min(100,
        BaseScore
        - SecurityPenalty
        + UsageAdjustment
        + MaintenanceAdjustment
        + DocumentationAdjustment
        - OverdueTaskPenalty
        - DataConflictPenalty
        - IncidentPenalty));

    /// <summary>
    /// Details about security findings impact.
    /// </summary>
    public SecurityScoreDetails? SecurityDetails { get; init; }

    /// <summary>
    /// Details about incident impact.
    /// </summary>
    public IncidentScoreDetails? IncidentDetails { get; init; }
}

/// <summary>
/// Details about security findings impact on health score.
/// </summary>
public record SecurityScoreDetails
{
    public int CriticalCount { get; init; }
    public int HighCount { get; init; }
    public int MediumCount { get; init; }
    public int LowCount { get; init; }

    /// <summary>
    /// Calculate penalty based on vulnerability counts.
    /// Critical: -15 each (max -60)
    /// High: -8 each (max -40)
    /// Medium: -2 each (max -20)
    /// Low: -0.5 each (max -10)
    /// </summary>
    public int CalculatePenalty()
    {
        var criticalPenalty = Math.Min(60, CriticalCount * 15);
        var highPenalty = Math.Min(40, HighCount * 8);
        var mediumPenalty = Math.Min(20, MediumCount * 2);
        var lowPenalty = Math.Min(10, (int)(LowCount * 0.5));

        return criticalPenalty + highPenalty + mediumPenalty + lowPenalty;
    }
}

/// <summary>
/// Details about incident impact on health score.
/// </summary>
public record IncidentScoreDetails
{
    /// <summary>
    /// Total incidents linked to this application.
    /// </summary>
    public int TotalIncidents { get; init; }

    /// <summary>
    /// Incidents in the last 90 days.
    /// </summary>
    public int RecentIncidents { get; init; }

    /// <summary>
    /// Number of repeat incident patterns (same close code appearing 3+ times).
    /// </summary>
    public int RepeatPatterns { get; init; }

    /// <summary>
    /// Most common close codes and their counts.
    /// </summary>
    public Dictionary<string, int> CloseCodeCounts { get; init; } = new();

    /// <summary>
    /// Calculate penalty based on incident metrics.
    /// Recent incidents (90 days): -2 each (max -20)
    /// Repeat patterns: -3 each (max -15)
    /// </summary>
    public int CalculatePenalty()
    {
        var recentPenalty = Math.Min(20, RecentIncidents * 2);
        var repeatPenalty = Math.Min(15, RepeatPatterns * 3);

        return recentPenalty + repeatPenalty;
    }
}

/// <summary>
/// Portfolio-wide health summary.
/// </summary>
public record PortfolioHealthSummary
{
    public int TotalApplications { get; init; }
    public int HealthyCount { get; init; }
    public int NeedsAttentionCount { get; init; }
    public int AtRiskCount { get; init; }
    public int CriticalCount { get; init; }
    public double AverageScore { get; init; }
    public int TrendDirection { get; init; } // -1, 0, or 1
    public DateTimeOffset LastUpdated { get; init; }

    public double HealthyPercentage => TotalApplications > 0
        ? Math.Round(100.0 * HealthyCount / TotalApplications, 1)
        : 0;
}
