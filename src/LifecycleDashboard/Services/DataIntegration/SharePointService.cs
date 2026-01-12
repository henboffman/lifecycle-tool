using System.Net.Http.Headers;
using System.Text.Json;
using LifecycleDashboard.Models;
using Microsoft.Extensions.Logging;

namespace LifecycleDashboard.Services.DataIntegration;

/// <summary>
/// Service for integrating with SharePoint to discover application documentation folders
/// and check documentation completeness using Microsoft Graph API.
/// </summary>
public class SharePointService : ISharePointService
{
    private readonly HttpClient _httpClient;
    private readonly ISecureStorageService _secureStorage;
    private readonly ILogger<SharePointService> _logger;

    private const string GraphBaseUrl = "https://graph.microsoft.com/v1.0";

    public SharePointService(
        HttpClient httpClient,
        ISecureStorageService secureStorage,
        ILogger<SharePointService> logger)
    {
        _httpClient = httpClient;
        _secureStorage = secureStorage;
        _logger = logger;
    }

    public async Task<ConnectionTestResult> TestConnectionAsync()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var (configured, error) = await ConfigureClientAsync();
            if (!configured)
            {
                return ConnectionTestResult.Failed(error ?? "Failed to configure SharePoint client");
            }

            var siteId = await _secureStorage.GetSecretAsync(SecretKeys.SharePointSiteUrl);
            var response = await _httpClient.GetAsync($"{GraphBaseUrl}/sites/{siteId}");

            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var doc = JsonDocument.Parse(content);
                var siteName = doc.RootElement.TryGetProperty("displayName", out var name)
                    ? name.GetString()
                    : null;

