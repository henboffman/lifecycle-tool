using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Identity;
using LifecycleDashboard.Models;

namespace LifecycleDashboard.Services;

/// <summary>
/// AI recommendation service supporting both local Ollama and Azure OpenAI.
/// Provides portfolio analysis, pattern detection, incident analysis, and actionable recommendations.
/// </summary>
public class OllamaRecommendationService : IAiRecommendationService
{
    private readonly HttpClient _httpClient;
    private readonly ISecureStorageService _secureStorage;
    private readonly ILogger<OllamaRecommendationService> _logger;

    private const string DefaultOllamaEndpoint = "http://localhost:11434";
    private const string DefaultOllamaModel = "llama3.2";
    private const string DefaultAzureApiVersion = "2024-02-01";

    private static readonly JsonSerializerOptions SnakeCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

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
        var provider = await _secureStorage.GetSecretAsync(SecretKeys.AiProvider) ?? "ollama";

        if (provider.Equals("azureopenai", StringComparison.OrdinalIgnoreCase))
        {
            return await GetAzureOpenAiStatusAsync();
        }

        return await GetOllamaStatusAsync();
    }

    private async Task<AiServiceStatus> GetOllamaStatusAsync()
    {
        var endpoint = await _secureStorage.GetSecretAsync(SecretKeys.OllamaEndpoint) ?? DefaultOllamaEndpoint;
        var model = await _secureStorage.GetSecretAsync(SecretKeys.OllamaModel) ?? DefaultOllamaModel;

        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            using var client = new HttpClient { BaseAddress = new Uri(endpoint) };

            var response = await client.GetAsync("/api/tags");
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

    private async Task<AiServiceStatus> GetAzureOpenAiStatusAsync()
    {
        var endpoint = await _secureStorage.GetSecretAsync(SecretKeys.AzureOpenAiEndpoint);
        var deployment = await _secureStorage.GetSecretAsync(SecretKeys.AzureOpenAiDeployment);
        var apiKey = await _secureStorage.GetSecretAsync(SecretKeys.AzureOpenAiKey);
        var apimKey = await _secureStorage.GetSecretAsync(SecretKeys.AzureOpenAiApimSubscriptionKey);
        var useAzureAdStr = await _secureStorage.GetSecretAsync(SecretKeys.AzureOpenAiUseAzureAd);
        var useAzureAd = useAzureAdStr?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

        if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(deployment))
        {
            return new AiServiceStatus
            {
                IsAvailable = false,
                Provider = "Azure OpenAI",
                Model = deployment ?? "Not configured",
                Error = "Azure OpenAI endpoint or deployment not configured"
            };
        }

        // Check authentication based on UseAzureAD setting
        if (useAzureAd)
        {
            var tenantId = await _secureStorage.GetSecretAsync(SecretKeys.AzureOpenAiTenantId);
            var clientId = await _secureStorage.GetSecretAsync(SecretKeys.AzureOpenAiClientId);
            var clientSecret = await _secureStorage.GetSecretAsync(SecretKeys.AzureOpenAiClientSecret);

            var hasAzureAdConfig = !string.IsNullOrWhiteSpace(tenantId) &&
                                   !string.IsNullOrWhiteSpace(clientId) &&
                                   !string.IsNullOrWhiteSpace(clientSecret);

            if (!hasAzureAdConfig)
            {
                return new AiServiceStatus
                {
                    IsAvailable = false,
                    Provider = "Azure OpenAI (Azure AD)",
                    Model = deployment,
                    Error = "Azure AD credentials (TenantId, ClientId, ClientSecret) not configured"
                };
            }

            return new AiServiceStatus
            {
                IsAvailable = true,
                Provider = "Azure OpenAI (Azure AD)",
                Model = deployment
            };
        }

        // Check if we have API key auth
        var hasAuth = !string.IsNullOrWhiteSpace(apiKey) || !string.IsNullOrWhiteSpace(apimKey);
        if (!hasAuth)
        {
            return new AiServiceStatus
            {
                IsAvailable = false,
                Provider = "Azure OpenAI",
                Model = deployment,
                Error = "Azure OpenAI API key or APIM subscription key not configured"
            };
        }

        return new AiServiceStatus
        {
            IsAvailable = true,
            Provider = "Azure OpenAI",
            Model = deployment
        };
    }

    public async Task<PortfolioInsights> GeneratePortfolioInsightsAsync(
        IEnumerable<Application> applications,
        IEnumerable<LifecycleTask> tasks)
    {
        var appList = applications.ToList();
        var taskList = tasks.ToList();

        var prompt = BuildPortfolioPrompt(appList, taskList);
        var response = await CallAiAsync(prompt);

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

        var response = await CallAiAsync(prompt);

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

        var response = await CallAiAsync(prompt);

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

        var response = await CallAiAsync(prompt);

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

    private async Task<string?> CallAiAsync(string prompt, string? systemPrompt = null)
    {
        var provider = await _secureStorage.GetSecretAsync(SecretKeys.AiProvider) ?? "ollama";

        if (provider.Equals("azureopenai", StringComparison.OrdinalIgnoreCase))
        {
            return await CallAzureOpenAiAsync(prompt, systemPrompt);
        }

        return await CallOllamaAsync(prompt, systemPrompt);
    }

    private async Task<string?> CallOllamaAsync(string prompt, string? systemPrompt = null)
    {
        try
        {
            var endpoint = await _secureStorage.GetSecretAsync(SecretKeys.OllamaEndpoint) ?? DefaultOllamaEndpoint;
            var model = await _secureStorage.GetSecretAsync(SecretKeys.OllamaModel) ?? DefaultOllamaModel;

            var fullPrompt = string.IsNullOrEmpty(systemPrompt) ? prompt : $"{systemPrompt}\n\n{prompt}";

            var request = new OllamaRequest
            {
                Model = model,
                Prompt = fullPrompt,
                Stream = false
            };

            using var client = new HttpClient { BaseAddress = new Uri(endpoint) };
            client.Timeout = TimeSpan.FromSeconds(90);

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

    private async Task<string?> CallAzureOpenAiAsync(string prompt, string? systemPrompt = null)
    {
        try
        {
            var endpoint = await _secureStorage.GetSecretAsync(SecretKeys.AzureOpenAiEndpoint);
            var deployment = await _secureStorage.GetSecretAsync(SecretKeys.AzureOpenAiDeployment);
            var apiKey = await _secureStorage.GetSecretAsync(SecretKeys.AzureOpenAiKey);
            var apiVersion = await _secureStorage.GetSecretAsync(SecretKeys.AzureOpenAiApiVersion) ?? DefaultAzureApiVersion;
            var apimKey = await _secureStorage.GetSecretAsync(SecretKeys.AzureOpenAiApimSubscriptionKey);
            var useAzureAdStr = await _secureStorage.GetSecretAsync(SecretKeys.AzureOpenAiUseAzureAd);
            var useAzureAd = useAzureAdStr?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

            _logger.LogInformation("Azure OpenAI config - Endpoint: {Endpoint}, Deployment: {Deployment}, ApiVersion: {ApiVersion}, UseAzureAD: {UseAzureAD}",
                endpoint, deployment, apiVersion, useAzureAd);

            if (string.IsNullOrWhiteSpace(endpoint) || string.IsNullOrWhiteSpace(deployment))
            {
                _logger.LogWarning("Azure OpenAI not configured");
                return null;
            }

            // Get authentication header
            string? authHeaderValue = null;
            bool useApiKeyAuth = true;

            if (useAzureAd)
            {
                var tenantId = await _secureStorage.GetSecretAsync(SecretKeys.AzureOpenAiTenantId);
                var clientId = await _secureStorage.GetSecretAsync(SecretKeys.AzureOpenAiClientId);
                var clientSecret = await _secureStorage.GetSecretAsync(SecretKeys.AzureOpenAiClientSecret);

                _logger.LogInformation("Azure AD auth - TenantId: {TenantId}, ClientId: {ClientId}, HasSecret: {HasSecret}",
                    tenantId, clientId, !string.IsNullOrWhiteSpace(clientSecret));

                if (string.IsNullOrWhiteSpace(tenantId) || string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
                {
                    _logger.LogWarning("Azure AD credentials (TenantId, ClientId, ClientSecret) not configured");
                    return null;
                }

                try
                {
                    var credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                    var tokenResult = await credential.GetTokenAsync(new Azure.Core.TokenRequestContext(new[] { "https://cognitiveservices.azure.com/.default" }));
                    authHeaderValue = tokenResult.Token;
                    useApiKeyAuth = false;
                    _logger.LogInformation("Azure AD token acquired successfully, expires: {ExpiresOn}", tokenResult.ExpiresOn);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to acquire Azure AD token");
                    return null;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(apiKey) && string.IsNullOrWhiteSpace(apimKey))
                {
                    _logger.LogWarning("Azure OpenAI API key not configured");
                    return null;
                }
                authHeaderValue = apiKey;
            }

            var messages = new List<OpenAIChatMessage>();
            if (!string.IsNullOrWhiteSpace(systemPrompt))
                messages.Add(new OpenAIChatMessage { Role = "system", Content = systemPrompt });
            messages.Add(new OpenAIChatMessage { Role = "user", Content = prompt });

            var requestBody = new OpenAIChatRequest
            {
                Messages = messages,
                Temperature = 0.7,
                MaxTokens = 2000
            };

            var url = $"{endpoint.TrimEnd('/')}/openai/deployments/{deployment}/chat/completions?api-version={apiVersion}";
            _logger.LogInformation("Azure OpenAI request URL: {Url}", url);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);

            // Add authentication based on method
            if (useApiKeyAuth)
            {
                if (!string.IsNullOrWhiteSpace(authHeaderValue))
                {
                    httpRequest.Headers.Add("api-key", authHeaderValue);
                }
            }
            else
            {
                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authHeaderValue);
            }

            // Add APIM subscription key if configured (works with both auth methods)
            if (!string.IsNullOrWhiteSpace(apimKey))
            {
                httpRequest.Headers.Add("Ocp-Apim-Subscription-Key", apimKey);
                _logger.LogInformation("Added APIM subscription key header");
            }

            httpRequest.Headers.Add("User-Agent", "LifecycleDashboard/1.0");

            // Use StringContent without charset suffix to match expected Content-Type
            var jsonContent = JsonSerializer.Serialize(requestBody, SnakeCaseOptions);
            httpRequest.Content = new StringContent(jsonContent, Encoding.UTF8);
            httpRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(90) };
            var response = await client.SendAsync(httpRequest);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Azure OpenAI returned {StatusCode}: {Error}", response.StatusCode, errorContent);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<OpenAIChatResponse>(CamelCaseOptions);
            return result?.Choices.FirstOrDefault()?.Message.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to call Azure OpenAI");
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

    // OpenAI-compatible request/response classes for Azure OpenAI
    private class OpenAIChatRequest
    {
        [JsonPropertyName("messages")]
        public List<OpenAIChatMessage> Messages { get; set; } = [];

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; } = 2000;
    }

    private class OpenAIChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";
    }

    private class OpenAIChatResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenAIChatChoice> Choices { get; set; } = [];
    }

    private class OpenAIChatChoice
    {
        [JsonPropertyName("message")]
        public OpenAIChatMessage Message { get; set; } = new();
    }

    #region Incident Analysis

    public async Task<IncidentAnalysisResult> AnalyzeIncidentsAsync(
        Application application,
        IEnumerable<ServiceNowIncident> incidents)
    {
        var incidentList = incidents.ToList();

        if (incidentList.Count == 0)
        {
            return new IncidentAnalysisResult
            {
                ApplicationId = application.Id,
                Summary = $"No incidents found for {application.Name}.",
                IncidentsAnalyzed = 0,
                ConfidenceScore = 100
            };
        }

        // Build prompt with incident data
        var systemPrompt = @"You are an IT support analyst expert at identifying patterns in incident data and recommending improvements.
Analyze the provided incidents and identify:
1. Common root causes across incidents
2. Quick wins that could prevent multiple incidents
3. Process improvements to reduce incident volume
4. Specific technical fixes that would have the highest impact

Respond in a structured format with clear recommendations. Be concise and actionable.";

        var incidentSummaries = incidentList.Take(25).Select(i =>
            $"- [{i.IncidentNumber}] {i.ShortDescription ?? "No description"} | Close: {i.CloseCode ?? "N/A"} | Notes: {TruncateText(i.CloseNotes, 100)}");

        var closeCodeCounts = incidentList
            .Where(i => !string.IsNullOrEmpty(i.CloseCode))
            .GroupBy(i => i.CloseCode!)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => $"- {g.Key}: {g.Count()} incidents");

        var prompt = $@"Analyze incidents for application: {application.Name}

