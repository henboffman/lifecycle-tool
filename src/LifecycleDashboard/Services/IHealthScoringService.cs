using LifecycleDashboard.Models;

namespace LifecycleDashboard.Services;

/// <summary>
/// Service for calculating and managing application health scores.
/// Implements the scoring algorithm from requirements:
/// - Security vulnerabilities: Critical -15, High -8, Medium -2, Low -0.5
/// - Usage: None -20, VeryLow -10, Low -5, Moderate 0, High +5
/// - Maintenance: Recent +10, Moderate +5, Low 0, Inactive -5, Stale -10
/// - Documentation: Both +10, One missing -10, Both missing -15
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
    /// Calculates documentation adjustment.
    /// </summary>
    int CalculateDocumentationAdjustment(DocumentationStatus documentation);

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
