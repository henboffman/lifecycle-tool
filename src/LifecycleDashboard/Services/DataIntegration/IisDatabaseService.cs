using LifecycleDashboard.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace LifecycleDashboard.Services.DataIntegration;

/// <summary>
/// Service for retrieving usage metrics from the IIS database.
/// Provides read-only access to application usage data including
/// monthly requests, users, and sessions.
/// </summary>
public class IisDatabaseService : IIisDatabaseService
{
    private readonly ISecureStorageService _secureStorage;
    private readonly IMockDataService _mockDataService;
    private readonly ILogger<IisDatabaseService> _logger;

    // Configuration keys
    private const string ConnectionStringKey = SecretKeys.IisDatabaseConnectionString;

    private IisDatabaseSchema _schema = new();

    public IisDatabaseService(
        ISecureStorageService secureStorage,
        IMockDataService mockDataService,
        ILogger<IisDatabaseService> logger)
    {
        _secureStorage = secureStorage;
        _mockDataService = mockDataService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ConnectionTestResult> TestConnectionAsync()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var connectionString = await _secureStorage.GetSecretAsync(ConnectionStringKey);

            if (string.IsNullOrEmpty(connectionString))
            {
                return ConnectionTestResult.Failed("IIS database connection string not configured");
            }

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Get server version
            await using var command = new SqlCommand("SELECT @@VERSION", connection);
            var version = await command.ExecuteScalarAsync() as string;

            stopwatch.Stop();

            return ConnectionTestResult.Succeeded(
                "Successfully connected to IIS database",
                stopwatch.Elapsed,
                version?.Split('\n').FirstOrDefault()?.Trim());
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error testing IIS database connection");
            return ConnectionTestResult.Failed($"Database error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing IIS database connection");
            return ConnectionTestResult.Failed($"Connection error: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<DataSyncResult<UsageMetrics>> GetUsageMetricsAsync(string applicationId)
    {
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            var connectionString = await _secureStorage.GetSecretAsync(ConnectionStringKey);

            if (string.IsNullOrEmpty(connectionString))
            {
                return DataSyncResult<UsageMetrics>.Failed(
                    DataSourceType.IisDatabase, startTime, "Connection string not configured");
            }

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Get current month and previous 12 months
            var endDate = DateOnly.FromDateTime(DateTime.Today);
            var startDate = endDate.AddMonths(-12);

            var query = BuildUsageQuery(applicationId, startDate, endDate);

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ApplicationId", applicationId);
            command.Parameters.AddWithValue("@StartDate", startDate.ToDateTime(TimeOnly.MinValue));
            command.Parameters.AddWithValue("@EndDate", endDate.ToDateTime(TimeOnly.MaxValue));

            if (!string.IsNullOrEmpty(_schema.EnvironmentColumn))
            {
                command.Parameters.AddWithValue("@Environment", _schema.ProductionEnvironmentValue);
            }

            await using var reader = await command.ExecuteReaderAsync();

            int totalRequests = 0, totalUsers = 0, totalSessions = 0;

            while (await reader.ReadAsync())
            {
                totalRequests += reader.GetInt32(reader.GetOrdinal("TotalRequests"));
                totalUsers += reader.GetInt32(reader.GetOrdinal("DistinctUsers"));
                totalSessions += reader.GetInt32(reader.GetOrdinal("Sessions"));
            }

            var metrics = new UsageMetrics
            {
                MonthlyRequests = totalRequests,
                MonthlyUsers = totalUsers,
                MonthlySessions = totalSessions
            };

            return new DataSyncResult<UsageMetrics>
            {
                Success = true,
                DataSource = DataSourceType.IisDatabase,
                Data = metrics,
                RecordsProcessed = 1,
                StartTime = startTime,
                EndTime = DateTimeOffset.UtcNow
            };
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error getting usage metrics for {ApplicationId}", applicationId);
            return DataSyncResult<UsageMetrics>.Failed(
                DataSourceType.IisDatabase, startTime, $"Database error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage metrics for {ApplicationId}", applicationId);
            return DataSyncResult<UsageMetrics>.Failed(
                DataSourceType.IisDatabase, startTime, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<DataSyncResult<Dictionary<string, UsageMetrics>>> GetAllUsageMetricsAsync()
    {
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            var connectionString = await _secureStorage.GetSecretAsync(ConnectionStringKey);

            if (string.IsNullOrEmpty(connectionString))
            {
                return DataSyncResult<Dictionary<string, UsageMetrics>>.Failed(
                    DataSourceType.IisDatabase, startTime, "Connection string not configured");
            }

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var endDate = DateOnly.FromDateTime(DateTime.Today);
            var startDate = endDate.AddMonths(-1); // Last month

            var query = BuildAllUsageQuery(startDate, endDate);

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StartDate", startDate.ToDateTime(TimeOnly.MinValue));
            command.Parameters.AddWithValue("@EndDate", endDate.ToDateTime(TimeOnly.MaxValue));

            if (!string.IsNullOrEmpty(_schema.EnvironmentColumn))
            {
                command.Parameters.AddWithValue("@Environment", _schema.ProductionEnvironmentValue);
            }

            await using var reader = await command.ExecuteReaderAsync();

            var metricsDict = new Dictionary<string, UsageMetrics>();

            while (await reader.ReadAsync())
            {
                var appId = reader.GetString(reader.GetOrdinal(_schema.ApplicationIdColumn));

                metricsDict[appId] = new UsageMetrics
                {
                    MonthlyRequests = reader.GetInt32(reader.GetOrdinal("TotalRequests")),
                    MonthlyUsers = reader.GetInt32(reader.GetOrdinal("DistinctUsers")),
                    MonthlySessions = reader.GetInt32(reader.GetOrdinal("Sessions"))
                };
            }

            return new DataSyncResult<Dictionary<string, UsageMetrics>>
            {
                Success = true,
                DataSource = DataSourceType.IisDatabase,
                Data = metricsDict,
                RecordsProcessed = metricsDict.Count,
                StartTime = startTime,
                EndTime = DateTimeOffset.UtcNow
            };
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error getting all usage metrics");
            return DataSyncResult<Dictionary<string, UsageMetrics>>.Failed(
                DataSourceType.IisDatabase, startTime, $"Database error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all usage metrics");
            return DataSyncResult<Dictionary<string, UsageMetrics>>.Failed(
                DataSourceType.IisDatabase, startTime, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<DataSyncResult<List<MonthlyUsage>>> GetUsageTrendAsync(string applicationId, int monthsBack = 12)
    {
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            var connectionString = await _secureStorage.GetSecretAsync(ConnectionStringKey);

            if (string.IsNullOrEmpty(connectionString))
            {
                return DataSyncResult<List<MonthlyUsage>>.Failed(
                    DataSourceType.IisDatabase, startTime, "Connection string not configured");
            }

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var endDate = DateOnly.FromDateTime(DateTime.Today);
            var startDate = endDate.AddMonths(-monthsBack);

            var query = BuildTrendQuery(applicationId, startDate, endDate);

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ApplicationId", applicationId);
            command.Parameters.AddWithValue("@StartDate", startDate.ToDateTime(TimeOnly.MinValue));
            command.Parameters.AddWithValue("@EndDate", endDate.ToDateTime(TimeOnly.MaxValue));

            if (!string.IsNullOrEmpty(_schema.EnvironmentColumn))
            {
                command.Parameters.AddWithValue("@Environment", _schema.ProductionEnvironmentValue);
            }

            await using var reader = await command.ExecuteReaderAsync();

            var usageData = new List<MonthlyUsage>();

            while (await reader.ReadAsync())
            {
                var monthValue = reader.GetDateTime(reader.GetOrdinal("MonthDate"));
                var usage = new MonthlyUsage
                {
                    ApplicationId = applicationId,
                    Month = DateOnly.FromDateTime(monthValue),
                    TotalRequests = reader.GetInt32(reader.GetOrdinal("TotalRequests")),
                    DistinctUsers = reader.GetInt32(reader.GetOrdinal("DistinctUsers")),
                    Sessions = reader.GetInt32(reader.GetOrdinal("Sessions"))
                };

                if (!string.IsNullOrEmpty(_schema.AvgResponseTimeColumn))
                {
                    var avgRespIndex = reader.GetOrdinal("AvgResponseTime");
                    if (!reader.IsDBNull(avgRespIndex))
                    {
                        usage = usage with { AvgResponseTimeMs = reader.GetDouble(avgRespIndex) };
                    }
                }

                if (!string.IsNullOrEmpty(_schema.ErrorCountColumn))
                {
                    var errorIndex = reader.GetOrdinal("ErrorCount");
                    if (!reader.IsDBNull(errorIndex))
                    {
                        usage = usage with { ErrorCount = reader.GetInt32(errorIndex) };
                    }
                }

                if (!string.IsNullOrEmpty(_schema.EnvironmentColumn))
                {
                    var envIndex = reader.GetOrdinal("Environment");
                    if (!reader.IsDBNull(envIndex))
                    {
                        usage = usage with { Environment = reader.GetString(envIndex) };
                    }
                }

                usageData.Add(usage);
            }

            return new DataSyncResult<List<MonthlyUsage>>
            {
                Success = true,
                DataSource = DataSourceType.IisDatabase,
                Data = usageData,
                RecordsProcessed = usageData.Count,
                StartTime = startTime,
                EndTime = DateTimeOffset.UtcNow
            };
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error getting usage trend for {ApplicationId}", applicationId);
            return DataSyncResult<List<MonthlyUsage>>.Failed(
                DataSourceType.IisDatabase, startTime, $"Database error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage trend for {ApplicationId}", applicationId);
            return DataSyncResult<List<MonthlyUsage>>.Failed(
                DataSourceType.IisDatabase, startTime, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<DataSyncResult<PortfolioUsageSummary>> GetPortfolioUsageSummaryAsync()
    {
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            var connectionString = await _secureStorage.GetSecretAsync(ConnectionStringKey);

            if (string.IsNullOrEmpty(connectionString))
            {
                return DataSyncResult<PortfolioUsageSummary>.Failed(
                    DataSourceType.IisDatabase, startTime, "Connection string not configured");
            }

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var endDate = DateOnly.FromDateTime(DateTime.Today);
            var startDate = endDate.AddMonths(-1);

            var query = BuildPortfolioSummaryQuery(startDate, endDate);

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StartDate", startDate.ToDateTime(TimeOnly.MinValue));
            command.Parameters.AddWithValue("@EndDate", endDate.ToDateTime(TimeOnly.MaxValue));

            if (!string.IsNullOrEmpty(_schema.EnvironmentColumn))
            {
                command.Parameters.AddWithValue("@Environment", _schema.ProductionEnvironmentValue);
            }

            await using var reader = await command.ExecuteReaderAsync();

            var appUsages = new List<(string AppId, string AppName, int Users, int Requests)>();

            while (await reader.ReadAsync())
            {
                appUsages.Add((
                    reader.GetString(0), // ApplicationId
                    reader.GetString(0), // Using ID as name for now
                    reader.GetInt32(1),  // Users
                    reader.GetInt32(2)   // Requests
                ));
            }

            var summary = new PortfolioUsageSummary
            {
                PeriodStart = startDate,
                PeriodEnd = endDate,
                ApplicationCount = appUsages.Count,
                TotalRequests = appUsages.Sum(a => a.Requests),
                TotalDistinctUsers = appUsages.Sum(a => a.Users),
                ZeroUsageCount = appUsages.Count(a => a.Users == 0),
                LowUsageCount = appUsages.Count(a => a.Users is > 0 and < 100),
                HighUsageCount = appUsages.Count(a => a.Users > 500),
                TopByUsers = appUsages
                    .OrderByDescending(a => a.Users)
                    .Take(10)
                    .Select((a, i) => new ApplicationUsageRank
                    {
                        ApplicationId = a.AppId,
                        ApplicationName = a.AppName,
                        Users = a.Users,
                        Requests = a.Requests,
                        Rank = i + 1
                    })
                    .ToList(),
                TopByRequests = appUsages
                    .OrderByDescending(a => a.Requests)
                    .Take(10)
                    .Select((a, i) => new ApplicationUsageRank
                    {
                        ApplicationId = a.AppId,
                        ApplicationName = a.AppName,
                        Users = a.Users,
                        Requests = a.Requests,
                        Rank = i + 1
                    })
                    .ToList()
            };

            return new DataSyncResult<PortfolioUsageSummary>
            {
                Success = true,
                DataSource = DataSourceType.IisDatabase,
                Data = summary,
                RecordsProcessed = appUsages.Count,
                StartTime = startTime,
                EndTime = DateTimeOffset.UtcNow
            };
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL error getting portfolio usage summary");
            return DataSyncResult<PortfolioUsageSummary>.Failed(
                DataSourceType.IisDatabase, startTime, $"Database error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting portfolio usage summary");
            return DataSyncResult<PortfolioUsageSummary>.Failed(
                DataSourceType.IisDatabase, startTime, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<DataSyncResult> SyncUsageDataAsync()
    {
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            var metricsResult = await GetAllUsageMetricsAsync();
            if (!metricsResult.Success)
            {
                return DataSyncResult.Failed(DataSourceType.IisDatabase, startTime,
                    metricsResult.ErrorMessage ?? "Failed to get usage metrics");
            }

            var apps = await _mockDataService.GetApplicationsAsync();
            var updated = 0;

            foreach (var app in apps)
            {
                if (metricsResult.Data?.TryGetValue(app.Id, out var metrics) == true)
                {
                    // TODO: Update application usage metrics via IMockDataService
                    updated++;
                }
            }

            return DataSyncResult.Succeeded(DataSourceType.IisDatabase, startTime,
                metricsResult.Data?.Count ?? 0, 0, updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing usage data");
            return DataSyncResult.Failed(DataSourceType.IisDatabase, startTime, ex.Message);
        }
    }

    /// <inheritdoc />
    public IisDatabaseSchema GetDatabaseSchema() => _schema;

    /// <inheritdoc />
    public Task SetDatabaseSchemaAsync(IisDatabaseSchema schema)
    {
        _schema = schema;
        return Task.CompletedTask;
    }

    #region Private Helpers

    private string BuildUsageQuery(string applicationId, DateOnly startDate, DateOnly endDate)
    {
        var whereClause = $"WHERE {_schema.ApplicationIdColumn} = @ApplicationId " +
                         $"AND {_schema.MonthColumn} BETWEEN @StartDate AND @EndDate";

        if (!string.IsNullOrEmpty(_schema.EnvironmentColumn))
        {
            whereClause += $" AND {_schema.EnvironmentColumn} = @Environment";
        }

        if (!string.IsNullOrEmpty(_schema.CustomWhereClause))
        {
            whereClause += $" AND {_schema.CustomWhereClause}";
        }

        return $@"
            SELECT
                SUM({_schema.RequestsColumn}) as TotalRequests,
                SUM({_schema.UsersColumn}) as DistinctUsers,
                SUM({_schema.SessionsColumn}) as Sessions
            FROM {_schema.TableName}
            {whereClause}";
    }

    private string BuildAllUsageQuery(DateOnly startDate, DateOnly endDate)
    {
        var whereClause = $"WHERE {_schema.MonthColumn} BETWEEN @StartDate AND @EndDate";

        if (!string.IsNullOrEmpty(_schema.EnvironmentColumn))
        {
            whereClause += $" AND {_schema.EnvironmentColumn} = @Environment";
        }

        if (!string.IsNullOrEmpty(_schema.CustomWhereClause))
        {
            whereClause += $" AND {_schema.CustomWhereClause}";
        }

        return $@"
            SELECT
                {_schema.ApplicationIdColumn},
                SUM({_schema.RequestsColumn}) as TotalRequests,
                SUM({_schema.UsersColumn}) as DistinctUsers,
                SUM({_schema.SessionsColumn}) as Sessions
            FROM {_schema.TableName}
            {whereClause}
            GROUP BY {_schema.ApplicationIdColumn}";
    }

    private string BuildTrendQuery(string applicationId, DateOnly startDate, DateOnly endDate)
    {
        var columns = new List<string>
        {
            $"DATEFROMPARTS(YEAR({_schema.MonthColumn}), MONTH({_schema.MonthColumn}), 1) as MonthDate",
            $"SUM({_schema.RequestsColumn}) as TotalRequests",
            $"SUM({_schema.UsersColumn}) as DistinctUsers",
            $"SUM({_schema.SessionsColumn}) as Sessions"
        };

        if (!string.IsNullOrEmpty(_schema.AvgResponseTimeColumn))
        {
            columns.Add($"AVG({_schema.AvgResponseTimeColumn}) as AvgResponseTime");
        }

        if (!string.IsNullOrEmpty(_schema.ErrorCountColumn))
        {
            columns.Add($"SUM({_schema.ErrorCountColumn}) as ErrorCount");
        }

        if (!string.IsNullOrEmpty(_schema.EnvironmentColumn))
        {
            columns.Add($"MAX({_schema.EnvironmentColumn}) as Environment");
        }

        var whereClause = $"WHERE {_schema.ApplicationIdColumn} = @ApplicationId " +
                         $"AND {_schema.MonthColumn} BETWEEN @StartDate AND @EndDate";

        if (!string.IsNullOrEmpty(_schema.EnvironmentColumn))
        {
            whereClause += $" AND {_schema.EnvironmentColumn} = @Environment";
        }

        if (!string.IsNullOrEmpty(_schema.CustomWhereClause))
        {
            whereClause += $" AND {_schema.CustomWhereClause}";
        }

        return $@"
            SELECT {string.Join(", ", columns)}
            FROM {_schema.TableName}
            {whereClause}
            GROUP BY YEAR({_schema.MonthColumn}), MONTH({_schema.MonthColumn})
            ORDER BY MonthDate";
    }

    private string BuildPortfolioSummaryQuery(DateOnly startDate, DateOnly endDate)
    {
        var whereClause = $"WHERE {_schema.MonthColumn} BETWEEN @StartDate AND @EndDate";

        if (!string.IsNullOrEmpty(_schema.EnvironmentColumn))
        {
            whereClause += $" AND {_schema.EnvironmentColumn} = @Environment";
        }

        if (!string.IsNullOrEmpty(_schema.CustomWhereClause))
        {
            whereClause += $" AND {_schema.CustomWhereClause}";
        }

        return $@"
            SELECT
                {_schema.ApplicationIdColumn},
                SUM({_schema.UsersColumn}) as TotalUsers,
                SUM({_schema.RequestsColumn}) as TotalRequests
            FROM {_schema.TableName}
            {whereClause}
            GROUP BY {_schema.ApplicationIdColumn}
            ORDER BY TotalUsers DESC";
    }

    #endregion
}