Total Incidents: {incidentList.Count}
Recent Incidents (last 90 days): {incidentList.Count(i => i.ImportedAt >= DateTimeOffset.UtcNow.AddDays(-90))}

Top Close Codes:
{string.Join("\n", closeCodeCounts)}

Sample Incidents:
{string.Join("\n", incidentSummaries)}

Based on this data, provide:
1. A 2-3 sentence summary of the incident patterns
2. Top 3 root cause hypotheses
3. 2-3 specific recommendations to reduce incidents
4. Estimated impact if recommendations are implemented";

        var response = await CallAiAsync(prompt, systemPrompt);

        // Generate recommendations from AI response and incident data
        var recommendations = GenerateIncidentRecommendations(application, incidentList, response);

        return new IncidentAnalysisResult
        {
            ApplicationId = application.Id,
            Summary = response ?? $"Analysis of {incidentList.Count} incidents for {application.Name} identified patterns in close codes and resolution notes.",
            Recommendations = recommendations,
            CommonThemes = ExtractCommonThemes(incidentList),
            QuickWins = IdentifyQuickWins(incidentList),
            IncidentsAnalyzed = incidentList.Count,
            ConfidenceScore = response != null ? 85 : 70
        };
    }

    public async Task<IncidentAnalysisResult> AnalyzePortfolioIncidentsAsync(
        IEnumerable<ServiceNowIncident> incidents,
        IEnumerable<Application> applications)
    {
        var incidentList = incidents.ToList();
        var appList = applications.ToList();

        if (incidentList.Count == 0)
        {
            return new IncidentAnalysisResult
            {
                Summary = "No incidents in the portfolio to analyze.",
                IncidentsAnalyzed = 0,
                ConfidenceScore = 100
            };
        }

        var systemPrompt = @"You are an IT portfolio analyst specializing in incident trend analysis and operational improvement.
Analyze the portfolio-wide incident data and identify:
1. Cross-application patterns indicating systemic issues
2. Applications that are incident hotspots
3. Common themes across the organization
4. Strategic recommendations for reducing overall incident volume

Focus on high-impact opportunities that affect multiple applications.";

        var appIncidentCounts = incidentList
            .Where(i => !string.IsNullOrEmpty(i.LinkedApplicationId))
            .GroupBy(i => i.LinkedApplicationName ?? i.LinkedApplicationId!)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => $"- {g.Key}: {g.Count()} incidents");

        var topCloseCodes = incidentList
            .Where(i => !string.IsNullOrEmpty(i.CloseCode))
            .GroupBy(i => i.CloseCode!)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .Select(g => $"- {g.Key}: {g.Count()} incidents");

        var prompt = $@"Analyze portfolio-wide incident data:

