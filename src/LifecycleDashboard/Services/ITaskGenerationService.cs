namespace LifecycleDashboard.Services;

using LifecycleDashboard.Models;

/// <summary>
/// Service for automatically generating lifecycle tasks based on application state,
/// role assignments, security findings, and review schedules.
/// </summary>
public interface ITaskGenerationService
{
    /// <summary>
    /// Runs the full task generation process for all applications.
    /// Should be called periodically (e.g., daily or after data sync).
    /// </summary>
    Task<TaskGenerationResult> GenerateTasksAsync();

    /// <summary>
    /// Generates tasks for a specific application.
    /// </summary>
    Task<TaskGenerationResult> GenerateTasksForApplicationAsync(string applicationId);

    /// <summary>
    /// Generates role revalidation tasks for roles that haven't been validated
    /// within the configured period (default 180 days).
    /// </summary>
    Task<TaskGenerationResult> GenerateRoleRevalidationTasksAsync();

    /// <summary>
    /// Generates documentation review tasks for applications where documentation
    /// hasn't been reviewed within the past year.
    /// </summary>
    Task<TaskGenerationResult> GenerateDocumentationReviewTasksAsync();

    /// <summary>
    /// Generates application information review tasks for applications
    /// that haven't had their info verified within the past year.
    /// </summary>
    Task<TaskGenerationResult> GenerateAppInfoReviewTasksAsync();

    /// <summary>
    /// Generates security remediation tasks for applications with unresolved
    /// vulnerabilities. Tasks are assigned to the Technical Architect.
    /// </summary>
    Task<TaskGenerationResult> GenerateSecurityRemediationTasksAsync();

    /// <summary>
    /// Gets the current task generation configuration.
    /// </summary>
    Task<TaskGenerationConfig> GetConfigurationAsync();

    /// <summary>
    /// Updates the task generation configuration.
    /// </summary>
    Task UpdateConfigurationAsync(TaskGenerationConfig config);
}

/// <summary>
/// Result of a task generation run.
/// </summary>
public record TaskGenerationResult
{
    /// <summary>Number of new tasks created.</summary>
    public int TasksCreated { get; init; }

    /// <summary>Number of tasks skipped (already exist or conditions not met).</summary>
    public int TasksSkipped { get; init; }

    /// <summary>Number of applications processed.</summary>
    public int ApplicationsProcessed { get; init; }

    /// <summary>Any errors encountered during generation.</summary>
    public List<string> Errors { get; init; } = [];

    /// <summary>Details about tasks created, grouped by type.</summary>
    public Dictionary<TaskType, int> TasksByType { get; init; } = [];

    /// <summary>When this generation run completed.</summary>
    public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Configuration for automatic task generation.
/// </summary>
public record TaskGenerationConfig
{
    /// <summary>
    /// Days between role revalidation tasks. Default is 180 (6 months).
    /// </summary>
    public int RoleRevalidationDays { get; init; } = 180;

    /// <summary>
    /// Days between documentation reviews. Default is 365 (annual).
    /// </summary>
    public int DocumentationReviewDays { get; init; } = 365;

    /// <summary>
    /// Days between application information reviews. Default is 365 (annual).
    /// </summary>
    public int AppInfoReviewDays { get; init; } = 365;

    /// <summary>
    /// Due date offset for Critical severity vulnerabilities (days from detection).
    /// </summary>
    public int CriticalVulnerabilityDueDays { get; init; } = 30;

    /// <summary>
    /// Due date offset for High severity vulnerabilities (days from detection).
    /// </summary>
    public int HighVulnerabilityDueDays { get; init; } = 60;

    /// <summary>
    /// Due date offset for Medium severity vulnerabilities (days from detection).
    /// </summary>
    public int MediumVulnerabilityDueDays { get; init; } = 90;

    /// <summary>
    /// Whether exposed secrets should be treated as Critical severity.
    /// </summary>
    public bool TreatExposedSecretsAsCritical { get; init; } = true;

    /// <summary>
    /// Whether task generation is enabled.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// When this configuration was last updated.
    /// </summary>
    public DateTimeOffset LastUpdated { get; init; } = DateTimeOffset.UtcNow;
}
