using LifecycleDashboard.Models;

namespace LifecycleDashboard.Services;

/// <summary>
/// Implementation of health scoring algorithm based on requirements.
///
/// Scoring Algorithm (0-100 scale, starts at 100):
///
/// Security Vulnerability Penalties:
/// - Critical: -15 points each (max -60)
/// - High: -8 points each (max -40)
/// - Medium: -2 points each (max -20)
/// - Low: -0.5 points each (max -10)
///
/// Usage Metrics:
/// - No usage: -20 points
/// - Very low (1-100 req/month): -10 points
/// - Low (101-1000): -5 points
/// - Moderate (1001-10000): 0 points
/// - High (10001+): +5 points
///
/// Active Maintenance Bonus:
/// - Recent commits (less than 30 days): +10 points
/// - Moderate (31-90 days): +5 points
/// - Low (91-180 days): 0 points
/// - Inactive (181-365 days): -5 points
/// - Stale (365+ days): -10 points
///
/// Documentation Completeness:
/// - Required: Architecture diagram + system documentation
/// - Both present: +10 points
/// - One missing: -10 points
/// - Both missing: -15 points
///
/// Overdue Tasks:
/// - Each overdue task: -3 points
/// - Overdue 30+ days: -5 points instead
///
/// Incident History:
/// - Recent incidents (90 days): -2 points each (max -20)
/// - Repeat patterns (same close code 3+ times): -3 points each (max -15)
///
/// Health Categories:
/// - Healthy: 80-100 (Green)
/// - Needs Attention: 60-79 (Yellow/Amber)
/// - At Risk: 40-59 (Orange)
/// - Critical: 0-39 (Red)
/// </summary>
public class HealthScoringService : IHealthScoringService
{
    private const int BaseScore = 100;

    // Security penalty caps
    private const int MaxCriticalPenalty = 60;
    private const int MaxHighPenalty = 40;
    private const int MaxMediumPenalty = 20;
    private const int MaxLowPenalty = 10;

    // Security penalty per finding
    private const int CriticalPenaltyPerFinding = 15;
    private const int HighPenaltyPerFinding = 8;
    private const int MediumPenaltyPerFinding = 2;
    private const double LowPenaltyPerFinding = 0.5;

    // Usage adjustments
    private const int NoUsagePenalty = -20;
    private const int VeryLowUsagePenalty = -10;
    private const int LowUsagePenalty = -5;
    private const int ModerateUsageAdjustment = 0;
    private const int HighUsageBonus = 5;

    // Maintenance adjustments
    private const int RecentMaintenanceBonus = 10;
    private const int ModerateMaintenanceBonus = 5;
    private const int LowMaintenanceAdjustment = 0;
    private const int InactiveMaintenancePenalty = -5;
    private const int StaleMaintenancePenalty = -10;

    // Documentation adjustments
    private const int BothDocsPresentBonus = 10;
    private const int OneDocMissingPenalty = -10;
    private const int BothDocsMissingPenalty = -15;

    // Overdue task penalties
    private const int OverdueTaskPenalty = 3;
    private const int SeverelyOverdueTaskPenalty = 5;
    private const int SeverelyOverdueDays = 30;

    // Incident penalties
    private const int RecentIncidentDays = 90;
    private const int RecentIncidentPenaltyPerIncident = 2;
    private const int MaxRecentIncidentPenalty = 20;
    private const int RepeatPatternThreshold = 3;
    private const int RepeatPatternPenalty = 3;
    private const int MaxRepeatPatternPenalty = 15;

    public HealthScoreBreakdown CalculateHealthScore(Application application, IEnumerable<LifecycleTask> tasks)
    {
        var securityPenalty = CalculateSecurityPenalty(application.SecurityFindings);
        var usageAdjustment = CalculateUsageAdjustment(application.Usage);
        var maintenanceAdjustment = CalculateMaintenanceAdjustment(application.LastActivityDate);
        var documentationAdjustment = CalculateDocumentationAdjustment(application.Documentation);
        var overdueTaskPenalty = CalculateOverdueTaskPenalty(tasks);
        var dataConflictPenalty = application.HasDataConflicts ? 5 : 0;

        var securityDetails = new SecurityScoreDetails
        {
            CriticalCount = application.SecurityFindings.Count(f => f.Severity == SecuritySeverity.Critical && !f.IsResolved),
            HighCount = application.SecurityFindings.Count(f => f.Severity == SecuritySeverity.High && !f.IsResolved),
            MediumCount = application.SecurityFindings.Count(f => f.Severity == SecuritySeverity.Medium && !f.IsResolved),
            LowCount = application.SecurityFindings.Count(f => f.Severity == SecuritySeverity.Low && !f.IsResolved)
        };

        return new HealthScoreBreakdown
        {
            BaseScore = BaseScore,
            SecurityPenalty = securityPenalty,
            UsageAdjustment = usageAdjustment,
            MaintenanceAdjustment = maintenanceAdjustment,
            DocumentationAdjustment = documentationAdjustment,
            OverdueTaskPenalty = overdueTaskPenalty,
            DataConflictPenalty = dataConflictPenalty,
            SecurityDetails = securityDetails
        };
    }

