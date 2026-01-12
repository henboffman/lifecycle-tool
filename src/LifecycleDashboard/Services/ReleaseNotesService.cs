using LifecycleDashboard.Models;

namespace LifecycleDashboard.Services;

/// <summary>
/// In-memory implementation of release notes service.
/// Release notes are defined in code to maintain traceability with features.
/// </summary>
public class ReleaseNotesService : IReleaseNotesService
{
    private static readonly List<ReleaseNote> _releaseNotes = InitializeReleaseNotes();

    private static List<ReleaseNote> InitializeReleaseNotes()
    {
        return
        [
            new ReleaseNote
            {
                Id = "v0.3.0",
                Version = "0.3.0",
                Date = new DateTimeOffset(2026, 1, 11, 18, 0, 0, TimeSpan.Zero),
                Title = "Detail Pages, Framework Tracking & Admin Enhancements",
                Description = "Major update with comprehensive detail views for applications and tasks, framework EOL tracking with admin management, and enhanced task documentation administration.",
                Category = ReleaseNoteCategory.Feature,
                IsHighlighted = true,
                Items =
                [
                    // Application Detail Page
                    new ReleaseNoteItem
                    {
                        Type = ReleaseNoteItemType.Added,
                        Text = "Application Detail page with 7 tabs: Overview, Security, Repository, Usage, Documentation, Tasks, Data Sources",
                        Component = "Applications"
                    },
                    new ReleaseNoteItem
                    {
                        Type = ReleaseNoteItemType.Added,
                        Text = "Health score breakdown showing security, usage, maintenance, and documentation factors",
                        Component = "Applications"
                    },
                    new ReleaseNoteItem
                    {
                        Type = ReleaseNoteItemType.Added,
                        Text = "Repository tab with package list, commit history, and contributor information",
                        Component = "Applications"
                    },

                    // Task Detail Page
                    new ReleaseNoteItem
                    {
                        Type = ReleaseNoteItemType.Added,
                        Text = "Task Detail page with step-by-step instructions, system guidance, and task history",
                        Component = "Tasks"
                    },
                    new ReleaseNoteItem
                    {
                        Type = ReleaseNoteItemType.Added,
                        Text = "Enhanced task cards with View Details button, priority badges, and status indicators",
                        Component = "Tasks"
                    },
                    new ReleaseNoteItem
                    {
                        Type = ReleaseNoteItemType.Added,
                        Text = "Composite task key display (ApplicationName/TaskType) for user-friendly reference",
                        Component = "Tasks"
                    },

                    // Framework EOL Tracking
                    new ReleaseNoteItem
                    {
                        Type = ReleaseNoteItemType.Added,
                        Text = "Frameworks data page with EOL tracking for .NET, .NET Framework, Python, R, and Node.js",
                        Component = "Frameworks"
                    },
                    new ReleaseNoteItem
                    {
                        Type = ReleaseNoteItemType.Added,
                        Text = "Pre-populated EOL dates from endoflife.date for major framework versions",
                        Component = "Frameworks"
                    },
                    new ReleaseNoteItem
                    {
                        Type = ReleaseNoteItemType.Added,
                        Text = "EOL urgency indicators (Critical, High, Medium, Low, Past EOL)",
                        Component = "Frameworks"
                    },
                    new ReleaseNoteItem
                    {
                        Type = ReleaseNoteItemType.Added,
                        Text = "Modal to view applications using each framework version",
                        Component = "Frameworks"
                    },

                    // Admin Enhancements
                    new ReleaseNoteItem
                    {
                        Type = ReleaseNoteItemType.Added,
                        Text = "Admin Frameworks tab for managing framework versions and EOL dates",
                        Component = "Admin"
                    },
                    new ReleaseNoteItem
                    {
                        Type = ReleaseNoteItemType.Added,
                        Text = "Admin Task Docs tab for configuring task instructions, system guidance, and prerequisites",
                        Component = "Admin"
                    },
                    new ReleaseNoteItem
                    {
                        Type = ReleaseNoteItemType.Added,
                        Text = "Add, edit, and delete framework version records with app impact warnings",
                        Component = "Admin"
                    },

                    // New Models
                    new ReleaseNoteItem
                    {
                        Type = ReleaseNoteItemType.Added,
                        Text = "FrameworkVersion model with EOL tracking, urgency calculation, and upgrade paths",
                        Component = "Data Layer"
                    },
                    new ReleaseNoteItem
                    {
                        Type = ReleaseNoteItemType.Added,
                        Text = "RepositoryInfo model with package references, tech stack detection, commit history",
                        Component = "Data Layer"
                    },
                    new ReleaseNoteItem
                    {
                        Type = ReleaseNoteItemType.Added,
                        Text = "TaskDocumentation model with instructions, system guidance, and troubleshooting tips",
                        Component = "Data Layer"
                    },
                    new ReleaseNoteItem
                    {
                        Type = ReleaseNoteItemType.Added,
                        Text = "TaskHistoryEntry model for tracking task status changes and notes",
                        Component = "Data Layer"
                    },

                    // Navigation
                    new ReleaseNoteItem
                    {
                        Type = ReleaseNoteItemType.Changed,
                        Text = "Navigation updated with Frameworks link in Analytics section",
                        Component = "Navigation"
                    }
                ],
                Tags = ["detail-pages", "frameworks", "eol-tracking", "admin", "task-documentation"]
            },
            new ReleaseNote
            {
                Id = "v0.2.0",
                Version = "0.2.0",
                Date = new DateTimeOffset(2026, 1, 11, 12, 0, 0, TimeSpan.Zero),
                Title = "Dashboard & Mock Data Foundation",
                Description = "Core dashboard with health metrics, task preview, and comprehensive mock data service for development.",
                Category = ReleaseNoteCategory.Feature,
                IsHighlighted = true,
                Items =
                [
                    new ReleaseNoteItem
                    {
                        Type = ReleaseNoteItemType.Added,
                        Text = "Dashboard home page with portfolio health overview",
                        Component = "Dashboard"
                    },
                    new ReleaseNoteItem
                    {
                        Type = ReleaseNoteItemType.Added,
                        Text = "Health status summary cards (Healthy, Needs Attention, At Risk, Critical)",
                        Component = "Dashboard"
                    },
                    new ReleaseNoteItem
                    {
                        Type = ReleaseNoteItemType.Added,
                        Text = "My Tasks widget with overdue/due soon/upcoming counts",
                        Component = "Dashboard"
                    },
                    new ReleaseNoteItem
                    {
                        Type = ReleaseNoteItemType.Added,
                        Text = "Recent Activity feed widget",
                        Component = "Dashboard"
                    },
                    new ReleaseNoteItem
                    {
                        Type = ReleaseNoteItemType.Added,
                        Text = "Mini heatmap preview widget",
                        Component = "Dashboard"
                    },
                    new ReleaseNoteItem
                    {
                        Type = ReleaseNoteItemType.Added,
                        Text = "Recommendations widget with priority indicators",
                        Component = "Dashboard"
                    },
                    new ReleaseNoteItem
                    {
                        Type = ReleaseNoteItemType.Added,
                        Text = "Core data models (Application, LifecycleTask, User, HealthScore)",
                        Component = "Data Layer"
                    },
                    new ReleaseNoteItem
                    {
                        Type = ReleaseNoteItemType.Added,
                        Text = "Mock data service with 40 realistic applications",
                        Component = "Services"
                    },
                    new ReleaseNoteItem
                    {
                        Type = ReleaseNoteItemType.Added,
                        Text = "Placeholder pages for Applications, Tasks, Heatmap, Reports, Admin, Data Jobs",
                        Component = "Pages"
                    }
                ],
                Tags = ["dashboard", "mock-data", "ui"]
            },
            new ReleaseNote
            {
                Id = "v0.1.0",
                Version = "0.1.0",
                Date = new DateTimeOffset(2026, 1, 11, 0, 0, 0, TimeSpan.Zero),
                Title = "Initial Project Setup",
                Description = "Foundation release establishing the core application structure and developer experience tooling.",
                Category = ReleaseNoteCategory.Infrastructure,
                Items =
                [
                    new ReleaseNoteItem
                    {
                        Type = ReleaseNoteItemType.Added,
                        Text = "Blazor Server application with .NET 10",
                        Component = "Infrastructure"
                    },
                    new ReleaseNoteItem
                    {
                        Type = ReleaseNoteItemType.Added,
                        Text = "DevMode floating sidebar for development feedback",
                        Component = "Developer Tools"
                    },
                    new ReleaseNoteItem
                    {
                        Type = ReleaseNoteItemType.Added,
                        Text = "Release notes page with full changelog",
                        Component = "Documentation"
                    },
                    new ReleaseNoteItem
                    {
                        Type = ReleaseNoteItemType.Added,
                        Text = "WIP (Work In Progress) badge component for unimplemented features",
                        Component = "UI Components"
                    },
                    new ReleaseNoteItem
                    {
                        Type = ReleaseNoteItemType.Added,
                        Text = "Main navigation structure with app layout",
                        Component = "Navigation"
                    },
                    new ReleaseNoteItem
                    {
                        Type = ReleaseNoteItemType.Added,
                        Text = "Global CSS design system with custom properties",
                        Component = "Styling"
                    }
                ],
                Tags = ["infrastructure", "developer-experience", "foundation"]
            }
        ];
    }

