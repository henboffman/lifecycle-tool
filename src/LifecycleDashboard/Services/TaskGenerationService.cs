namespace LifecycleDashboard.Services;

using System.Text.Json;
using LifecycleDashboard.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for automatically generating lifecycle tasks based on application state,
/// role assignments, security findings, and review schedules.
/// </summary>
public class TaskGenerationService : ITaskGenerationService
{
    private readonly IMockDataService _dataService;
    private readonly ILogger<TaskGenerationService> _logger;
    private static TaskGenerationConfig _config = new();
    private static readonly object _configLock = new();

    public TaskGenerationService(
        IMockDataService dataService,
        ILogger<TaskGenerationService> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    public async Task<TaskGenerationResult> GenerateTasksAsync()
    {
        var config = await GetConfigurationAsync();
        if (!config.IsEnabled)
        {
            return new TaskGenerationResult
            {
                TasksSkipped = 0,
                TasksCreated = 0,
                Errors = ["Task generation is disabled"]
            };
        }

        _logger.LogInformation("Starting task generation run...");

        var results = new List<TaskGenerationResult>();

        // Run all task generation types
        results.Add(await GenerateRoleRevalidationTasksAsync());
        results.Add(await GenerateDocumentationReviewTasksAsync());
        results.Add(await GenerateAppInfoReviewTasksAsync());
        results.Add(await GenerateSecurityRemediationTasksAsync());

        // Aggregate results
        var aggregated = new TaskGenerationResult
        {
            TasksCreated = results.Sum(r => r.TasksCreated),
            TasksSkipped = results.Sum(r => r.TasksSkipped),
            ApplicationsProcessed = results.Max(r => r.ApplicationsProcessed),
            Errors = results.SelectMany(r => r.Errors).ToList(),
            TasksByType = results
                .SelectMany(r => r.TasksByType)
                .GroupBy(kvp => kvp.Key)
                .ToDictionary(g => g.Key, g => g.Sum(kvp => kvp.Value))
        };

        _logger.LogInformation(
            "Task generation complete: {Created} created, {Skipped} skipped, {Errors} errors",
            aggregated.TasksCreated, aggregated.TasksSkipped, aggregated.Errors.Count);

        return aggregated;
    }

    public async Task<TaskGenerationResult> GenerateTasksForApplicationAsync(string applicationId)
    {
        var app = await _dataService.GetApplicationAsync(applicationId);
        if (app == null)
        {
            return new TaskGenerationResult
            {
                Errors = [$"Application {applicationId} not found"]
            };
        }

        var results = new List<TaskGenerationResult>();

        results.Add(await GenerateRoleRevalidationTasksForAppAsync(app));
        results.Add(await GenerateDocumentationReviewTaskForAppAsync(app));
        results.Add(await GenerateAppInfoReviewTaskForAppAsync(app));
        results.Add(await GenerateSecurityRemediationTasksForAppAsync(app));

        return new TaskGenerationResult
        {
            TasksCreated = results.Sum(r => r.TasksCreated),
            TasksSkipped = results.Sum(r => r.TasksSkipped),
            ApplicationsProcessed = 1,
            Errors = results.SelectMany(r => r.Errors).ToList(),
            TasksByType = results
                .SelectMany(r => r.TasksByType)
                .GroupBy(kvp => kvp.Key)
                .ToDictionary(g => g.Key, g => g.Sum(kvp => kvp.Value))
        };
    }

    public async Task<TaskGenerationResult> GenerateRoleRevalidationTasksAsync()
    {
        var config = await GetConfigurationAsync();
        var applications = await _dataService.GetApplicationsAsync();
        var tasksCreated = 0;
        var tasksSkipped = 0;
        var errors = new List<string>();

        foreach (var app in applications)
        {
            try
            {
                var result = await GenerateRoleRevalidationTasksForAppAsync(app);
                tasksCreated += result.TasksCreated;
                tasksSkipped += result.TasksSkipped;
                errors.AddRange(result.Errors);
            }
            catch (Exception ex)
            {
                errors.Add($"Error processing {app.Name}: {ex.Message}");
                _logger.LogError(ex, "Error generating role revalidation tasks for {AppName}", app.Name);
            }
        }

        return new TaskGenerationResult
        {
            TasksCreated = tasksCreated,
            TasksSkipped = tasksSkipped,
            ApplicationsProcessed = applications.Count,
            Errors = errors,
            TasksByType = tasksCreated > 0
                ? new Dictionary<TaskType, int> { [TaskType.RoleValidation] = tasksCreated }
                : []
        };
    }

    private async Task<TaskGenerationResult> GenerateRoleRevalidationTasksForAppAsync(Application app)
    {
        var config = await GetConfigurationAsync();
        var existingTasks = await _dataService.GetTasksForApplicationAsync(app.Id);
        var tasksCreated = 0;
        var tasksSkipped = 0;

        var revalidationThreshold = DateTimeOffset.UtcNow.AddDays(-config.RoleRevalidationDays);

        foreach (var role in app.RoleAssignments)
        {
            // Check if role needs revalidation
            var lastValidated = role.LastValidatedDate ?? role.AssignedDate;
            if (lastValidated > revalidationThreshold && !role.NeedsRevalidation)
            {
                tasksSkipped++;
                continue;
            }

            // Check if a pending/in-progress task already exists for this role
            var existingTask = existingTasks.FirstOrDefault(t =>
                t.Type == TaskType.RoleValidation &&
                t.Status != TaskStatus.Completed &&
                t.Status != TaskStatus.Cancelled &&
                t.AssigneeId == role.UserId);

            if (existingTask != null)
            {
                tasksSkipped++;
                continue;
            }

            // Create the revalidation task
            var task = new LifecycleTask
            {
                Id = Guid.NewGuid().ToString(),
                Title = $"Revalidate {role.Role} role for {app.Name}",
                Description = $"Please verify that {role.UserName} should continue as {role.Role} for {app.Name}.\n\n" +
                              $"**Why you're receiving this task:** You are currently assigned as {role.Role} and role assignments must be revalidated every {config.RoleRevalidationDays} days.\n\n" +
                              $"Last validated: {(role.LastValidatedDate?.ToString("MMM d, yyyy") ?? "Never")}\n" +
                              $"Assigned: {role.AssignedDate:MMM d, yyyy}",
                Type = TaskType.RoleValidation,
                Priority = TaskPriority.Medium,
                Status = TaskStatus.Pending,
                ApplicationId = app.Id,
                ApplicationName = app.Name,
                AssigneeId = role.UserId,
                AssigneeName = role.UserName,
                DueDate = DateTimeOffset.UtcNow.AddDays(30),
                CreatedDate = DateTimeOffset.UtcNow
            };

            await _dataService.CreateTaskAsync(task);
            tasksCreated++;

            _logger.LogInformation(
                "Created role revalidation task for {UserName} ({Role}) on {AppName}",
                role.UserName, role.Role, app.Name);
        }

        return new TaskGenerationResult
        {
            TasksCreated = tasksCreated,
            TasksSkipped = tasksSkipped,
            ApplicationsProcessed = 1,
            TasksByType = tasksCreated > 0
                ? new Dictionary<TaskType, int> { [TaskType.RoleValidation] = tasksCreated }
                : []
        };
    }

    public async Task<TaskGenerationResult> GenerateDocumentationReviewTasksAsync()
    {
        var applications = await _dataService.GetApplicationsAsync();
        var tasksCreated = 0;
        var tasksSkipped = 0;
        var errors = new List<string>();

        foreach (var app in applications)
        {
            try
            {
                var result = await GenerateDocumentationReviewTaskForAppAsync(app);
                tasksCreated += result.TasksCreated;
                tasksSkipped += result.TasksSkipped;
                errors.AddRange(result.Errors);
            }
            catch (Exception ex)
            {
                errors.Add($"Error processing {app.Name}: {ex.Message}");
                _logger.LogError(ex, "Error generating documentation review task for {AppName}", app.Name);
            }
        }

        return new TaskGenerationResult
        {
            TasksCreated = tasksCreated,
            TasksSkipped = tasksSkipped,
            ApplicationsProcessed = applications.Count,
            Errors = errors,
            TasksByType = tasksCreated > 0
                ? new Dictionary<TaskType, int> { [TaskType.DocumentationReview] = tasksCreated }
                : []
        };
    }

    private async Task<TaskGenerationResult> GenerateDocumentationReviewTaskForAppAsync(Application app)
    {
        var config = await GetConfigurationAsync();
        var existingTasks = await _dataService.GetTasksForApplicationAsync(app.Id);

        var reviewThreshold = DateTimeOffset.UtcNow.AddDays(-config.DocumentationReviewDays);

        // Use SecurityReview.NextReviewDate or LastSyncDate as proxy for last documentation review
        var lastReview = app.SecurityReview.CompletedDate
                         ?? app.LastSyncDate.AddDays(-config.DocumentationReviewDays - 1); // Force review for never-reviewed

        // If documentation was reviewed recently, skip
        if (lastReview > reviewThreshold)
        {
            return new TaskGenerationResult { TasksSkipped = 1, ApplicationsProcessed = 1 };
        }

        // Check for existing pending task
        var existingTask = existingTasks.FirstOrDefault(t =>
            t.Type == TaskType.DocumentationReview &&
            t.Status != TaskStatus.Completed &&
            t.Status != TaskStatus.Cancelled);

        if (existingTask != null)
        {
            return new TaskGenerationResult { TasksSkipped = 1, ApplicationsProcessed = 1 };
        }

        // Find the owner or functional architect to assign the task
        var assignee = app.RoleAssignments.FirstOrDefault(r => r.Role == ApplicationRole.Owner)
                       ?? app.RoleAssignments.FirstOrDefault(r => r.Role == ApplicationRole.FunctionalArchitect)
                       ?? app.RoleAssignments.FirstOrDefault(r => r.Role == ApplicationRole.ProductManager)
                       ?? app.RoleAssignments.FirstOrDefault();

        if (assignee == null)
        {
            _logger.LogWarning("No assignee found for documentation review task on {AppName}", app.Name);
            return new TaskGenerationResult
            {
                TasksSkipped = 1,
                ApplicationsProcessed = 1,
                Errors = [$"No assignee found for {app.Name}"]
            };
        }

        var docStatus = GetDocumentationStatus(app.Documentation);

        var task = new LifecycleTask
        {
            Id = Guid.NewGuid().ToString(),
            Title = $"Annual documentation review for {app.Name}",
            Description = $"Please review and update the documentation for {app.Name}.\n\n" +
                          $"**Why you're receiving this task:** You are the {assignee.Role} for this application, and documentation must be reviewed annually.\n\n" +
                          $"**Current documentation status:**\n{docStatus}\n\n" +
                          $"Please verify that all documentation is current and complete, updating any outdated information.",
            Type = TaskType.DocumentationReview,
            Priority = app.Documentation.CompletenessScore < 50 ? TaskPriority.High : TaskPriority.Medium,
            Status = TaskStatus.Pending,
            ApplicationId = app.Id,
            ApplicationName = app.Name,
            AssigneeId = assignee.UserId,
            AssigneeName = assignee.UserName,
            DueDate = DateTimeOffset.UtcNow.AddDays(30),
            CreatedDate = DateTimeOffset.UtcNow
        };

        await _dataService.CreateTaskAsync(task);

        _logger.LogInformation(
            "Created documentation review task for {AppName}, assigned to {UserName}",
            app.Name, assignee.UserName);

        return new TaskGenerationResult
        {
            TasksCreated = 1,
            ApplicationsProcessed = 1,
            TasksByType = new Dictionary<TaskType, int> { [TaskType.DocumentationReview] = 1 }
        };
    }

    public async Task<TaskGenerationResult> GenerateAppInfoReviewTasksAsync()
    {
        var applications = await _dataService.GetApplicationsAsync();
        var tasksCreated = 0;
        var tasksSkipped = 0;
        var errors = new List<string>();

        foreach (var app in applications)
        {
            try
            {
                var result = await GenerateAppInfoReviewTaskForAppAsync(app);
                tasksCreated += result.TasksCreated;
                tasksSkipped += result.TasksSkipped;
                errors.AddRange(result.Errors);
            }
            catch (Exception ex)
            {
                errors.Add($"Error processing {app.Name}: {ex.Message}");
                _logger.LogError(ex, "Error generating app info review task for {AppName}", app.Name);
            }
        }

        return new TaskGenerationResult
        {
            TasksCreated = tasksCreated,
            TasksSkipped = tasksSkipped,
            ApplicationsProcessed = applications.Count,
            Errors = errors,
            TasksByType = tasksCreated > 0
                ? new Dictionary<TaskType, int> { [TaskType.ArchitectureReview] = tasksCreated }
                : []
        };
    }

    private async Task<TaskGenerationResult> GenerateAppInfoReviewTaskForAppAsync(Application app)
    {
        var config = await GetConfigurationAsync();
        var existingTasks = await _dataService.GetTasksForApplicationAsync(app.Id);

        var reviewThreshold = DateTimeOffset.UtcNow.AddDays(-config.AppInfoReviewDays);

        // Use LastActivityDate or LastSyncDate as proxy for "last review" if no explicit field
        // In a real implementation, we'd track LastAppInfoReviewDate on the Application
        var lastReview = app.LastActivityDate ?? app.LastSyncDate;

        // Check for KeyDates that might indicate a recent review (e.g., Audit type or description contains "review")
        var lastInfoReview = app.KeyDates
            .Where(kd => kd.Type == KeyDateType.Audit || kd.Description?.Contains("review", StringComparison.OrdinalIgnoreCase) == true)
            .OrderByDescending(kd => kd.Date)
            .FirstOrDefault()?.Date;

        if (lastInfoReview.HasValue && lastInfoReview.Value > reviewThreshold)
        {
            return new TaskGenerationResult { TasksSkipped = 1, ApplicationsProcessed = 1 };
        }

        // If synced within the year, don't require review yet (assume data is fresh)
        if (app.LastSyncDate > reviewThreshold)
        {
            return new TaskGenerationResult { TasksSkipped = 1, ApplicationsProcessed = 1 };
        }

        // Check for existing pending task
        var existingTask = existingTasks.FirstOrDefault(t =>
            t.Type == TaskType.ArchitectureReview &&
            t.Status != TaskStatus.Completed &&
            t.Status != TaskStatus.Cancelled);

        if (existingTask != null)
        {
            return new TaskGenerationResult { TasksSkipped = 1, ApplicationsProcessed = 1 };
        }

        // Find the owner or business owner to assign the task
        var assignee = app.RoleAssignments.FirstOrDefault(r => r.Role == ApplicationRole.Owner)
                       ?? app.RoleAssignments.FirstOrDefault(r => r.Role == ApplicationRole.BusinessOwner)
                       ?? app.RoleAssignments.FirstOrDefault(r => r.Role == ApplicationRole.ProductManager)
                       ?? app.RoleAssignments.FirstOrDefault();

        if (assignee == null)
        {
            _logger.LogWarning("No assignee found for app info review task on {AppName}", app.Name);
            return new TaskGenerationResult
            {
                TasksSkipped = 1,
                ApplicationsProcessed = 1,
                Errors = [$"No assignee found for {app.Name}"]
            };
        }

        var task = new LifecycleTask
        {
            Id = Guid.NewGuid().ToString(),
            Title = $"Annual application information review for {app.Name}",
            Description = $"Please review and verify the application information for {app.Name}.\n\n" +
                          $"**Why you're receiving this task:** You are the {assignee.Role} for this application, and application metadata must be verified annually.\n\n" +
                          $"**Please verify:**\n" +
                          $"- Application description and purpose are accurate\n" +
                          $"- Capability/business area assignment is correct\n" +
                          $"- Application type and architecture classification are accurate\n" +
                          $"- All role assignments are current\n" +
                          $"- Key dates and milestones are up to date\n\n" +
                          $"Last synced: {app.LastSyncDate:MMM d, yyyy}",
            Type = TaskType.ArchitectureReview, // Using ArchitectureReview for app info review
            Priority = TaskPriority.Low,
            Status = TaskStatus.Pending,
            ApplicationId = app.Id,
            ApplicationName = app.Name,
            AssigneeId = assignee.UserId,
            AssigneeName = assignee.UserName,
            DueDate = DateTimeOffset.UtcNow.AddDays(60),
            CreatedDate = DateTimeOffset.UtcNow
        };

        await _dataService.CreateTaskAsync(task);

        _logger.LogInformation(
            "Created app info review task for {AppName}, assigned to {UserName}",
            app.Name, assignee.UserName);

        return new TaskGenerationResult
        {
            TasksCreated = 1,
            ApplicationsProcessed = 1,
            TasksByType = new Dictionary<TaskType, int> { [TaskType.ArchitectureReview] = 1 }
        };
    }

    public async Task<TaskGenerationResult> GenerateSecurityRemediationTasksAsync()
    {
        var applications = await _dataService.GetApplicationsAsync();
        var tasksCreated = 0;
        var tasksSkipped = 0;
        var errors = new List<string>();

        foreach (var app in applications)
        {
            try
            {
                var result = await GenerateSecurityRemediationTasksForAppAsync(app);
                tasksCreated += result.TasksCreated;
                tasksSkipped += result.TasksSkipped;
                errors.AddRange(result.Errors);
            }
            catch (Exception ex)
            {
                errors.Add($"Error processing {app.Name}: {ex.Message}");
                _logger.LogError(ex, "Error generating security remediation tasks for {AppName}", app.Name);
            }
        }

        return new TaskGenerationResult
        {
            TasksCreated = tasksCreated,
            TasksSkipped = tasksSkipped,
            ApplicationsProcessed = applications.Count,
            Errors = errors,
            TasksByType = tasksCreated > 0
                ? new Dictionary<TaskType, int> { [TaskType.SecurityRemediation] = tasksCreated }
                : []
        };
    }

    private async Task<TaskGenerationResult> GenerateSecurityRemediationTasksForAppAsync(Application app)
    {
        var config = await GetConfigurationAsync();
        var existingTasks = await _dataService.GetTasksForApplicationAsync(app.Id);
        var tasksCreated = 0;
        var tasksSkipped = 0;
        var errors = new List<string>();

        // Find the Technical Architect for this application
        var techArchitect = app.RoleAssignments.FirstOrDefault(r => r.Role == ApplicationRole.TechnicalArchitect)
                           ?? app.RoleAssignments.FirstOrDefault(r => r.Role == ApplicationRole.TechnicalLead)
                           ?? app.RoleAssignments.FirstOrDefault(r => r.Role == ApplicationRole.SecurityChampion)
                           ?? app.RoleAssignments.FirstOrDefault(r => r.Role == ApplicationRole.Developer);

        if (techArchitect == null)
        {
            // Can't create security tasks without a technical assignee
            if (app.SecurityFindings.Any(f => !f.IsResolved))
            {
                errors.Add($"No technical role found for {app.Name} - cannot assign security tasks");
            }
            return new TaskGenerationResult
            {
                TasksSkipped = app.SecurityFindings.Count(f => !f.IsResolved),
                ApplicationsProcessed = 1,
                Errors = errors
            };
        }

        // Get unresolved security findings
        var unresolvedFindings = app.SecurityFindings.Where(f => !f.IsResolved).ToList();

        // Group findings by severity for task creation
        var criticalFindings = unresolvedFindings.Where(f => f.Severity == SecuritySeverity.Critical).ToList();
        var highFindings = unresolvedFindings.Where(f => f.Severity == SecuritySeverity.High).ToList();
        var mediumFindings = unresolvedFindings.Where(f => f.Severity == SecuritySeverity.Medium).ToList();

        // Check if we need to also check for exposed secrets (treated as Critical)
        // This would come from the linked repository if available
        var linkedRepos = await _dataService.GetSyncedRepositoriesAsync();
        var appRepo = linkedRepos.FirstOrDefault(r =>
            r.Name.Equals(app.Name, StringComparison.OrdinalIgnoreCase) ||
            app.RepositoryUrl?.Contains(r.Name, StringComparison.OrdinalIgnoreCase) == true);

        var hasExposedSecrets = appRepo?.ExposedSecretsCount > 0;
        var exposedSecretsCount = appRepo?.ExposedSecretsCount ?? 0;

        // Create task for Critical vulnerabilities (including exposed secrets)
        if (criticalFindings.Count > 0 || (config.TreatExposedSecretsAsCritical && hasExposedSecrets))
        {
            var existingCriticalTask = existingTasks.FirstOrDefault(t =>
                t.Type == TaskType.SecurityRemediation &&
                t.Priority == TaskPriority.Critical &&
                t.Status != TaskStatus.Completed &&
                t.Status != TaskStatus.Cancelled);

            if (existingCriticalTask == null)
            {
                var description = BuildSecurityTaskDescription(
                    app, techArchitect, criticalFindings, "Critical",
                    config.CriticalVulnerabilityDueDays, exposedSecretsCount);

                var task = new LifecycleTask
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = $"URGENT: Remediate critical security vulnerabilities in {app.Name}",
                    Description = description,
                    Type = TaskType.SecurityRemediation,
                    Priority = TaskPriority.Critical,
                    Status = TaskStatus.Pending,
                    ApplicationId = app.Id,
                    ApplicationName = app.Name,
                    AssigneeId = techArchitect.UserId,
                    AssigneeName = techArchitect.UserName,
                    DueDate = DateTimeOffset.UtcNow.AddDays(config.CriticalVulnerabilityDueDays),
                    CreatedDate = DateTimeOffset.UtcNow
                };

                await _dataService.CreateTaskAsync(task);
                tasksCreated++;

                _logger.LogInformation(
                    "Created CRITICAL security remediation task for {AppName} ({Count} findings), assigned to {UserName}",
                    app.Name, criticalFindings.Count + (hasExposedSecrets ? 1 : 0), techArchitect.UserName);
            }
            else
            {
                tasksSkipped++;
            }
        }

        // Create task for High severity vulnerabilities
        if (highFindings.Count > 0)
        {
            var existingHighTask = existingTasks.FirstOrDefault(t =>
                t.Type == TaskType.SecurityRemediation &&
                t.Priority == TaskPriority.High &&
                t.Status != TaskStatus.Completed &&
                t.Status != TaskStatus.Cancelled);

            if (existingHighTask == null)
            {
                var description = BuildSecurityTaskDescription(
                    app, techArchitect, highFindings, "High",
                    config.HighVulnerabilityDueDays, 0);

                var task = new LifecycleTask
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = $"Remediate high severity vulnerabilities in {app.Name}",
                    Description = description,
                    Type = TaskType.SecurityRemediation,
                    Priority = TaskPriority.High,
                    Status = TaskStatus.Pending,
                    ApplicationId = app.Id,
                    ApplicationName = app.Name,
                    AssigneeId = techArchitect.UserId,
                    AssigneeName = techArchitect.UserName,
                    DueDate = DateTimeOffset.UtcNow.AddDays(config.HighVulnerabilityDueDays),
                    CreatedDate = DateTimeOffset.UtcNow
                };

                await _dataService.CreateTaskAsync(task);
                tasksCreated++;

                _logger.LogInformation(
                    "Created HIGH security remediation task for {AppName} ({Count} findings), assigned to {UserName}",
                    app.Name, highFindings.Count, techArchitect.UserName);
            }
            else
            {
                tasksSkipped++;
            }
        }

