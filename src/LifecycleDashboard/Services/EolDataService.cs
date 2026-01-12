using System.Text.Json;
using System.Text.Json.Serialization;
using LifecycleDashboard.Models;

namespace LifecycleDashboard.Services;

/// <summary>
/// Service that fetches end-of-life data from endoflife.date API
/// </summary>
public class EolDataService : IEolDataService
{
    private readonly HttpClient _httpClient;
    private readonly IMockDataService _dataService;
    private readonly ILogger<EolDataService> _logger;

    private static readonly Dictionary<FrameworkType, string> FrameworkApiEndpoints = new()
    {
        { FrameworkType.DotNet, "https://endoflife.date/api/dotnet.json" },
        { FrameworkType.DotNetFramework, "https://endoflife.date/api/dotnet-framework.json" },
        { FrameworkType.Python, "https://endoflife.date/api/python.json" },
        { FrameworkType.NodeJs, "https://endoflife.date/api/nodejs.json" }
        // Note: R is not available on endoflife.date
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public EolDataService(HttpClient httpClient, IMockDataService dataService, ILogger<EolDataService> logger)
    {
        _httpClient = httpClient;
        _dataService = dataService;
        _logger = logger;

        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "LifecycleDashboard/1.0");
    }

    public async Task<EolRefreshResult> RefreshEolDataAsync()
    {
        var result = new EolRefreshResult
        {
            Success = true,
            Added = [],
            Updated = [],
            Unchanged = [],
            Errors = []
        };

        foreach (var (frameworkType, _) in FrameworkApiEndpoints)
        {
            try
            {
                var frameworkResult = await RefreshFrameworkEolDataAsync(frameworkType);
                result.Added.AddRange(frameworkResult.Added);
                result.Updated.AddRange(frameworkResult.Updated);
                result.Unchanged.AddRange(frameworkResult.Unchanged);
                result.Errors.AddRange(frameworkResult.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh EOL data for {Framework}", frameworkType);
                result.Errors.Add(new EolFetchError
                {
                    Framework = frameworkType,
                    ErrorMessage = ex.Message
                });
            }
        }

        result = result with { Success = result.Errors.Count == 0 };
        return result;
    }

    public async Task<EolRefreshResult> RefreshFrameworkEolDataAsync(FrameworkType frameworkType)
    {
        var result = new EolRefreshResult
        {
            Success = true,
            Added = [],
            Updated = [],
            Unchanged = [],
            Errors = []
        };

        if (!FrameworkApiEndpoints.TryGetValue(frameworkType, out var apiUrl))
        {
            result = result with
            {
                Success = false,
                ErrorMessage = $"No API endpoint configured for {frameworkType}"
            };
            return result;
        }

        try
        {
            _logger.LogInformation("Fetching EOL data from {Url}", apiUrl);
            var response = await _httpClient.GetAsync(apiUrl);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var eolEntries = JsonSerializer.Deserialize<List<EndOfLifeEntry>>(json, JsonOptions);

            if (eolEntries == null || eolEntries.Count == 0)
            {
                result.Unchanged.Add(GetFrameworkLabel(frameworkType));
                return result;
            }

            // Get existing versions
            var existingVersions = (await _dataService.GetFrameworkVersionsByTypeAsync(frameworkType)).ToList();

            foreach (var entry in eolEntries)
            {
                var version = MapToFrameworkVersion(entry, frameworkType);
                if (version == null) continue;

                var existing = existingVersions.FirstOrDefault(v =>
                    v.Version.Equals(version.Version, StringComparison.OrdinalIgnoreCase));

                if (existing == null)
                {
                    // New version - add it
                    await _dataService.CreateFrameworkVersionAsync(version);
                    result.Added.Add(version);
                    _logger.LogInformation("Added new framework version: {DisplayName}", version.DisplayName);
                }
                else if (HasChanges(existing, version))
                {
                    // Existing version with changes - update it
                    var updateInfo = new EolUpdateInfo
                    {
                        Version = version,
                        PreviousEolDate = existing.EndOfLifeDate,
                        NewEolDate = version.EndOfLifeDate,
                        PreviousStatus = existing.Status,
                        NewStatus = version.Status,
                        ChangeDescription = GetChangeDescription(existing, version)
                    };

                    var updated = existing with
                    {
                        EndOfLifeDate = version.EndOfLifeDate,
                        EndOfActiveSupportDate = version.EndOfActiveSupportDate,
                        Status = version.Status,
                        IsLts = version.IsLts,
                        LatestPatchVersion = version.LatestPatchVersion,
                        LastUpdated = DateTimeOffset.UtcNow
                    };

                    await _dataService.UpdateFrameworkVersionAsync(updated);
                    result.Updated.Add(updateInfo);
                    _logger.LogInformation("Updated framework version: {DisplayName}", version.DisplayName);
                }
                else
                {
                    result.Unchanged.Add(version.DisplayName);
                }
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching EOL data for {Framework}", frameworkType);
            result = result with { Success = false };
            result.Errors.Add(new EolFetchError
            {
                Framework = frameworkType,
                ErrorMessage = $"Network error: {ex.Message}"
            });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error for {Framework}", frameworkType);
            result = result with { Success = false };
            result.Errors.Add(new EolFetchError
            {
                Framework = frameworkType,
                ErrorMessage = $"Data format error: {ex.Message}"
            });
        }

        return result;
    }

    private static FrameworkVersion? MapToFrameworkVersion(EndOfLifeEntry entry, FrameworkType frameworkType)
    {
        if (string.IsNullOrEmpty(entry.Cycle)) return null;

        var version = entry.Cycle;
        var displayName = GetDisplayName(frameworkType, version);

        // Parse dates
        DateTimeOffset? releaseDate = ParseDate(entry.ReleaseDate);
        DateTimeOffset? eolDate = ParseEolDate(entry.Eol);
        DateTimeOffset? activeSupportDate = ParseEolDate(entry.Support);

        // Determine status
        var status = DetermineStatus(eolDate, activeSupportDate);

        return new FrameworkVersion
        {
            Id = $"{frameworkType}-{version}".ToLowerInvariant(),
            Framework = frameworkType,
            Version = version,
            DisplayName = displayName,
            ReleaseDate = releaseDate,
            EndOfLifeDate = eolDate,
            EndOfActiveSupportDate = activeSupportDate,
            Status = status,
            IsLts = entry.Lts,
            LatestPatchVersion = entry.Latest,
            Notes = null,
            RecommendedUpgradePath = null,
            LastUpdated = DateTimeOffset.UtcNow
        };
    }

    private static DateTimeOffset? ParseDate(string? dateStr)
    {
        if (string.IsNullOrEmpty(dateStr)) return null;
        if (DateTimeOffset.TryParse(dateStr, out var date))
            return date;
        return null;
    }

    private static DateTimeOffset? ParseEolDate(object? eolValue)
    {
        if (eolValue == null) return null;

        // Handle boolean false (means no EOL date / indefinite support)
        if (eolValue is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.False) return null;
            if (element.ValueKind == JsonValueKind.True) return DateTimeOffset.UtcNow; // Already EOL
            if (element.ValueKind == JsonValueKind.String)
            {
                var str = element.GetString();
                if (DateTimeOffset.TryParse(str, out var date))
                    return date;
            }
        }

        if (eolValue is bool b && !b) return null;
        if (eolValue is string s && DateTimeOffset.TryParse(s, out var parsed))
            return parsed;

        return null;
    }

    private static SupportStatus DetermineStatus(DateTimeOffset? eolDate, DateTimeOffset? activeSupportDate)
    {
        var now = DateTimeOffset.UtcNow;

        if (eolDate.HasValue && eolDate.Value < now)
            return SupportStatus.EndOfLife;

        if (activeSupportDate.HasValue && activeSupportDate.Value < now)
            return SupportStatus.Maintenance;

        return SupportStatus.Active;
    }

    private static string GetDisplayName(FrameworkType frameworkType, string version) => frameworkType switch
    {
        FrameworkType.DotNet => version.Contains('.') ? $".NET {version}" : $".NET {version}.0",
        FrameworkType.DotNetFramework => $".NET Framework {version}",
        FrameworkType.Python => $"Python {version}",
        FrameworkType.NodeJs => $"Node.js {version}",
        FrameworkType.R => $"R {version}",
        FrameworkType.Java => $"Java {version}",
        _ => $"{frameworkType} {version}"
    };

    private static string GetFrameworkLabel(FrameworkType type) => type switch
    {
        FrameworkType.DotNet => ".NET",
        FrameworkType.DotNetFramework => ".NET Framework",
        FrameworkType.Python => "Python",
        FrameworkType.NodeJs => "Node.js",
        FrameworkType.R => "R",
        FrameworkType.Java => "Java",
        _ => type.ToString()
    };

    private static bool HasChanges(FrameworkVersion existing, FrameworkVersion updated)
    {
        return existing.EndOfLifeDate != updated.EndOfLifeDate
            || existing.EndOfActiveSupportDate != updated.EndOfActiveSupportDate
            || existing.Status != updated.Status
            || existing.IsLts != updated.IsLts
            || existing.LatestPatchVersion != updated.LatestPatchVersion;
    }

    private static string GetChangeDescription(FrameworkVersion existing, FrameworkVersion updated)
    {
        var changes = new List<string>();

        if (existing.EndOfLifeDate != updated.EndOfLifeDate)
        {
            var oldDate = existing.EndOfLifeDate?.ToString("MMM d, yyyy") ?? "N/A";
            var newDate = updated.EndOfLifeDate?.ToString("MMM d, yyyy") ?? "N/A";
            changes.Add($"EOL: {oldDate} → {newDate}");
        }

        if (existing.Status != updated.Status)
        {
            changes.Add($"Status: {existing.Status} → {updated.Status}");
        }

        if (existing.IsLts != updated.IsLts)
        {
            changes.Add(updated.IsLts ? "Now LTS" : "No longer LTS");
        }

        if (existing.LatestPatchVersion != updated.LatestPatchVersion)
        {
            changes.Add($"Latest: {existing.LatestPatchVersion ?? "N/A"} → {updated.LatestPatchVersion ?? "N/A"}");
        }

        return string.Join("; ", changes);
    }

    /// <summary>
    /// DTO for endoflife.date API response
    /// </summary>
    private class EndOfLifeEntry
    {
        [JsonPropertyName("cycle")]
        public string? Cycle { get; set; }

        [JsonPropertyName("releaseDate")]
        public string? ReleaseDate { get; set; }

        [JsonPropertyName("eol")]
        public object? Eol { get; set; }

        [JsonPropertyName("support")]
        public object? Support { get; set; }

        [JsonPropertyName("lts")]
        public bool Lts { get; set; }

        [JsonPropertyName("latest")]
        public string? Latest { get; set; }

        [JsonPropertyName("latestReleaseDate")]
        public string? LatestReleaseDate { get; set; }

        [JsonPropertyName("discontinued")]
        public object? Discontinued { get; set; }
    }
}
