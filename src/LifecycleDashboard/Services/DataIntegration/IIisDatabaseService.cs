using LifecycleDashboard.Models;

namespace LifecycleDashboard.Services.DataIntegration;

/// <summary>
/// Service for retrieving usage metrics from the IIS database.
/// Provides read-only access to application usage data including
/// monthly requests, users, and sessions.
/// </summary>
public interface IIisDatabaseService
{
    /// <summary>
    /// Tests the connection to the IIS database.
    /// </summary>
    Task<ConnectionTestResult> TestConnectionAsync();

    /// <summary>
    /// Gets usage metrics for a specific application.
    /// </summary>
    Task<DataSyncResult<UsageMetrics>> GetUsageMetricsAsync(string applicationId);

    /// <summary>
    /// Gets usage metrics for all applications.
    /// </summary>
    Task<DataSyncResult<Dictionary<string, UsageMetrics>>> GetAllUsageMetricsAsync();

    /// <summary>
    /// Gets usage trend data for an application over time.
    /// </summary>
    Task<DataSyncResult<List<MonthlyUsage>>> GetUsageTrendAsync(string applicationId, int monthsBack = 12);

    /// <summary>
    /// Gets aggregated usage summary for the entire portfolio.
    /// </summary>
    Task<DataSyncResult<PortfolioUsageSummary>> GetPortfolioUsageSummaryAsync();

    /// <summary>
    /// Syncs usage data for all applications.
    /// </summary>
    Task<DataSyncResult> SyncUsageDataAsync();

    /// <summary>
    /// Gets the configured database schema/table information.
    /// </summary>
    IisDatabaseSchema GetDatabaseSchema();

    /// <summary>
    /// Updates the database schema configuration.
    /// </summary>
    Task SetDatabaseSchemaAsync(IisDatabaseSchema schema);
}

/// <summary>
/// Monthly usage data point.
/// </summary>
public record MonthlyUsage
{
    /// <summary>Application identifier.</summary>
    public required string ApplicationId { get; init; }

    /// <summary>Month of the usage data.</summary>
    public required DateOnly Month { get; init; }

    /// <summary>Total HTTP requests in the month.</summary>
    public int TotalRequests { get; init; }

    /// <summary>Distinct users in the month.</summary>
    public int DistinctUsers { get; init; }

    /// <summary>User sessions in the month.</summary>
    public int Sessions { get; init; }

    /// <summary>Average response time in milliseconds.</summary>
    public double? AvgResponseTimeMs { get; init; }

    /// <summary>Error count (4xx/5xx responses).</summary>
    public int? ErrorCount { get; init; }

    /// <summary>Environment (e.g., Production, Staging).</summary>
    public string? Environment { get; init; }
}

/// <summary>
/// Portfolio-wide usage summary.
/// </summary>
public record PortfolioUsageSummary
{
    /// <summary>Period start date.</summary>
    public DateOnly PeriodStart { get; init; }

    /// <summary>Period end date.</summary>
    public DateOnly PeriodEnd { get; init; }

    /// <summary>Total applications with usage data.</summary>
    public int ApplicationCount { get; init; }

    /// <summary>Total requests across all applications.</summary>
    public long TotalRequests { get; init; }

    /// <summary>Total distinct users across all applications.</summary>
    public int TotalDistinctUsers { get; init; }

    /// <summary>Applications with no usage.</summary>
    public int ZeroUsageCount { get; init; }

    /// <summary>Applications with low usage (< 100 users).</summary>
    public int LowUsageCount { get; init; }

    /// <summary>Applications with high usage (> 500 users).</summary>
    public int HighUsageCount { get; init; }

    /// <summary>Top 10 applications by user count.</summary>
    public List<ApplicationUsageRank> TopByUsers { get; init; } = [];

    /// <summary>Top 10 applications by request count.</summary>
    public List<ApplicationUsageRank> TopByRequests { get; init; } = [];
}

/// <summary>
/// Application usage rank entry.
/// </summary>
public record ApplicationUsageRank
{
    /// <summary>Application ID.</summary>
    public required string ApplicationId { get; init; }

    /// <summary>Application name.</summary>
    public required string ApplicationName { get; init; }

    /// <summary>User count.</summary>
    public int Users { get; init; }

    /// <summary>Request count.</summary>
    public int Requests { get; init; }

    /// <summary>Rank position.</summary>
    public int Rank { get; init; }
}

/// <summary>
/// Configuration for IIS database schema.
/// Allows customization of table/column names for different IIS log database structures.
/// </summary>
public record IisDatabaseSchema
{
    /// <summary>Table name containing usage data.</summary>
    public string TableName { get; init; } = "ApplicationUsage";

    /// <summary>Column name for application identifier.</summary>
    public string ApplicationIdColumn { get; init; } = "ApplicationId";

    /// <summary>Column name for month/date.</summary>
    public string MonthColumn { get; init; } = "Month";

    /// <summary>Column name for request count.</summary>
    public string RequestsColumn { get; init; } = "TotalRequests";

    /// <summary>Column name for user count.</summary>
    public string UsersColumn { get; init; } = "DistinctUsers";

    /// <summary>Column name for session count.</summary>
    public string SessionsColumn { get; init; } = "UsageSessions";

    /// <summary>Column name for environment (optional).</summary>
    public string? EnvironmentColumn { get; init; } = "Environment";

    /// <summary>Column name for average response time (optional).</summary>
    public string? AvgResponseTimeColumn { get; init; }

    /// <summary>Column name for error count (optional).</summary>
    public string? ErrorCountColumn { get; init; }

    /// <summary>Value to filter for production environment.</summary>
    public string ProductionEnvironmentValue { get; init; } = "Production";

    /// <summary>Custom WHERE clause to add to queries (optional).</summary>
    public string? CustomWhereClause { get; init; }

    /// <summary>
    /// Generates the base SELECT query for usage metrics.
    /// </summary>
    public string GenerateBaseQuery()
    {
        var columns = new List<string>
        {
            ApplicationIdColumn,
            MonthColumn,
            $"SUM({RequestsColumn}) as TotalRequests",
            $"SUM({UsersColumn}) as DistinctUsers",
            $"SUM({SessionsColumn}) as Sessions"
        };

        if (!string.IsNullOrEmpty(AvgResponseTimeColumn))
            columns.Add($"AVG({AvgResponseTimeColumn}) as AvgResponseTime");

        if (!string.IsNullOrEmpty(ErrorCountColumn))
            columns.Add($"SUM({ErrorCountColumn}) as ErrorCount");

        return $"SELECT {string.Join(", ", columns)} FROM {TableName}";
    }
}
