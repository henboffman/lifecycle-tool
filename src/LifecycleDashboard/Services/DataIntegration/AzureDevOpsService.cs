using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using LifecycleDashboard.Models;
using Microsoft.Extensions.Logging;

namespace LifecycleDashboard.Services.DataIntegration;

/// <summary>
/// Service for integrating with Azure DevOps to retrieve repository data,
/// detect technology stacks, analyze packages, and track commit history.
/// </summary>
public partial class AzureDevOpsService : IAzureDevOpsService
{
    private readonly HttpClient _httpClient;
    private readonly ISecureStorageService _secureStorage;
    private readonly IMockDataService _mockDataService;
    private readonly ILogger<AzureDevOpsService> _logger;

    public AzureDevOpsService(
        HttpClient httpClient,
        ISecureStorageService secureStorage,
        IMockDataService mockDataService,
        ILogger<AzureDevOpsService> logger)
    {
        _httpClient = httpClient;
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
            var (configured, error, baseUrl, auth) = await GetConfigurationAsync();
            if (!configured || baseUrl == null || auth == null)
            {
                return ConnectionTestResult.Failed(error ?? "Failed to configure Azure DevOps client");
            }

            // Test connection using organization-level projects endpoint (no project in path)
            // URL format: https://dev.azure.com/{organization}/_apis/projects
            var response = await SendRequestAsync($"{baseUrl}_apis/projects?api-version=7.1", auth);

            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var projectCount = "unknown";
                try
                {
                    var json = System.Text.Json.JsonDocument.Parse(content);
                    if (json.RootElement.TryGetProperty("count", out var count))
                    {
                        projectCount = count.GetInt32().ToString();
                    }
                }
                catch { }

                return ConnectionTestResult.Succeeded(
                    $"Successfully connected to Azure DevOps ({projectCount} projects found)",
                    stopwatch.Elapsed,
                    null);
            }

