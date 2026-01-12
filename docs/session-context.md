# Lifecycle Dashboard - Session Context Document

**Created:** 2026-01-11
**Purpose:** Complete context capture for session continuity

---

## Project Overview

**Project Name:** Application Portfolio Lifecycle Health Dashboard

**Goal:** A dashboard for IT organizations to track application portfolio lifecycle health using data aggregated from multiple sources (Azure DevOps, SharePoint, ServiceNow, IIS logs). Users can see where they need to take action, with tasks having target windows and role-based assignments.

**Repository Location:** `/Users/benjaminhoffman/Documents/code/lifecycle`

---

## Current Project State

### Files Created
1. `docs/ideas.md` - Original concept document (user-created)
2. `docs/requirements.md` - Comprehensive requirements document (Version 2.0 - Complete)
3. `AGENTS.md` - Contains note: "Use 'bd' for task tracking"
4. `docs/session-context.md` - This file

### Phase Completed
- Requirements gathering: **COMPLETE** (all 60 questions answered)
- Next phase: Architecture design and implementation planning

---

## Technology Stack Decisions

| Component | Decision |
|-----------|----------|
| **Frontend** | Blazor (.NET 10) |
| **Backend** | .NET 10 |
| **Database** | SQL Server (self-hosted) |
| **Deployment** | On-premise Windows Server |
| **Containerization** | No |
| **Authentication** | Microsoft Entra ID (SSO) |
| **UI Framework** | Vanilla CSS, Bootstrap as fallback |
| **User Settings Storage** | IndexedDB (with export/import backup) |
| **AI/ML (Production)** | Azure OpenAI via API |
| **AI/ML (Local Dev)** | Ollama |

---

## Key Feature Decisions

### Data Sources
1. **Azure DevOps** - Repository data, CodeQL/security findings, dependencies
2. **SharePoint** - Documentation (hierarchy: documents/capabilities/apps/[technical, project, userfacing, support, architecture])
3. **ServiceNow** - Role assignments (CSV export initially)
4. **IIS Database** - Usage metrics (existing separate database, read-only connection)

### Data Integration
- **Refresh Frequency:** Weekly (sufficient for all sources)
- **Current State:** One-off Node.js scripts (AzDo) and PowerShell scripts (SharePoint) generating CSV/JSON
- **Goal:** Productionize into scheduled, monitored jobs with admin dashboard
- **Development Mode:** Mock data with toggle (network-isolated development)

### Health Scoring Algorithm (0-100 scale)

**Security Vulnerability Penalties:**
- Critical: -15 points each (max -60)
- High: -8 points each (max -40)
- Medium: -2 points each (max -20)
- Low: -0.5 points each (max -10)

**Usage Metrics:**
- No usage: -20 points
- Very low (1-100 req/month): -10 points
- Low (101-1000): -5 points
- Moderate (1001-10000): 0 points
- High (10001+): +5 points

**Active Maintenance Bonus:**
- Recent commits (<30 days): +10 points
- Moderate (31-90 days): +5 points
- Low (91-180 days): 0 points
- Inactive (181-365 days): -5 points
- Stale (365+ days): -10 points

**Documentation Completeness:**
- Required: Architecture diagram + system documentation
- Both present: +10 points
- One missing: -10 points
- Both missing: -15 points

**Health Categories:**
- Healthy: 80-100 (Green)
- Needs Attention: 60-79 (Yellow/Amber)
- At Risk: 40-59 (Orange)
- Critical: 0-39 (Red)

### Heatmap Visualization
- **Both** grid view AND treemap view
- WCAG accessibility compliance required
- Color contrast guidelines

### User Experience
- **Primary Users:** Individual contributors with assigned tasks
- **Device:** Desktop browser only
- **Customization:** Custom dashboards, saved views, light/dark mode
- **Portfolio Size:** ~300 applications, ~100 users
- **List Handling:** Infinite scroll (performance optimized)

### Notifications
- **Primary Channel:** In-app notifications
- **Secondary (later):** Email
- **Triggers:** 30-day warning, 14-day warning, overdue
- **Special:** Organizational workload warning (40+ tasks) triggers leadership email
- **User Configurable:** Yes

