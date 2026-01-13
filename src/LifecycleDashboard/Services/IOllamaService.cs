using LifecycleDashboard.Models;

namespace LifecycleDashboard.Services;

/// <summary>
/// Service for AI-powered recommendations using Ollama (local) or Azure OpenAI.
/// </summary>
public interface IAiRecommendationService
{
    /// <summary>
    /// Checks if the AI service is available and configured.
    /// </summary>
    Task<AiServiceStatus> GetServiceStatusAsync();

    /// <summary>
    /// Generates portfolio-wide insights from application data.
    /// </summary>
    Task<PortfolioInsights> GeneratePortfolioInsightsAsync(
        IEnumerable<Application> applications,
        IEnumerable<LifecycleTask> tasks);

    /// <summary>
    /// Analyzes a specific application and provides recommendations.
    /// </summary>
    Task<ApplicationAnalysis> AnalyzeApplicationAsync(Application application);

    /// <summary>
    /// Identifies patterns and correlations across applications.
    /// </summary>
    Task<PatternAnalysis> IdentifyPatternsAsync(IEnumerable<Application> applications);

    /// <summary>
    /// Predicts which applications are at risk of health decline.
    /// </summary>
    Task<RiskPrediction> PredictHealthRisksAsync(IEnumerable<Application> applications);

    /// <summary>
    /// Generates actionable next steps for the user.
    /// </summary>
    Task<ActionPlan> GenerateActionPlanAsync(
        IEnumerable<Application> criticalApps,
        IEnumerable<LifecycleTask> overdueTasks);

    /// <summary>
    /// Analyzes incidents for an application to identify patterns, root causes, and recommendations.
    /// </summary>
    Task<IncidentAnalysisResult> AnalyzeIncidentsAsync(
        Application application,
        IEnumerable<ServiceNowIncident> incidents);

    /// <summary>
    /// Analyzes all incidents across the portfolio to identify common patterns and quick wins.
    /// </summary>
    Task<IncidentAnalysisResult> AnalyzePortfolioIncidentsAsync(
        IEnumerable<ServiceNowIncident> incidents,
        IEnumerable<Application> applications);
}

public record AiServiceStatus
{
    public bool IsAvailable { get; init; }
    public string Provider { get; init; } = "None";
    public string Model { get; init; } = "";
    public string? Error { get; init; }
    public TimeSpan? ResponseTime { get; init; }
}

public record PortfolioInsights
{
    public string Summary { get; init; } = "";
    public List<InsightItem> KeyInsights { get; init; } = [];
    public List<TrendPrediction> Trends { get; init; } = [];
    public int ConfidenceScore { get; init; }
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;
}

public record InsightItem
{
    public required string Category { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public string Severity { get; init; } = "Info";
    public List<string> AffectedApplications { get; init; } = [];
    public string? RecommendedAction { get; init; }
}

public record TrendPrediction
{
    public required string Metric { get; init; }
    public required string Direction { get; init; }
    public required string Timeframe { get; init; }
    public required string Reasoning { get; init; }
    public int Confidence { get; init; }
}

public record ApplicationAnalysis
{
    public required string ApplicationId { get; init; }
    public required string Summary { get; init; }
    public List<string> Strengths { get; init; } = [];
    public List<string> Weaknesses { get; init; } = [];
    public List<string> Recommendations { get; init; } = [];
    public int PredictedHealthIn30Days { get; init; }
    public string RiskLevel { get; init; } = "Low";
}

public record PatternAnalysis
{
    public List<VulnerabilityPattern> VulnerabilityPatterns { get; init; } = [];
    public List<MaintenancePattern> MaintenancePatterns { get; init; } = [];
    public List<ApplicationCluster> Clusters { get; init; } = [];
}

public record VulnerabilityPattern
{
    public required string PatternName { get; init; }
    public required string Description { get; init; }
    public List<string> AffectedApplications { get; init; } = [];
    public string? RootCauseSuggestion { get; init; }
    public int PotentialImpact { get; init; }
}

public record MaintenancePattern
{
    public required string Pattern { get; init; }
    public required string Description { get; init; }
    public List<string> ApplicationsFollowing { get; init; } = [];
}

public record ApplicationCluster
{
    public required string ClusterName { get; init; }
    public required string CommonCharacteristic { get; init; }
    public List<string> Applications { get; init; } = [];
}

public record RiskPrediction
{
    public List<AtRiskApplication> AtRiskApplications { get; init; } = [];
    public string OverallAssessment { get; init; } = "";
}

public record AtRiskApplication
{
    public required string ApplicationId { get; init; }
    public required string ApplicationName { get; init; }
    public int CurrentScore { get; init; }
    public int PredictedScore { get; init; }
    public required string RiskReason { get; init; }
    public required string TimeToDecline { get; init; }
    public List<string> PreventiveActions { get; init; } = [];
}

public record ActionPlan
{
    public required string Summary { get; init; }
    public List<PrioritizedAction> Actions { get; init; } = [];
    public string EstimatedImpact { get; init; } = "";
}

public record PrioritizedAction
{
    public int Priority { get; init; }
    public required string Action { get; init; }
    public required string Rationale { get; init; }
    public List<string> TargetApplications { get; init; } = [];
    public string ExpectedOutcome { get; init; } = "";
}