            // Include response body for better error diagnosis
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Azure DevOps connection test failed. Status: {Status}, Body: {Body}",
                response.StatusCode, errorBody);

            return ConnectionTestResult.Failed($"Connection failed: {response.StatusCode} - {response.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing Azure DevOps connection");
            return ConnectionTestResult.Failed($"Connection error: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<DataSyncResult<List<AzureDevOpsRepository>>> GetRepositoriesAsync()
    {
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            var (configured, error, baseUrl, auth) = await GetConfigurationAsync();
            if (!configured || baseUrl == null || auth == null)
            {
                return DataSyncResult<List<AzureDevOpsRepository>>.Failed(
                    DataSourceType.AzureDevOps, startTime, error ?? "Not configured");
            }

            var project = Uri.EscapeDataString(await _secureStorage.GetSecretAsync(SecretKeys.AzureDevOpsProject) ?? "");
            var response = await SendRequestAsync($"{baseUrl}{project}/_apis/git/repositories?api-version=7.1", auth);

            if (!response.IsSuccessStatusCode)
            {
                return DataSyncResult<List<AzureDevOpsRepository>>.Failed(
                    DataSourceType.AzureDevOps, startTime, $"API returned {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(content);
            var repositories = new List<AzureDevOpsRepository>();

            if (jsonDoc.RootElement.TryGetProperty("value", out var reposElement))
            {
                foreach (var repo in reposElement.EnumerateArray())
                {
                    repositories.Add(new AzureDevOpsRepository
                    {
                        Id = repo.GetProperty("id").GetString() ?? "",
                        Name = repo.GetProperty("name").GetString() ?? "",
                        Url = repo.GetProperty("webUrl").GetString() ?? "",
                        CloneUrl = repo.TryGetProperty("remoteUrl", out var remote) ? remote.GetString() : null,
                        DefaultBranch = repo.TryGetProperty("defaultBranch", out var branch)
                            ? branch.GetString()?.Replace("refs/heads/", "")
                            : null,
                        ProjectName = repo.TryGetProperty("project", out var proj)
                            ? proj.GetProperty("name").GetString()
                            : null,
                        SizeBytes = repo.TryGetProperty("size", out var size) ? size.GetInt64() : null,
                        IsDisabled = repo.TryGetProperty("isDisabled", out var disabled) && disabled.GetBoolean()
                    });
                }
            }

            return new DataSyncResult<List<AzureDevOpsRepository>>
            {
                Success = true,
                DataSource = DataSourceType.AzureDevOps,
                Data = repositories,
                RecordsProcessed = repositories.Count,
                StartTime = startTime,
                EndTime = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting repositories from Azure DevOps");
            return DataSyncResult<List<AzureDevOpsRepository>>.Failed(
                DataSourceType.AzureDevOps, startTime, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<DataSyncResult<TechStackDetectionResult>> DetectTechStackAsync(string repositoryId)
    {
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            var (configured, error, baseUrl, auth) = await GetConfigurationAsync();
            if (!configured || baseUrl == null || auth == null)
            {
                return DataSyncResult<TechStackDetectionResult>.Failed(
                    DataSourceType.AzureDevOps, startTime, error ?? "Not configured");
            }

            var project = Uri.EscapeDataString(await _secureStorage.GetSecretAsync(SecretKeys.AzureDevOpsProject) ?? "");

            // Get the file tree
            var itemsResponse = await SendRequestAsync(
                $"{baseUrl}{project}/_apis/git/repositories/{repositoryId}/items?recursionLevel=Full&api-version=7.1", auth);

            if (!itemsResponse.IsSuccessStatusCode)
            {
                return DataSyncResult<TechStackDetectionResult>.Failed(
                    DataSourceType.AzureDevOps, startTime,
                    $"Failed to get repository items: {itemsResponse.StatusCode}");
            }

            var content = await itemsResponse.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(content);

            var result = new TechStackDetectionResult
            {
                PrimaryStack = PrimaryStack.Unknown,
                Frameworks = [],
                Languages = [],
                ProjectFiles = []
            };

            if (!jsonDoc.RootElement.TryGetProperty("value", out var items))
            {
                return DataSyncResult<TechStackDetectionResult>.Succeeded(DataSourceType.AzureDevOps, result);
            }

            // Analyze file tree
            var files = items.EnumerateArray()
                .Where(i => i.TryGetProperty("gitObjectType", out var type) && type.GetString() == "blob")
                .Select(i => i.GetProperty("path").GetString() ?? "")
                .ToList();

            // Detect project files
            var csprojFiles = files.Where(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)).ToList();
            var packageJsonFiles = files.Where(f => f.EndsWith("package.json", StringComparison.OrdinalIgnoreCase)).ToList();
            var requirementsTxt = files.Any(f => f.EndsWith("requirements.txt", StringComparison.OrdinalIgnoreCase));
            var pomXml = files.Any(f => f.EndsWith("pom.xml", StringComparison.OrdinalIgnoreCase));

            result = result with { ProjectFiles = [..csprojFiles, ..packageJsonFiles] };

            // Detect languages
            var languages = new List<string>();
            if (csprojFiles.Count != 0) languages.Add("C#");
            if (packageJsonFiles.Count != 0) languages.AddRange(["JavaScript", "TypeScript"]);
            if (requirementsTxt) languages.Add("Python");
            if (pomXml) languages.Add("Java");
            result = result with { Languages = languages };

            // Parse .csproj files for framework info
            if (csprojFiles.Count != 0)
            {
                var csprojContent = await GetFileContentAsync(baseUrl, auth, project, repositoryId, csprojFiles.First());
                if (csprojContent != null)
                {
                    result = await ParseCsprojAsync(result, csprojContent);
                }
            }

            // Parse package.json for frontend frameworks
            if (packageJsonFiles.Count != 0)
            {
                var packageJsonContent = await GetFileContentAsync(baseUrl, auth, project, repositoryId, packageJsonFiles.First());
                if (packageJsonContent != null)
                {
                    result = ParsePackageJson(result, packageJsonContent);
                }
            }

            // Determine primary stack
            result = result with { PrimaryStack = DeterminePrimaryStack(result) };

            // Generate detected pattern
            result = result with { DetectedPattern = GeneratePatternDescription(result) };

            return DataSyncResult<TechStackDetectionResult>.Succeeded(DataSourceType.AzureDevOps, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting tech stack for repository {RepositoryId}", repositoryId);
            return DataSyncResult<TechStackDetectionResult>.Failed(
                DataSourceType.AzureDevOps, startTime, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<DataSyncResult<List<PackageReference>>> GetPackagesAsync(string repositoryId)
    {
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            var (configured, error, baseUrl, auth) = await GetConfigurationAsync();
            if (!configured || baseUrl == null || auth == null)
            {
                return DataSyncResult<List<PackageReference>>.Failed(
                    DataSourceType.AzureDevOps, startTime, error ?? "Not configured");
            }

            var project = Uri.EscapeDataString(await _secureStorage.GetSecretAsync(SecretKeys.AzureDevOpsProject) ?? "");
            var packages = new List<PackageReference>();

            // Get the file tree
            var itemsResponse = await SendRequestAsync(
                $"{baseUrl}{project}/_apis/git/repositories/{repositoryId}/items?recursionLevel=Full&api-version=7.1", auth);

            if (!itemsResponse.IsSuccessStatusCode)
            {
                return DataSyncResult<List<PackageReference>>.Failed(
                    DataSourceType.AzureDevOps, startTime,
                    $"Failed to get repository items: {itemsResponse.StatusCode}");
            }

            var content = await itemsResponse.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(content);

            if (!jsonDoc.RootElement.TryGetProperty("value", out var items))
            {
                return DataSyncResult<List<PackageReference>>.Succeeded(DataSourceType.AzureDevOps, packages);
            }

            var files = items.EnumerateArray()
                .Where(i => i.TryGetProperty("gitObjectType", out var type) && type.GetString() == "blob")
                .Select(i => i.GetProperty("path").GetString() ?? "")
                .ToList();

            // Parse .csproj files for NuGet packages
            foreach (var csproj in files.Where(f => f.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)))
            {
                var csprojContent = await GetFileContentAsync(baseUrl, auth, project, repositoryId, csproj);
                if (csprojContent != null)
                {
                    packages.AddRange(ParseNuGetPackages(csprojContent, csproj));
                }
            }

            // Parse package.json for npm packages
            foreach (var packageJson in files.Where(f => f.EndsWith("package.json", StringComparison.OrdinalIgnoreCase)))
            {
                var packageJsonContent = await GetFileContentAsync(baseUrl, auth, project, repositoryId, packageJson);
                if (packageJsonContent != null)
                {
                    packages.AddRange(ParseNpmPackages(packageJsonContent, packageJson));
                }
            }

            return new DataSyncResult<List<PackageReference>>
            {
                Success = true,
                DataSource = DataSourceType.AzureDevOps,
                Data = packages,
                RecordsProcessed = packages.Count,
                StartTime = startTime,
                EndTime = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting packages for repository {RepositoryId}", repositoryId);
            return DataSyncResult<List<PackageReference>>.Failed(
                DataSourceType.AzureDevOps, startTime, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<DataSyncResult<CommitHistory>> GetCommitHistoryAsync(string repositoryId, int daysBefore = 365)
    {
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            var (configured, error, baseUrl, auth) = await GetConfigurationAsync();
            if (!configured || baseUrl == null || auth == null)
            {
                return DataSyncResult<CommitHistory>.Failed(
                    DataSourceType.AzureDevOps, startTime, error ?? "Not configured");
            }

            var project = Uri.EscapeDataString(await _secureStorage.GetSecretAsync(SecretKeys.AzureDevOpsProject) ?? "");
            var sinceDate = DateTimeOffset.UtcNow.AddDays(-daysBefore).ToString("o");

            var response = await SendRequestAsync(
                $"{baseUrl}{project}/_apis/git/repositories/{repositoryId}/commits?searchCriteria.fromDate={sinceDate}&api-version=7.1", auth);

            if (!response.IsSuccessStatusCode)
            {
                return DataSyncResult<CommitHistory>.Failed(
                    DataSourceType.AzureDevOps, startTime, $"Failed to get commits: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(content);

            var history = new CommitHistory
            {
                RepositoryId = repositoryId,
                PeriodStart = DateTimeOffset.UtcNow.AddDays(-daysBefore),
                PeriodEnd = DateTimeOffset.UtcNow
            };

            if (jsonDoc.RootElement.TryGetProperty("value", out var commits))
            {
                var commitList = commits.EnumerateArray().ToList();
                history = history with
                {
                    TotalCommits = commitList.Count,
                    LastCommitDate = commitList.FirstOrDefault().TryGetProperty("committer", out var committer)
                        ? DateTimeOffset.Parse(committer.GetProperty("date").GetString() ?? DateTimeOffset.MinValue.ToString())
                        : null,
                    Contributors = commitList
                        .Where(c => c.TryGetProperty("author", out _))
                        .Select(c => c.GetProperty("author").GetProperty("name").GetString() ?? "")
                        .Distinct()
                        .ToList()
                };
            }

            return DataSyncResult<CommitHistory>.Succeeded(DataSourceType.AzureDevOps, history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting commit history for repository {RepositoryId}", repositoryId);
            return DataSyncResult<CommitHistory>.Failed(
                DataSourceType.AzureDevOps, startTime, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<DataSyncResult<ReadmeStatus>> GetReadmeStatusAsync(string repositoryId)
    {
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            var (configured, error, baseUrl, auth) = await GetConfigurationAsync();
            if (!configured || baseUrl == null || auth == null)
            {
                return DataSyncResult<ReadmeStatus>.Failed(
                    DataSourceType.AzureDevOps, startTime, error ?? "Not configured");
            }

            var project = Uri.EscapeDataString(await _secureStorage.GetSecretAsync(SecretKeys.AzureDevOpsProject) ?? "");

            // Try to get README.md
            var readmeResponse = await SendRequestAsync(
                $"{baseUrl}{project}/_apis/git/repositories/{repositoryId}/items?path=/README.md&api-version=7.1", auth);

            var status = new ReadmeStatus
            {
                RepositoryId = repositoryId,
                Exists = readmeResponse.IsSuccessStatusCode
            };

            if (readmeResponse.IsSuccessStatusCode)
            {
                var content = await readmeResponse.Content.ReadAsStringAsync();
                var hasHeadings = content.Contains('#');
                var hasCodeBlocks = content.Contains("```");
                var hasLinks = content.Contains("](");
                var wordCount = content.Split([' ', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries).Length;

                status = status with
                {
                    SizeBytes = content.Length,
                    LineCount = content.Split('\n').Length,
                    QualityScore = CalculateReadmeQuality(hasHeadings, hasCodeBlocks, hasLinks, wordCount)
                };
            }

            return DataSyncResult<ReadmeStatus>.Succeeded(DataSourceType.AzureDevOps, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting README status for repository {RepositoryId}", repositoryId);
            return DataSyncResult<ReadmeStatus>.Failed(
                DataSourceType.AzureDevOps, startTime, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<DataSyncResult<PipelineStatus>> GetPipelineStatusAsync(string repositoryId)
    {
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            var (configured, error, baseUrl, auth) = await GetConfigurationAsync();
            if (!configured || baseUrl == null || auth == null)
            {
                return DataSyncResult<PipelineStatus>.Failed(
                    DataSourceType.AzureDevOps, startTime, error ?? "Not configured");
            }

            var project = Uri.EscapeDataString(await _secureStorage.GetSecretAsync(SecretKeys.AzureDevOpsProject) ?? "");

            // Get builds for this repository
            var response = await SendRequestAsync(
                $"{baseUrl}{project}/_apis/build/builds?repositoryId={repositoryId}&repositoryType=TfsGit&$top=1&api-version=7.1", auth);

            if (!response.IsSuccessStatusCode)
            {
                return DataSyncResult<PipelineStatus>.Failed(
                    DataSourceType.AzureDevOps, startTime, $"Failed to get builds: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(content);

            var status = new PipelineStatus();

            if (jsonDoc.RootElement.TryGetProperty("value", out var builds) &&
                builds.GetArrayLength() > 0)
            {
                var build = builds.EnumerateArray().First();

                status = new PipelineStatus
                {
                    LastBuildId = build.TryGetProperty("id", out var id) ? id.GetInt32().ToString() : null,
                    PipelineName = build.TryGetProperty("definition", out var def)
                        ? def.GetProperty("name").GetString()
                        : null,
                    Status = ParseBuildStatus(build.TryGetProperty("status", out var s) ? s.GetString() : null),
                    Result = ParseBuildResult(build.TryGetProperty("result", out var r) ? r.GetString() : null),
                    StartTime = build.TryGetProperty("startTime", out var start)
                        ? DateTimeOffset.Parse(start.GetString()!)
                        : null,
                    FinishTime = build.TryGetProperty("finishTime", out var finish)
                        ? DateTimeOffset.Parse(finish.GetString()!)
                        : null,
                    RequestedBy = build.TryGetProperty("requestedFor", out var req)
                        ? req.GetProperty("displayName").GetString()
                        : null,
                    SourceBranch = build.TryGetProperty("sourceBranch", out var branch)
                        ? branch.GetString()?.Replace("refs/heads/", "")
                        : null
                };
            }

            return DataSyncResult<PipelineStatus>.Succeeded(DataSourceType.AzureDevOps, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pipeline status for repository {RepositoryId}", repositoryId);
            return DataSyncResult<PipelineStatus>.Failed(
                DataSourceType.AzureDevOps, startTime, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<DataSyncResult<List<SystemDependency>>> DetectSystemDependenciesAsync(string repositoryId)
    {
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            var (configured, error, baseUrl, auth) = await GetConfigurationAsync();
            if (!configured || baseUrl == null || auth == null)
            {
                return DataSyncResult<List<SystemDependency>>.Failed(
                    DataSourceType.AzureDevOps, startTime, error ?? "Not configured");
            }

            var project = Uri.EscapeDataString(await _secureStorage.GetSecretAsync(SecretKeys.AzureDevOpsProject) ?? "");
            var dependencies = new List<SystemDependency>();

            // Get file tree
            var itemsResponse = await SendRequestAsync(
                $"{baseUrl}{project}/_apis/git/repositories/{repositoryId}/items?recursionLevel=Full&api-version=7.1", auth);

            if (!itemsResponse.IsSuccessStatusCode)
            {
                return DataSyncResult<List<SystemDependency>>.Failed(
                    DataSourceType.AzureDevOps, startTime,
                    $"Failed to get repository items: {itemsResponse.StatusCode}");
            }

            var content = await itemsResponse.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(content);

            if (!jsonDoc.RootElement.TryGetProperty("value", out var items))
            {
                return DataSyncResult<List<SystemDependency>>.Succeeded(DataSourceType.AzureDevOps, dependencies);
            }

            var files = items.EnumerateArray()
                .Where(i => i.TryGetProperty("gitObjectType", out var type) && type.GetString() == "blob")
                .Select(i => i.GetProperty("path").GetString() ?? "")
                .ToList();

            // Parse appsettings.json files
            foreach (var appsettings in files.Where(f =>
                f.Contains("appsettings", StringComparison.OrdinalIgnoreCase) &&
                f.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
            {
                var appsettingsContent = await GetFileContentAsync(baseUrl, auth, project, repositoryId, appsettings);
                if (appsettingsContent != null)
                {
                    dependencies.AddRange(ParseConnectionStrings(appsettingsContent, appsettings));
                }
            }

            return new DataSyncResult<List<SystemDependency>>
            {
                Success = true,
                DataSource = DataSourceType.AzureDevOps,
                Data = dependencies,
                RecordsProcessed = dependencies.Count,
                StartTime = startTime,
                EndTime = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting system dependencies for repository {RepositoryId}", repositoryId);
            return DataSyncResult<List<SystemDependency>>.Failed(
                DataSourceType.AzureDevOps, startTime, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<DataSyncResult> SyncRepositoryDataAsync(string applicationId, string repositoryUrl)
    {
        var startTime = DateTimeOffset.UtcNow;

        try
        {
            // Extract repository ID from URL or find by name
            var reposResult = await GetRepositoriesAsync();
            if (!reposResult.Success)
            {
                return DataSyncResult.Failed(DataSourceType.AzureDevOps, startTime,
                    reposResult.ErrorMessage ?? "Failed to get repositories");
            }

            var repo = reposResult.Data?.FirstOrDefault(r =>
                r.Url.Equals(repositoryUrl, StringComparison.OrdinalIgnoreCase) ||
                r.CloneUrl?.Equals(repositoryUrl, StringComparison.OrdinalIgnoreCase) == true);

            if (repo == null)
            {
                return DataSyncResult.Failed(DataSourceType.AzureDevOps, startTime,
                    $"Repository not found: {repositoryUrl}");
            }

            var errors = new List<SyncError>();

            // Sync all data types
            var techStackResult = await DetectTechStackAsync(repo.Id);
            if (!techStackResult.Success)
                errors.Add(new SyncError { Message = techStackResult.ErrorMessage ?? "Tech stack detection failed" });

            var packagesResult = await GetPackagesAsync(repo.Id);
            if (!packagesResult.Success)
                errors.Add(new SyncError { Message = packagesResult.ErrorMessage ?? "Package detection failed" });

            var commitsResult = await GetCommitHistoryAsync(repo.Id);
            if (!commitsResult.Success)
                errors.Add(new SyncError { Message = commitsResult.ErrorMessage ?? "Commit history failed" });

            var readmeResult = await GetReadmeStatusAsync(repo.Id);
            if (!readmeResult.Success)
                errors.Add(new SyncError { Message = readmeResult.ErrorMessage ?? "README check failed" });

            var pipelineResult = await GetPipelineStatusAsync(repo.Id);
            if (!pipelineResult.Success)
                errors.Add(new SyncError { Message = pipelineResult.ErrorMessage ?? "Pipeline check failed" });

            return new DataSyncResult
            {
                Success = errors.Count == 0,
                DataSource = DataSourceType.AzureDevOps,
                RecordsProcessed = 1,
                Errors = errors,
                StartTime = startTime,
                EndTime = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing repository data for application {ApplicationId}", applicationId);
            return DataSyncResult.Failed(DataSourceType.AzureDevOps, startTime, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task<DataSyncResult> SyncAllRepositoriesAsync()
    {
        var startTime = DateTimeOffset.UtcNow;
        var errors = new List<SyncError>();
        var processed = 0;

        try
        {
            var apps = await _mockDataService.GetApplicationsAsync();

            foreach (var app in apps.Where(a => !string.IsNullOrEmpty(a.RepositoryUrl)))
            {
                var result = await SyncRepositoryDataAsync(app.Id, app.RepositoryUrl!);
                if (result.Success)
                {
                    processed++;
                }
                else
                {
                    errors.AddRange(result.Errors);
                }
            }

            return new DataSyncResult
            {
                Success = errors.Count == 0,
                DataSource = DataSourceType.AzureDevOps,
                RecordsProcessed = processed,
                Errors = errors,
                StartTime = startTime,
                EndTime = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing all repositories");
            return DataSyncResult.Failed(DataSourceType.AzureDevOps, startTime, ex.Message);
        }
    }

    #region Private Helpers

    private async Task<(bool Configured, string? Error, string? BaseUrl, AuthenticationHeaderValue? Auth)> GetConfigurationAsync()
    {
        var orgUrl = await _secureStorage.GetSecretAsync(SecretKeys.AzureDevOpsOrganization);
        var pat = await _secureStorage.GetSecretAsync(SecretKeys.AzureDevOpsPat);

        if (string.IsNullOrEmpty(orgUrl))
        {
            return (false, "Azure DevOps organization URL not configured", null, null);
        }

        if (string.IsNullOrEmpty(pat))
        {
            return (false, "Azure DevOps PAT not configured", null, null);
        }

        var username = await _secureStorage.GetSecretAsync(SecretKeys.AzureDevOpsUsername);
        if (string.IsNullOrEmpty(username))
        {
            return (false, "Azure DevOps username not configured", null, null);
        }

        var baseUrl = orgUrl.TrimEnd('/') + "/";
        // Azure DevOps PAT auth: username:PAT format
        var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{username}:{pat}"));
        var auth = new AuthenticationHeaderValue("Basic", credentials);

        return (true, null, baseUrl, auth);
    }

    private async Task<HttpResponseMessage> SendRequestAsync(string url, AuthenticationHeaderValue auth)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = auth;
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        return await _httpClient.SendAsync(request);
    }

    private async Task<string?> GetFileContentAsync(string baseUrl, AuthenticationHeaderValue auth, string project, string repositoryId, string path)
    {
        try
        {
            var response = await SendRequestAsync(
                $"{baseUrl}{project}/_apis/git/repositories/{repositoryId}/items?path={Uri.EscapeDataString(path)}&api-version=7.1",
                auth);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get file content: {Path}", path);
        }

        return null;
    }

    private static async Task<TechStackDetectionResult> ParseCsprojAsync(TechStackDetectionResult result, string content)
    {
        await Task.CompletedTask;

        var frameworks = new List<string>(result.Frameworks);
        string? targetFramework = null;

        var tfMatch = TargetFrameworkRegex().Match(content);
        if (tfMatch.Success)
        {
            targetFramework = tfMatch.Groups[1].Value;

            if (targetFramework.StartsWith("net8") || targetFramework.StartsWith("net7") ||
                targetFramework.StartsWith("net6") || targetFramework.StartsWith("net5"))
            {
                frameworks.Add(".NET Core");
            }
            else if (targetFramework.StartsWith("net4"))
            {
                frameworks.Add(".NET Framework");
            }
        }

        if (content.Contains("Microsoft.AspNetCore.Components", StringComparison.OrdinalIgnoreCase))
            frameworks.Add("Blazor");
        if (content.Contains("Microsoft.AspNetCore", StringComparison.OrdinalIgnoreCase))
            frameworks.Add("ASP.NET Core");

        return result with
        {
            TargetFramework = targetFramework,
            Frameworks = frameworks.Distinct().ToList()
        };
    }

    private static TechStackDetectionResult ParsePackageJson(TechStackDetectionResult result, string content)
    {
        try
        {
            var doc = JsonDocument.Parse(content);
            var frameworks = new List<string>(result.Frameworks);

            var deps = new List<string>();
            if (doc.RootElement.TryGetProperty("dependencies", out var dependencies))
                deps.AddRange(dependencies.EnumerateObject().Select(p => p.Name));
            if (doc.RootElement.TryGetProperty("devDependencies", out var devDeps))
                deps.AddRange(devDeps.EnumerateObject().Select(p => p.Name));

            if (deps.Any(d => d.Contains("aurelia", StringComparison.OrdinalIgnoreCase)))
                frameworks.Add("Aurelia");
            if (deps.Any(d => d.Equals("react", StringComparison.OrdinalIgnoreCase)))
                frameworks.Add("React");
            if (deps.Any(d => d.Equals("vue", StringComparison.OrdinalIgnoreCase)))
                frameworks.Add("Vue");
            if (deps.Any(d => d.Contains("angular", StringComparison.OrdinalIgnoreCase)))
                frameworks.Add("Angular");

            return result with { Frameworks = frameworks.Distinct().ToList() };
        }
        catch
        {
            return result;
        }
    }

    private static PrimaryStack DeterminePrimaryStack(TechStackDetectionResult result)
    {
        if (result.Frameworks.Contains("Blazor")) return PrimaryStack.Blazor;
        if (result.Frameworks.Contains(".NET Core") || result.Frameworks.Contains("ASP.NET Core")) return PrimaryStack.DotNetCore;
        if (result.Frameworks.Contains(".NET Framework")) return PrimaryStack.DotNetFramework;
        if (result.Languages.Contains("Python")) return PrimaryStack.Python;
        if (result.Languages.Contains("Java")) return PrimaryStack.Java;
        if (result.Languages.Contains("JavaScript") || result.Languages.Contains("TypeScript")) return PrimaryStack.NodeJs;
        if (result.Frameworks.Count > 2) return PrimaryStack.Mixed;
        return PrimaryStack.Unknown;
    }

    private static string GeneratePatternDescription(TechStackDetectionResult result)
    {
        var parts = new List<string>();
        if (result.Frameworks.Contains(".NET Core") || result.Frameworks.Contains(".NET Framework")) parts.Add("dotnet");
        if (result.Frameworks.Contains("Blazor")) parts.Add("blazor");
        if (result.Frameworks.Contains("Aurelia")) parts.Add("aurelia");
        if (result.Frameworks.Contains("React")) parts.Add("react");
        if (result.Frameworks.Contains("Angular")) parts.Add("angular");
        if (result.Frameworks.Contains("Vue")) parts.Add("vue");
        return parts.Count != 0 ? string.Join("+", parts) : "unknown";
    }

    private static List<PackageReference> ParseNuGetPackages(string content, string sourceFile)
    {
        var packages = new List<PackageReference>();
        var matches = PackageReferenceRegex().Matches(content);

        foreach (Match match in matches)
        {
            packages.Add(new PackageReference
            {
                Name = match.Groups[1].Value,
                Version = match.Groups[2].Value,
                PackageManager = "NuGet",
                SourceFile = sourceFile
            });
        }

        return packages;
    }

    private static List<PackageReference> ParseNpmPackages(string content, string sourceFile)
    {
        var packages = new List<PackageReference>();

        try
        {
            var doc = JsonDocument.Parse(content);

            void AddPackages(string propertyName, bool isDev)
            {
                if (doc.RootElement.TryGetProperty(propertyName, out var deps))
                {
                    foreach (var dep in deps.EnumerateObject())
                    {
                        packages.Add(new PackageReference
                        {
                            Name = dep.Name,
                            Version = dep.Value.GetString() ?? "",
                            PackageManager = "npm",
                            SourceFile = sourceFile,
                            IsDevelopmentDependency = isDev
                        });
                    }
                }
            }

            AddPackages("dependencies", false);
            AddPackages("devDependencies", true);
        }
        catch { }

        return packages;
    }

    private static int CalculateReadmeQuality(bool hasHeadings, bool hasCodeBlocks, bool hasLinks, int wordCount)
    {
        var score = 0;
        if (hasHeadings) score += 20;
        if (hasCodeBlocks) score += 20;
        if (hasLinks) score += 20;
        score += wordCount switch { < 50 => 10, < 200 => 20, < 500 => 30, _ => 40 };
        return Math.Min(score, 100);
    }

    private static BuildStatus ParseBuildStatus(string? status) => status?.ToLowerInvariant() switch
    {
        "notstarted" => BuildStatus.NotStarted,
        "inprogress" => BuildStatus.InProgress,
        "completed" => BuildStatus.Completed,
        "cancelling" => BuildStatus.Cancelling,
        "postponed" => BuildStatus.Postponed,
        _ => BuildStatus.Unknown
    };

    private static BuildResult ParseBuildResult(string? result) => result?.ToLowerInvariant() switch
    {
        "succeeded" => BuildResult.Succeeded,
        "partiallysucceeded" => BuildResult.PartiallySucceeded,
        "failed" => BuildResult.Failed,
        "cancelled" or "canceled" => BuildResult.Cancelled,
        _ => BuildResult.Unknown
    };

    private static List<SystemDependency> ParseConnectionStrings(string content, string sourceFile)
    {
        var dependencies = new List<SystemDependency>();

        try
        {
            var doc = JsonDocument.Parse(content);

            if (doc.RootElement.TryGetProperty("ConnectionStrings", out var connStrings))
            {
                foreach (var conn in connStrings.EnumerateObject())
                {
                    var connString = conn.Value.GetString() ?? "";
                    dependencies.Add(new SystemDependency
                    {
                        Name = conn.Name,
                        Type = DetectConnectionType(connString),
                        ConnectionString = MaskConnectionString(connString),
                        SourceFile = sourceFile
                    });
                }
            }
        }
        catch { }

        return dependencies;
    }

    private static string DetectConnectionType(string connectionString)
    {
        var lower = connectionString.ToLowerInvariant();
        if (lower.Contains("server=") || lower.Contains("data source=")) return "SQL Server";
        if (lower.Contains("mongodb://")) return "MongoDB";
        if (lower.Contains("redis")) return "Redis";
        if (lower.Contains("rabbitmq://") || lower.Contains("amqp://")) return "RabbitMQ";
        if (lower.Contains("servicebus")) return "Azure Service Bus";
        if (lower.Contains("blob.core.windows.net")) return "Azure Blob Storage";
        return "Unknown";
    }

    private static string MaskConnectionString(string connectionString)
    {
        var masked = PasswordRegex().Replace(connectionString, "$1=***;");
        masked = AccountKeyRegex().Replace(masked, "$1=***;");
        return masked;
    }

    [GeneratedRegex(@"<TargetFramework>(.*?)</TargetFramework>", RegexOptions.IgnoreCase)]
    private static partial Regex TargetFrameworkRegex();

    [GeneratedRegex(@"<PackageReference\s+Include=""([^""]+)""\s+Version=""([^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex PackageReferenceRegex();

    [GeneratedRegex(@"(password|pwd)=[^;]+;", RegexOptions.IgnoreCase)]
    private static partial Regex PasswordRegex();

    [GeneratedRegex(@"(AccountKey)=[^;]+;", RegexOptions.IgnoreCase)]
    private static partial Regex AccountKeyRegex();

    #endregion
}