### Task Management
- **Role Validation:** Annually OR when role occupant leaves (not in Entra)
- **Scheduling:** Intelligent distribution (don't stack 100 tasks on one person)
- **Overdue Handling:** Escalate + impacts health score (-3 per task, -5 if 30+ days)
- **Delegation:** By assignee or privileged admins (defined in appsettings)

### Data Conflicts
- Raise to user for remediation (this is the "lifecycle rot" to correct)
- Flag prominently with visual indicators
- Impacts health score

### Exports & Data Access
- **Formats:** CSV and JSON prioritized (open formats)
- **Scope:** ALL data exportable
- **Feeds:** RSS/Atom or similar subscription mechanism
- **Reports:** On-demand only (no scheduled)

### External Dashboards
- Likely Power BI dashboards
- Simple hyperlinks for MVP
- No SSO required initially

### Extensibility (Critical Requirement)
- Plugin architecture
- API-first design
- System as NODE in larger ecosystem
- Webhooks for external integrations
- End user as DATA CONSUMER focus

### AI/ML Integration
- Azure OpenAI (production) via API
- Ollama (local development/testing)
- Features: Predictive scoring, anomaly detection, intelligent recommendations

---

## Environments & Deployment

- **Environments:** Dev, Test, Prod
- **Current Phase:** Local development only
- **Deployment:** User will clone repo and deploy when ready
- **Monitoring:** Basic admin dashboard
- **DR:** Not necessary
- **Migration:** None (no existing system)
- **Rollout:** Big-bang (user testing)

---

## Constraints & Assumptions

### Constraints
- Development environment disconnected from network (mock data required)
- Weekly refresh frequency (acceptable)
- On-premise Windows Server deployment
- No containerization
- Privacy: Aggregate usage metrics only, no individual user tracking
- Privileged admin IDs in appsettings.json

### Assumptions
- ~300 applications in portfolio
- ~100 users accessing system
- SharePoint folder names align with ServiceNow application names
- IIS database schema is stable
- Users have modern browsers with JavaScript enabled

---

## Data Model - Core Entities

1. **Application/Capability** - Central entity with unique identifier
2. **User** - Individuals interacting with the system
3. **Role Assignment** - Links users to applications via roles
4. **Task** - Lifecycle management activities
5. **Health Score** - Time-series data for application health
6. **Data Source Sync** - Metadata about sync operations
7. **Job Execution** - Record of scheduled job runs
8. **Audit Log** - Historical record of activities

---

## User Roles

1. **Read-Only User** - View dashboards and application data
2. **Standard User** - Complete assigned tasks, view personal data
3. **Power User** - Create custom reports, view all applications, export data
4. **Administrator** - Full system access, configure settings, manage users
5. **Security Administrator** (Optional) - Security-focused admin access

---

## SharePoint Document Structure

```
documents/
  capabilities/
    [capability-name]/
      apps/
        [app-name]/
          technical/
          project/
          userfacing/
          support/
          architecture/
```

- Template folders (technical, project, userfacing, support) indicate valid application
- Folder name MUST match ServiceNow application name
- ServiceNow contains links to Azure DevOps repositories

---

## IIS Usage Metrics (Existing Database)

**Metrics Captured:**
- Total requests per month
- Distinct users per month
- Usages per month (handles SPA scenarios)

**Privacy:** NO individual user data - aggregate only

**Integration:** Direct SQL connection (read-only) to existing database

---

## MVP Scope

**EVERYTHING discussed is MVP** - comprehensive initial release including:
- Dashboard with heatmaps (grid + treemap)
- Health scoring algorithm
- Task management with scheduling
- Data integration jobs with monitoring
- User customization (themes, dashboards, saved views)
- In-app notifications
- Export capabilities (CSV, JSON, feeds)
- Admin interface for all configuration
- Mock data mode for development
- Basic AI/ML integration foundation
- API-first extensible architecture

---

## Next Steps (Ready to Execute)

1. **Architecture Design** - Design system architecture based on requirements
2. **Database Schema** - Design SQL Server schema for all entities
3. **Project Setup** - Create Blazor/.NET 10 solution structure
4. **Mock Data** - Create comprehensive mock data for all scenarios
5. **Core Features** - Implement dashboard, heatmaps, health scoring
6. **Data Integration** - Build job scheduling and monitoring
7. **User Features** - Customization, notifications, exports
8. **AI/ML** - Integrate Ollama for local development
9. **Testing** - User acceptance testing with mock data

---

## Key Documents Reference

| Document | Path | Status |
|----------|------|--------|
| Ideas/Concept | `docs/ideas.md` | Original |
| Requirements | `docs/requirements.md` | v2.0 Complete |
| Session Context | `docs/session-context.md` | This file |
| AGENTS.md | `AGENTS.md` | Task tracking note |

---

## Stakeholder Notes

- This is the stakeholder's "brainchild"
- They will handle testing, training, and deployment
- Building disconnected from network - mock data essential
- Ready to start building immediately ("let's build it right now")
- Strong interest in AI/ML capabilities
- Focus on extensibility for future growth

---

**Document Status:** Complete context capture for session restart
**Next Action:** Ready to begin implementation planning and architecture design