                return ConnectionTestResult.Succeeded(
                    $"Successfully connected to SharePoint site: {siteName}",
                    stopwatch.Elapsed);
            }

            return ConnectionTestResult.Failed($"Connection failed: {response.StatusCode} - {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing SharePoint connection");
            return ConnectionTestResult.Failed($"Connection error: {ex.Message}");
        }
    }

    public async Task<DataSyncResult<List<ApplicationFolder>>> DiscoverApplicationFoldersAsync()
    {
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            var (configured, error) = await ConfigureClientAsync();
            if (!configured)
            {
                return DataSyncResult<List<ApplicationFolder>>.Failed(
                    DataSourceType.SharePoint, startTime, error ?? "Not configured");
            }

            var applicationFolders = new List<ApplicationFolder>();

            // Get capabilities from the root path
            var capabilitiesResult = await GetCapabilitiesAsync();
            if (!capabilitiesResult.Success)
            {
                return DataSyncResult<List<ApplicationFolder>>.Failed(
                    DataSourceType.SharePoint, startTime,
                    capabilitiesResult.ErrorMessage ?? "Failed to get capabilities");
            }

            // For each capability, get application folders
            foreach (var capability in capabilitiesResult.Data ?? [])
            {
                var appsResult = await GetApplicationsInCapabilityAsync(capability);
                if (appsResult.Success && appsResult.Data != null)
                {
                    applicationFolders.AddRange(appsResult.Data);
                }
            }

            return new DataSyncResult<List<ApplicationFolder>>
            {
                Success = true,
                DataSource = DataSourceType.SharePoint,
                Data = applicationFolders,
                RecordsProcessed = applicationFolders.Count,
                StartTime = startTime,
                EndTime = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering application folders");
            return DataSyncResult<List<ApplicationFolder>>.Failed(
                DataSourceType.SharePoint, startTime, ex.Message);
        }
    }

    public async Task<DataSyncResult<DocumentationStatus>> CheckDocumentationCompletenessAsync(string folderPath)
    {
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            var (configured, error) = await ConfigureClientAsync();
            if (!configured)
            {
                return DataSyncResult<DocumentationStatus>.Failed(
                    DataSourceType.SharePoint, startTime, error ?? "Not configured");
            }

            // Stub implementation - return empty status
            var docStatus = new DocumentationStatus();

            return DataSyncResult<DocumentationStatus>.Succeeded(DataSourceType.SharePoint, docStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking documentation completeness for {FolderPath}", folderPath);
            return DataSyncResult<DocumentationStatus>.Failed(
                DataSourceType.SharePoint, startTime, ex.Message);
        }
    }

    public async Task<DataSyncResult<List<SharePointDocument>>> GetDocumentsAsync(string folderPath)
    {
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            var (configured, error) = await ConfigureClientAsync();
            if (!configured)
            {
                return DataSyncResult<List<SharePointDocument>>.Failed(
                    DataSourceType.SharePoint, startTime, error ?? "Not configured");
            }

            var documents = new List<SharePointDocument>();

            return new DataSyncResult<List<SharePointDocument>>
            {
                Success = true,
                DataSource = DataSourceType.SharePoint,
                Data = documents,
                RecordsProcessed = documents.Count,
                StartTime = startTime,
                EndTime = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting documents from {FolderPath}", folderPath);
            return DataSyncResult<List<SharePointDocument>>.Failed(
                DataSourceType.SharePoint, startTime, ex.Message);
        }
    }

    public async Task<DataSyncResult<List<string>>> GetCapabilitiesAsync()
    {
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            var (configured, error) = await ConfigureClientAsync();
            if (!configured)
            {
                return DataSyncResult<List<string>>.Failed(
                    DataSourceType.SharePoint, startTime, error ?? "Not configured");
            }

            var capabilities = new List<string>();

            return new DataSyncResult<List<string>>
            {
                Success = true,
                DataSource = DataSourceType.SharePoint,
                Data = capabilities,
                RecordsProcessed = capabilities.Count,
                StartTime = startTime,
                EndTime = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting capabilities");
            return DataSyncResult<List<string>>.Failed(
                DataSourceType.SharePoint, startTime, ex.Message);
        }
    }

    public async Task<DataSyncResult<List<ApplicationFolder>>> GetApplicationsInCapabilityAsync(string capability)
    {
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            var (configured, error) = await ConfigureClientAsync();
            if (!configured)
            {
                return DataSyncResult<List<ApplicationFolder>>.Failed(
                    DataSourceType.SharePoint, startTime, error ?? "Not configured");
            }

            var applicationFolders = new List<ApplicationFolder>();

            return new DataSyncResult<List<ApplicationFolder>>
            {
                Success = true,
                DataSource = DataSourceType.SharePoint,
                Data = applicationFolders,
                RecordsProcessed = applicationFolders.Count,
                StartTime = startTime,
                EndTime = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting applications in capability {Capability}", capability);
            return DataSyncResult<List<ApplicationFolder>>.Failed(
                DataSourceType.SharePoint, startTime, ex.Message);
        }
    }

    public async Task<DataSyncResult> SyncDocumentationStatusAsync()
    {
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            var foldersResult = await DiscoverApplicationFoldersAsync();
            if (!foldersResult.Success)
            {
                return DataSyncResult.Failed(DataSourceType.SharePoint, startTime,
                    foldersResult.ErrorMessage ?? "Failed to discover folders");
            }

            return DataSyncResult.Succeeded(DataSourceType.SharePoint, startTime,
                foldersResult.RecordsProcessed, 0, 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing documentation status");
            return DataSyncResult.Failed(DataSourceType.SharePoint, startTime, ex.Message);
        }
    }

    private async Task<(bool Configured, string? Error)> ConfigureClientAsync()
    {
        var siteUrl = await _secureStorage.GetSecretAsync(SecretKeys.SharePointSiteUrl);
        var clientId = await _secureStorage.GetSecretAsync(SecretKeys.SharePointClientId);
        var clientSecret = await _secureStorage.GetSecretAsync(SecretKeys.SharePointClientSecret);

        if (string.IsNullOrEmpty(siteUrl) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            return (false, "SharePoint configuration incomplete");
        }

        // TODO: Get OAuth token and configure client
        return (true, null);
    }
}