    public HealthCategory GetCategory(int score)
    {
        return score switch
        {
            >= 80 => HealthCategory.Healthy,
            >= 60 => HealthCategory.NeedsAttention,
            >= 40 => HealthCategory.AtRisk,
            _ => HealthCategory.Critical
        };
    }

    public int CalculateSecurityPenalty(IEnumerable<SecurityFinding> findings)
    {
        var unresolvedFindings = findings.Where(f => !f.IsResolved).ToList();

        var criticalCount = unresolvedFindings.Count(f => f.Severity == SecuritySeverity.Critical);
        var highCount = unresolvedFindings.Count(f => f.Severity == SecuritySeverity.High);
        var mediumCount = unresolvedFindings.Count(f => f.Severity == SecuritySeverity.Medium);
        var lowCount = unresolvedFindings.Count(f => f.Severity == SecuritySeverity.Low);

        var criticalPenalty = Math.Min(MaxCriticalPenalty, criticalCount * CriticalPenaltyPerFinding);
        var highPenalty = Math.Min(MaxHighPenalty, highCount * HighPenaltyPerFinding);
        var mediumPenalty = Math.Min(MaxMediumPenalty, mediumCount * MediumPenaltyPerFinding);
        var lowPenalty = Math.Min(MaxLowPenalty, (int)(lowCount * LowPenaltyPerFinding));

        return criticalPenalty + highPenalty + mediumPenalty + lowPenalty;
    }

    public int CalculateUsageAdjustment(UsageMetrics? usage)
    {
        if (usage == null)
            return NoUsagePenalty;

        return usage.Level switch
        {
            UsageLevel.None => NoUsagePenalty,
            UsageLevel.VeryLow => VeryLowUsagePenalty,
            UsageLevel.Low => LowUsagePenalty,
            UsageLevel.Moderate => ModerateUsageAdjustment,
            UsageLevel.High => HighUsageBonus,
            _ => 0
        };
    }

    public int CalculateMaintenanceAdjustment(DateTimeOffset? lastActivityDate)
    {
        if (!lastActivityDate.HasValue)
            return StaleMaintenancePenalty;

        var daysSinceActivity = (DateTimeOffset.UtcNow - lastActivityDate.Value).TotalDays;

        return daysSinceActivity switch
        {
            <= 30 => RecentMaintenanceBonus,
            <= 90 => ModerateMaintenanceBonus,
            <= 180 => LowMaintenanceAdjustment,
            <= 365 => InactiveMaintenancePenalty,
            _ => StaleMaintenancePenalty
        };
    }

    public int CalculateDocumentationAdjustment(DocumentationStatus documentation)
    {
        var hasArchitecture = documentation.HasArchitectureDiagram;
        var hasSystemDocs = documentation.HasSystemDocumentation;

        return (hasArchitecture, hasSystemDocs) switch
        {
            (true, true) => BothDocsPresentBonus,
            (false, false) => BothDocsMissingPenalty,
            _ => OneDocMissingPenalty
        };
    }

    public (int Adjustment, DocumentationScoreDetails Details) CalculateDocumentationAdjustment(
        DocumentationStatus documentation,
        bool hasReadme,
        int? readmeQualityScore,
        DocumentationScoringWeights? weights = null)
    {
        weights ??= new DocumentationScoringWeights();

        // Calculate README adjustment
        int readmeAdjustment;
        if (hasReadme)
        {
            readmeAdjustment = weights.ReadmePresent;
            // Add quality bonus if README is high quality (score >= 70)
            if (readmeQualityScore.HasValue && readmeQualityScore.Value >= 70)
            {
                readmeAdjustment += weights.ReadmeQuality;
            }
        }
        else
        {
            readmeAdjustment = weights.ReadmeMissing;
        }

        // Calculate SharePoint documentation adjustments
        var architectureAdjustment = documentation.HasArchitectureDiagram
            ? weights.ArchitectureDiagram
            : weights.ArchitectureMissing;

        var systemDocsAdjustment = documentation.HasSystemDocumentation
            ? weights.SystemDocumentation
            : weights.SystemDocsMissing;

        var userDocsAdjustment = documentation.HasUserDocumentation
            ? weights.UserDocumentation
            : 0; // No penalty for missing user docs

        var supportDocsAdjustment = documentation.HasSupportDocumentation
            ? weights.SupportDocumentation
            : 0; // No penalty for missing support docs

        // For project documents, we don't have a direct flag, but we can use HasUserDocumentation
        // or consider this as always 0 for now (would need SharePoint folder checking)
        var projectDocsAdjustment = 0;

        // Calculate total
        var rawTotal = readmeAdjustment + architectureAdjustment + systemDocsAdjustment
                     + userDocsAdjustment + supportDocsAdjustment + projectDocsAdjustment;

        // Apply limits
        var finalAdjustment = rawTotal > 0
            ? Math.Min(rawTotal, weights.MaxBonus)
            : Math.Max(rawTotal, weights.MaxPenalty);

        var details = new DocumentationScoreDetails
        {
            HasReadme = hasReadme,
            ReadmeQualityScore = readmeQualityScore,
            ReadmeAdjustment = readmeAdjustment,
            HasArchitectureDiagram = documentation.HasArchitectureDiagram,
            ArchitectureAdjustment = architectureAdjustment,
            HasSystemDocumentation = documentation.HasSystemDocumentation,
            SystemDocsAdjustment = systemDocsAdjustment,
            HasUserDocumentation = documentation.HasUserDocumentation,
            UserDocsAdjustment = userDocsAdjustment,
            HasSupportDocumentation = documentation.HasSupportDocumentation,
            SupportDocsAdjustment = supportDocsAdjustment,
            HasProjectDocuments = false, // Would need SharePoint integration
            ProjectDocsAdjustment = projectDocsAdjustment,
            FinalAdjustment = finalAdjustment
        };

        return (finalAdjustment, details);
    }

