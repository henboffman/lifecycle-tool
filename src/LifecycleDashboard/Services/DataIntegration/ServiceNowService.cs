using System.Globalization;
using LifecycleDashboard.Models;
using Microsoft.Extensions.Logging;

namespace LifecycleDashboard.Services.DataIntegration;

/// <summary>
/// Service for integrating with ServiceNow CMDB data via CSV exports.
/// </summary>
public class ServiceNowService : IServiceNowService
{
    private readonly ISecureStorageService _secureStorage;
    private readonly IMockDataService _mockDataService;
    private readonly ILogger<ServiceNowService> _logger;

    private ServiceNowCsvMapping _mapping = new();
    private DateTimeOffset? _lastImportDate;

    public ServiceNowService(
        ISecureStorageService secureStorage,
        IMockDataService mockDataService,
        ILogger<ServiceNowService> logger)
    {
        _secureStorage = secureStorage;
        _mockDataService = mockDataService;
        _logger = logger;
    }

    public async Task<ConnectionTestResult> TestConnectionAsync()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var csvDirectory = await _secureStorage.GetSecretAsync(SecretKeys.ServiceNowInstance);

            if (string.IsNullOrEmpty(csvDirectory))
            {
                return ConnectionTestResult.Failed("CSV directory not configured");
            }

            if (!Directory.Exists(csvDirectory))
            {
                return ConnectionTestResult.Failed($"CSV directory does not exist: {csvDirectory}");
            }

            stopwatch.Stop();

