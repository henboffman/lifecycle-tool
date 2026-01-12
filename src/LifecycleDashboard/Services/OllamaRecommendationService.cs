using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using LifecycleDashboard.Models;

namespace LifecycleDashboard.Services;

/// <summary>
/// AI recommendation service using local Ollama instance.
/// Provides portfolio analysis, pattern detection, and actionable recommendations.
/// </summary>
public class OllamaRecommendationService : IAiRecommendationService
{
    private readonly HttpClient _httpClient;
    private readonly ISecureStorageService _secureStorage;
    private readonly ILogger<OllamaRecommendationService> _logger;

    private const string DefaultEndpoint = "http://localhost:11434";
    private const string DefaultModel = "llama3.2";

    public OllamaRecommendationService(
        HttpClient httpClient,
        ISecureStorageService secureStorage,
        ILogger<OllamaRecommendationService> logger)
    {
        _httpClient = httpClient;
        _secureStorage = secureStorage;
        _logger = logger;
    }

    public async Task<AiServiceStatus> GetServiceStatusAsync()
    {
        var endpoint = await _secureStorage.GetSecretAsync(SecretKeys.OllamaEndpoint) ?? DefaultEndpoint;
        var model = await _secureStorage.GetSecretAsync(SecretKeys.OllamaModel) ?? DefaultModel;

        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            _httpClient.BaseAddress = new Uri(endpoint);

            var response = await _httpClient.GetAsync("/api/tags");
            sw.Stop();

            if (response.IsSuccessStatusCode)
            {
                return new AiServiceStatus
                {
                    IsAvailable = true,
                    Provider = "Ollama (Local)",
                    Model = model,
                    ResponseTime = sw.Elapsed
                };
            }

            return new AiServiceStatus
            {
                IsAvailable = false,
                Provider = "Ollama (Local)",
                Model = model,
                Error = $"Ollama returned {response.StatusCode}"
            };
        }
        catch (Exception ex)
        {
            return new AiServiceStatus
            {
                IsAvailable = false,
                Provider = "Ollama (Local)",
                Model = model,
                Error = $"Cannot connect to Ollama at {endpoint}: {ex.Message}"
            };
        }
    }

    public async Task<PortfolioInsights> GeneratePortfolioInsightsAsync(
        IEnumerable<Application> applications,
        IEnumerable<LifecycleTask> tasks)
    {
        var appList = applications.ToList();
        var taskList = tasks.ToList();

        var prompt = BuildPortfolioPrompt(appList, taskList);
        var response = await CallOllamaAsync(prompt);

        if (string.IsNullOrEmpty(response))
        {
            return GenerateFallbackInsightsWithApps(appList, taskList);
        }

        return ParsePortfolioInsights(response, appList);
    }

    public async Task<ApplicationAnalysis> AnalyzeApplicationAsync(Application application)
    {
        var prompt = $@"Analyze this application for an IT portfolio health dashboard:

Application: {application.Name}
Capability: {application.Capability}
Health Score: {application.HealthScore}/100 ({application.HealthCategory})
Security Issues: {application.SecurityFindings.Count(f => !f.IsResolved)} unresolved
Technology Stack: {string.Join(", ", application.TechnologyStack)}
Last Activity: {application.LastActivityDate?.ToString("yyyy-MM-dd") ?? "Unknown"}
Usage Level: {application.Usage?.Level.ToString() ?? "Unknown"}
Documentation: Architecture={application.Documentation.HasArchitectureDiagram}, System={application.Documentation.HasSystemDocumentation}

Provide a brief analysis with:
1. Key strengths (2-3 points)
2. Areas of concern (2-3 points)
3. Specific recommendations (2-3 actionable items)
4. Predicted health score in 30 days if no action taken
5. Overall risk level (Low/Medium/High/Critical)

Be concise and specific.";

        var response = await CallOllamaAsync(prompt);

        // Parse or generate fallback
        return new ApplicationAnalysis
        {
            ApplicationId = application.Id,
            Summary = response ?? $"{application.Name} requires attention due to health score of {application.HealthScore}",
            Strengths = ExtractListFromResponse(response, "strengths") ?? GenerateStrengths(application),
            Weaknesses = ExtractListFromResponse(response, "concerns") ?? GenerateWeaknesses(application),
            Recommendations = ExtractListFromResponse(response, "recommendations") ?? GenerateRecommendations(application),
            PredictedHealthIn30Days = PredictHealth(application),
            RiskLevel = DetermineRiskLevel(application)
        };
    }

    public async Task<PatternAnalysis> IdentifyPatternsAsync(IEnumerable<Application> applications)
    {
        var appList = applications.ToList();

        var prompt = $@"Analyze these {appList.Count} applications for patterns:

{string.Join("\n", appList.Take(20).Select(a => $"- {a.Name}: Score={a.HealthScore}, Vulns={a.SecurityFindings.Count(f => !f.IsResolved)}, Stack=[{string.Join(",", a.TechnologyStack.Take(3))}]"))}

Identify:
1. Common vulnerability patterns (shared issues that could have a common fix)
2. Maintenance patterns (groupings by activity level)
3. Technology clusters (apps sharing similar stacks)

Focus on actionable patterns where fixing one root cause helps multiple apps.";

        var response = await CallOllamaAsync(prompt);

        return new PatternAnalysis
        {
            VulnerabilityPatterns = IdentifyVulnPatterns(appList),
            MaintenancePatterns = IdentifyMaintenancePatterns(appList),
            Clusters = IdentifyTechClusters(appList)
        };
    }

    public async Task<RiskPrediction> PredictHealthRisksAsync(IEnumerable<Application> applications)
    {
        var appList = applications.ToList();
        var atRisk = new List<AtRiskApplication>();

        foreach (var app in appList.Where(a => a.HealthCategory != HealthCategory.Healthy))
        {
            var predictedScore = PredictHealth(app);
            if (predictedScore < app.HealthScore)
            {
                atRisk.Add(new AtRiskApplication
                {
                    ApplicationId = app.Id,
                    ApplicationName = app.Name,
                    CurrentScore = app.HealthScore,
                    PredictedScore = predictedScore,
                    RiskReason = GetRiskReason(app),
                    TimeToDecline = GetDeclineTimeframe(app),
                    PreventiveActions = GenerateRecommendations(app)
                });
            }
        }

        var prompt = $@"Given {atRisk.Count} applications predicted to decline in health, provide a 2-sentence overall risk assessment for the portfolio.";

        var response = await CallOllamaAsync(prompt);

        return new RiskPrediction
        {
            AtRiskApplications = atRisk.OrderBy(a => a.PredictedScore).ToList(),
            OverallAssessment = response ?? $"{atRisk.Count} applications are at risk of health decline. Prioritize addressing security vulnerabilities and documentation gaps."
        };
    }

    public async Task<ActionPlan> GenerateActionPlanAsync(
        IEnumerable<Application> criticalApps,
        IEnumerable<LifecycleTask> overdueTasks)
    {
        var apps = criticalApps.ToList();
        var tasks = overdueTasks.ToList();

        var actions = new List<PrioritizedAction>();
        var priority = 1;

        // Generate structured actions for critical apps
        foreach (var app in apps.OrderBy(a => a.HealthScore).Take(5))
        {
            var criticalVulns = app.SecurityFindings.Count(f => !f.IsResolved && f.Severity == SecuritySeverity.Critical);
            var highVulns = app.SecurityFindings.Count(f => !f.IsResolved && f.Severity == SecuritySeverity.High);

            actions.Add(new PrioritizedAction
            {
                Priority = priority++,
                Action = $"Remediate security vulnerabilities in {app.Name}",
                Rationale = criticalVulns > 0
                    ? $"Contains {criticalVulns} critical and {highVulns} high-severity vulnerabilities requiring immediate attention"
                    : $"Health score of {app.HealthScore} indicates multiple issues requiring remediation",
                TargetApplications = [app.Name],
                ExpectedOutcome = $"Improve health score from {app.HealthScore} to {Math.Min(100, app.HealthScore + 15)}"
            });
        }

        // Generate structured actions for overdue tasks
        foreach (var task in tasks.OrderByDescending(t => t.DaysOverdue).Take(3))
        {
            actions.Add(new PrioritizedAction
            {
                Priority = priority++,
                Action = task.Title,
                Rationale = $"Task is {task.DaysOverdue} days overdue and impacts {task.ApplicationName}",
                TargetApplications = [task.ApplicationName],
                ExpectedOutcome = "Resolve compliance issue and reduce overdue task count"
            });
        }

        // Generate clean summary
        var summary = apps.Count > 0 || tasks.Count > 0
            ? $"Prioritize addressing {apps.Count} critical application{(apps.Count == 1 ? "" : "s")} and {tasks.Count} overdue task{(tasks.Count == 1 ? "" : "s")} to stabilize portfolio health."
            : "No critical issues requiring immediate action. Continue monitoring portfolio health.";

        return new ActionPlan
        {
            Summary = summary,
            Actions = actions,
            EstimatedImpact = apps.Count > 0 || tasks.Count > 0
                ? $"Expected portfolio health improvement: {Math.Min(20, apps.Count * 4 + tasks.Count * 2)}%"
                : "Maintain current health levels"
        };
    }

    private async Task<string?> CallOllamaAsync(string prompt)
    {
        try
        {
            var endpoint = await _secureStorage.GetSecretAsync(SecretKeys.OllamaEndpoint) ?? DefaultEndpoint;
            var model = await _secureStorage.GetSecretAsync(SecretKeys.OllamaModel) ?? DefaultModel;

            var request = new OllamaRequest
            {
                Model = model,
                Prompt = prompt,
                Stream = false
            };

            using var client = new HttpClient { BaseAddress = new Uri(endpoint) };
            client.Timeout = TimeSpan.FromSeconds(60);

            var response = await client.PostAsJsonAsync("/api/generate", request);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Ollama returned {StatusCode}", response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<OllamaResponse>();
            return result?.Response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to call Ollama");
            return null;
        }
    }

    private string BuildPortfolioPrompt(List<Application> apps, List<LifecycleTask> tasks)
    {
        var healthyCount = apps.Count(a => a.HealthCategory == HealthCategory.Healthy);
        var criticalCount = apps.Count(a => a.HealthCategory == HealthCategory.Critical);
        var avgScore = apps.Average(a => a.HealthScore);
        var totalVulns = apps.Sum(a => a.SecurityFindings.Count(f => !f.IsResolved));
        var overdueTasks = tasks.Count(t => t.IsOverdue);

        return $@"Analyze this IT application portfolio for a material science R&D organization:

Portfolio Summary:
- Total Applications: {apps.Count}
- Healthy: {healthyCount}, Critical: {criticalCount}
- Average Health Score: {avgScore:F1}/100
- Total Unresolved Vulnerabilities: {totalVulns}
- Overdue Tasks: {overdueTasks}

Top 10 applications by concern:
{string.Join("\n", apps.OrderBy(a => a.HealthScore).Take(10).Select(a => $"- {a.Name}: Score={a.HealthScore}, {a.SecurityFindings.Count(f => !f.IsResolved)} vulns, {a.HealthCategory}"))}

Provide:
1. A 2-3 sentence executive summary of portfolio health
2. Top 3 key insights (patterns, correlations, anomalies)
3. 2 trend predictions for the next 30 days
4. Overall confidence score (0-100)

Be concise and focus on actionable insights.";
    }

    private List<InsightItem> GenerateKeyInsights(List<Application> apps, List<LifecycleTask> tasks)
    {
        var insights = new List<InsightItem>();

        // Vulnerability clustering
        var vulnApps = apps.Where(a => a.SecurityFindings.Any(f => !f.IsResolved && f.Severity == SecuritySeverity.Critical)).ToList();
        if (vulnApps.Count > 0)
        {
            insights.Add(new InsightItem
            {
                Category = "Security",
                Title = "Critical Vulnerability Cluster",
                Description = $"{vulnApps.Count} applications share critical vulnerabilities that may have a common root cause.",
                Severity = "Critical",
                AffectedApplications = vulnApps.Select(a => a.Name).Take(5).ToList(),
                RecommendedAction = "Investigate if these share common dependencies or configurations"
            });
        }

        // Documentation gaps
        var undocumented = apps.Where(a => !a.Documentation.HasArchitectureDiagram && !a.Documentation.HasSystemDocumentation).ToList();
        if (undocumented.Count > 3)
        {
            insights.Add(new InsightItem
            {
                Category = "Documentation",
                Title = "Documentation Debt",
                Description = $"{undocumented.Count} applications lack both architecture diagrams and system documentation.",
                Severity = "Medium",
                AffectedApplications = undocumented.Select(a => a.Name).Take(5).ToList(),
                RecommendedAction = "Prioritize documentation for critical path applications"
            });
        }

        // Stale applications
        var stale = apps.Where(a => a.LastActivityDate.HasValue && (DateTimeOffset.UtcNow - a.LastActivityDate.Value).TotalDays > 180).ToList();
        if (stale.Count > 0)
        {
            insights.Add(new InsightItem
            {
                Category = "Maintenance",
                Title = "Stale Application Pattern",
                Description = $"{stale.Count} applications have had no activity in 6+ months.",
                Severity = "Warning",
                AffectedApplications = stale.Select(a => a.Name).Take(5).ToList(),
                RecommendedAction = "Review for retirement or initiate maintenance cycles"
            });
        }

        return insights;
    }

    private List<TrendPrediction> GenerateTrends(List<Application> apps)
    {
        return new List<TrendPrediction>
        {
            new()
            {
                Metric = "Portfolio Health",
                Direction = apps.Count(a => a.HealthCategory == HealthCategory.NeedsAttention) > apps.Count(a => a.HealthCategory == HealthCategory.AtRisk) ? "Stable" : "Declining",
                Timeframe = "30 days",
                Reasoning = "Based on current vulnerability backlog and maintenance patterns",
                Confidence = 72
            },
            new()
            {
                Metric = "Security Posture",
                Direction = apps.Sum(a => a.SecurityFindings.Count(f => !f.IsResolved && f.Severity == SecuritySeverity.Critical)) > 5 ? "Declining" : "Improving",
                Timeframe = "30 days",
                Reasoning = "Critical vulnerability count trend analysis",
                Confidence = 68
            }
        };
    }

    private List<string>? ExtractListFromResponse(string? response, string section)
    {
        // Simple extraction - in production would use more sophisticated parsing
        return null;
    }

    private List<string> GenerateStrengths(Application app)
    {
        var strengths = new List<string>();
        if (app.HealthScore >= 80) strengths.Add("Strong overall health score");
        if (app.Documentation.HasArchitectureDiagram && app.Documentation.HasSystemDocumentation)
            strengths.Add("Complete documentation");
        if (app.Usage?.Level == UsageLevel.High) strengths.Add("High usage indicates business value");
        if (app.LastActivityDate.HasValue && (DateTimeOffset.UtcNow - app.LastActivityDate.Value).TotalDays < 30)
            strengths.Add("Active maintenance");
        if (!strengths.Any()) strengths.Add("Established in portfolio");
        return strengths;
    }

    private List<string> GenerateWeaknesses(Application app)
    {
        var weaknesses = new List<string>();
        var criticalVulns = app.SecurityFindings.Count(f => !f.IsResolved && f.Severity == SecuritySeverity.Critical);
        if (criticalVulns > 0) weaknesses.Add($"{criticalVulns} critical vulnerabilities");
        if (!app.Documentation.HasArchitectureDiagram) weaknesses.Add("Missing architecture diagram");
        if (!app.Documentation.HasSystemDocumentation) weaknesses.Add("Missing system documentation");
        if (app.Usage?.Level == UsageLevel.None) weaknesses.Add("No detected usage");
        return weaknesses;
    }

    private List<string> GenerateRecommendations(Application app)
    {
        var recs = new List<string>();
        var criticalVulns = app.SecurityFindings.Where(f => !f.IsResolved && f.Severity == SecuritySeverity.Critical).ToList();
        if (criticalVulns.Any())
            recs.Add($"Remediate {criticalVulns.Count} critical vulnerabilities immediately");
        if (!app.Documentation.HasArchitectureDiagram)
            recs.Add("Create and upload architecture diagram");
        if (app.Usage?.Level == UsageLevel.None || app.Usage?.Level == UsageLevel.VeryLow)
            recs.Add("Conduct usage review and retirement assessment");
        if (!recs.Any()) recs.Add("Continue current maintenance practices");
        return recs;
    }

    private int PredictHealth(Application app)
    {
        var score = app.HealthScore;
        // Predict decline based on factors
        if (app.SecurityFindings.Any(f => !f.IsResolved && f.Severity == SecuritySeverity.Critical))
            score -= 5;
        if (app.LastActivityDate.HasValue && (DateTimeOffset.UtcNow - app.LastActivityDate.Value).TotalDays > 90)
            score -= 3;
        if (!app.Documentation.HasArchitectureDiagram && !app.Documentation.HasSystemDocumentation)
            score -= 2;
        return Math.Max(0, score);
    }

    private string DetermineRiskLevel(Application app) => app.HealthCategory switch
    {
        HealthCategory.Critical => "Critical",
        HealthCategory.AtRisk => "High",
        HealthCategory.NeedsAttention => "Medium",
        _ => "Low"
    };

    private string GetRiskReason(Application app)
    {
        if (app.SecurityFindings.Any(f => !f.IsResolved && f.Severity == SecuritySeverity.Critical))
            return "Unresolved critical vulnerabilities";
        if (app.LastActivityDate.HasValue && (DateTimeOffset.UtcNow - app.LastActivityDate.Value).TotalDays > 180)
            return "No recent maintenance activity";
        if (app.Usage?.Level == UsageLevel.None)
            return "No detected usage - potential orphan application";
        return "Multiple health factors declining";
    }

    private string GetDeclineTimeframe(Application app)
    {
        if (app.HealthCategory == HealthCategory.Critical) return "Immediate";
        if (app.HealthCategory == HealthCategory.AtRisk) return "2-4 weeks";
        return "4-8 weeks";
    }

    private List<VulnerabilityPattern> IdentifyVulnPatterns(List<Application> apps)
    {
        var patterns = new List<VulnerabilityPattern>();

        // Group by vulnerability types
        var vulnTypes = apps
            .SelectMany(a => a.SecurityFindings.Where(f => !f.IsResolved))
            .GroupBy(f => f.Title.Split(' ').First())
            .Where(g => g.Count() > 1)
            .Take(3);

        foreach (var group in vulnTypes)
        {
            var affectedApps = apps
                .Where(a => a.SecurityFindings.Any(f => f.Title.StartsWith(group.Key)))
                .Select(a => a.Name)
                .ToList();

            if (affectedApps.Count > 1)
            {
                patterns.Add(new VulnerabilityPattern
                {
                    PatternName = $"Shared {group.Key} Vulnerability",
                    Description = $"{affectedApps.Count} applications affected by similar {group.Key} issues",
                    AffectedApplications = affectedApps,
                    RootCauseSuggestion = "May indicate shared dependency or configuration",
                    PotentialImpact = affectedApps.Count * 5
                });
            }
        }

        return patterns;
    }

    private List<MaintenancePattern> IdentifyMaintenancePatterns(List<Application> apps)
    {
        var active = apps.Where(a => a.LastActivityDate.HasValue && (DateTimeOffset.UtcNow - a.LastActivityDate.Value).TotalDays < 30).ToList();
        var stale = apps.Where(a => a.LastActivityDate.HasValue && (DateTimeOffset.UtcNow - a.LastActivityDate.Value).TotalDays > 180).ToList();

        return new List<MaintenancePattern>
        {
            new()
            {
                Pattern = "Actively Maintained",
                Description = $"{active.Count} applications with recent activity (< 30 days)",
                ApplicationsFollowing = active.Select(a => a.Name).Take(5).ToList()
            },
            new()
            {
                Pattern = "Maintenance Gap",
                Description = $"{stale.Count} applications with no activity in 6+ months",
                ApplicationsFollowing = stale.Select(a => a.Name).Take(5).ToList()
            }
        };
    }

    private List<ApplicationCluster> IdentifyTechClusters(List<Application> apps)
    {
        var clusters = new List<ApplicationCluster>();

        var techGroups = apps
            .SelectMany(a => a.TechnologyStack.Select(t => new { App = a.Name, Tech = t }))
            .GroupBy(x => x.Tech)
            .Where(g => g.Count() > 2)
            .OrderByDescending(g => g.Count())
            .Take(3);

        foreach (var group in techGroups)
        {
            clusters.Add(new ApplicationCluster
            {
                ClusterName = $"{group.Key} Stack",
                CommonCharacteristic = $"Applications using {group.Key}",
                Applications = group.Select(x => x.App).ToList()
            });
        }

        return clusters;
    }

    private PortfolioInsights ParsePortfolioInsights(string response, List<Application> apps)
    {
        // Clean and use AI response as summary, generate structured data for other fields
        var cleanSummary = CleanMarkdown(response);
        if (cleanSummary.Length > 400)
            cleanSummary = cleanSummary[..400] + "...";

        return new PortfolioInsights
        {
            Summary = cleanSummary,
            KeyInsights = GenerateKeyInsights(apps, []),
            Trends = GenerateTrends(apps),
            ConfidenceScore = 80
        };
    }

    private PortfolioInsights GenerateFallbackInsightsWithApps(List<Application> apps, List<LifecycleTask> tasks)
    {
        var criticalApps = apps.Where(a => a.HealthCategory == HealthCategory.Critical).ToList();
        var avgScore = apps.Average(a => a.HealthScore);
        var healthyCount = apps.Count(a => a.HealthCategory == HealthCategory.Healthy);
        var totalVulns = apps.Sum(a => a.SecurityFindings.Count(f => !f.IsResolved));

        var summary = $"The IT application portfolio consists of {apps.Count} applications with an average health score of {avgScore:F0}/100. " +
                      $"{healthyCount} applications are healthy, while {criticalApps.Count} require immediate attention. " +
                      $"There are {totalVulns} unresolved vulnerabilities across the portfolio.";

        return new PortfolioInsights
        {
            Summary = summary,
            KeyInsights = GenerateKeyInsights(apps, tasks),
            Trends = GenerateTrends(apps),
            ConfidenceScore = 80
        };
    }

    private static string CleanMarkdown(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // Remove markdown bold/italic markers
        var cleaned = text
            .Replace("**", "")
            .Replace("__", "")
            .Replace("*", "")
            .Replace("_", " ")
            .Replace("#", "")
            .Replace("`", "");

        // Clean up multiple spaces
        while (cleaned.Contains("  "))
            cleaned = cleaned.Replace("  ", " ");

        return cleaned.Trim();
    }

    private class OllamaRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "";

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = "";

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }
    }

    private class OllamaResponse
    {
        [JsonPropertyName("response")]
        public string? Response { get; set; }
    }
}