    public int CalculateOverdueTaskPenalty(IEnumerable<LifecycleTask> tasks)
    {
        var overdueTasks = tasks.Where(t => t.IsOverdue).ToList();

        if (overdueTasks.Count == 0)
            return 0;

        var totalPenalty = 0;

        foreach (var task in overdueTasks)
        {
            if (task.DaysOverdue >= SeverelyOverdueDays)
            {
                totalPenalty += SeverelyOverdueTaskPenalty;
            }
            else
            {
                totalPenalty += OverdueTaskPenalty;
            }
        }

        return totalPenalty;
    }

    public HealthScoreBreakdown CalculateHealthScore(Application application, IEnumerable<LifecycleTask> tasks, IEnumerable<ServiceNowIncident> incidents)
    {
        var securityPenalty = CalculateSecurityPenalty(application.SecurityFindings);
        var usageAdjustment = CalculateUsageAdjustment(application.Usage);
        var maintenanceAdjustment = CalculateMaintenanceAdjustment(application.LastActivityDate);
        var documentationAdjustment = CalculateDocumentationAdjustment(application.Documentation);
        var overdueTaskPenalty = CalculateOverdueTaskPenalty(tasks);
        var dataConflictPenalty = application.HasDataConflicts ? 5 : 0;
        var (incidentPenalty, incidentDetails) = CalculateIncidentPenalty(incidents);

        var securityDetails = new SecurityScoreDetails
        {
            CriticalCount = application.SecurityFindings.Count(f => f.Severity == SecuritySeverity.Critical && !f.IsResolved),
            HighCount = application.SecurityFindings.Count(f => f.Severity == SecuritySeverity.High && !f.IsResolved),
            MediumCount = application.SecurityFindings.Count(f => f.Severity == SecuritySeverity.Medium && !f.IsResolved),
            LowCount = application.SecurityFindings.Count(f => f.Severity == SecuritySeverity.Low && !f.IsResolved)
        };

        return new HealthScoreBreakdown
        {
            BaseScore = BaseScore,
            SecurityPenalty = securityPenalty,
            UsageAdjustment = usageAdjustment,
            MaintenanceAdjustment = maintenanceAdjustment,
            DocumentationAdjustment = documentationAdjustment,
            OverdueTaskPenalty = overdueTaskPenalty,
            DataConflictPenalty = dataConflictPenalty,
            IncidentPenalty = incidentPenalty,
            SecurityDetails = securityDetails,
            IncidentDetails = incidentDetails
        };
    }

    public (int Penalty, IncidentScoreDetails Details) CalculateIncidentPenalty(IEnumerable<ServiceNowIncident> incidents)
    {
        var incidentList = incidents.ToList();

        if (incidentList.Count == 0)
        {
            return (0, new IncidentScoreDetails());
        }

        // Calculate recent incidents (last 90 days)
        var cutoffDate = DateTimeOffset.UtcNow.AddDays(-RecentIncidentDays);
        var recentIncidents = incidentList.Count(i => i.ImportedAt >= cutoffDate);

        // Calculate repeat patterns (same close code appearing 3+ times)
        var closeCodeCounts = incidentList
            .Where(i => !string.IsNullOrEmpty(i.CloseCode))
            .GroupBy(i => i.CloseCode!)
            .ToDictionary(g => g.Key, g => g.Count());

        var repeatPatterns = closeCodeCounts.Count(kv => kv.Value >= RepeatPatternThreshold);

        // Calculate penalties
        var recentPenalty = Math.Min(MaxRecentIncidentPenalty, recentIncidents * RecentIncidentPenaltyPerIncident);
        var repeatPenalty = Math.Min(MaxRepeatPatternPenalty, repeatPatterns * RepeatPatternPenalty);
        var totalPenalty = recentPenalty + repeatPenalty;

        var details = new IncidentScoreDetails
        {
            TotalIncidents = incidentList.Count,
            RecentIncidents = recentIncidents,
            RepeatPatterns = repeatPatterns,
            CloseCodeCounts = closeCodeCounts
        };

        return (totalPenalty, details);
    }
}
