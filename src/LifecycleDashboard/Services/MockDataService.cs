using LifecycleDashboard.Models;

namespace LifecycleDashboard.Services;

/// <summary>
/// Mock data service providing realistic test data for development.
/// </summary>
public class MockDataService : IMockDataService
{
    // Mock data collections (pre-generated test data)
    private readonly List<Application> _mockApplications;
    private readonly List<LifecycleTask> _mockTasks;
    private readonly List<User> _mockUsers;
    private readonly Dictionary<string, RepositoryInfo> _mockRepositoryInfo;
    private readonly List<TaskDocumentation> _mockTaskDocumentation;
    private readonly List<FrameworkVersion> _mockFrameworkVersions;

    // Real data collections (populated by syncs) - used when mock mode is off
    private readonly List<Application> _realApplications = [];
    private readonly List<LifecycleTask> _realTasks = [];
    private readonly List<User> _realUsers = [];
    private readonly List<TaskDocumentation> _realTaskDocumentation = [];
    private readonly List<FrameworkVersion> _realFrameworkVersions = [];
    private readonly Dictionary<string, RepositoryInfo> _realRepositoryInfo = [];

    // Active collections that return either mock or real data based on mode
    private List<Application> Applications => IsMockDataEnabled ? _mockApplications : _realApplications;
    private List<LifecycleTask> Tasks => IsMockDataEnabled ? _mockTasks : _realTasks;
    private List<User> Users => IsMockDataEnabled ? _mockUsers : _realUsers;
    private Dictionary<string, RepositoryInfo> RepositoryInfo => IsMockDataEnabled ? _mockRepositoryInfo : _realRepositoryInfo;
    private List<TaskDocumentation> TaskDocumentationList => IsMockDataEnabled ? _mockTaskDocumentation : _realTaskDocumentation;
    private List<FrameworkVersion> FrameworkVersions => IsMockDataEnabled ? _mockFrameworkVersions : _realFrameworkVersions;

    public bool IsMockDataEnabled => _systemSettings.MockDataEnabled;

    public event EventHandler<bool>? MockDataModeChanged;

    public Task SetMockDataEnabledAsync(bool enabled)
    {
        if (_systemSettings.MockDataEnabled != enabled)
        {
            _systemSettings = _systemSettings with
            {
                MockDataEnabled = enabled,
                LastUpdated = DateTimeOffset.UtcNow
            };

            RecordAuditLogAsync(new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                EventType = enabled ? "MockDataEnabled" : "MockDataDisabled",
                Category = "Config",
                Message = $"Mock data mode was {(enabled ? "enabled" : "disabled")}",
                UserId = "system",
                UserName = "System",
                EntityType = "SystemSettings"
            });

            MockDataModeChanged?.Invoke(this, enabled);
        }