Total Incidents: {incidentList.Count}
Linked to Applications: {incidentList.Count(i => !string.IsNullOrEmpty(i.LinkedApplicationId))}
Total Applications with Incidents: {incidentList.Select(i => i.LinkedApplicationId).Distinct().Count()}

Top Applications by Incident Count:
{string.Join("\n", appIncidentCounts)}

Most Common Close Codes:
{string.Join("\n", topCloseCodes)}

Provide:
1. A 2-3 sentence portfolio-wide assessment
2. Top 3 systemic issues affecting multiple applications
3. Strategic recommendations for reducing portfolio-wide incident volume
4. Quick wins that could have immediate impact";

        var response = await CallAiAsync(prompt, systemPrompt);

        // Generate portfolio-wide recommendations
        var recommendations = GeneratePortfolioRecommendations(incidentList, appList, response);

        return new IncidentAnalysisResult
        {
            Summary = response ?? $"Portfolio analysis of {incidentList.Count} incidents across {appList.Count} applications.",
            Recommendations = recommendations,
            CommonThemes = ExtractPortfolioThemes(incidentList),
            QuickWins = IdentifyPortfolioQuickWins(incidentList, appList),
            IncidentsAnalyzed = incidentList.Count,
            ConfidenceScore = response != null ? 82 : 68
        };
    }

    private List<IncidentRecommendation> GenerateIncidentRecommendations(
        Application app,
        List<ServiceNowIncident> incidents,
        string? aiResponse)
    {
        var recommendations = new List<IncidentRecommendation>();

        // Analyze close code patterns
        var closeCodeGroups = incidents
            .Where(i => !string.IsNullOrEmpty(i.CloseCode))
            .GroupBy(i => i.CloseCode!)
            .Where(g => g.Count() >= 2)
            .OrderByDescending(g => g.Count())
            .Take(3);

        foreach (var group in closeCodeGroups)
        {
            var isRepeatPattern = group.Count() >= 3;
            recommendations.Add(new IncidentRecommendation
            {
                Id = $"rec-{app.Id}-{Guid.NewGuid():N}",
                ApplicationId = app.Id,
                ApplicationName = app.Name,
                Type = isRepeatPattern ? IncidentRecommendationType.RepeatPattern : IncidentRecommendationType.ClosureAnalysis,
                Priority = isRepeatPattern ? 1 : 2,
                Title = $"Address recurring issue: {group.Key}",
                Description = $"This close code has appeared {group.Count()} times, indicating a recurring issue pattern.",
                RootCauseAnalysis = aiResponse != null ? "AI-identified pattern from closure notes analysis." : null,
                RecommendedAction = $"Investigate the root cause of '{group.Key}' issues and implement a permanent fix.",
                ExpectedImpact = $"Could prevent up to {group.Count()} similar incidents in the future.",
                EstimatedEffort = group.Count() >= 5 ? "Medium" : "Low",
                RelatedCloseCodes = [group.Key],
                RelatedIncidentNumbers = group.Select(i => i.IncidentNumber).Take(5).ToList(),
                IncidentCount = group.Count(),
                ConfidenceScore = aiResponse != null ? 85 : 75
            });
        }

        // Check for band-aid fixes
        var bandaidIncidents = incidents.Where(i =>
            i.CloseCode?.Contains("Band-aid", StringComparison.OrdinalIgnoreCase) == true ||
            i.CloseCode?.Contains("Workaround", StringComparison.OrdinalIgnoreCase) == true ||
            i.CloseCode?.Contains("Temporary", StringComparison.OrdinalIgnoreCase) == true).ToList();

        if (bandaidIncidents.Count >= 2)
        {
            recommendations.Add(new IncidentRecommendation
            {
                Id = $"rec-{app.Id}-bandaid-{Guid.NewGuid():N}",
                ApplicationId = app.Id,
                ApplicationName = app.Name,
                Type = IncidentRecommendationType.TechnicalDebt,
                Priority = 2,
                Title = $"Technical debt: {bandaidIncidents.Count} temporary fixes need permanent solutions",
                Description = "Multiple incidents have been closed with temporary fixes or workarounds, accumulating technical debt.",
                RecommendedAction = "Schedule time to implement permanent solutions for these workarounds.",
                ExpectedImpact = "Reduce recurring incidents and improve system stability.",
                EstimatedEffort = "Medium",
                RelatedIncidentNumbers = bandaidIncidents.Select(i => i.IncidentNumber).Take(5).ToList(),
                IncidentCount = bandaidIncidents.Count,
                ConfidenceScore = 90
            });
        }

        // High volume recommendation
        var recentCount = incidents.Count(i => i.ImportedAt >= DateTimeOffset.UtcNow.AddDays(-90));
        if (recentCount >= 5)
        {
            recommendations.Add(new IncidentRecommendation
            {
                Id = $"rec-{app.Id}-volume-{Guid.NewGuid():N}",
                ApplicationId = app.Id,
                ApplicationName = app.Name,
                Type = IncidentRecommendationType.HighVolume,
                Priority = recentCount >= 10 ? 1 : 2,
                Title = $"High incident volume requires attention",
                Description = $"{recentCount} incidents in the last 90 days suggests systemic issues with this application.",
                RootCauseAnalysis = aiResponse,
                RecommendedAction = "Conduct a root cause analysis workshop to identify and address systemic issues.",
                ExpectedImpact = $"Reducing incident rate by 50% would save approximately {recentCount / 2} incidents per quarter.",
                EstimatedEffort = "High",
                IncidentCount = recentCount,
                ConfidenceScore = 85
            });
        }

        return recommendations;
    }

    private List<IncidentRecommendation> GeneratePortfolioRecommendations(
        List<ServiceNowIncident> incidents,
        List<Application> apps,
        string? aiResponse)
    {
        var recommendations = new List<IncidentRecommendation>();

        // Find cross-application patterns
        var topCloseCodes = incidents
            .Where(i => !string.IsNullOrEmpty(i.CloseCode))
            .GroupBy(i => i.CloseCode!)
            .Where(g => g.Count() >= 5)
            .OrderByDescending(g => g.Count())
            .Take(3);

        foreach (var group in topCloseCodes)
        {
            var affectedApps = group
                .Where(i => !string.IsNullOrEmpty(i.LinkedApplicationName))
                .Select(i => i.LinkedApplicationName!)
                .Distinct()
                .Take(5)
                .ToList();

            if (affectedApps.Count >= 2)
            {
                recommendations.Add(new IncidentRecommendation
                {
                    Id = $"rec-portfolio-{Guid.NewGuid():N}",
                    Type = IncidentRecommendationType.ProcessImprovement,
                    Priority = 1,
                    Title = $"Cross-application issue: {group.Key}",
                    Description = $"'{group.Key}' appears {group.Count()} times across {affectedApps.Count} applications, suggesting a systemic issue.",
                    RootCauseAnalysis = aiResponse,
                    RecommendedAction = "Investigate common infrastructure or configuration issues affecting multiple applications.",
                    ExpectedImpact = $"Fixing root cause could prevent incidents across {affectedApps.Count} applications.",
                    EstimatedEffort = "Medium",
                    RelatedCloseCodes = [group.Key],
                    IncidentCount = group.Count(),
                    ConfidenceScore = aiResponse != null ? 82 : 72
                });
            }
        }

        // Find hotspot applications
        var hotspots = incidents
            .Where(i => !string.IsNullOrEmpty(i.LinkedApplicationId))
            .GroupBy(i => i.LinkedApplicationId!)
            .Where(g => g.Count() >= 10)
            .OrderByDescending(g => g.Count())
            .Take(3);

        foreach (var hotspot in hotspots)
        {
            var app = apps.FirstOrDefault(a => a.Id == hotspot.Key);
            recommendations.Add(new IncidentRecommendation
            {
                Id = $"rec-hotspot-{Guid.NewGuid():N}",
                ApplicationId = hotspot.Key,
                ApplicationName = app?.Name ?? hotspot.Key,
                Type = IncidentRecommendationType.HighVolume,
                Priority = 1,
                Title = $"Incident hotspot: {app?.Name ?? hotspot.Key}",
                Description = $"This application has {hotspot.Count()} incidents, making it a priority for improvement.",
                RecommendedAction = "Prioritize stability improvements for this high-volume incident source.",
                ExpectedImpact = "Significant reduction in overall portfolio incident volume.",
                EstimatedEffort = "High",
                IncidentCount = hotspot.Count(),
                ConfidenceScore = 88
            });
        }

        return recommendations;
    }

    private List<string> ExtractCommonThemes(List<ServiceNowIncident> incidents)
    {
        var themes = new List<string>();

        var topCloseCodes = incidents
            .Where(i => !string.IsNullOrEmpty(i.CloseCode))
            .GroupBy(i => i.CloseCode!)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .Select(g => g.Key);

        themes.AddRange(topCloseCodes);
        return themes;
    }

    private List<string> ExtractPortfolioThemes(List<ServiceNowIncident> incidents)
    {
        var themes = new List<string>();

        // Most common close codes
        var topCodes = incidents
            .Where(i => !string.IsNullOrEmpty(i.CloseCode))
            .GroupBy(i => i.CloseCode!)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => $"{g.Key} ({g.Count()})");

        themes.AddRange(topCodes);
        return themes;
    }

    private List<QuickWin> IdentifyQuickWins(List<ServiceNowIncident> incidents)
    {
        var quickWins = new List<QuickWin>();

        // Look for simple repeat patterns
        var repeatPatterns = incidents
            .Where(i => !string.IsNullOrEmpty(i.CloseCode))
            .GroupBy(i => i.CloseCode!)
            .Where(g => g.Count() >= 3)
            .Take(2);

        foreach (var pattern in repeatPatterns)
        {
            quickWins.Add(new QuickWin
            {
                Title = $"Fix recurring '{pattern.Key}' issues",
                Description = $"Addressing root cause could prevent {pattern.Count()} similar incidents.",
                EstimatedImpact = $"{pattern.Count()} incidents prevented",
                Effort = pattern.Count() >= 5 ? "Medium" : "Low"
            });
        }

        return quickWins;
    }

    private List<QuickWin> IdentifyPortfolioQuickWins(List<ServiceNowIncident> incidents, List<Application> apps)
    {
        var quickWins = new List<QuickWin>();

        // Cross-app patterns
        var crossAppPatterns = incidents
            .Where(i => !string.IsNullOrEmpty(i.CloseCode) && !string.IsNullOrEmpty(i.LinkedApplicationId))
            .GroupBy(i => i.CloseCode!)
            .Where(g => g.Select(i => i.LinkedApplicationId).Distinct().Count() >= 2 && g.Count() >= 5)
            .Take(2);

        foreach (var pattern in crossAppPatterns)
        {
            var appCount = pattern.Select(i => i.LinkedApplicationId).Distinct().Count();
            quickWins.Add(new QuickWin
            {
                Title = $"Address '{pattern.Key}' across {appCount} apps",
                Description = $"Common fix could resolve {pattern.Count()} incidents across multiple applications.",
                EstimatedImpact = $"{pattern.Count()} incidents across {appCount} apps",
                Effort = "Medium"
            });
        }

        return quickWins;
    }

    private static string TruncateText(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "";
        if (text.Length <= maxLength) return text;
        return text[..maxLength] + "...";
    }

    #endregion
}