    public Task<IReadOnlyList<ReleaseNote>> GetAllAsync()
    {
        var notes = _releaseNotes
            .OrderByDescending(n => n.Date)
            .ThenByDescending(n => n.Version)
            .ToList();
        return Task.FromResult<IReadOnlyList<ReleaseNote>>(notes);
    }

    public Task<IReadOnlyList<ReleaseNote>> GetRecentAsync(int count = 5)
    {
        var notes = _releaseNotes
            .OrderByDescending(n => n.Date)
            .ThenByDescending(n => n.Version)
            .Take(count)
            .ToList();
        return Task.FromResult<IReadOnlyList<ReleaseNote>>(notes);
    }

    public Task<ReleaseNote?> GetByVersionAsync(string version)
    {
        var note = _releaseNotes.FirstOrDefault(n =>
            n.Version.Equals(version, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(note);
    }

    public Task<IReadOnlyList<ReleaseNote>> GetByCategoryAsync(ReleaseNoteCategory category)
    {
        var notes = _releaseNotes
            .Where(n => n.Category == category)
            .OrderByDescending(n => n.Date)
            .ToList();
        return Task.FromResult<IReadOnlyList<ReleaseNote>>(notes);
    }

    public Task<string> GetLatestVersionAsync()
    {
        var latest = _releaseNotes
            .OrderByDescending(n => n.Date)
            .ThenByDescending(n => n.Version)
            .FirstOrDefault();
        return Task.FromResult(latest?.Version ?? "0.0.0");
    }

    /// <summary>
    /// Helper method to add a new release note programmatically.
    /// This can be called during development to track new features.
    /// </summary>
    public static void AddReleaseNote(ReleaseNote note)
    {
        _releaseNotes.Add(note);
    }
}