        return Task.CompletedTask;
    }

    public MockDataService()
    {
        _mockUsers = GenerateMockUsers();
        _mockApplications = GenerateMockApplications();
        _mockTasks = GenerateMockTasks();
        _mockRepositoryInfo = GenerateMockRepositoryInfo();
        _mockTaskDocumentation = GenerateMockTaskDocumentation();
        _mockFrameworkVersions = GenerateMockFrameworkVersions();
    }

    #region Application Methods

    public Task<IReadOnlyList<Application>> GetApplicationsAsync()
    {
        return Task.FromResult<IReadOnlyList<Application>>(Applications.OrderBy(a => a.Name).ToList());
    }

    public Task<Application?> GetApplicationAsync(string id)
    {
        var app = Applications.FirstOrDefault(a => a.Id == id);
        return Task.FromResult(app);
    }

    public Task<IReadOnlyList<Application>> GetApplicationsByHealthAsync(HealthCategory category)
    {
        var apps = Applications.Where(a => a.HealthCategory == category).ToList();
        return Task.FromResult<IReadOnlyList<Application>>(apps);
    }

    #endregion

    #region Task Methods

    public Task<IReadOnlyList<LifecycleTask>> GetTasksForUserAsync(string userId)
    {
        var tasks = Tasks.Where(t => t.AssigneeId == userId).OrderBy(t => t.DueDate).ToList();
        return Task.FromResult<IReadOnlyList<LifecycleTask>>(tasks);
    }

    public Task<IReadOnlyList<LifecycleTask>> GetTasksForApplicationAsync(string applicationId)
    {
        var tasks = Tasks.Where(t => t.ApplicationId == applicationId).OrderBy(t => t.DueDate).ToList();
        return Task.FromResult<IReadOnlyList<LifecycleTask>>(tasks);
    }

    public Task<IReadOnlyList<LifecycleTask>> GetOverdueTasksAsync()
    {
        var tasks = Tasks.Where(t => t.IsOverdue).OrderByDescending(t => t.DaysOverdue).ToList();
        return Task.FromResult<IReadOnlyList<LifecycleTask>>(tasks);
    }

    public Task<TaskSummary> GetTaskSummaryForUserAsync(string userId)
    {
        var userTasks = Tasks.Where(t => t.AssigneeId == userId).ToList();
        var now = DateTimeOffset.UtcNow;
        var weekFromNow = now.AddDays(7);
        var monthFromNow = now.AddDays(30);

        var summary = new TaskSummary
        {
            Total = userTasks.Count,
            Overdue = userTasks.Count(t => t.IsOverdue),
            DueThisWeek = userTasks.Count(t => !t.IsOverdue && t.DueDate <= weekFromNow && t.Status != Models.TaskStatus.Completed),
            DueThisMonth = userTasks.Count(t => !t.IsOverdue && t.DueDate <= monthFromNow && t.Status != Models.TaskStatus.Completed),
            Completed = userTasks.Count(t => t.Status == Models.TaskStatus.Completed),
            InProgress = userTasks.Count(t => t.Status == Models.TaskStatus.InProgress)
        };

        return Task.FromResult(summary);
    }

    #endregion

    #region Portfolio Methods

    public Task<PortfolioHealthSummary> GetPortfolioHealthSummaryAsync()
    {
        var summary = new PortfolioHealthSummary
        {
            TotalApplications = Applications.Count,
            HealthyCount = Applications.Count(a => a.HealthCategory == HealthCategory.Healthy),
            NeedsAttentionCount = Applications.Count(a => a.HealthCategory == HealthCategory.NeedsAttention),
            AtRiskCount = Applications.Count(a => a.HealthCategory == HealthCategory.AtRisk),
            CriticalCount = Applications.Count(a => a.HealthCategory == HealthCategory.Critical),
            AverageScore = Applications.Average(a => a.HealthScore),
            TrendDirection = 1, // Simulated improving trend
            LastUpdated = DateTimeOffset.UtcNow.AddHours(-2)
        };

        return Task.FromResult(summary);
    }

    #endregion

    #region User Methods

    public Task<User> GetCurrentUserAsync()
    {
        // Return first user as "current user" for mock purposes
        return Task.FromResult(Users.First());
    }

    public Task<IReadOnlyList<User>> GetUsersAsync()
    {
        return Task.FromResult<IReadOnlyList<User>>(Users);
    }

    #endregion

    #region Task Detail Methods

    public Task<LifecycleTask?> GetTaskAsync(string taskId)
    {
        var task = Tasks.FirstOrDefault(t => t.Id == taskId);
        return Task.FromResult(task);
    }

    #endregion

    #region Repository Info Methods

    public Task<RepositoryInfo?> GetRepositoryInfoAsync(string applicationId)
    {
        RepositoryInfo.TryGetValue(applicationId, out var repoInfo);
        return Task.FromResult(repoInfo);
    }

    #endregion

    #region Task Documentation Methods

    public Task<TaskDocumentation?> GetTaskDocumentationAsync(TaskType taskType)
    {
        var doc = TaskDocumentationList.FirstOrDefault(d => d.TaskType == taskType);
        return Task.FromResult(doc);
    }

    public Task<IReadOnlyList<TaskDocumentation>> GetAllTaskDocumentationAsync()
    {
        return Task.FromResult<IReadOnlyList<TaskDocumentation>>(TaskDocumentationList);
    }

    #endregion

    #region Mock Data Generation

    private static List<User> GenerateMockUsers()
    {
        return
        [
            new User
            {
                Id = "user-001",
                Name = "Dr. Elena Vasquez",
                Email = "elena.vasquez@company.com",
                Department = "Materials Characterization",
                Title = "Senior Research Scientist",
                Role = SystemRole.StandardUser,
                IsActive = true,
                LastLoginDate = DateTimeOffset.UtcNow.AddHours(-1)
            },
            new User
            {
                Id = "user-002",
                Name = "Dr. Marcus Chen",
                Email = "marcus.chen@company.com",
                Department = "Polymer Science",
                Title = "Principal Investigator",
                Role = SystemRole.PowerUser,
                IsActive = true,
                LastLoginDate = DateTimeOffset.UtcNow.AddHours(-3)
            },
            new User
            {
                Id = "user-003",
                Name = "Dr. Priya Sharma",
                Email = "priya.sharma@company.com",
                Department = "Laboratory IT",
                Title = "Lab Systems Administrator",
                Role = SystemRole.Administrator,
                IsActive = true,
                LastLoginDate = DateTimeOffset.UtcNow.AddDays(-1)
            },
            new User
            {
                Id = "user-004",
                Name = "Dr. Thomas Okonkwo",
                Email = "thomas.okonkwo@company.com",
                Department = "Quality Assurance",
                Title = "QA Manager",
                Role = SystemRole.SecurityAdministrator,
                IsActive = true,
                LastLoginDate = DateTimeOffset.UtcNow.AddHours(-5)
            },
            new User
            {
                Id = "user-005",
                Name = "Dr. Yuki Tanaka",
                Email = "yuki.tanaka@company.com",
                Department = "Computational Materials",
                Title = "Research Director",
                Role = SystemRole.ReadOnly,
                IsActive = true,
                LastLoginDate = DateTimeOffset.UtcNow.AddDays(-2)
            }
        ];
    }

    private List<Application> GenerateMockApplications()
    {
        var capabilities = new[] { "Characterization", "Synthesis", "Simulation", "Quality", "Compliance", "Data Management", "Instrumentation" };
        // Use specific framework versions for matching with framework EOL tracking
        var techStacks = new[]
        {
            new[] { ".NET 8", "SQL Server", "On-Premise" },            // Current LTS
            new[] { ".NET 6", "PostgreSQL", "Azure" },                 // Still supported LTS
            new[] { "Python 3.11", "PostgreSQL", "On-Premise" },       // Current Python
            new[] { "Python 3.9", "MongoDB", "On-Premise" },           // Older Python
            new[] { "LabVIEW 2023", "SQL Server", "On-Premise" },      // Lab automation
            new[] { "MATLAB R2023b", "PostgreSQL", "On-Premise" },     // Scientific computing
            new[] { "Python 3.11", "HDF5", "On-Premise" },             // Scientific data
            new[] { "Java 21", "Oracle", "On-Premise" },               // Enterprise LIMS
            new[] { ".NET Framework 4.8", "SQL Server", "On-Premise" }, // Legacy systems
            new[] { "Python 2.7", "MySQL", "On-Premise" },             // Past EOL legacy
            new[] { "C++17", "PostgreSQL", "On-Premise" },             // High-performance
            new[] { ".NET 8", "CosmosDB", "Azure" },                   // Cloud-native
            new[] { "R 4.3", "PostgreSQL", "On-Premise" }              // Statistical analysis
        };

        var appNames = new[]
        {
            "LIMS", "Spectroscopy Analysis Suite", "Materials Property Database", "Electron Microscopy Control",
            "Sample Tracking System", "XRD Analysis Platform", "Thermal Analysis Software", "Chemical Inventory Manager",
            "Research Data Repository", "Computational Materials Platform", "Quality Management System", "Equipment Calibration Tracker",
            "Patent Analytics Dashboard", "Safety Data Sheet Manager", "Lab Notebook Electronic", "Publication Tracker",
            "Collaboration Portal", "Instrument Scheduler", "Polymer Database", "Metallurgy Calculator",
            "Crystallography Suite", "NMR Data Processor", "Mass Spectrometry Manager", "Rheology Analysis Tool",
            "Coating Formulation System", "Corrosion Testing Database", "Tensile Testing Software", "Fatigue Analysis Platform",
            "Surface Analysis Suite", "Particle Size Analyzer", "Microscopy Image Manager", "Spectral Library Manager",
            "Legacy LIMS", "Legacy Sample Tracker", "Legacy Instrument Control", "Data Migration Tool",
            "Lab Integration Hub", "Experiment Workflow Engine", "Protocol Builder", "Materials Search Service"
        };

        var random = new Random(42); // Seeded for consistent mock data
        var applications = new List<Application>();

        for (int i = 0; i < appNames.Length; i++)
        {
            var capability = capabilities[random.Next(capabilities.Length)];
            var techStack = techStacks[random.Next(techStacks.Length)].ToList();
            var healthScore = GenerateHealthScore(random, appNames[i]);
            var securityFindings = GenerateSecurityFindings(random, healthScore);
            var usage = GenerateUsageMetrics(random);
            var documentation = GenerateDocumentationStatus(random);

            applications.Add(new Application
            {
                Id = $"app-{i + 1:D3}",
                Name = appNames[i],
                Description = $"The {appNames[i]} application provides essential functionality for the {capability} capability.",
                Capability = capability,
                RepositoryUrl = $"https://dev.azure.com/company/project/_git/{appNames[i].ToLower().Replace(" ", "-")}",
                DocumentationUrl = $"https://sharepoint.company.com/docs/{capability.ToLower().Replace(" ", "-")}/{appNames[i].ToLower().Replace(" ", "-")}",
                ServiceNowId = $"SN-{random.Next(10000, 99999)}",
                IsMockData = true, // This is mock/seed data
                HealthScore = healthScore,
                LastActivityDate = DateTimeOffset.UtcNow.AddDays(-random.Next(1, 400)),
                LastSyncDate = DateTimeOffset.UtcNow.AddHours(-random.Next(1, 48)),
                TechnologyStack = techStack,
                Tags = GenerateTags(random, capability),
                SecurityFindings = securityFindings,
                RoleAssignments = GenerateRoleAssignments(random),
                Usage = usage,
                Documentation = documentation,
                HasDataConflicts = random.Next(10) == 0,
                DataConflicts = random.Next(10) == 0 ? ["ServiceNow name mismatch", "Missing repository link"] : [],
                UsageAvailability = GenerateUsageAvailability(random, appNames[i], capability),
                CriticalPeriods = GenerateCriticalPeriods(random, appNames[i], capability),
                KeyDates = GenerateKeyDates(random, appNames[i])
            });
        }

        return applications;
    }

    private static int GenerateHealthScore(Random random, string appName)
    {
        // Make "Legacy" apps have lower scores
        if (appName.StartsWith("Legacy"))
            return random.Next(25, 55);

        // Most apps are healthy
        var roll = random.Next(100);
        return roll switch
        {
            < 60 => random.Next(80, 100),  // 60% healthy
            < 85 => random.Next(60, 79),   // 25% needs attention
            < 95 => random.Next(40, 59),   // 10% at risk
            _ => random.Next(20, 39)       // 5% critical
        };
    }

    private static List<SecurityFinding> GenerateSecurityFindings(Random random, int healthScore)
    {
        var findings = new List<SecurityFinding>();

        // Lower health scores have more findings
        var findingCount = healthScore switch
        {
            < 40 => random.Next(3, 8),
            < 60 => random.Next(2, 5),
            < 80 => random.Next(0, 3),
            _ => random.Next(0, 2)
        };

        var findingTypes = new[]
        {
            ("SQL Injection vulnerability", SecuritySeverity.Critical),
            ("Cross-Site Scripting (XSS)", SecuritySeverity.High),
            ("Insecure Direct Object Reference", SecuritySeverity.High),
            ("Missing Authentication", SecuritySeverity.Critical),
            ("Sensitive Data Exposure", SecuritySeverity.Medium),
            ("Outdated Dependency", SecuritySeverity.Medium),
            ("Weak Cryptography", SecuritySeverity.High),
            ("Missing HTTPS", SecuritySeverity.Medium),
            ("Information Disclosure", SecuritySeverity.Low),
            ("Missing Security Headers", SecuritySeverity.Low)
        };

        for (int i = 0; i < findingCount; i++)
        {
            var (title, severity) = findingTypes[random.Next(findingTypes.Length)];
            findings.Add(new SecurityFinding
            {
                Id = $"CVE-2026-{random.Next(1000, 9999)}",
                Title = title,
                Severity = severity,
                Description = $"A {severity.ToString().ToLower()} severity {title.ToLower()} was detected.",
                FilePath = $"src/Controllers/{GenerateRandomFileName(random)}.cs",
                LineNumber = random.Next(10, 500),
                DetectedDate = DateTimeOffset.UtcNow.AddDays(-random.Next(1, 90))
            });
        }

        return findings;
    }

    private static UsageMetrics GenerateUsageMetrics(Random random)
    {
        var level = random.Next(100);
        var (requests, users, sessions) = level switch
        {
            < 5 => (0, 0, 0),
            < 15 => (random.Next(1, 100), random.Next(1, 20), random.Next(1, 50)),
            < 35 => (random.Next(100, 1000), random.Next(20, 100), random.Next(50, 200)),
            < 75 => (random.Next(1000, 10000), random.Next(100, 500), random.Next(200, 1000)),
            _ => (random.Next(10000, 100000), random.Next(500, 2000), random.Next(1000, 5000))
        };

        return new UsageMetrics
        {
            MonthlyRequests = requests,
            MonthlyUsers = users,
            MonthlySessions = sessions,
            Trend = (UsageTrend)random.Next(3)
        };
    }

    private static DocumentationStatus GenerateDocumentationStatus(Random random)
    {
        return new DocumentationStatus
        {
            HasArchitectureDiagram = random.Next(100) < 70,
            HasSystemDocumentation = random.Next(100) < 75,
            HasUserDocumentation = random.Next(100) < 60,
            HasSupportDocumentation = random.Next(100) < 50
        };
    }

    private List<RoleAssignment> GenerateRoleAssignments(Random random)
    {
        var roles = new[] { ApplicationRole.Owner, ApplicationRole.TechnicalLead, ApplicationRole.Developer, ApplicationRole.SecurityChampion };
        var assignments = new List<RoleAssignment>();

        foreach (var role in roles.Take(random.Next(2, 4)))
        {
            var user = _mockUsers[random.Next(_mockUsers.Count)];
            assignments.Add(new RoleAssignment
            {
                UserId = user.Id,
                UserName = user.Name,
                UserEmail = user.Email,
                Role = role,
                AssignedDate = DateTimeOffset.UtcNow.AddDays(-random.Next(30, 365)),
                LastValidatedDate = DateTimeOffset.UtcNow.AddDays(-random.Next(1, 180)),
                NeedsRevalidation = random.Next(100) < 15
            });
        }

        return assignments;
    }

    private static List<string> GenerateTags(Random random, string capability)
    {
        var allTags = new[] { "production", "gmp-validated", "critical", "research-only", "21cfr11", "instrument-control", "data-analysis", "laboratory", "calibrated", "iso-compliant" };
        var tags = new List<string> { capability.ToLower().Replace(" ", "-") };

        for (int i = 0; i < random.Next(1, 4); i++)
        {
            tags.Add(allTags[random.Next(allTags.Length)]);
        }

        return tags.Distinct().ToList();
    }

    private static string GenerateRandomFileName(Random random)
    {
        var names = new[] { "SampleController", "InstrumentService", "SpectrumProcessor", "DataExporter", "CalibrationClient", "AnalysisService" };
        return names[random.Next(names.Length)];
    }

    private static UsageDataAvailability GenerateUsageAvailability(Random random, string appName, string capability)
    {
        // Certain app types don't have meaningful usage data
        var isBackend = appName.Contains("Service") || appName.Contains("Integration") || appName.Contains("ETL");
        var isBatch = appName.Contains("Pipeline") || appName.Contains("Processor") || appName.Contains("Migration");
        var isLegacy = appName.StartsWith("Legacy");
        var isSeasonal = appName.Contains("Patent") || appName.Contains("Publication") || capability == "Compliance";

        if (isBackend)
        {
            return new UsageDataAvailability
            {
                IsAvailable = false,
                Reason = UsageDataReason.BackendService,
                Notes = "Backend service with no direct user interaction. Usage is measured by dependent applications.",
                IsSeasonal = false,
                LastReviewedDate = DateTimeOffset.UtcNow.AddDays(-random.Next(30, 180)),
                ReviewedBy = "Dr. Priya Sharma"
            };
        }

        if (isBatch)
        {
            return new UsageDataAvailability
            {
                IsAvailable = false,
                Reason = UsageDataReason.BatchProcess,
                Notes = "Automated batch process with scheduled execution. No interactive user sessions.",
                IsSeasonal = false,
                LastReviewedDate = DateTimeOffset.UtcNow.AddDays(-random.Next(30, 180)),
                ReviewedBy = "Dr. Marcus Chen"
            };
        }

        if (isSeasonal)
        {
            return new UsageDataAvailability
            {
                IsAvailable = true,
                Reason = UsageDataReason.SeasonalUsage,
                Notes = "Usage varies significantly by time of year. Monthly averages may not be representative.",
                IsSeasonal = true,
                SeasonalPattern = capability == "Compliance"
                    ? "Peak usage in Q1 (audit season) and Q4 (year-end compliance reviews)"
                    : "Higher activity during publication deadlines (March, September)",
                LastReviewedDate = DateTimeOffset.UtcNow.AddDays(-random.Next(30, 90)),
                ReviewedBy = "Dr. Yuki Tanaka"
            };
        }

        if (isLegacy)
        {
            return new UsageDataAvailability
            {
                IsAvailable = true,
                Reason = UsageDataReason.BeingRetired,
                Notes = "Application is in retirement planning. Usage is expected to decline as users migrate to replacement system.",
                IsSeasonal = false,
                LastReviewedDate = DateTimeOffset.UtcNow.AddDays(-random.Next(15, 60)),
                ReviewedBy = "Dr. Thomas Okonkwo"
            };
        }

        // Default: usage data is available
        return new UsageDataAvailability
        {
            IsAvailable = true,
            Reason = UsageDataReason.Available,
            IsSeasonal = false,
            LastReviewedDate = random.Next(3) == 0 ? DateTimeOffset.UtcNow.AddDays(-random.Next(30, 365)) : null
        };
    }

    private static List<CriticalPeriod> GenerateCriticalPeriods(Random random, string appName, string capability)
    {
        var periods = new List<CriticalPeriod>();

        // Lab instruments have specific critical periods
        if (appName.Contains("Microscopy") || appName.Contains("Spectroscopy") || appName.Contains("XRD") || appName.Contains("NMR"))
        {
            periods.Add(new CriticalPeriod
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Quarterly Instrument Qualification",
                Description = "Instruments must be operational for quarterly qualification runs. No maintenance allowed.",
                Criticality = PeriodCriticality.Blackout,
                StartMonth = 3, StartDay = 25,
                EndMonth = 3, EndDay = 31,
                IsRecurring = true
            });
            periods.Add(new CriticalPeriod
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Q2 Qualification",
                Criticality = PeriodCriticality.Blackout,
                StartMonth = 6, StartDay = 25,
                EndMonth = 6, EndDay = 30,
                IsRecurring = true
            });
        }

        // Quality systems have audit periods
        if (capability == "Quality" || appName.Contains("Quality") || appName.Contains("Compliance"))
        {
            periods.Add(new CriticalPeriod
            {
                Id = Guid.NewGuid().ToString(),
                Name = "FDA Audit Preparation",
                Description = "System must be fully operational during regulatory audit preparation and execution.",
                Criticality = PeriodCriticality.Blackout,
                StartMonth = 2, StartDay = 1,
                EndMonth = 3, EndDay = 15,
                IsRecurring = true
            });
            periods.Add(new CriticalPeriod
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Year-End Compliance Review",
                Description = "Critical period for annual compliance documentation and review.",
                Criticality = PeriodCriticality.Critical,
                StartMonth = 11, StartDay = 15,
                EndMonth = 12, EndDay = 31,
                IsRecurring = true
            });
        }

        // LIMS has sample processing peaks
        if (appName.Contains("LIMS") || appName.Contains("Sample"))
        {
            periods.Add(new CriticalPeriod
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Research Project Deadlines",
                Description = "Peak sample processing period aligned with grant reporting deadlines.",
                Criticality = PeriodCriticality.Critical,
                StartMonth = 9, StartDay = 1,
                EndMonth = 9, EndDay = 30,
                IsRecurring = true
            });
        }

        // Add maintenance windows for some apps
        if (random.Next(3) == 0)
        {
            periods.Add(new CriticalPeriod
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Summer Maintenance Window",
                Description = "Reduced lab activity - ideal time for system updates and maintenance.",
                Criticality = PeriodCriticality.MaintenanceWindow,
                StartMonth = 7, StartDay = 15,
                EndMonth = 8, EndDay = 15,
                IsRecurring = true
            });
        }

        // Holiday period
        if (random.Next(2) == 0)
        {
            periods.Add(new CriticalPeriod
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Holiday Period",
                Description = "Reduced staffing - maintenance acceptable but emergency support limited.",
                Criticality = PeriodCriticality.LowActivity,
                StartMonth = 12, StartDay = 20,
                EndMonth = 1, EndDay = 5,
                IsRecurring = true
            });
        }

        return periods;
    }

    private static List<KeyDate> GenerateKeyDates(Random random, string appName)
    {
        var dates = new List<KeyDate>();

        // Some apps have upcoming releases
        if (random.Next(3) == 0)
        {
            dates.Add(new KeyDate
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Version 2.0 Release",
                Description = "Major release with new analysis features. All testing must be complete before this date.",
                Date = DateTimeOffset.UtcNow.AddDays(random.Next(30, 90)),
                Type = KeyDateType.Release,
                BlocksMaintenance = true,
                WarningDaysBefore = 14,
                WarningDaysAfter = 3,
                AddedBy = "Dr. Elena Vasquez",
                AddedDate = DateTimeOffset.UtcNow.AddDays(-random.Next(10, 30))
            });
        }

        // Audit dates for regulated apps
        if (appName.Contains("Quality") || appName.Contains("LIMS") || appName.Contains("Calibration"))
        {
            dates.Add(new KeyDate
            {
                Id = Guid.NewGuid().ToString(),
                Title = "GMP Audit",
                Description = "Annual GMP compliance audit - system must be fully operational and documented.",
                Date = DateTimeOffset.UtcNow.AddDays(random.Next(45, 120)),
                Type = KeyDateType.Audit,
                BlocksMaintenance = true,
                WarningDaysBefore = 21,
                WarningDaysAfter = 0,
                AddedBy = "Dr. Thomas Okonkwo",
                AddedDate = DateTimeOffset.UtcNow.AddDays(-random.Next(30, 60))
            });
        }

        // License expirations
        if (random.Next(4) == 0)
        {
            dates.Add(new KeyDate
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Software License Renewal",
                Description = "Annual license renewal required. Ensure procurement process is initiated.",
                Date = DateTimeOffset.UtcNow.AddDays(random.Next(60, 180)),
                Type = KeyDateType.Expiration,
                BlocksMaintenance = false,
                WarningDaysBefore = 30,
                AddedBy = "Dr. Priya Sharma",
                AddedDate = DateTimeOffset.UtcNow.AddDays(-random.Next(5, 20))
            });
        }

        // Scheduled maintenance
        if (random.Next(5) == 0)
        {
            dates.Add(new KeyDate
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Database Migration",
                Description = "Scheduled database migration to new server infrastructure.",
                Date = DateTimeOffset.UtcNow.AddDays(random.Next(14, 45)),
                Type = KeyDateType.ScheduledMaintenance,
                BlocksMaintenance = false,
                WarningDaysBefore = 7,
                AddedBy = "Dr. Priya Sharma",
                AddedDate = DateTimeOffset.UtcNow.AddDays(-random.Next(3, 10))
            });
        }

        return dates;
    }

    private List<LifecycleTask> GenerateMockTasks()
    {
        var tasks = new List<LifecycleTask>();
        var random = new Random(42);
        var taskId = 1;

        var taskTemplates = new (string Title, TaskType Type, string Description)[]
        {
            ("Role Validation", TaskType.RoleValidation, "Validate that role assignments are current and accurate"),
            ("Security Remediation", TaskType.SecurityRemediation, "Address security vulnerabilities identified in scans"),
            ("Documentation Review", TaskType.DocumentationReview, "Review and update application documentation"),
            ("Architecture Review", TaskType.ArchitectureReview, "Annual architecture review and assessment"),
            ("Retirement Review", TaskType.RetirementReview, "Evaluate application for potential retirement"),
            ("Compliance Check", TaskType.ComplianceCheck, "Verify compliance with organizational standards")
        };

        // Create tasks for each application
        foreach (var app in _mockApplications.Take(25)) // Tasks for some apps
        {
            var template = taskTemplates[random.Next(taskTemplates.Length)];
            var assignee = _mockUsers[random.Next(_mockUsers.Count)];

            // Vary due dates - some overdue, some due soon, some upcoming
            var daysOffset = random.Next(100) switch
            {
                < 15 => -random.Next(1, 30),  // 15% overdue
                < 40 => random.Next(0, 7),     // 25% due this week
                _ => random.Next(8, 90)        // 60% upcoming
            };

            var createdDate = DateTimeOffset.UtcNow.AddDays(-random.Next(30, 180));

            tasks.Add(new LifecycleTask
            {
                Id = $"task-{taskId++:D4}",
                Title = $"{template.Title} - {app.Name}",
                Description = template.Description,
                Type = template.Type,
                Priority = (TaskPriority)random.Next(4),
                Status = daysOffset < 0 ? Models.TaskStatus.Pending : (Models.TaskStatus)random.Next(3),
                ApplicationId = app.Id,
                ApplicationName = app.Name,
                AssigneeId = assignee.Id,
                AssigneeName = assignee.Name,
                AssigneeEmail = assignee.Email,
                DueDate = DateTimeOffset.UtcNow.AddDays(daysOffset),
                CreatedDate = createdDate,
                IsEscalated = daysOffset < -14,
                History =
                [
                    new TaskHistoryEntry
                    {
                        Id = Guid.NewGuid().ToString(),
                        Timestamp = createdDate,
                        Action = "Created",
                        PerformedBy = "System",
                        PerformedById = "system",
                        Notes = "Task automatically created based on lifecycle rules"
                    }
                ]
            });
        }

        return tasks;
    }

    private Dictionary<string, RepositoryInfo> GenerateMockRepositoryInfo()
    {
        var repoInfo = new Dictionary<string, RepositoryInfo>();
        var random = new Random(42);

        var packageNames = new[]
        {
            ("Microsoft.EntityFrameworkCore", "8.0.1", "8.0.2"),
            ("Newtonsoft.Json", "13.0.3", "13.0.3"),
            ("Serilog", "3.1.1", "4.0.0"),
            ("AutoMapper", "12.0.1", "13.0.1"),
            ("FluentValidation", "11.9.0", "11.9.0"),
            ("MediatR", "12.2.0", "12.2.0"),
            ("Microsoft.AspNetCore.Authentication.JwtBearer", "8.0.1", "8.0.2"),
            ("Azure.Storage.Blobs", "12.19.1", "12.20.0"),
            ("Microsoft.ApplicationInsights", "2.22.0", "2.22.0"),
            ("Polly", "8.2.1", "8.3.0")
        };

        var contributors = new[]
        {
            ("Dr. Elena Vasquez", "elena.vasquez@company.com"),
            ("Dr. Marcus Chen", "marcus.chen@company.com"),
            ("Dr. Priya Sharma", "priya.sharma@company.com"),
            ("Dr. Thomas Okonkwo", "thomas.okonkwo@company.com"),
            ("Dr. Yuki Tanaka", "yuki.tanaka@company.com"),
            ("Dr. Stefan Mueller", "stefan.mueller@company.com"),
            ("Dr. Aisha Patel", "aisha.patel@company.com")
        };

        foreach (var app in _mockApplications)
        {
            var stackType = random.Next(10) switch
            {
                < 3 => StackType.DotNetCore,
                < 5 => StackType.DotNetFramework,
                < 7 => StackType.Blazor,
                < 8 => StackType.NodeJs,
                _ => StackType.Mixed
            };

            var detectedPattern = stackType switch
            {
                StackType.DotNetCore => random.Next(2) == 0 ? "dotnet+aurelia" : "dotnet-api",
                StackType.DotNetFramework => "full-framework",
                StackType.Blazor => "blazor+dotnet",
                StackType.NodeJs => "node+react",
                _ => "mixed-stack"
            };

            var packages = new List<PackageReference>();
            var packageCount = random.Next(3, 8);
            for (int i = 0; i < packageCount; i++)
            {
                var (name, version, latest) = packageNames[random.Next(packageNames.Length)];
                if (!packages.Any(p => p.Name == name))
                {
                    packages.Add(new PackageReference
                    {
                        Name = name,
                        Version = version,
                        LatestVersion = latest,
                        Type = PackageType.NuGet,
                        HasKnownVulnerability = random.Next(20) == 0
                    });
                }
            }

            var topContribs = new List<ContributorInfo>();
            var contribCount = random.Next(2, 5);
            for (int i = 0; i < contribCount; i++)
            {
                var (name, email) = contributors[random.Next(contributors.Length)];
                if (!topContribs.Any(c => c.Email == email))
                {
                    topContribs.Add(new ContributorInfo
                    {
                        Name = name,
                        Email = email,
                        CommitCount = random.Next(10, 500),
                        Last365DaysCommitCount = random.Next(5, 100),
                        LastCommitDate = DateTimeOffset.UtcNow.AddDays(-random.Next(1, 180))
                    });
                }
            }

            var firstCommitDate = DateTimeOffset.UtcNow.AddDays(-random.Next(365, 2500));
            var lastCommitDate = DateTimeOffset.UtcNow.AddDays(-random.Next(1, 400));
            var (lastCommitter, lastCommitterEmail) = contributors[random.Next(contributors.Length)];
            var (firstCommitter, firstCommitterEmail) = contributors[random.Next(contributors.Length)];
            var topContributor = topContribs.OrderByDescending(c => c.Last365DaysCommitCount).First();

            var dependencies = new List<SystemDependency>();
            if (random.Next(2) == 0)
            {
                dependencies.Add(new SystemDependency
                {
                    Name = "Primary Database",
                    Type = DependencyType.SqlServer,
                    ConnectionInfo = "Server=sql-prod-01.company.com;Database=***;",
                    ConfigSource = "appsettings.json",
                    IsProduction = true
                });
            }
            if (random.Next(3) == 0)
            {
                dependencies.Add(new SystemDependency
                {
                    Name = "Redis Cache",
                    Type = DependencyType.Redis,
                    ConnectionInfo = "redis-prod-01.company.com:6379",
                    ConfigSource = "appsettings.json",
                    IsProduction = true
                });
            }
            if (random.Next(2) == 0)
            {
                dependencies.Add(new SystemDependency
                {
                    Name = "Service Bus",
                    Type = DependencyType.ServiceBus,
                    ConnectionInfo = "sb-prod.servicebus.windows.net",
                    ConfigSource = "appsettings.json",
                    IsProduction = true
                });
            }

            repoInfo[app.Id] = new RepositoryInfo
            {
                RepositoryId = $"repo-{app.Id}",
                Name = app.Name.ToLower().Replace(" ", "-"),
                DefaultBranch = "main",
                Url = app.RepositoryUrl,
                Packages = packages,
                Stack = new TechnologyStackInfo
                {
                    PrimaryStack = stackType,
                    Frameworks = stackType switch
                    {
                        StackType.DotNetCore => ["ASP.NET Core", "Entity Framework Core"],
                        StackType.DotNetFramework => ["ASP.NET MVC", "Entity Framework"],
                        StackType.Blazor => ["Blazor Server", "ASP.NET Core"],
                        StackType.NodeJs => ["Express", "React"],
                        _ => ["Mixed"]
                    },
                    Languages = stackType switch
                    {
                        StackType.NodeJs => ["TypeScript", "JavaScript"],
                        _ => ["C#", "TypeScript"]
                    },
                    DetectedPattern = detectedPattern,
                    TargetFramework = stackType switch
                    {
                        StackType.DotNetCore => "net8.0",
                        StackType.DotNetFramework => "net472",
                        StackType.Blazor => "net8.0",
                        _ => null
                    }
                },
                Commits = new CommitHistory
                {
                    FirstCommitDate = firstCommitDate,
                    LastCommitDate = lastCommitDate,
                    FirstCommitter = firstCommitter,
                    FirstCommitterEmail = firstCommitterEmail,
                    LastCommitter = lastCommitter,
                    LastCommitterEmail = lastCommitterEmail,
                    TopCommitter = topContributor.Name,
                    TopCommitterEmail = topContributor.Email,
                    TotalCommitCount = random.Next(100, 5000),
                    Last30DaysCommitCount = random.Next(0, 50),
                    Last90DaysCommitCount = random.Next(10, 150),
                    Last365DaysCommitCount = random.Next(50, 500),
                    TopContributors = topContribs.OrderByDescending(c => c.CommitCount).ToList()
                },
                Readme = new ReadmeStatus
                {
                    Exists = random.Next(100) < 85,
                    IsTemplate = random.Next(100) < 30,
                    CharacterCount = random.Next(100, 5000),
                    LastModified = DateTimeOffset.UtcNow.AddDays(-random.Next(30, 365))
                },
                HasApplicationInsights = random.Next(100) < 70,
                ApplicationInsightsKey = random.Next(100) < 70 ? Guid.NewGuid().ToString() : null,
                SystemDependencies = dependencies,
                LastBuildDate = DateTimeOffset.UtcNow.AddDays(-random.Next(1, 30)),
                LastBuildStatus = random.Next(10) < 9 ? "Succeeded" : "Failed",
                LastSyncDate = DateTimeOffset.UtcNow.AddHours(-random.Next(1, 24))
            };
        }

        return repoInfo;
    }

    private static List<TaskDocumentation> GenerateMockTaskDocumentation()
    {
        return
        [
            new TaskDocumentation
            {
                Id = "doc-role-validation",
                TaskType = TaskType.RoleValidation,
                Title = "Role Validation",
                Description = "Verify that all role assignments for your application are current and accurate. This ensures that the right people have the right access and responsibilities.",
                EstimatedDuration = TimeSpan.FromMinutes(30),
                Prerequisites = ["Access to ServiceNow", "Knowledge of current team assignments"],
                TypicalRoles = [ApplicationRole.Owner, ApplicationRole.TechnicalLead],
                LastUpdated = DateTimeOffset.UtcNow.AddDays(-30),
                LastUpdatedBy = "System Administrator",
                Instructions =
                [
                    new TaskInstruction
                    {
                        StepNumber = 1,
                        Title = "Review Current Assignments",
                        Description = "Open the application in ServiceNow and review all current role assignments. Compare against your actual team roster.",
                        SystemReference = "ServiceNow",
                        ActionUrl = "https://company.service-now.com/cmdb_ci_list.do"
                    },
                    new TaskInstruction
                    {
                        StepNumber = 2,
                        Title = "Verify Owner Information",
                        Description = "Ensure the application owner is still accurate. If the owner has changed, update the assignment.",
                        SystemReference = "ServiceNow"
                    },
                    new TaskInstruction
                    {
                        StepNumber = 3,
                        Title = "Check Technical Lead",
                        Description = "Verify the technical lead assignment is current. This person should be actively involved in technical decisions.",
                        SystemReference = "ServiceNow"
                    },
                    new TaskInstruction
                    {
                        StepNumber = 4,
                        Title = "Update SharePoint",
                        Description = "If any changes were made, also update the application entry in SharePoint to keep documentation in sync.",
                        SystemReference = "SharePoint",
                        IsOptional = true
                    },
                    new TaskInstruction
                    {
                        StepNumber = 5,
                        Title = "Mark Complete",
                        Description = "Once all roles are validated, mark this task as complete in the Lifecycle Dashboard.",
                        SystemReference = "Lifecycle Dashboard"
                    }
                ],
                SystemGuidance =
                [
                    new SystemGuidance
                    {
                        SystemName = "ServiceNow",
                        ActionType = "Revalidate Roles",
                        Instructions = "1. Navigate to the Configuration Item (CI) for your application\n2. Click 'Edit' in the top-right corner\n3. Scroll to the 'Support' section\n4. Update the Owner, Technical Lead, and other role fields as needed\n5. Click 'Update' to save changes\n6. Changes will sync to the Lifecycle Dashboard within 24 hours",
                        DirectLink = "https://company.service-now.com/cmdb_ci_list.do",
                        RequiredPermissions = "CMDB Editor role or higher",
                        TroubleshootingTips =
                        [
                            new TroubleshootingTip
                            {
                                Issue = "Cannot find my application in ServiceNow",
                                Resolution = "Search by the ServiceNow ID shown in the Lifecycle Dashboard, or contact the CMDB team"
                            },
                            new TroubleshootingTip
                            {
                                Issue = "I don't have permission to edit",
                                Resolution = "Request CMDB Editor access through the IT Service Portal"
                            }
                        ]
                    }
                ],
                RelatedLinks =
                [
                    new DocumentationLink
                    {
                        Title = "ServiceNow CMDB Guide",
                        Url = "https://sharepoint.company.com/docs/servicenow-cmdb-guide",
                        Description = "Complete guide to managing Configuration Items",
                        Type = DocumentationLinkType.SharePoint
                    },
                    new DocumentationLink
                    {
                        Title = "Role Definitions",
                        Url = "https://sharepoint.company.com/docs/application-roles",
                        Description = "Definitions and responsibilities for each application role",
                        Type = DocumentationLinkType.SharePoint
                    }
                ]
            },
            new TaskDocumentation
            {
                Id = "doc-security-remediation",
                TaskType = TaskType.SecurityRemediation,
                Title = "Security Remediation",
                Description = "Address security vulnerabilities identified through automated scans (CodeQL, dependency scanning, secret detection). Prioritize based on severity.",
                EstimatedDuration = TimeSpan.FromHours(4),
                Prerequisites = ["Access to Azure DevOps", "Understanding of the identified vulnerability", "Access to source code"],
                TypicalRoles = [ApplicationRole.Developer, ApplicationRole.SecurityChampion, ApplicationRole.TechnicalLead],
                LastUpdated = DateTimeOffset.UtcNow.AddDays(-15),
                LastUpdatedBy = "Security Team",
                Instructions =
                [
                    new TaskInstruction
                    {
                        StepNumber = 1,
                        Title = "Review Findings",
                        Description = "Open Azure DevOps Advanced Security and review the specific findings for your application. Note the severity, affected files, and recommended fixes.",
                        SystemReference = "Azure DevOps",
                        ActionUrl = "https://dev.azure.com/company/project/_settings/security"
                    },
                    new TaskInstruction
                    {
                        StepNumber = 2,
                        Title = "Prioritize by Severity",
                        Description = "Address Critical and High severity issues first. Low severity issues can be addressed as time permits.",
                        Notes = "Critical issues should be fixed within 7 days, High within 30 days"
                    },
                    new TaskInstruction
                    {
                        StepNumber = 3,
                        Title = "Implement Fix",
                        Description = "Make the necessary code changes to address the vulnerability. Follow the remediation guidance provided in the scan results.",
                        SystemReference = "Azure DevOps"
                    },
                    new TaskInstruction
                    {
                        StepNumber = 4,
                        Title = "Submit Pull Request",
                        Description = "Create a pull request with your fix. Include the CVE or finding ID in the PR description.",
                        SystemReference = "Azure DevOps"
                    },
                    new TaskInstruction
                    {
                        StepNumber = 5,
                        Title = "Verify Resolution",
                        Description = "After the PR is merged, verify the finding is resolved in the next scan. This may take up to 24 hours.",
                        SystemReference = "Azure DevOps"
                    }
                ],
                SystemGuidance =
                [
                    new SystemGuidance
                    {
                        SystemName = "Azure DevOps",
                        ActionType = "View Security Findings",
                        Instructions = "1. Open your repository in Azure DevOps\n2. Go to Repos > Advanced Security\n3. Review the Alerts tab for active findings\n4. Click on each alert for detailed remediation guidance\n5. Use the 'Dismiss' option only if the finding is a false positive (requires justification)",
                        DirectLink = "https://dev.azure.com/company/project/_settings/security",
                        RequiredPermissions = "Contributor access to repository"
                    }
                ],
                RelatedLinks =
                [
                    new DocumentationLink
                    {
                        Title = "Security Remediation SLA",
                        Url = "https://sharepoint.company.com/docs/security-sla",
                        Description = "Expected remediation timeframes by severity",
                        Type = DocumentationLinkType.SharePoint
                    },
                    new DocumentationLink
                    {
                        Title = "Secure Coding Guidelines",
                        Url = "https://sharepoint.company.com/docs/secure-coding",
                        Description = "Best practices for secure development",
                        Type = DocumentationLinkType.SharePoint
                    }
                ]
            },
            new TaskDocumentation
            {
                Id = "doc-documentation-review",
                TaskType = TaskType.DocumentationReview,
                Title = "Documentation Review",
                Description = "Review and update application documentation to ensure it is current, complete, and useful for support and onboarding.",
                EstimatedDuration = TimeSpan.FromHours(2),
                Prerequisites = ["Access to SharePoint documentation site", "Access to Azure DevOps repository"],
                TypicalRoles = [ApplicationRole.Owner, ApplicationRole.TechnicalLead, ApplicationRole.Developer],
                LastUpdated = DateTimeOffset.UtcNow.AddDays(-45),
                LastUpdatedBy = "Documentation Team",
                Instructions =
                [
                    new TaskInstruction
                    {
                        StepNumber = 1,
                        Title = "Review README",
                        Description = "Check the README.md file in your repository. Ensure it has been customized from the template and includes accurate setup instructions.",
                        SystemReference = "Azure DevOps"
                    },
                    new TaskInstruction
                    {
                        StepNumber = 2,
                        Title = "Check Architecture Diagram",
                        Description = "Verify an architecture diagram exists in SharePoint. If missing or outdated, create or update it.",
                        SystemReference = "SharePoint"
                    },
                    new TaskInstruction
                    {
                        StepNumber = 3,
                        Title = "Update System Documentation",
                        Description = "Review and update the system documentation page. Include deployment procedures, key contacts, and troubleshooting guides.",
                        SystemReference = "SharePoint"
                    },
                    new TaskInstruction
                    {
                        StepNumber = 4,
                        Title = "Verify Support Runbook",
                        Description = "Ensure a support runbook exists with common issues and resolutions. This is critical for after-hours support.",
                        SystemReference = "SharePoint",
                        Notes = "Contact the support team if you need the runbook template"
                    }
                ],
                SystemGuidance =
                [
                    new SystemGuidance
                    {
                        SystemName = "SharePoint",
                        ActionType = "Update Documentation",
                        Instructions = "1. Navigate to your application's documentation folder in SharePoint\n2. Edit the appropriate page or document\n3. Use the standard templates when available\n4. Include last-updated date at the bottom of each document\n5. Request review from a team member before finalizing",
                        DirectLink = "https://sharepoint.company.com/sites/AppDocs",
                        RequiredPermissions = "Edit access to the documentation site"
                    }
                ],
                RelatedLinks =
                [
                    new DocumentationLink
                    {
                        Title = "Documentation Standards",
                        Url = "https://sharepoint.company.com/docs/documentation-standards",
                        Description = "Company standards for application documentation",
                        Type = DocumentationLinkType.SharePoint
                    },
                    new DocumentationLink
                    {
                        Title = "README Template",
                        Url = "https://dev.azure.com/company/project/_git/templates?path=/README-template.md",
                        Description = "Standard README template for new applications",
                        Type = DocumentationLinkType.Template
                    }
                ]
            },
            new TaskDocumentation
            {
                Id = "doc-architecture-review",
                TaskType = TaskType.ArchitectureReview,
                Title = "Architecture Review",
                Description = "Annual review of application architecture to ensure it meets current standards, is well-documented, and identifies technical debt.",
                EstimatedDuration = TimeSpan.FromHours(8),
                Prerequisites = ["Access to architecture documentation", "Understanding of current best practices", "Time scheduled with architecture review board"],
                TypicalRoles = [ApplicationRole.TechnicalLead, ApplicationRole.Owner],
                LastUpdated = DateTimeOffset.UtcNow.AddDays(-60),
                LastUpdatedBy = "Architecture Team",
                Instructions =
                [
                    new TaskInstruction
                    {
                        StepNumber = 1,
                        Title = "Update Architecture Diagram",
                        Description = "Create or update the architecture diagram to reflect current state. Include all integrations and dependencies.",
                        SystemReference = "SharePoint"
                    },
                    new TaskInstruction
                    {
                        StepNumber = 2,
                        Title = "Document Technical Debt",
                        Description = "List known technical debt items, their impact, and estimated remediation effort.",
                        SystemReference = "Azure DevOps"
                    },
                    new TaskInstruction
                    {
                        StepNumber = 3,
                        Title = "Schedule Review Meeting",
                        Description = "Schedule time with the Architecture Review Board to present your application.",
                        Notes = "Allow at least 2 weeks lead time for scheduling"
                    },
                    new TaskInstruction
                    {
                        StepNumber = 4,
                        Title = "Present and Document",
                        Description = "Present to the review board and document any findings or recommendations.",
                        SystemReference = "SharePoint"
                    }
                ],
                SystemGuidance =
                [
                    new SystemGuidance
                    {
                        SystemName = "SharePoint",
                        ActionType = "Architecture Documentation",
                        Instructions = "Use the standard architecture template located in the Templates library. Include deployment diagrams, data flow diagrams, and integration points.",
                        DirectLink = "https://sharepoint.company.com/sites/Architecture",
                        RequiredPermissions = "Contributor access"
                    }
                ],
                RelatedLinks =
                [
                    new DocumentationLink
                    {
                        Title = "Architecture Standards",
                        Url = "https://sharepoint.company.com/sites/Architecture/standards",
                        Description = "Current architecture standards and patterns",
                        Type = DocumentationLinkType.SharePoint
                    }
                ]
            },
            new TaskDocumentation
            {
                Id = "doc-retirement-review",
                TaskType = TaskType.RetirementReview,
                Title = "Retirement Review",
                Description = "Evaluate whether an application should be retired. Consider usage, maintenance cost, and available alternatives.",
                EstimatedDuration = TimeSpan.FromHours(4),
                Prerequisites = ["Usage data access", "Cost information", "Knowledge of alternative solutions"],
                TypicalRoles = [ApplicationRole.Owner, ApplicationRole.BusinessOwner],
                LastUpdated = DateTimeOffset.UtcNow.AddDays(-90),
                LastUpdatedBy = "Portfolio Management",
                Instructions =
                [
                    new TaskInstruction
                    {
                        StepNumber = 1,
                        Title = "Analyze Usage",
                        Description = "Review the usage metrics in the Lifecycle Dashboard. Note monthly active users and request trends.",
                        SystemReference = "Lifecycle Dashboard"
                    },
                    new TaskInstruction
                    {
                        StepNumber = 2,
                        Title = "Calculate TCO",
                        Description = "Estimate total cost of ownership including hosting, maintenance, and support hours.",
                        Notes = "Contact Finance for hosting cost details"
                    },
                    new TaskInstruction
                    {
                        StepNumber = 3,
                        Title = "Identify Alternatives",
                        Description = "Research if functionality can be handled by other existing applications or modern solutions."
                    },
                    new TaskInstruction
                    {
                        StepNumber = 4,
                        Title = "Document Recommendation",
                        Description = "Create a recommendation document with your analysis and proposed action (keep, modernize, or retire).",
                        SystemReference = "SharePoint"
                    },
                    new TaskInstruction
                    {
                        StepNumber = 5,
                        Title = "Submit for Approval",
                        Description = "Submit your recommendation through the portfolio governance process.",
                        Notes = "Retirement decisions require business owner approval"
                    }
                ],
                SystemGuidance =
                [
                    new SystemGuidance
                    {
                        SystemName = "ServiceNow",
                        ActionType = "Update Lifecycle Status",
                        Instructions = "If retirement is approved, update the CI lifecycle status to 'Retiring' and set the planned retirement date.",
                        DirectLink = "https://company.service-now.com/cmdb_ci_list.do",
                        RequiredPermissions = "CMDB Editor role"
                    }
                ],
                RelatedLinks =
                [
                    new DocumentationLink
                    {
                        Title = "Retirement Process Guide",
                        Url = "https://sharepoint.company.com/docs/retirement-process",
                        Description = "Step-by-step guide for application retirement",
                        Type = DocumentationLinkType.SharePoint
                    }
                ]
            },
            new TaskDocumentation
            {
                Id = "doc-compliance-check",
                TaskType = TaskType.ComplianceCheck,
                Title = "Compliance Check",
                Description = "Verify that the application meets organizational compliance requirements including security controls and data handling standards.",
                EstimatedDuration = TimeSpan.FromHours(3),
                Prerequisites = ["Access to compliance checklist", "Understanding of data classification"],
                TypicalRoles = [ApplicationRole.SecurityChampion, ApplicationRole.Owner],
                LastUpdated = DateTimeOffset.UtcNow.AddDays(-20),
                LastUpdatedBy = "Compliance Team",
                Instructions =
                [
                    new TaskInstruction
                    {
                        StepNumber = 1,
                        Title = "Review Data Classification",
                        Description = "Verify the application's data classification is accurate in ServiceNow.",
                        SystemReference = "ServiceNow"
                    },
                    new TaskInstruction
                    {
                        StepNumber = 2,
                        Title = "Complete Compliance Checklist",
                        Description = "Work through the compliance checklist for your application's classification level.",
                        SystemReference = "SharePoint"
                    },
                    new TaskInstruction
                    {
                        StepNumber = 3,
                        Title = "Document Exceptions",
                        Description = "If any requirements cannot be met, document the exception and mitigation plan.",
                        Notes = "All exceptions require security team approval"
                    },
                    new TaskInstruction
                    {
                        StepNumber = 4,
                        Title = "Submit Evidence",
                        Description = "Upload compliance evidence to the designated SharePoint folder.",
                        SystemReference = "SharePoint"
                    }
                ],
                SystemGuidance =
                [
                    new SystemGuidance
                    {
                        SystemName = "SharePoint",
                        ActionType = "Compliance Documentation",
                        Instructions = "Upload all compliance evidence to the Compliance folder under your application's documentation site. Use the standard naming convention: [AppName]_[Year]_[CheckType].pdf",
                        DirectLink = "https://sharepoint.company.com/sites/Compliance",
                        RequiredPermissions = "Contributor access to Compliance site"
                    }
                ],
                RelatedLinks =
                [
                    new DocumentationLink
                    {
                        Title = "Compliance Requirements",
                        Url = "https://sharepoint.company.com/sites/Compliance/requirements",
                        Description = "Full list of compliance requirements by classification",
                        Type = DocumentationLinkType.SharePoint
                    }
                ]
            },
            new TaskDocumentation
            {
                Id = "doc-data-conflict",
                TaskType = TaskType.DataConflictResolution,
                Title = "Data Conflict Resolution",
                Description = "Resolve discrepancies between data sources (ServiceNow, SharePoint, Azure DevOps). Ensure all systems have consistent information.",
                EstimatedDuration = TimeSpan.FromMinutes(45),
                Prerequisites = ["Access to all relevant systems", "Knowledge of correct values"],
                TypicalRoles = [ApplicationRole.Owner, ApplicationRole.TechnicalLead],
                LastUpdated = DateTimeOffset.UtcNow.AddDays(-10),
                LastUpdatedBy = "Data Quality Team",
                Instructions =
                [
                    new TaskInstruction
                    {
                        StepNumber = 1,
                        Title = "Identify Conflicts",
                        Description = "Review the data conflicts shown in the Lifecycle Dashboard for your application.",
                        SystemReference = "Lifecycle Dashboard"
                    },
                    new TaskInstruction
                    {
                        StepNumber = 2,
                        Title = "Determine Correct Value",
                        Description = "For each conflict, determine which source has the correct information."
                    },
                    new TaskInstruction
                    {
                        StepNumber = 3,
                        Title = "Update Sources",
                        Description = "Update the incorrect source(s) to match the correct value. ServiceNow is typically the system of record.",
                        SystemReference = "ServiceNow"
                    },
                    new TaskInstruction
                    {
                        StepNumber = 4,
                        Title = "Wait for Sync",
                        Description = "Allow up to 24 hours for changes to sync across systems. Verify conflicts are resolved.",
                        Notes = "Contact the data quality team if conflicts persist after 48 hours"
                    }
                ],
                SystemGuidance =
                [
                    new SystemGuidance
                    {
                        SystemName = "ServiceNow",
                        ActionType = "Update Application Data",
                        Instructions = "ServiceNow is the system of record for application metadata. When resolving conflicts, update ServiceNow first, then ensure other systems align.",
                        DirectLink = "https://company.service-now.com/cmdb_ci_list.do",
                        RequiredPermissions = "CMDB Editor role"
                    }
                ],
                RelatedLinks =
                [
                    new DocumentationLink
                    {
                        Title = "Data Quality Guidelines",
                        Url = "https://sharepoint.company.com/docs/data-quality",
                        Description = "Guidelines for maintaining data quality across systems",
                        Type = DocumentationLinkType.SharePoint
                    }
                ]
            },
            new TaskDocumentation
            {
                Id = "doc-maintenance-review",
                TaskType = TaskType.MaintenanceReview,
                Title = "Maintenance Review",
                Description = "Review application maintenance status including dependency updates, performance, and operational health.",
                EstimatedDuration = TimeSpan.FromHours(2),
                Prerequisites = ["Access to Azure DevOps", "Access to monitoring tools"],
                TypicalRoles = [ApplicationRole.Developer, ApplicationRole.TechnicalLead],
                LastUpdated = DateTimeOffset.UtcNow.AddDays(-25),
                LastUpdatedBy = "Platform Team",
                Instructions =
                [
                    new TaskInstruction
                    {
                        StepNumber = 1,
                        Title = "Review Dependencies",
                        Description = "Check for outdated dependencies in the Repository tab. Plan updates for any packages with known vulnerabilities.",
                        SystemReference = "Lifecycle Dashboard"
                    },
                    new TaskInstruction
                    {
                        StepNumber = 2,
                        Title = "Check Build Status",
                        Description = "Review recent build history and address any failures.",
                        SystemReference = "Azure DevOps"
                    },
                    new TaskInstruction
                    {
                        StepNumber = 3,
                        Title = "Review Performance",
                        Description = "Check Application Insights for any performance degradation or errors.",
                        SystemReference = "Azure Portal",
                        IsOptional = true
                    },
                    new TaskInstruction
                    {
                        StepNumber = 4,
                        Title = "Document Action Items",
                        Description = "Create work items for any maintenance tasks identified during review.",
                        SystemReference = "Azure DevOps"
                    }
                ],
                SystemGuidance =
                [
                    new SystemGuidance
                    {
                        SystemName = "Azure DevOps",
                        ActionType = "Dependency Updates",
                        Instructions = "1. Open the repository in Azure DevOps\n2. Check the Dependabot alerts (if enabled)\n3. Create a branch for updates\n4. Update packages using dotnet outdated or npm update\n5. Run tests and submit PR",
                        DirectLink = "https://dev.azure.com/company/project/_git",
                        RequiredPermissions = "Contributor access"
                    }
                ],
                RelatedLinks =
                [
                    new DocumentationLink
                    {
                        Title = "Dependency Update Guide",
                        Url = "https://sharepoint.company.com/docs/dependency-updates",
                        Description = "Best practices for updating dependencies safely",
                        Type = DocumentationLinkType.SharePoint
                    }
                ]
            },
            new TaskDocumentation
            {
                Id = "doc-custom",
                TaskType = TaskType.Custom,
                Title = "Custom Task",
                Description = "A custom task created for specific application needs. Refer to the task description for details.",
                EstimatedDuration = null,
                Prerequisites = [],
                TypicalRoles = [ApplicationRole.Owner, ApplicationRole.TechnicalLead],
                LastUpdated = DateTimeOffset.UtcNow.AddDays(-5),
                LastUpdatedBy = "System",
                Instructions =
                [
                    new TaskInstruction
                    {
                        StepNumber = 1,
                        Title = "Review Task Description",
                        Description = "Read the task description carefully as custom tasks have unique requirements."
                    },
                    new TaskInstruction
                    {
                        StepNumber = 2,
                        Title = "Complete Required Actions",
                        Description = "Complete the actions specified in the task description."
                    },
                    new TaskInstruction
                    {
                        StepNumber = 3,
                        Title = "Document Completion",
                        Description = "Add notes documenting what was done before marking the task complete."
                    }
                ],
                SystemGuidance = [],
                RelatedLinks = []
            }
        ];
    }

    #endregion

    #region Task Documentation Update Methods

    public Task<TaskDocumentation> UpdateTaskDocumentationAsync(TaskDocumentation documentation)
    {
        var index = TaskDocumentationList.FindIndex(d => d.TaskType == documentation.TaskType);
        if (index >= 0)
        {
            TaskDocumentationList[index] = documentation with { LastUpdated = DateTimeOffset.UtcNow };
        }
        else
        {
            TaskDocumentationList.Add(documentation with { LastUpdated = DateTimeOffset.UtcNow });
        }
        return Task.FromResult(TaskDocumentationList.First(d => d.TaskType == documentation.TaskType));
    }

    public Task<TaskDocumentation> CreateTaskDocumentationAsync(TaskDocumentation documentation)
    {
        var newDoc = documentation with
        {
            Id = Guid.NewGuid().ToString(),
            LastUpdated = DateTimeOffset.UtcNow
        };
        TaskDocumentationList.Add(newDoc);
        return Task.FromResult(newDoc);
    }

    public Task DeleteTaskDocumentationAsync(string id)
    {
        TaskDocumentationList.RemoveAll(d => d.Id == id);
        return Task.CompletedTask;
    }

    #endregion

    #region Task CRUD Operations

    public Task<LifecycleTask> CreateTaskAsync(LifecycleTask task)
    {
        var newTask = task with
        {
            Id = string.IsNullOrEmpty(task.Id) ? Guid.NewGuid().ToString() : task.Id,
            CreatedDate = DateTimeOffset.UtcNow,
            History = task.History.Count == 0
                ? [new TaskHistoryEntry
                {
                    Id = Guid.NewGuid().ToString(),
                    Timestamp = DateTimeOffset.UtcNow,
                    Action = "Created",
                    PerformedBy = task.AssigneeName,
                    PerformedById = task.AssigneeId,
                    Notes = "Task created"
                }]
                : task.History
        };

        Tasks.Add(newTask);
        return Task.FromResult(newTask);
    }

    public Task DeleteTaskAsync(string taskId)
    {
        Tasks.RemoveAll(t => t.Id == taskId);
        return Task.CompletedTask;
    }

    public Task<LifecycleTask> UpdateTaskStatusAsync(string taskId, Models.TaskStatus newStatus, string performedByUserId, string performedByName, string? notes = null)
    {
        var index = Tasks.FindIndex(t => t.Id == taskId);
        if (index < 0)
            throw new KeyNotFoundException($"Task {taskId} not found");

        var task = Tasks[index];
        var oldStatus = task.Status;

        var historyEntry = new TaskHistoryEntry
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            Action = "StatusChanged",
            PerformedBy = performedByName,
            PerformedById = performedByUserId,
            OldValue = oldStatus.ToString(),
            NewValue = newStatus.ToString(),
            Notes = notes
        };

        var updatedTask = task with
        {
            Status = newStatus,
            CompletedDate = newStatus == Models.TaskStatus.Completed ? DateTimeOffset.UtcNow : task.CompletedDate,
            History = [.. task.History, historyEntry]
        };

        Tasks[index] = updatedTask;
        return Task.FromResult(updatedTask);
    }

    public Task<LifecycleTask> AssignTaskAsync(string taskId, string userId, string userName, string userEmail, string performedByUserId, string performedByName)
    {
        var index = Tasks.FindIndex(t => t.Id == taskId);
        if (index < 0)
            throw new KeyNotFoundException($"Task {taskId} not found");

        var task = Tasks[index];

        var historyEntry = new TaskHistoryEntry
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            Action = "Assigned",
            PerformedBy = performedByName,
            PerformedById = performedByUserId,
            OldValue = $"{task.AssigneeName} ({task.AssigneeId})",
            NewValue = $"{userName} ({userId})",
            Notes = $"Task assigned to {userName}"
        };

        var updatedTask = task with
        {
            AssigneeId = userId,
            AssigneeName = userName,
            AssigneeEmail = userEmail,
            History = [.. task.History, historyEntry]
        };

        Tasks[index] = updatedTask;
        return Task.FromResult(updatedTask);
    }

    public Task<LifecycleTask> DelegateTaskAsync(string taskId, string fromUserId, string toUserId, string toUserName, string toUserEmail, string reason, string performedByUserId, string performedByName)
    {
        var index = Tasks.FindIndex(t => t.Id == taskId);
        if (index < 0)
            throw new KeyNotFoundException($"Task {taskId} not found");

        var task = Tasks[index];

        var historyEntry = new TaskHistoryEntry
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            Action = "Delegated",
            PerformedBy = performedByName,
            PerformedById = performedByUserId,
            OldValue = $"{task.AssigneeName} ({task.AssigneeId})",
            NewValue = $"{toUserName} ({toUserId})",
            Notes = $"Delegated from {task.AssigneeName}: {reason}"
        };

        var updatedTask = task with
        {
            AssigneeId = toUserId,
            AssigneeName = toUserName,
            AssigneeEmail = toUserEmail,
            OriginalAssigneeId = task.OriginalAssigneeId ?? fromUserId,
            DelegationReason = reason,
            History = [.. task.History, historyEntry]
        };

        Tasks[index] = updatedTask;
        return Task.FromResult(updatedTask);
    }

    public Task<LifecycleTask> EscalateTaskAsync(string taskId, string reason, string performedByUserId, string performedByName)
    {
        var index = Tasks.FindIndex(t => t.Id == taskId);
        if (index < 0)
            throw new KeyNotFoundException($"Task {taskId} not found");

        var task = Tasks[index];

        var historyEntry = new TaskHistoryEntry
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            Action = "Escalated",
            PerformedBy = performedByName,
            PerformedById = performedByUserId,
            Notes = reason
        };

        var updatedTask = task with
        {
            IsEscalated = true,
            EscalatedDate = DateTimeOffset.UtcNow,
            History = [.. task.History, historyEntry]
        };

        Tasks[index] = updatedTask;
        return Task.FromResult(updatedTask);
    }

    public Task<LifecycleTask> CompleteTaskAsync(string taskId, string performedByUserId, string performedByName, string? notes = null)
    {
        return UpdateTaskStatusAsync(taskId, Models.TaskStatus.Completed, performedByUserId, performedByName, notes);
    }

    public Task<LifecycleTask> AddTaskNoteAsync(string taskId, string performedByUserId, string performedByName, string note)
    {
        var index = Tasks.FindIndex(t => t.Id == taskId);
        if (index < 0)
            throw new KeyNotFoundException($"Task {taskId} not found");

        var task = Tasks[index];

        var historyEntry = new TaskHistoryEntry
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            Action = "NoteAdded",
            PerformedBy = performedByName,
            PerformedById = performedByUserId,
            Notes = note
        };

        var newNotes = string.IsNullOrEmpty(task.Notes)
            ? $"[{performedByName} - {DateTimeOffset.UtcNow:MMM d, yyyy h:mm tt}]\n{note}"
            : $"{task.Notes}\n\n[{performedByName} - {DateTimeOffset.UtcNow:MMM d, yyyy h:mm tt}]\n{note}";

        var updatedTask = task with
        {
            Notes = newNotes,
            History = [.. task.History, historyEntry]
        };

        Tasks[index] = updatedTask;
        return Task.FromResult(updatedTask);
    }

    public Task<IReadOnlyList<LifecycleTask>> GetAllTasksAsync()
    {
        return Task.FromResult<IReadOnlyList<LifecycleTask>>(Tasks.ToList());
    }

    #endregion

    #region Framework Version Methods

    public Task<IReadOnlyList<FrameworkVersion>> GetAllFrameworkVersionsAsync()
    {
        return Task.FromResult<IReadOnlyList<FrameworkVersion>>(
            FrameworkVersions.OrderBy(f => f.Framework).ThenByDescending(f => f.Version).ToList());
    }

    public Task<FrameworkVersion?> GetFrameworkVersionAsync(string id)
    {
        var version = FrameworkVersions.FirstOrDefault(f => f.Id == id);
        return Task.FromResult(version);
    }

    public Task<IReadOnlyList<FrameworkVersion>> GetFrameworkVersionsByTypeAsync(FrameworkType type)
    {
        var versions = FrameworkVersions.Where(f => f.Framework == type)
            .OrderByDescending(f => f.Version).ToList();
        return Task.FromResult<IReadOnlyList<FrameworkVersion>>(versions);
    }

    public Task<FrameworkVersion> UpdateFrameworkVersionAsync(FrameworkVersion version)
    {
        var index = FrameworkVersions.FindIndex(f => f.Id == version.Id);
        if (index >= 0)
        {
            FrameworkVersions[index] = version with { LastUpdated = DateTimeOffset.UtcNow };
        }
        return Task.FromResult(FrameworkVersions.First(f => f.Id == version.Id));
    }

    public Task<FrameworkVersion> CreateFrameworkVersionAsync(FrameworkVersion version)
    {
        var newVersion = version with
        {
            Id = Guid.NewGuid().ToString(),
            LastUpdated = DateTimeOffset.UtcNow
        };
        FrameworkVersions.Add(newVersion);
        return Task.FromResult(newVersion);
    }

    public Task DeleteFrameworkVersionAsync(string id)
    {
        FrameworkVersions.RemoveAll(f => f.Id == id);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Application>> GetApplicationsByFrameworkAsync(string frameworkVersionId)
    {
        var framework = FrameworkVersions.FirstOrDefault(f => f.Id == frameworkVersionId);
        if (framework == null)
            return Task.FromResult<IReadOnlyList<Application>>([]);

        // For mock data, match applications by their technology stack containing the framework
        var matchingApps = Applications.Where(a =>
            a.TechnologyStack.Any(t =>
                t.Contains(framework.DisplayName, StringComparison.OrdinalIgnoreCase) ||
                t.Contains(framework.Version, StringComparison.OrdinalIgnoreCase))).ToList();

        return Task.FromResult<IReadOnlyList<Application>>(matchingApps);
    }

    public Task<FrameworkEolSummary> GetFrameworkEolSummaryAsync()
    {
        var eolFrameworks = FrameworkVersions.Where(f => f.IsPastEol || f.IsApproachingEol).ToList();
        var details = eolFrameworks.Select(f =>
        {
            var apps = Applications.Where(a =>
                a.TechnologyStack.Any(t =>
                    t.Contains(f.DisplayName, StringComparison.OrdinalIgnoreCase) ||
                    t.Contains(f.Version, StringComparison.OrdinalIgnoreCase))).ToList();

            return new FrameworkEolDetail
            {
                Framework = f,
                ApplicationCount = apps.Count,
                ApplicationNames = apps.Select(a => a.Name).ToList()
            };
        }).Where(d => d.ApplicationCount > 0).ToList();

        var summary = new FrameworkEolSummary
        {
            TotalApplications = Applications.Count,
            ApplicationsWithEolFrameworks = details.Where(d => d.Framework.IsPastEol).Sum(d => d.ApplicationCount),
            ApplicationsApproachingEol = details.Where(d => d.Framework.IsApproachingEol).Sum(d => d.ApplicationCount),
            CriticalEolCount = details.Count(d => d.Framework.EolUrgency == EolUrgency.Critical),
            HighEolCount = details.Count(d => d.Framework.EolUrgency == EolUrgency.High),
            MediumEolCount = details.Count(d => d.Framework.EolUrgency == EolUrgency.Medium),
            Details = details.OrderBy(d => d.Framework.DaysUntilEol ?? int.MaxValue).ToList()
        };

        return Task.FromResult(summary);
    }

    private List<FrameworkVersion> GenerateMockFrameworkVersions()
    {
        var now = DateTimeOffset.UtcNow;

        return
        [
            // Modern .NET versions
            new FrameworkVersion
            {
                Id = "dotnet-10",
                Framework = FrameworkType.DotNet,
                Version = "10.0",
                DisplayName = ".NET 10",
                ReleaseDate = new DateTimeOffset(2025, 11, 11, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = new DateTimeOffset(2028, 11, 14, 0, 0, 0, TimeSpan.Zero),
                IsLts = true,
                Status = SupportStatus.Active,
                LatestPatchVersion = "10.0.1",
                RecommendedUpgradePath = null,
                Notes = "Current LTS release",
                LastUpdated = now
            },
            new FrameworkVersion
            {
                Id = "dotnet-9",
                Framework = FrameworkType.DotNet,
                Version = "9.0",
                DisplayName = ".NET 9",
                ReleaseDate = new DateTimeOffset(2024, 11, 12, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = new DateTimeOffset(2026, 11, 10, 0, 0, 0, TimeSpan.Zero),
                IsLts = false,
                Status = SupportStatus.Active,
                LatestPatchVersion = "9.0.11",
                RecommendedUpgradePath = ".NET 10",
                Notes = "Standard Term Support release",
                LastUpdated = now
            },
            new FrameworkVersion
            {
                Id = "dotnet-8",
                Framework = FrameworkType.DotNet,
                Version = "8.0",
                DisplayName = ".NET 8",
                ReleaseDate = new DateTimeOffset(2023, 11, 14, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = new DateTimeOffset(2026, 11, 10, 0, 0, 0, TimeSpan.Zero),
                IsLts = true,
                Status = SupportStatus.Active,
                LatestPatchVersion = "8.0.22",
                RecommendedUpgradePath = ".NET 10",
                Notes = "LTS release, widely adopted",
                LastUpdated = now
            },
            new FrameworkVersion
            {
                Id = "dotnet-7",
                Framework = FrameworkType.DotNet,
                Version = "7.0",
                DisplayName = ".NET 7",
                ReleaseDate = new DateTimeOffset(2022, 11, 8, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = new DateTimeOffset(2024, 5, 14, 0, 0, 0, TimeSpan.Zero),
                IsLts = false,
                Status = SupportStatus.EndOfLife,
                LatestPatchVersion = "7.0.20",
                RecommendedUpgradePath = ".NET 8",
                Notes = "End of support - upgrade recommended",
                LastUpdated = now
            },
            new FrameworkVersion
            {
                Id = "dotnet-6",
                Framework = FrameworkType.DotNet,
                Version = "6.0",
                DisplayName = ".NET 6",
                ReleaseDate = new DateTimeOffset(2021, 11, 8, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = new DateTimeOffset(2024, 11, 12, 0, 0, 0, TimeSpan.Zero),
                IsLts = true,
                Status = SupportStatus.EndOfLife,
                LatestPatchVersion = "6.0.36",
                RecommendedUpgradePath = ".NET 8",
                Notes = "End of support - upgrade to .NET 8 recommended",
                LastUpdated = now
            },
            new FrameworkVersion
            {
                Id = "dotnet-5",
                Framework = FrameworkType.DotNet,
                Version = "5.0",
                DisplayName = ".NET 5",
                ReleaseDate = new DateTimeOffset(2020, 11, 10, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = new DateTimeOffset(2022, 5, 10, 0, 0, 0, TimeSpan.Zero),
                IsLts = false,
                Status = SupportStatus.EndOfLife,
                LatestPatchVersion = "5.0.17",
                RecommendedUpgradePath = ".NET 8",
                Notes = "End of support - upgrade to .NET 8 recommended",
                LastUpdated = now
            },
            new FrameworkVersion
            {
                Id = "dotnet-core-31",
                Framework = FrameworkType.DotNet,
                Version = "3.1",
                DisplayName = ".NET Core 3.1",
                ReleaseDate = new DateTimeOffset(2019, 12, 3, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = new DateTimeOffset(2022, 12, 13, 0, 0, 0, TimeSpan.Zero),
                IsLts = true,
                Status = SupportStatus.EndOfLife,
                LatestPatchVersion = "3.1.32",
                RecommendedUpgradePath = ".NET 8",
                Notes = "End of support - upgrade to .NET 8 recommended",
                LastUpdated = now
            },
            new FrameworkVersion
            {
                Id = "dotnet-core-21",
                Framework = FrameworkType.DotNet,
                Version = "2.1",
                DisplayName = ".NET Core 2.1",
                ReleaseDate = new DateTimeOffset(2018, 5, 30, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = new DateTimeOffset(2021, 8, 21, 0, 0, 0, TimeSpan.Zero),
                IsLts = true,
                Status = SupportStatus.EndOfLife,
                LatestPatchVersion = "2.1.30",
                RecommendedUpgradePath = ".NET 8",
                Notes = "End of support - significant upgrade required",
                LastUpdated = now
            },

            // .NET Framework versions
            new FrameworkVersion
            {
                Id = "netfx-481",
                Framework = FrameworkType.DotNetFramework,
                Version = "4.8.1",
                DisplayName = ".NET Framework 4.8.1",
                ReleaseDate = new DateTimeOffset(2022, 8, 9, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = null, // Tied to Windows lifecycle
                IsLts = true,
                Status = SupportStatus.Active,
                LatestPatchVersion = "4.8.1",
                RecommendedUpgradePath = ".NET 8 (migration)",
                Notes = "Latest .NET Framework - indefinite support tied to Windows",
                LastUpdated = now
            },
            new FrameworkVersion
            {
                Id = "netfx-48",
                Framework = FrameworkType.DotNetFramework,
                Version = "4.8",
                DisplayName = ".NET Framework 4.8",
                ReleaseDate = new DateTimeOffset(2019, 4, 18, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = null, // Tied to Windows lifecycle
                IsLts = true,
                Status = SupportStatus.Active,
                LatestPatchVersion = "4.8",
                RecommendedUpgradePath = ".NET 8 (migration)",
                Notes = "Stable - indefinite support tied to Windows",
                LastUpdated = now
            },
            new FrameworkVersion
            {
                Id = "netfx-472",
                Framework = FrameworkType.DotNetFramework,
                Version = "4.7.2",
                DisplayName = ".NET Framework 4.7.2",
                ReleaseDate = new DateTimeOffset(2018, 4, 30, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = null,
                IsLts = true,
                Status = SupportStatus.Active,
                LatestPatchVersion = "4.7.2",
                RecommendedUpgradePath = ".NET Framework 4.8",
                Notes = "Supported - consider upgrade to 4.8",
                LastUpdated = now
            },
            new FrameworkVersion
            {
                Id = "netfx-462",
                Framework = FrameworkType.DotNetFramework,
                Version = "4.6.2",
                DisplayName = ".NET Framework 4.6.2",
                ReleaseDate = new DateTimeOffset(2016, 8, 2, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = new DateTimeOffset(2027, 1, 12, 0, 0, 0, TimeSpan.Zero),
                IsLts = true,
                Status = SupportStatus.Maintenance,
                LatestPatchVersion = "4.6.2",
                RecommendedUpgradePath = ".NET Framework 4.8",
                Notes = "Extended support until 2027",
                LastUpdated = now
            },
            new FrameworkVersion
            {
                Id = "netfx-461",
                Framework = FrameworkType.DotNetFramework,
                Version = "4.6.1",
                DisplayName = ".NET Framework 4.6.1",
                ReleaseDate = new DateTimeOffset(2015, 11, 30, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = new DateTimeOffset(2022, 4, 26, 0, 0, 0, TimeSpan.Zero),
                IsLts = false,
                Status = SupportStatus.EndOfLife,
                LatestPatchVersion = "4.6.1",
                RecommendedUpgradePath = ".NET Framework 4.8",
                Notes = "End of support - upgrade required",
                LastUpdated = now
            },
            new FrameworkVersion
            {
                Id = "netfx-452",
                Framework = FrameworkType.DotNetFramework,
                Version = "4.5.2",
                DisplayName = ".NET Framework 4.5.2",
                ReleaseDate = new DateTimeOffset(2014, 5, 5, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = new DateTimeOffset(2022, 4, 26, 0, 0, 0, TimeSpan.Zero),
                IsLts = false,
                Status = SupportStatus.EndOfLife,
                LatestPatchVersion = "4.5.2",
                RecommendedUpgradePath = ".NET Framework 4.8",
                Notes = "End of support - upgrade required",
                LastUpdated = now
            },
            new FrameworkVersion
            {
                Id = "netfx-35",
                Framework = FrameworkType.DotNetFramework,
                Version = "3.5",
                DisplayName = ".NET Framework 3.5 SP1",
                ReleaseDate = new DateTimeOffset(2007, 11, 19, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = new DateTimeOffset(2029, 1, 9, 0, 0, 0, TimeSpan.Zero),
                IsLts = true,
                Status = SupportStatus.Maintenance,
                LatestPatchVersion = "3.5 SP1",
                RecommendedUpgradePath = ".NET Framework 4.8 or .NET 8",
                Notes = "Legacy - support tied to Windows lifecycle",
                LastUpdated = now
            },

            // Python versions
            new FrameworkVersion
            {
                Id = "python-314",
                Framework = FrameworkType.Python,
                Version = "3.14",
                DisplayName = "Python 3.14",
                ReleaseDate = new DateTimeOffset(2025, 10, 7, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = new DateTimeOffset(2030, 10, 31, 0, 0, 0, TimeSpan.Zero),
                EndOfActiveSupportDate = new DateTimeOffset(2027, 10, 1, 0, 0, 0, TimeSpan.Zero),
                IsLts = false,
                Status = SupportStatus.Active,
                LatestPatchVersion = "3.14.2",
                Notes = "Latest Python release",
                LastUpdated = now
            },
            new FrameworkVersion
            {
                Id = "python-313",
                Framework = FrameworkType.Python,
                Version = "3.13",
                DisplayName = "Python 3.13",
                ReleaseDate = new DateTimeOffset(2024, 10, 7, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = new DateTimeOffset(2029, 10, 31, 0, 0, 0, TimeSpan.Zero),
                EndOfActiveSupportDate = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero),
                IsLts = false,
                Status = SupportStatus.Active,
                LatestPatchVersion = "3.13.11",
                Notes = "Current stable release",
                LastUpdated = now
            },
            new FrameworkVersion
            {
                Id = "python-312",
                Framework = FrameworkType.Python,
                Version = "3.12",
                DisplayName = "Python 3.12",
                ReleaseDate = new DateTimeOffset(2023, 10, 2, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = new DateTimeOffset(2028, 10, 31, 0, 0, 0, TimeSpan.Zero),
                EndOfActiveSupportDate = new DateTimeOffset(2025, 4, 2, 0, 0, 0, TimeSpan.Zero),
                IsLts = false,
                Status = SupportStatus.Active,
                LatestPatchVersion = "3.12.12",
                Notes = "Widely adopted version",
                LastUpdated = now
            },
            new FrameworkVersion
            {
                Id = "python-311",
                Framework = FrameworkType.Python,
                Version = "3.11",
                DisplayName = "Python 3.11",
                ReleaseDate = new DateTimeOffset(2022, 10, 24, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = new DateTimeOffset(2027, 10, 31, 0, 0, 0, TimeSpan.Zero),
                EndOfActiveSupportDate = new DateTimeOffset(2024, 4, 1, 0, 0, 0, TimeSpan.Zero),
                IsLts = false,
                Status = SupportStatus.Maintenance,
                LatestPatchVersion = "3.11.14",
                RecommendedUpgradePath = "Python 3.12",
                Notes = "Security fixes only",
                LastUpdated = now
            },
            new FrameworkVersion
            {
                Id = "python-310",
                Framework = FrameworkType.Python,
                Version = "3.10",
                DisplayName = "Python 3.10",
                ReleaseDate = new DateTimeOffset(2021, 10, 4, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = new DateTimeOffset(2026, 10, 31, 0, 0, 0, TimeSpan.Zero),
                EndOfActiveSupportDate = new DateTimeOffset(2023, 4, 5, 0, 0, 0, TimeSpan.Zero),
                IsLts = false,
                Status = SupportStatus.Maintenance,
                LatestPatchVersion = "3.10.19",
                RecommendedUpgradePath = "Python 3.12",
                Notes = "Security fixes only",
                LastUpdated = now
            },
            new FrameworkVersion
            {
                Id = "python-39",
                Framework = FrameworkType.Python,
                Version = "3.9",
                DisplayName = "Python 3.9",
                ReleaseDate = new DateTimeOffset(2020, 10, 5, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = new DateTimeOffset(2025, 10, 31, 0, 0, 0, TimeSpan.Zero),
                EndOfActiveSupportDate = new DateTimeOffset(2022, 5, 17, 0, 0, 0, TimeSpan.Zero),
                IsLts = false,
                Status = SupportStatus.Maintenance,
                LatestPatchVersion = "3.9.25",
                RecommendedUpgradePath = "Python 3.12",
                Notes = "Approaching EOL - plan upgrade",
                LastUpdated = now
            },
            new FrameworkVersion
            {
                Id = "python-38",
                Framework = FrameworkType.Python,
                Version = "3.8",
                DisplayName = "Python 3.8",
                ReleaseDate = new DateTimeOffset(2019, 10, 14, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = new DateTimeOffset(2024, 10, 7, 0, 0, 0, TimeSpan.Zero),
                EndOfActiveSupportDate = new DateTimeOffset(2021, 5, 3, 0, 0, 0, TimeSpan.Zero),
                IsLts = false,
                Status = SupportStatus.EndOfLife,
                LatestPatchVersion = "3.8.20",
                RecommendedUpgradePath = "Python 3.12",
                Notes = "End of life - upgrade required",
                LastUpdated = now
            },
            new FrameworkVersion
            {
                Id = "python-27",
                Framework = FrameworkType.Python,
                Version = "2.7",
                DisplayName = "Python 2.7",
                ReleaseDate = new DateTimeOffset(2010, 7, 3, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero),
                IsLts = false,
                Status = SupportStatus.EndOfLife,
                LatestPatchVersion = "2.7.18",
                RecommendedUpgradePath = "Python 3.12",
                Notes = "Legacy - migration to Python 3 required",
                LastUpdated = now
            },

            // R versions (simplified - R has different versioning)
            new FrameworkVersion
            {
                Id = "r-44",
                Framework = FrameworkType.R,
                Version = "4.4",
                DisplayName = "R 4.4",
                ReleaseDate = new DateTimeOffset(2024, 4, 24, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = null,
                IsLts = false,
                Status = SupportStatus.Active,
                LatestPatchVersion = "4.4.2",
                Notes = "Current stable release",
                LastUpdated = now
            },
            new FrameworkVersion
            {
                Id = "r-43",
                Framework = FrameworkType.R,
                Version = "4.3",
                DisplayName = "R 4.3",
                ReleaseDate = new DateTimeOffset(2023, 4, 21, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = null,
                IsLts = false,
                Status = SupportStatus.Maintenance,
                LatestPatchVersion = "4.3.3",
                RecommendedUpgradePath = "R 4.4",
                Notes = "Previous stable - upgrade when convenient",
                LastUpdated = now
            },

            // Node.js for Aurelia apps
            new FrameworkVersion
            {
                Id = "nodejs-22",
                Framework = FrameworkType.NodeJs,
                Version = "22",
                DisplayName = "Node.js 22",
                ReleaseDate = new DateTimeOffset(2024, 4, 24, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = new DateTimeOffset(2027, 4, 30, 0, 0, 0, TimeSpan.Zero),
                IsLts = true,
                Status = SupportStatus.Active,
                LatestPatchVersion = "22.13.0",
                Notes = "Current LTS release",
                LastUpdated = now
            },
            new FrameworkVersion
            {
                Id = "nodejs-20",
                Framework = FrameworkType.NodeJs,
                Version = "20",
                DisplayName = "Node.js 20",
                ReleaseDate = new DateTimeOffset(2023, 4, 18, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = new DateTimeOffset(2026, 4, 30, 0, 0, 0, TimeSpan.Zero),
                IsLts = true,
                Status = SupportStatus.Active,
                LatestPatchVersion = "20.18.2",
                RecommendedUpgradePath = "Node.js 22",
                Notes = "Active LTS release",
                LastUpdated = now
            },
            new FrameworkVersion
            {
                Id = "nodejs-18",
                Framework = FrameworkType.NodeJs,
                Version = "18",
                DisplayName = "Node.js 18",
                ReleaseDate = new DateTimeOffset(2022, 4, 19, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = new DateTimeOffset(2025, 4, 30, 0, 0, 0, TimeSpan.Zero),
                IsLts = true,
                Status = SupportStatus.Maintenance,
                LatestPatchVersion = "18.20.5",
                RecommendedUpgradePath = "Node.js 22",
                Notes = "Maintenance LTS - approaching EOL",
                LastUpdated = now
            },
            new FrameworkVersion
            {
                Id = "nodejs-16",
                Framework = FrameworkType.NodeJs,
                Version = "16",
                DisplayName = "Node.js 16",
                ReleaseDate = new DateTimeOffset(2021, 4, 20, 0, 0, 0, TimeSpan.Zero),
                EndOfLifeDate = new DateTimeOffset(2023, 9, 11, 0, 0, 0, TimeSpan.Zero),
                IsLts = true,
                Status = SupportStatus.EndOfLife,
                LatestPatchVersion = "16.20.2",
                RecommendedUpgradePath = "Node.js 22",
                Notes = "End of life - upgrade required",
                LastUpdated = now
            }
        ];
    }

    #endregion

    #region User Management

    public Task<User?> GetUserAsync(string userId)
    {
        var user = Users.FirstOrDefault(u => u.Id == userId);
        return Task.FromResult(user);
    }

    public Task<User> CreateUserAsync(User user)
    {
        var newUser = user with { Id = Guid.NewGuid().ToString() };
        Users.Add(newUser);

        // Record audit log
        RecordAuditLogAsync(new AuditLogEntry
        {
            Id = Guid.NewGuid().ToString(),
            EventType = "UserCreated",
            Category = "User",
            Message = $"User '{newUser.Name}' was created with role {newUser.Role}",
            UserId = "system",
            UserName = "System",
            EntityType = "User",
            EntityId = newUser.Id
        });

        return Task.FromResult(newUser);
    }

    public Task<User> UpdateUserAsync(User user)
    {
        var index = Users.FindIndex(u => u.Id == user.Id);
        if (index >= 0)
        {
            var oldUser = Users[index];
            Users[index] = user;

            // Record audit log
            RecordAuditLogAsync(new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                EventType = "UserUpdated",
                Category = "User",
                Message = $"User '{user.Name}' was updated",
                UserId = "system",
                UserName = "System",
                EntityType = "User",
                EntityId = user.Id,
                Details = new Dictionary<string, string>
                {
                    { "PreviousRole", oldUser.Role.ToString() },
                    { "NewRole", user.Role.ToString() }
                }
            });
        }
        return Task.FromResult(user);
    }

    public Task DeleteUserAsync(string userId)
    {
        var user = Users.FirstOrDefault(u => u.Id == userId);
        if (user != null)
        {
            Users.Remove(user);

            RecordAuditLogAsync(new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                EventType = "UserDeleted",
                Category = "User",
                Message = $"User '{user.Name}' was deleted",
                UserId = "system",
                UserName = "System",
                EntityType = "User",
                EntityId = userId
            });
        }
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Application>> GetApplicationsForUserAsync(string userId)
    {
        var apps = Applications
            .Where(a => a.RoleAssignments.Any(r => r.UserId == userId))
            .ToList();
        return Task.FromResult<IReadOnlyList<Application>>(apps);
    }

    #endregion

    #region Task Settings

    private TaskSchedulingConfig _taskSchedulingConfig = new();

    public Task<TaskSchedulingConfig> GetTaskSchedulingConfigAsync()
    {
        return Task.FromResult(_taskSchedulingConfig);
    }

    public Task<TaskSchedulingConfig> UpdateTaskSchedulingConfigAsync(TaskSchedulingConfig config)
    {
        var oldConfig = _taskSchedulingConfig;
        _taskSchedulingConfig = config with { LastUpdated = DateTimeOffset.UtcNow };

        RecordAuditLogAsync(new AuditLogEntry
        {
            Id = Guid.NewGuid().ToString(),
            EventType = "ConfigUpdated",
            Category = "Config",
            Message = "Task scheduling configuration was updated",
            UserId = config.UpdatedBy ?? "system",
            UserName = config.UpdatedBy ?? "System",
            EntityType = "TaskSchedulingConfig",
            EntityId = "singleton",
            Details = new Dictionary<string, string>
            {
                { "RoleValidationFrequency", $"{oldConfig.RoleValidationFrequencyDays} -> {config.RoleValidationFrequencyDays} days" },
                { "EscalationDays", $"{oldConfig.EscalationDaysAfterOverdue} -> {config.EscalationDaysAfterOverdue} days" }
            }
        });

        return Task.FromResult(_taskSchedulingConfig);
    }

    #endregion

    #region Audit Log

    private readonly List<AuditLogEntry> _auditLog = InitializeAuditLog();

    private static List<AuditLogEntry> InitializeAuditLog()
    {
        var now = DateTimeOffset.UtcNow;
        return
        [
            new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                EventType = "DataSync",
                Category = "Sync",
                Message = "Weekly data sync completed successfully - 40 applications updated",
                UserId = "system",
                UserName = "System",
                Timestamp = now.AddHours(-2)
            },
            new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                EventType = "ConfigUpdated",
                Category = "Config",
                Message = "Health scoring weights updated - Critical vulnerability penalty changed from -12 to -15",
                UserId = "user-001",
                UserName = "John Admin",
                Timestamp = now.AddDays(-1),
                EntityType = "HealthScoringConfig"
            },
            new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                EventType = "UserAssigned",
                Category = "User",
                Message = "User 'Jane Developer' added to application 'Customer Portal'",
                UserId = "user-001",
                UserName = "John Admin",
                Timestamp = now.AddDays(-2),
                EntityType = "Application",
                EntityId = "app-001"
            },
            new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                EventType = "TasksCreated",
                Category = "Task",
                Message = "Bulk task assignment completed - 15 role validation tasks created",
                UserId = "system",
                UserName = "System",
                Timestamp = now.AddDays(-3)
            },
            new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                EventType = "ConfigUpdated",
                Category = "Config",
                Message = "Task escalation rules updated - Now escalates after 14 days overdue",
                UserId = "user-001",
                UserName = "John Admin",
                Timestamp = now.AddDays(-5),
                EntityType = "TaskSchedulingConfig"
            },
            new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                EventType = "DataSync",
                Category = "Sync",
                Message = "ServiceNow import completed - 12 new security findings added",
                UserId = "system",
                UserName = "System",
                Timestamp = now.AddDays(-7)
            },
            new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                EventType = "UserUpdated",
                Category = "User",
                Message = "User 'Bob Manager' role changed to Administrator",
                UserId = "user-admin",
                UserName = "System Admin",
                Timestamp = now.AddDays(-10),
                EntityType = "User",
                EntityId = "user-002"
            },
            new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                EventType = "ConfigUpdated",
                Category = "Config",
                Message = "Documentation scoring enabled - +10 points for complete documentation",
                UserId = "user-001",
                UserName = "John Admin",
                Timestamp = now.AddDays(-14),
                EntityType = "HealthScoringConfig"
            }
        ];
    }

    public Task<IReadOnlyList<AuditLogEntry>> GetAuditLogAsync(AuditLogFilter? filter = null)
    {
        var entries = _auditLog.AsEnumerable();

        if (filter != null)
        {
            if (!string.IsNullOrEmpty(filter.Category))
                entries = entries.Where(e => e.Category.Equals(filter.Category, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(filter.EventType))
                entries = entries.Where(e => e.EventType.Equals(filter.EventType, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(filter.UserId))
                entries = entries.Where(e => e.UserId == filter.UserId);

            if (filter.StartDate.HasValue)
                entries = entries.Where(e => e.Timestamp >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                entries = entries.Where(e => e.Timestamp <= filter.EndDate.Value);

            if (filter.Limit.HasValue)
                entries = entries.Take(filter.Limit.Value);
        }

        var result = entries.OrderByDescending(e => e.Timestamp).ToList();
        return Task.FromResult<IReadOnlyList<AuditLogEntry>>(result);
    }

    public Task RecordAuditLogAsync(AuditLogEntry entry)
    {
        _auditLog.Add(entry);
        return Task.CompletedTask;
    }

    #endregion

    #region System Settings

    private SystemSettings _systemSettings = new();
    private readonly List<DataSourceConfig> _dataSourceConfigs = InitializeDataSourceConfigs();

    private static List<DataSourceConfig> InitializeDataSourceConfigs()
    {
        return
        [
            new DataSourceConfig
            {
                Id = "ds-azure-devops",
                Name = "Azure DevOps",
                Type = DataSourceType.AzureDevOps,
                IsEnabled = false,
                IsConnected = false,
                ConnectionSettings = new DataSourceConnectionSettings
                {
                    Organization = "",
                    Project = "",
                    ApiKey = ""
                },
                LastSyncStatus = "Not configured"
            },
            new DataSourceConfig
            {
                Id = "ds-sharepoint",
                Name = "SharePoint",
                Type = DataSourceType.SharePoint,
                IsEnabled = false,
                IsConnected = false,
                ConnectionSettings = new DataSourceConnectionSettings
                {
                    SiteUrl = "",
                    RootPath = "Documents/general/Offerings"
                },
                LastSyncStatus = "Not configured"
            },
            new DataSourceConfig
            {
                Id = "ds-servicenow",
                Name = "ServiceNow",
                Type = DataSourceType.ServiceNow,
                IsEnabled = false,
                IsConnected = false,
                ConnectionSettings = new DataSourceConnectionSettings
                {
                    Instance = "",
                    ApiKey = ""
                },
                LastSyncStatus = "Not configured"
            },
            new DataSourceConfig
            {
                Id = "ds-iis",
                Name = "IIS Database",
                Type = DataSourceType.IisDatabase,
                IsEnabled = false,
                IsConnected = false,
                ConnectionSettings = new DataSourceConnectionSettings
                {
                    ConnectionString = ""
                },
                LastSyncStatus = "Not configured"
            }
        ];
    }

    public Task<SystemSettings> GetSystemSettingsAsync()
    {
        return Task.FromResult(_systemSettings);
    }

    public Task<SystemSettings> UpdateSystemSettingsAsync(SystemSettings settings)
    {
        _systemSettings = settings with { LastUpdated = DateTimeOffset.UtcNow };

        RecordAuditLogAsync(new AuditLogEntry
        {
            Id = Guid.NewGuid().ToString(),
            EventType = "ConfigUpdated",
            Category = "Config",
            Message = "System settings were updated",
            UserId = "system",
            UserName = "System",
            EntityType = "SystemSettings"
        });

        return Task.FromResult(_systemSettings);
    }

    public Task<IReadOnlyList<DataSourceConfig>> GetDataSourceConfigsAsync()
    {
        return Task.FromResult<IReadOnlyList<DataSourceConfig>>(_dataSourceConfigs);
    }

    public Task<DataSourceConfig> UpdateDataSourceConfigAsync(DataSourceConfig config)
    {
        var index = _dataSourceConfigs.FindIndex(c => c.Id == config.Id);
        if (index >= 0)
        {
            _dataSourceConfigs[index] = config;

            RecordAuditLogAsync(new AuditLogEntry
            {
                Id = Guid.NewGuid().ToString(),
                EventType = "DataSourceUpdated",
                Category = "Config",
                Message = $"Data source '{config.Name}' configuration was updated",
                UserId = "system",
                UserName = "System",
                EntityType = "DataSourceConfig",
                EntityId = config.Id
            });
        }
        return Task.FromResult(config);
    }

    public async Task<DataSourceTestResult> TestDataSourceConnectionAsync(string dataSourceId)
    {
        var config = _dataSourceConfigs.FirstOrDefault(c => c.Id == dataSourceId);
        if (config == null)
        {
            return new DataSourceTestResult
            {
                Success = false,
                Message = "Data source not found"
            };
        }

        // Simulate connection test delay
        await Task.Delay(500);

        // In mock mode, always return a "not connected" result with helpful message
        return new DataSourceTestResult
        {
            Success = false,
            Message = $"Mock mode: {config.Name} connection test simulated. Configure real credentials in production.",
            ResponseTime = TimeSpan.FromMilliseconds(500)
        };
    }

    #endregion

    #region Synced Repository Storage

    private readonly List<SyncedRepository> _syncedRepositories = [];

    public Task<IReadOnlyList<SyncedRepository>> GetSyncedRepositoriesAsync()
    {
        return Task.FromResult<IReadOnlyList<SyncedRepository>>(
            _syncedRepositories.OrderBy(r => r.Name).ToList());
    }

    public Task StoreSyncedRepositoriesAsync(IEnumerable<SyncedRepository> repositories)
    {
        foreach (var repo in repositories)
        {
            var existingIndex = _syncedRepositories.FindIndex(r => r.Id == repo.Id);
            if (existingIndex >= 0)
            {
                _syncedRepositories[existingIndex] = repo;
            }
            else
            {
                _syncedRepositories.Add(repo);
            }
        }

        RecordAuditLogAsync(new AuditLogEntry
        {
            Id = Guid.NewGuid().ToString(),
            EventType = "RepositoriesSynced",
            Category = "Sync",
            Message = $"Synced {repositories.Count()} repositories from Azure DevOps",
            UserId = "system",
            UserName = "System",
            EntityType = "Repository"
        });

        return Task.CompletedTask;
    }

    public Task<SyncedRepository?> GetSyncedRepositoryAsync(string repositoryId)
    {
        var repo = _syncedRepositories.FirstOrDefault(r => r.Id == repositoryId);
        return Task.FromResult(repo);
    }

    public Task ClearSyncedRepositoriesAsync()
    {
        _syncedRepositories.Clear();
        return Task.CompletedTask;
    }

    #endregion

    #region ServiceNow Application Storage

    private readonly List<ImportedServiceNowApplication> _importedServiceNowApps = [];
    private ServiceNowColumnMapping? _serviceNowColumnMapping;

    public Task<IReadOnlyList<ImportedServiceNowApplication>> GetImportedServiceNowApplicationsAsync()
    {
        return Task.FromResult<IReadOnlyList<ImportedServiceNowApplication>>(
            _importedServiceNowApps.OrderBy(a => a.Name).ToList());
    }

    public Task StoreServiceNowApplicationsAsync(IEnumerable<ImportedServiceNowApplication> applications)
    {
        foreach (var app in applications)
        {
            var existingIndex = _importedServiceNowApps.FindIndex(a => a.ServiceNowId == app.ServiceNowId);
            if (existingIndex >= 0)
            {
                _importedServiceNowApps[existingIndex] = app;
            }
            else
            {
                _importedServiceNowApps.Add(app);
            }
        }

        RecordAuditLogAsync(new AuditLogEntry
        {
            Id = Guid.NewGuid().ToString(),
            EventType = "ServiceNowApplicationsImported",
            Category = "Sync",
            Message = $"Imported {applications.Count()} applications from ServiceNow CSV",
            UserId = "system",
            UserName = "System",
            EntityType = "ServiceNowApplication"
        });

        return Task.CompletedTask;
    }

    public Task ClearServiceNowApplicationsAsync()
    {
        _importedServiceNowApps.Clear();
        return Task.CompletedTask;
    }

    public Task<int> CreateApplicationsFromServiceNowImportAsync()
    {
        int count = 0;
        var apps = IsMockDataEnabled ? _mockApplications : _realApplications;

        foreach (var imported in _importedServiceNowApps)
        {
            // Check if app already exists by ServiceNow ID
            var existing = apps.FirstOrDefault(a => a.ServiceNowId == imported.ServiceNowId);
            if (existing != null)
            {
                apps.Remove(existing);
            }

            var app = new Application
            {
                Id = existing?.Id ?? Guid.NewGuid().ToString(),
                Name = imported.Name,
                Description = imported.Description,
                ShortDescription = imported.ShortDescription,
                Capability = imported.Capability ?? "Uncategorized",
                ApplicationType = ParseAppType(imported.ApplicationType),
                ArchitectureType = ParseArchitectureType(imported.ArchitectureType),
                UserBaseEstimate = imported.UserBase,
                Importance = imported.Importance,
                ServiceNowId = imported.ServiceNowId,
                RepositoryUrl = imported.RepositoryUrl,
                DocumentationUrl = imported.DocumentationUrl,
                HealthScore = 70,
                LastSyncDate = DateTimeOffset.UtcNow,
                RoleAssignments = BuildRoleAssignments(imported)
            };

            apps.Add(app);
            count++;
        }
        return Task.FromResult(count);
    }

    private static AppType ParseAppType(string? value)
    {
        if (string.IsNullOrEmpty(value)) return AppType.Unknown;
        return value.ToUpperInvariant() switch
        {
            "COTS" => AppType.COTS,
            "HOMEGROWN" or "CUSTOM" or "IN-HOUSE" => AppType.Homegrown,
            "HYBRID" => AppType.Hybrid,
            "SAAS" => AppType.SaaS,
            "OPEN SOURCE" or "OPENSOURCE" => AppType.OpenSource,
            _ => AppType.Unknown
        };
    }

    private static ArchitectureType ParseArchitectureType(string? value)
    {
        if (string.IsNullOrEmpty(value)) return ArchitectureType.Unknown;
        return value.ToUpperInvariant() switch
        {
            "WEB BASED" or "WEB-BASED" or "WEB" => ArchitectureType.WebBased,
            "CLIENT SERVER" or "CLIENT-SERVER" or "CLIENT/SERVER" => ArchitectureType.ClientServer,
            "DESKTOP APP" or "DESKTOP" => ArchitectureType.DesktopApp,
            "MOBILE APP" or "MOBILE" => ArchitectureType.MobileApp,
            "API" => ArchitectureType.API,
            "BATCH PROCESS" or "BATCH" => ArchitectureType.BatchProcess,
            "MICROSERVICES" => ArchitectureType.Microservices,
            "MONOLITHIC" or "MONOLITH" => ArchitectureType.Monolithic,
            "OTHER" => ArchitectureType.Other,
            _ => ArchitectureType.Unknown
        };
    }

    private static List<RoleAssignment> BuildRoleAssignments(ImportedServiceNowApplication imported)
    {
        var assignments = new List<RoleAssignment>();

        if (!string.IsNullOrEmpty(imported.ProductManagerName))
        {
            assignments.Add(new RoleAssignment
            {
                UserId = imported.ProductManagerId ?? Guid.NewGuid().ToString(),
                UserName = imported.ProductManagerName,
                UserEmail = GenerateEmailFromName(imported.ProductManagerName),
                Role = ApplicationRole.ProductManager
            });
        }

        if (!string.IsNullOrEmpty(imported.BusinessOwnerName))
        {
            assignments.Add(new RoleAssignment
            {
                UserId = imported.BusinessOwnerId ?? Guid.NewGuid().ToString(),
                UserName = imported.BusinessOwnerName,
                UserEmail = GenerateEmailFromName(imported.BusinessOwnerName),
                Role = ApplicationRole.BusinessOwner
            });
        }

        if (!string.IsNullOrEmpty(imported.FunctionalArchitectName))
        {
            assignments.Add(new RoleAssignment
            {
                UserId = imported.FunctionalArchitectId ?? Guid.NewGuid().ToString(),
                UserName = imported.FunctionalArchitectName,
                UserEmail = GenerateEmailFromName(imported.FunctionalArchitectName),
                Role = ApplicationRole.FunctionalArchitect
            });
        }

        if (!string.IsNullOrEmpty(imported.TechnicalArchitectName))
        {
            assignments.Add(new RoleAssignment
            {
                UserId = imported.TechnicalArchitectId ?? Guid.NewGuid().ToString(),
                UserName = imported.TechnicalArchitectName,
                UserEmail = GenerateEmailFromName(imported.TechnicalArchitectName),
                Role = ApplicationRole.TechnicalArchitect
            });
        }

        if (!string.IsNullOrEmpty(imported.OwnerName))
        {
            assignments.Add(new RoleAssignment
            {
                UserId = imported.OwnerId ?? Guid.NewGuid().ToString(),
                UserName = imported.OwnerName,
                UserEmail = GenerateEmailFromName(imported.OwnerName),
                Role = ApplicationRole.Owner
            });
        }

        if (!string.IsNullOrEmpty(imported.TechnicalLeadName))
        {
            assignments.Add(new RoleAssignment
            {
                UserId = imported.TechnicalLeadId ?? Guid.NewGuid().ToString(),
                UserName = imported.TechnicalLeadName,
                UserEmail = GenerateEmailFromName(imported.TechnicalLeadName),
                Role = ApplicationRole.TechnicalLead
            });
        }

        return assignments;
    }

    private static string GenerateEmailFromName(string name)
    {
        // Generate a placeholder email from the name (will be updated when synced with Entra ID)
        var normalized = name.ToLowerInvariant().Replace(" ", ".").Replace(",", "");
        return $"{normalized}@example.com";
    }

    public Task<ServiceNowColumnMapping?> GetServiceNowColumnMappingAsync()
    {
        return Task.FromResult(_serviceNowColumnMapping);
    }

    public Task SaveServiceNowColumnMappingAsync(ServiceNowColumnMapping mapping)
    {
        _serviceNowColumnMapping = mapping with { SavedAt = DateTimeOffset.UtcNow };

        RecordAuditLogAsync(new AuditLogEntry
        {
            Id = Guid.NewGuid().ToString(),
            EventType = "ServiceNowMappingSaved",
            Category = "Config",
            Message = "ServiceNow CSV column mapping was saved",
            UserId = "system",
            UserName = "System",
            EntityType = "ServiceNowColumnMapping"
        });

        return Task.CompletedTask;
    }

    #endregion

    #region App Name Mapping Storage

    private readonly List<AppNameMapping> _appNameMappings = [];
    private AppNameMappingConfig? _appNameMappingConfig;

    public Task<IReadOnlyList<AppNameMapping>> GetAppNameMappingsAsync()
    {
        return Task.FromResult<IReadOnlyList<AppNameMapping>>(
            _appNameMappings.OrderBy(m => m.ServiceNowAppName).ToList());
    }

    public Task StoreAppNameMappingsAsync(IEnumerable<AppNameMapping> mappings)
    {
        foreach (var mapping in mappings)
        {
            // Check for existing mapping by ServiceNow name
            var existingIndex = _appNameMappings.FindIndex(m =>
                m.ServiceNowAppName.Equals(mapping.ServiceNowAppName, StringComparison.OrdinalIgnoreCase));

            if (existingIndex >= 0)
            {
                _appNameMappings[existingIndex] = mapping with { UpdatedAt = DateTimeOffset.UtcNow };
            }
            else
            {
                _appNameMappings.Add(mapping);
            }
        }

        RecordAuditLogAsync(new AuditLogEntry
        {
            Id = Guid.NewGuid().ToString(),
            EventType = "AppNameMappingsImported",
            Category = "DataSync",
            Message = $"Imported {mappings.Count()} app name mappings",
            UserId = "system",
            UserName = "System",
            EntityType = "AppNameMapping",
            Details = new Dictionary<string, string> { ["Count"] = mappings.Count().ToString() }
        });

        return Task.CompletedTask;
    }

    public Task<AppNameMapping?> GetAppNameMappingByServiceNowNameAsync(string serviceNowAppName)
    {
        var mapping = _appNameMappings.FirstOrDefault(m =>
            m.ServiceNowAppName.Equals(serviceNowAppName, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(mapping);
    }

    public Task<AppNameMapping?> GetAppNameMappingBySharePointFolderAsync(string sharePointFolderName)
    {
        var mapping = _appNameMappings.FirstOrDefault(m =>
            m.SharePointFolderName?.Equals(sharePointFolderName, StringComparison.OrdinalIgnoreCase) == true);
        return Task.FromResult(mapping);
    }

    public Task<AppNameMapping?> GetAppNameMappingByRepoNameAsync(string repoName)
    {
        var mapping = _appNameMappings.FirstOrDefault(m =>
            m.AzureDevOpsRepoNames.Any(r => r.Equals(repoName, StringComparison.OrdinalIgnoreCase)));
        return Task.FromResult(mapping);
    }

    public Task ClearAppNameMappingsAsync()
    {
        _appNameMappings.Clear();
        return Task.CompletedTask;
    }

    public Task<AppNameMappingConfig?> GetAppNameMappingConfigAsync()
    {
        return Task.FromResult(_appNameMappingConfig);
    }

    public Task SaveAppNameMappingConfigAsync(AppNameMappingConfig config)
    {
        _appNameMappingConfig = config with { SavedAt = DateTimeOffset.UtcNow };

        RecordAuditLogAsync(new AuditLogEntry
        {
            Id = Guid.NewGuid().ToString(),
            EventType = "AppNameMappingConfigSaved",
            Category = "Config",
            Message = "App name mapping CSV column configuration was saved",
            UserId = "system",
            UserName = "System",
            EntityType = "AppNameMappingConfig"
        });

        return Task.CompletedTask;
    }

    #endregion

    #region Capability Mappings

    private readonly List<CapabilityMapping> _capabilityMappings = [];

    public Task<IReadOnlyList<CapabilityMapping>> GetCapabilityMappingsAsync()
    {
        return Task.FromResult<IReadOnlyList<CapabilityMapping>>(
            _capabilityMappings.OrderBy(m => m.Capability).ThenBy(m => m.ApplicationName).ToList());
    }

    public Task StoreCapabilityMappingsAsync(IEnumerable<CapabilityMapping> mappings)
    {
        foreach (var mapping in mappings)
        {
            var existingIndex = _capabilityMappings.FindIndex(m =>
                m.ApplicationName.Equals(mapping.ApplicationName, StringComparison.OrdinalIgnoreCase));

            if (existingIndex >= 0)
            {
                _capabilityMappings[existingIndex] = mapping with { UpdatedAt = DateTimeOffset.UtcNow };
            }
            else
            {
                _capabilityMappings.Add(mapping);
            }
        }

        RecordAuditLogAsync(new AuditLogEntry
        {
            Id = Guid.NewGuid().ToString(),
            EventType = "CapabilityMappingsImported",
            Category = "DataImport",
            Message = $"Imported {mappings.Count()} capability mappings",
            UserId = "system",
            UserName = "System",
            EntityType = "CapabilityMapping"
        });

        return Task.CompletedTask;
    }

    public Task ClearCapabilityMappingsAsync()
    {
        _capabilityMappings.Clear();
        return Task.CompletedTask;
    }

    public Task<string?> GetCapabilityForApplicationAsync(string applicationName)
    {
        var mapping = _capabilityMappings.FirstOrDefault(m =>
            m.ApplicationName.Equals(applicationName, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(mapping?.Capability);
    }

    #endregion

    #region SharePoint Folder Storage

    private readonly List<DiscoveredSharePointFolder> _discoveredSharePointFolders = [];

    public Task<IReadOnlyList<DiscoveredSharePointFolder>> GetDiscoveredSharePointFoldersAsync()
    {
        return Task.FromResult<IReadOnlyList<DiscoveredSharePointFolder>>(
            _discoveredSharePointFolders.OrderBy(f => f.Capability).ThenBy(f => f.Name).ToList());
    }

    public Task StoreDiscoveredSharePointFoldersAsync(IEnumerable<DiscoveredSharePointFolder> folders)
    {
        foreach (var folder in folders)
        {
            var existingIndex = _discoveredSharePointFolders.FindIndex(f => f.FullPath == folder.FullPath);
            if (existingIndex >= 0)
            {
                _discoveredSharePointFolders[existingIndex] = folder with { SyncedAt = DateTimeOffset.UtcNow };
            }
            else
            {
                _discoveredSharePointFolders.Add(folder);
            }
        }

        RecordAuditLogAsync(new AuditLogEntry
        {
            Id = Guid.NewGuid().ToString(),
            EventType = "SharePointFoldersSynced",
            Category = "DataSync",
            Message = $"Synced {folders.Count()} SharePoint folders",
            UserId = "system",
            UserName = "System",
            EntityType = "DiscoveredSharePointFolder",
            Details = new Dictionary<string, string> { ["Count"] = folders.Count().ToString() }
        });

        return Task.CompletedTask;
    }

    public Task ClearDiscoveredSharePointFoldersAsync()
    {
        _discoveredSharePointFolders.Clear();
        return Task.CompletedTask;
    }

    #endregion
}
