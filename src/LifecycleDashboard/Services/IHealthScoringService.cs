using LifecycleDashboard.Models;

namespace LifecycleDashboard.Services;

/// <summary>
/// Configurable weights for documentation health scoring.
/// </summary>
public record DocumentationScoringWeights
{
    // Repository README
    public int ReadmePresent { get; init; } = 5;
    public int ReadmeQuality { get; init; } = 5;
    public int ReadmeMissing { get; init; } = -10;

    // SharePoint Documentation
    public int ArchitectureDiagram { get; init; } = 8;
    public int SystemDocumentation { get; init; } = 8;
    public int UserDocumentation { get; init; } = 3;
    public int SupportDocumentation { get; init; } = 3;
    public int ProjectDocuments { get; init; } = 2;

    // Penalties for missing critical docs
    public int ArchitectureMissing { get; init; } = -8;
    public int SystemDocsMissing { get; init; } = -8;

    // Limits
    public int MaxBonus { get; init; } = 20;
    public int MaxPenalty { get; init; } = -25;
}

/// <summary>
/// Service for calculating and managing application health scores.
/// Implements the scoring algorithm from requirements:
/// - Security vulnerabilities: Critical -15, High -8, Medium -2, Low -0.5
/// - Usage: None -20, VeryLow -10, Low -5, Moderate 0, High +5
/// - Maintenance: Recent +10, Moderate +5, Low 0, Inactive -5, Stale -10
/// - Documentation: Configurable weights for README, architecture, system docs, etc.
/// - Overdue tasks: -3 each, -5 if 30+ days overdue
/// - Incidents: -2 per recent incident (max -20), -3 per repeat pattern (max -15)
/// </summary>
public interface IHealthScoringService
{
    /// <summary>
    /// Calculates the health score for an application.
    /// </summary>
    HealthScoreBreakdown CalculateHealthScore(Application application, IEnumerable<LifecycleTask> tasks);

    /// <summary>
    /// Calculates the health score for an application including incident data.
    /// </summary>
    HealthScoreBreakdown CalculateHealthScore(Application application, IEnumerable<LifecycleTask> tasks, IEnumerable<ServiceNowIncident> incidents);

    /// <summary>
    /// Gets the health category for a score.
    /// </summary>
    HealthCategory GetCategory(int score);

    /// <summary>
    /// Calculates security penalty based on findings.
    /// </summary>
    int CalculateSecurityPenalty(IEnumerable<SecurityFinding> findings);

    /// <summary>
    /// Calculates usage adjustment.
    /// </summary>
    int CalculateUsageAdjustment(UsageMetrics? usage);

    /// <summary>
    /// Calculates maintenance activity adjustment.
    /// </summary>
    int CalculateMaintenanceAdjustment(DateTimeOffset? lastActivityDate);

    /// <summary>
    /// Calculates documentation adjustment using default weights.
    /// </summary>
    int CalculateDocumentationAdjustment(DocumentationStatus documentation);

    /// <summary>
    /// Calculates documentation adjustment with configurable weights and README info.
    /// </summary>
    /// <param name="documentation">SharePoint documentation status.</param>
    /// <param name="hasReadme">Whether repository has README.md.</param>
    /// <param name="readmeQualityScore">README quality score (0-100), null if no README.</param>
    /// <param name="weights">Configurable weights for each documentation type.</param>
    /// <returns>Tuple of final adjustment and detailed breakdown.</returns>
    (int Adjustment, DocumentationScoreDetails Details) CalculateDocumentationAdjustment(
        DocumentationStatus documentation,
        bool hasReadme,
        int? readmeQualityScore,
        DocumentationScoringWeights? weights = null);

    /// <summary>
    /// Calculates overdue task penalty.
    /// </summary>
    int CalculateOverdueTaskPenalty(IEnumerable<LifecycleTask> tasks);

    /// <summary>
    /// Calculates incident penalty based on incident history.
    /// Recent incidents (90 days): -2 each (max -20)
    /// Repeat patterns (same close code 3+ times): -3 each (max -15)
    /// </summary>
    (int Penalty, IncidentScoreDetails Details) CalculateIncidentPenalty(IEnumerable<ServiceNowIncident> incidents);
}
