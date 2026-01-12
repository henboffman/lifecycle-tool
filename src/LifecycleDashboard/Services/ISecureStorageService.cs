namespace LifecycleDashboard.Services;

/// <summary>
/// Service for securely storing and retrieving sensitive configuration values.
/// Values are encrypted at rest and stored outside the web application.
/// </summary>
public interface ISecureStorageService
{
    /// <summary>
    /// Stores a secret value with encryption.
    /// </summary>
    Task StoreSecretAsync(string key, string value);

    /// <summary>
    /// Retrieves a decrypted secret value.
    /// </summary>
    Task<string?> GetSecretAsync(string key);

    /// <summary>
    /// Checks if a secret exists without retrieving its value.
    /// </summary>
    Task<bool> HasSecretAsync(string key);

    /// <summary>
    /// Removes a stored secret.
    /// </summary>
    Task RemoveSecretAsync(string key);

    /// <summary>
    /// Gets all secret keys (not values) for a given prefix.
    /// </summary>
    Task<IReadOnlyList<string>> GetSecretKeysAsync(string prefix);

    /// <summary>
    /// Gets credential status for a data source (has values, last updated).
    /// </summary>
    Task<CredentialStatus> GetCredentialStatusAsync(string dataSourceId);
}

/// <summary>
/// Status of stored credentials for a data source.
/// </summary>
public record CredentialStatus
{
    public bool HasCredentials { get; init; }
    public DateTimeOffset? LastUpdated { get; init; }
    public IReadOnlyList<string> ConfiguredFields { get; init; } = [];
}

/// <summary>
/// Well-known secret keys for the application.
/// </summary>
public static class SecretKeys
{
    public const string Prefix = "lifecycle:";

    // Azure DevOps
    public const string AzureDevOpsOrganization = "lifecycle:datasource:azuredevops:organization";
    public const string AzureDevOpsProject = "lifecycle:datasource:azuredevops:project";
    public const string AzureDevOpsUsername = "lifecycle:datasource:azuredevops:username";
    public const string AzureDevOpsPat = "lifecycle:datasource:azuredevops:pat";

    // SharePoint
    public const string SharePointSiteUrl = "lifecycle:datasource:sharepoint:siteurl";
    public const string SharePointClientId = "lifecycle:datasource:sharepoint:clientid";
    public const string SharePointClientSecret = "lifecycle:datasource:sharepoint:clientsecret";
    public const string SharePointRootPath = "lifecycle:datasource:sharepoint:rootpath";

    // ServiceNow
    public const string ServiceNowInstance = "lifecycle:datasource:servicenow:instance";
    public const string ServiceNowUsername = "lifecycle:datasource:servicenow:username";
    public const string ServiceNowPassword = "lifecycle:datasource:servicenow:password";

    // IIS Database
    public const string IisDatabaseConnectionString = "lifecycle:datasource:iisdb:connectionstring";

    // Azure OpenAI
    public const string AzureOpenAiEndpoint = "lifecycle:ai:azureopenai:endpoint";
    public const string AzureOpenAiKey = "lifecycle:ai:azureopenai:key";
    public const string AzureOpenAiDeployment = "lifecycle:ai:azureopenai:deployment";

    // Ollama (local)
    public const string OllamaEndpoint = "lifecycle:ai:ollama:endpoint";
    public const string OllamaModel = "lifecycle:ai:ollama:model";

    /// <summary>
    /// Gets the data source prefix for a given data source ID.
    /// </summary>
    public static string GetDataSourcePrefix(string dataSourceId) =>
        $"lifecycle:datasource:{dataSourceId.ToLowerInvariant()}:";

    /// <summary>
    /// Determines if a key represents a sensitive value that should be masked.
    /// </summary>
    public static bool IsSensitive(string key) =>
        key.Contains(":pat", StringComparison.OrdinalIgnoreCase) ||
        key.Contains(":password", StringComparison.OrdinalIgnoreCase) ||
        key.Contains(":secret", StringComparison.OrdinalIgnoreCase) ||
        key.Contains(":key", StringComparison.OrdinalIgnoreCase) ||
        key.Contains(":connectionstring", StringComparison.OrdinalIgnoreCase);
}