            return ConnectionTestResult.Succeeded(
                $"CSV directory accessible: {csvDirectory}",
                stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing ServiceNow CSV access");
            return ConnectionTestResult.Failed($"Error: {ex.Message}");
        }
    }

    public async Task<DataSyncResult<List<ServiceNowApplication>>> ImportApplicationsAsync(Stream csvStream)
    {
        var startTime = DateTimeOffset.UtcNow;
        var applications = new List<ServiceNowApplication>();

        try
        {
            using var reader = new StreamReader(csvStream);
            var headerLine = await reader.ReadLineAsync();

            if (string.IsNullOrEmpty(headerLine))
            {
                return DataSyncResult<List<ServiceNowApplication>>.Failed(
                    DataSourceType.ServiceNow, startTime, "CSV file is empty");
            }

            var headers = ParseCsvLine(headerLine);
            var columnMap = BuildColumnMap(headers);
            var rowNumber = 1;

            while (!reader.EndOfStream)
            {
                rowNumber++;
                var line = await reader.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var values = ParseCsvLine(line);
                    var app = ParseApplicationRow(values, columnMap);
                    if (app != null)
                    {
                        applications.Add(app);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error parsing row {RowNumber}", rowNumber);
                }
            }

            _lastImportDate = DateTimeOffset.UtcNow;

            return new DataSyncResult<List<ServiceNowApplication>>
            {
                Success = true,
                DataSource = DataSourceType.ServiceNow,
                Data = applications,
                RecordsProcessed = rowNumber - 1,
                RecordsCreated = applications.Count,
                StartTime = startTime,
                EndTime = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing applications from CSV");
            return DataSyncResult<List<ServiceNowApplication>>.Failed(
                DataSourceType.ServiceNow, startTime, ex.Message);
        }
    }

    public async Task<DataSyncResult<List<ServiceNowApplication>>> ImportApplicationsAsync(string csvFilePath)
    {
        if (!File.Exists(csvFilePath))
        {
            return DataSyncResult<List<ServiceNowApplication>>.Failed(
                DataSourceType.ServiceNow, DateTimeOffset.UtcNow, $"File not found: {csvFilePath}");
        }

        await using var stream = File.OpenRead(csvFilePath);
        return await ImportApplicationsAsync(stream);
    }

    public async Task<DataSyncResult<List<RoleAssignment>>> ImportRoleAssignmentsAsync(Stream csvStream)
    {
        var startTime = DateTimeOffset.UtcNow;
        var assignments = new List<RoleAssignment>();

        try
        {
            using var reader = new StreamReader(csvStream);
            var headerLine = await reader.ReadLineAsync();

            if (string.IsNullOrEmpty(headerLine))
            {
                return DataSyncResult<List<RoleAssignment>>.Failed(
                    DataSourceType.ServiceNow, startTime, "CSV file is empty");
            }

            var headers = ParseCsvLine(headerLine);
            var columnMap = BuildColumnMap(headers);
            var rowNumber = 1;

            while (!reader.EndOfStream)
            {
                rowNumber++;
                var line = await reader.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var values = ParseCsvLine(line);
                    var assignment = ParseRoleAssignmentRow(values, columnMap);
                    if (assignment != null)
                    {
                        assignments.Add(assignment);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error parsing row {RowNumber}", rowNumber);
                }
            }

            return new DataSyncResult<List<RoleAssignment>>
            {
                Success = true,
                DataSource = DataSourceType.ServiceNow,
                Data = assignments,
                RecordsProcessed = rowNumber - 1,
                RecordsCreated = assignments.Count,
                StartTime = startTime,
                EndTime = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing role assignments from CSV");
            return DataSyncResult<List<RoleAssignment>>.Failed(
                DataSourceType.ServiceNow, startTime, ex.Message);
        }
    }

    public async Task<DataSyncResult<List<RoleAssignment>>> ImportRoleAssignmentsAsync(string csvFilePath)
    {
        if (!File.Exists(csvFilePath))
        {
            return DataSyncResult<List<RoleAssignment>>.Failed(
                DataSourceType.ServiceNow, DateTimeOffset.UtcNow, $"File not found: {csvFilePath}");
        }

        await using var stream = File.OpenRead(csvFilePath);
        return await ImportRoleAssignmentsAsync(stream);
    }

    public Task<DateTimeOffset?> GetLastImportDateAsync() => Task.FromResult(_lastImportDate);

    public async Task<DataSyncResult> SyncFromCsvAsync(string applicationsFilePath, string? roleAssignmentsFilePath = null)
    {
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            var appsResult = await ImportApplicationsAsync(applicationsFilePath);
            if (!appsResult.Success)
            {
                return DataSyncResult.Failed(DataSourceType.ServiceNow, startTime,
                    appsResult.ErrorMessage ?? "Failed to import applications");
            }

            if (!string.IsNullOrEmpty(roleAssignmentsFilePath))
            {
                var rolesResult = await ImportRoleAssignmentsAsync(roleAssignmentsFilePath);
                if (!rolesResult.Success)
                {
                    return DataSyncResult.Failed(DataSourceType.ServiceNow, startTime,
                        rolesResult.ErrorMessage ?? "Failed to import role assignments");
                }
            }

            return DataSyncResult.Succeeded(DataSourceType.ServiceNow, startTime,
                appsResult.RecordsProcessed, appsResult.RecordsCreated, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing from CSV");
            return DataSyncResult.Failed(DataSourceType.ServiceNow, startTime, ex.Message);
        }
    }

    public async Task<List<DataConflict>> ValidateUsersAsync(List<RoleAssignment> assignments)
    {
        var conflicts = new List<DataConflict>();

        try
        {
            var users = await _mockDataService.GetUsersAsync();
            var userIds = users.Select(u => u.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var assignment in assignments)
            {
                if (!userIds.Contains(assignment.UserId))
                {
                    conflicts.Add(new DataConflict
                    {
                        Id = Guid.NewGuid().ToString(),
                        ApplicationId = assignment.ApplicationId ?? "",
                        ApplicationName = "",
                        Type = ConflictType.UserNotFound,
                        Description = $"User '{assignment.UserName}' ({assignment.UserId}) not found in user directory",
                        SourceA = "ServiceNow",
                        SourceB = "User Directory",
                        ValueA = $"{assignment.UserName} ({assignment.UserId})",
                        ValueB = "Not found",
                        DetectedAt = DateTimeOffset.UtcNow
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating users");
        }

        return conflicts;
    }

    public ServiceNowCsvMapping GetCsvMapping() => _mapping;

    public Task SetCsvMappingAsync(ServiceNowCsvMapping mapping)
    {
        _mapping = mapping;
        return Task.CompletedTask;
    }

    #region Private Helpers

    private Dictionary<string, int> BuildColumnMap(List<string> headers)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Count; i++)
        {
            map[headers[i].Trim()] = i;
        }
        return map;
    }

    private ServiceNowApplication? ParseApplicationRow(List<string> values, Dictionary<string, int> columnMap)
    {
        string? GetValue(string columnName)
        {
            if (columnMap.TryGetValue(columnName, out var index) && index < values.Count)
            {
                var value = values[index].Trim();
                return string.IsNullOrEmpty(value) ? null : value;
            }
            return null;
        }

        var serviceNowId = GetValue(_mapping.ServiceNowIdColumn);
        var name = GetValue(_mapping.NameColumn);

        if (string.IsNullOrEmpty(serviceNowId) || string.IsNullOrEmpty(name))
        {
            return null;
        }

        return new ServiceNowApplication
        {
            ServiceNowId = serviceNowId,
            Name = name,
            Description = GetValue(_mapping.DescriptionColumn),
            Capability = GetValue(_mapping.CapabilityColumn),
            Status = GetValue(_mapping.StatusColumn)
        };
    }

    private RoleAssignment? ParseRoleAssignmentRow(List<string> values, Dictionary<string, int> columnMap)
    {
        string? GetValue(string columnName)
        {
            if (columnMap.TryGetValue(columnName, out var index) && index < values.Count)
            {
                var value = values[index].Trim();
                return string.IsNullOrEmpty(value) ? null : value;
            }
            return null;
        }

        var appId = GetValue(_mapping.RoleAppIdColumn);
        var userId = GetValue(_mapping.RoleUserIdColumn);
        var roleType = GetValue(_mapping.RoleTypeColumn);

        if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(roleType))
        {
            return null;
        }

        return new RoleAssignment
        {
            Id = Guid.NewGuid().ToString(),
            ApplicationId = appId,
            UserId = userId,
            UserName = GetValue(_mapping.RoleUserNameColumn) ?? "",
            UserEmail = GetValue(_mapping.RoleUserEmailColumn) ?? "",
            Role = ParseRoleType(roleType),
            AssignedDate = DateTimeOffset.UtcNow
        };
    }

    private static ApplicationRole ParseRoleType(string roleType) => roleType.ToLowerInvariant() switch
    {
        "owner" or "application owner" => ApplicationRole.Owner,
        "technical lead" or "tech lead" => ApplicationRole.TechnicalLead,
        "business owner" => ApplicationRole.BusinessOwner,
        "developer" => ApplicationRole.Developer,
        "security champion" => ApplicationRole.SecurityChampion,
        "support" or "operations" => ApplicationRole.Support,
        _ => ApplicationRole.Support
    };

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var inQuotes = false;
        var currentValue = new System.Text.StringBuilder();

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    currentValue.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentValue.ToString());
                currentValue.Clear();
            }
            else
            {
                currentValue.Append(c);
            }
        }

        result.Add(currentValue.ToString());
        return result;
    }

    #endregion
}