        // Create task for Medium severity vulnerabilities
        if (mediumFindings.Count > 0)
        {
            var existingMediumTask = existingTasks.FirstOrDefault(t =>
                t.Type == TaskType.SecurityRemediation &&
                t.Priority == TaskPriority.Medium &&
                t.Status != TaskStatus.Completed &&
                t.Status != TaskStatus.Cancelled);

            if (existingMediumTask == null)
            {
                var description = BuildSecurityTaskDescription(
                    app, techArchitect, mediumFindings, "Medium",
                    config.MediumVulnerabilityDueDays, 0);

                var task = new LifecycleTask
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = $"Address medium severity vulnerabilities in {app.Name}",
                    Description = description,
                    Type = TaskType.SecurityRemediation,
                    Priority = TaskPriority.Medium,
                    Status = TaskStatus.Pending,
                    ApplicationId = app.Id,
                    ApplicationName = app.Name,
                    AssigneeId = techArchitect.UserId,
                    AssigneeName = techArchitect.UserName,
                    DueDate = DateTimeOffset.UtcNow.AddDays(config.MediumVulnerabilityDueDays),
                    CreatedDate = DateTimeOffset.UtcNow
                };

                await _dataService.CreateTaskAsync(task);
                tasksCreated++;

                _logger.LogInformation(
                    "Created MEDIUM security remediation task for {AppName} ({Count} findings), assigned to {UserName}",
                    app.Name, mediumFindings.Count, techArchitect.UserName);
            }
            else
            {
                tasksSkipped++;
            }
        }

        return new TaskGenerationResult
        {
            TasksCreated = tasksCreated,
            TasksSkipped = tasksSkipped,
            ApplicationsProcessed = 1,
            Errors = errors,
            TasksByType = tasksCreated > 0
                ? new Dictionary<TaskType, int> { [TaskType.SecurityRemediation] = tasksCreated }
                : []
        };
    }

    private static string BuildSecurityTaskDescription(
        Application app,
        RoleAssignment assignee,
        List<SecurityFinding> findings,
        string severity,
        int dueDays,
        int exposedSecretsCount)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"**Why you're receiving this task:** You are the {assignee.Role} for {app.Name} and are responsible for addressing security vulnerabilities.");
        sb.AppendLine();
        sb.AppendLine($"**{severity} Severity Vulnerabilities:** {findings.Count}");

        if (exposedSecretsCount > 0)
        {
            sb.AppendLine($"**Exposed Secrets (CRITICAL):** {exposedSecretsCount}");
        }

        sb.AppendLine();
        sb.AppendLine($"**Due:** {dueDays} days from discovery");
        sb.AppendLine();

        if (findings.Count > 0)
        {
            sb.AppendLine("**Findings:**");
            foreach (var finding in findings.Take(10))
            {
                sb.AppendLine($"- {finding.Title}");
                if (!string.IsNullOrEmpty(finding.FilePath))
                {
                    sb.AppendLine($"  - File: `{finding.FilePath}`{(finding.LineNumber.HasValue ? $" (line {finding.LineNumber})" : "")}");
                }
            }

            if (findings.Count > 10)
            {
                sb.AppendLine($"- ... and {findings.Count - 10} more");
            }
        }

        sb.AppendLine();
        sb.AppendLine("**Required Actions:**");
        sb.AppendLine("1. Review each vulnerability in Azure DevOps Advanced Security");
        sb.AppendLine("2. Assess the risk and determine remediation approach");
        sb.AppendLine("3. Implement fixes or document exceptions with justification");
        sb.AppendLine("4. Verify fixes don't introduce regressions");
        sb.AppendLine("5. Mark vulnerabilities as resolved once fixed");

        if (exposedSecretsCount > 0)
        {
            sb.AppendLine();
            sb.AppendLine("**IMPORTANT:** Exposed secrets require immediate action:");
            sb.AppendLine("- Rotate the affected credentials immediately");
            sb.AppendLine("- Check for unauthorized access using the compromised credentials");
            sb.AppendLine("- Remove secrets from code and use secure storage (Key Vault, env vars)");
        }

        return sb.ToString();
    }

    private static string GetDocumentationStatus(DocumentationStatus docs)
    {
        var items = new List<string>();

        if (docs.HasSystemDocumentation) items.Add("- System documentation: Present");
        else items.Add("- System documentation: **Missing**");

        if (docs.HasArchitectureDiagram) items.Add("- Architecture diagram: Present");
        else items.Add("- Architecture diagram: **Missing**");

        if (docs.HasUserDocumentation) items.Add("- User documentation: Present");
        else items.Add("- User documentation: **Missing**");

        if (docs.HasSupportDocumentation) items.Add("- Support documentation: Present");
        else items.Add("- Support documentation: **Missing**");

        items.Add($"- Overall completeness: {docs.CompletenessScore}%");

        return string.Join("\n", items);
    }

    public Task<TaskGenerationConfig> GetConfigurationAsync()
    {
        lock (_configLock)
        {
            return Task.FromResult(_config);
        }
    }

    public Task UpdateConfigurationAsync(TaskGenerationConfig config)
    {
        lock (_configLock)
        {
            _config = config with { LastUpdated = DateTimeOffset.UtcNow };
        }
        return Task.CompletedTask;
    }
}
