# Changelog

All notable changes to the Lifecycle Dashboard will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

---

## [Unreleased]

### Added

#### Application Detail Page (`/applications/{id}`)
- Comprehensive application detail view with 7 tabs:
  - **Overview**: Health score breakdown, quick stats, role assignments, technology stack
  - **Security**: Security findings table with severity counts and sorting
  - **Repository**: Stack info, packages list, commit activity, top contributors, README status
  - **Usage**: Usage metrics with trend indicators and analysis
  - **Documentation**: Documentation checklist with completion progress
  - **Tasks**: Active and completed tasks for the application
  - **Data Sources**: ServiceNow, SharePoint, and Azure DevOps sync status
- Navigation from Applications page and Heatmap modal

#### Task Detail Page (`/tasks/{id}`)
- Individual task view with:
  - Task header with priority, status, due date, and composite key
  - Step-by-step instructions from admin-editable documentation
  - System-specific guidance (ServiceNow, SharePoint, Azure DevOps)
  - Prerequisites and related documentation links
  - Task history timeline
  - Assignee information with delegation notices

#### Enhanced Task Cards (Tasks Page)
- Added "View Details" button (eye icon) for quick navigation
- Display composite key (`ApplicationName/TaskType`) for user-friendly reference
- Added priority badges with color coding
- Added status badges (In Progress, Blocked)
- Clickable application names to navigate to application detail
- Show created date and assignee in task meta info

#### New Data Models
- **RepositoryInfo** (`/Models/RepositoryInfo.cs`)
  - Package references with version tracking and vulnerability flags
  - Technology stack detection (StackType enum: DotNetCore, DotNetFramework, DotNetAurelia, Python, R, etc.)
  - Commit history with contributor information
  - README status analysis (exists, is template, has meaningful content)
  - System dependencies extracted from config files
  - Application Insights configuration status

- **TaskDocumentation** (`/Models/TaskDocumentation.cs`)
  - Admin-editable task instructions per TaskType
  - Step-by-step instructions with system references and action URLs
  - System-specific guidance (ServiceNow, SharePoint, Azure DevOps, Azure Portal)
  - Troubleshooting tips
  - Related documentation links
  - Estimated duration and prerequisites

- **TaskHistoryEntry** (`/Models/TaskDocumentation.cs`)
  - Track task status changes, assignments, delegations, notes, escalations

#### Extended LifecycleTask Model
- `CompositeKey` property: `{ApplicationName}/{TaskType}` for display
- `FullCompositeKey` property: `{AssigneeName}/{ApplicationName}/{TaskType}` for unique identification
- `History` list for tracking all task changes

#### Framework Version Tracking (`/frameworks`)
- **FrameworkVersion Model** (`/Models/FrameworkVersion.cs`)
  - Track framework EOL dates for .NET, .NET Framework, Python, R, Node.js, Java
  - EOL urgency calculation (None, Low, Medium, High, Critical, PastEol)
  - Support status tracking (Active, Maintenance, EndOfLife, Preview)
  - LTS (Long Term Support) designation
  - Recommended upgrade paths

- **Frameworks Data Page** (`/frameworks`)
  - EOL summary cards showing total tracked, past EOL, critical, approaching, supported counts
  - Filter by framework type, support status, and EOL urgency
  - Sortable table with all framework version details
  - Modal to view applications using each framework version
  - Click-through to application detail pages

- **Pre-populated EOL Data**
  - .NET 10, 9, 8 (LTS), 7, 6 (LTS), 5, Core 3.1, Core 2.1
  - .NET Framework 4.8.1, 4.8, 4.7.2, 4.6.2, 4.6.1, 4.5.2, 3.5
  - Python 3.14 (Preview), 3.13, 3.12, 3.11, 3.10, 3.9, 3.8, 2.7
  - R 4.4, 4.3
  - Node.js 22 (LTS), 20 (LTS), 18, 16

#### Admin Enhancements
- **Framework Version Management** (Admin > Frameworks tab)
  - Quick stats for total versions, past EOL, critical, and actively supported
  - Filter and manage all framework versions
  - Add new framework versions with full EOL tracking
  - Edit existing framework versions (EOL dates, status, upgrade paths)
  - Delete framework versions with confirmation and app impact warning
  - Color-coded urgency indicators in table rows

- **Task Documentation Admin** (Admin > Task Docs tab)
  - View all task documentation entries with summary cards
  - Edit documentation for each task type:
    - Description and estimated duration
    - Prerequisites (multi-line entry)
    - Step-by-step instructions with drag-and-drop reordering capability
    - System-specific guidance (ServiceNow, SharePoint, Azure DevOps, IIS)
    - Related documentation links

#### Service Layer Updates
- `GetTaskAsync(string taskId)` - Retrieve individual task by ID
- `GetRepositoryInfoAsync(string applicationId)` - Get repository data for an application
- `GetTaskDocumentationAsync(TaskType taskType)` - Get documentation for a task type
- `GetAllTaskDocumentationAsync()` - Get all task documentation entries
- `UpdateTaskDocumentationAsync(TaskDocumentation doc)` - Save documentation changes
- `GetAllFrameworkVersionsAsync()` - Get all framework versions
- `GetFrameworkVersionAsync(string id)` - Get specific framework version
- `GetFrameworkVersionsByTypeAsync(FrameworkType type)` - Filter by framework type
- `CreateFrameworkVersionAsync(FrameworkVersion version)` - Add new framework version
- `UpdateFrameworkVersionAsync(FrameworkVersion version)` - Update framework version
- `DeleteFrameworkVersionAsync(string id)` - Remove framework version
- `GetApplicationsByFrameworkAsync(string frameworkVersionId)` - Get apps using a framework
- `GetFrameworkEolSummaryAsync()` - Get portfolio-wide EOL summary
- Mock data generators for repository info, task documentation, and framework versions

### Changed
- Applications page now navigates to `/applications/{id}` on "View Details" click
- Heatmap modal "View Full Details" button now enabled and navigates to application detail
- Tasks page task cards redesigned with more information density and better visual hierarchy
- Admin page now has 7 tabs (added Frameworks and Task Docs)
- Navigation sidebar updated with Frameworks link in Analytics section

### Technical Notes
- Task IDs use internal GUIDs for performance/stability
- Composite keys provide user-friendly display without sacrificing lookup performance
- Task documentation is designed to be admin-editable (UI pending)
- All new pages follow Dark Nebula theme with purple accent colors

---

## [0.1.0] - 2025-01-11

### Added
- Initial Blazor application scaffolding
- Dashboard home page with health summary
- Applications list page with filtering and search
- Heatmap visualization (Grid and Treemap views) with WCAG accessibility
- Tasks page with list and calendar views
- Admin page with tabbed interface
- Mock data service with 40 sample applications
- Dark Nebula theme with light mode backgrounds
- Responsive layout with sidebar navigation

### Data Models
- Application model with health scoring
- LifecycleTask model with priority, status, due dates
- User model with role assignments
- SecurityFinding, UsageMetrics, DocumentationStatus models

### Features
- Health score calculation (0-100) based on security, usage, maintenance, documentation
- Health categories: Healthy (80-100), Needs Attention (60-79), At Risk (40-59), Critical (0-39)
- Task categorization: Overdue, Due This Week, Upcoming, Completed
- Export tasks to Outlook (.ics format)
- High contrast mode for heatmap accessibility
