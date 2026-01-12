# Lifecycle Dashboard - Claude Context

## Quick Summary

Building an **Application Portfolio Lifecycle Health Dashboard** - a Blazor/.NET 10 web app for IT organizations to track ~300 applications across their portfolio using data from Azure DevOps, SharePoint, ServiceNow, and IIS logs.

## Tech Stack

- **Frontend/Backend:** Blazor, .NET 10
- **Database:** SQL Server (self-hosted)
- **Deployment:** On-premise Windows Server
- **Auth:** Microsoft Entra ID (SSO)
- **AI/ML:** Azure OpenAI (prod), Ollama (local dev)
- **User Settings:** IndexedDB with backup

## Current Phase

**Requirements: COMPLETE** (see `docs/requirements.md` v2.0)
**Next: Architecture design and implementation**

## Key Features (All MVP)

1. **Health Scoring** (0-100): Vulnerabilities, usage, maintenance activity, documentation
2. **Heatmaps:** Both grid AND treemap views, WCAG accessible
3. **Task Management:** Role validation, intelligent scheduling, escalation
4. **Data Jobs:** Weekly pulls with monitoring dashboard
5. **User Customization:** Themes, dashboards, saved views (IndexedDB)
6. **Exports:** CSV/JSON, RSS/Atom feeds, ALL data exportable
7. **Extensibility:** API-first, webhooks, plugin architecture
8. **Mock Data Mode:** Toggle for development (network-isolated)

## Health Score Formula

- Critical vulns: -15 each | High: -8 | Medium: -2 | Low: -0.5
- No usage: -20 | High usage: +5
- Recent commits: +10 | Stale (365+ days): -10
- Docs complete: +10 | Missing: -15
- Categories: Healthy (80-100), Needs Attention (60-79), At Risk (40-59), Critical (0-39)

## Data Sources

| Source | Method | Frequency |
|--------|--------|-----------|
| Azure DevOps | REST API | Weekly |
| SharePoint | Graph API | Weekly |
| ServiceNow | CSV export | Weekly |
| IIS Database | SQL (read-only) | Weekly |

## Key Constraints

- Development is network-isolated (use mock data)
- Privacy: Aggregate metrics only, no individual user tracking
- Desktop browser only
- ~300 apps, ~100 users

## Important Files

- `docs/requirements.md` - Complete requirements (v2.0)
- `docs/session-context.md` - Full session context
- `docs/ideas.md` - Original concept
- `AGENTS.md` - Notes: "Use 'bd' for task tracking"

## Stakeholder Direction

- "Let's build it right now"
- Everything discussed is MVP
- Strong interest in AI/ML integration
- System should be extensible as node in larger ecosystem
