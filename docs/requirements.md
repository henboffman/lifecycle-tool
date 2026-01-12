# IT Application Portfolio Lifecycle Health Dashboard - Requirements

**STATUS: REQUIREMENTS GATHERING COMPLETE - READY FOR IMPLEMENTATION**

All 60 stakeholder questions have been answered. This document represents the complete, final requirements for the IT Application Portfolio Lifecycle Health Dashboard and is ready for architecture design and implementation planning.

## 1. Executive Summary

### 1.1 Project Overview

A centralized dashboard application for IT organizations to monitor and manage the health of their application portfolio throughout the software development lifecycle. The system aggregates data from multiple sources in near-real time to provide actionable insights, track task completion, and ensure organizational compliance with lifecycle management processes.

**Key Characteristics:**
- Comprehensive MVP including all discussed features (stakeholder directive)
- Desktop-first web application (Blazor/.NET 10)
- API-first architecture designed as node in larger ecosystem
- AI/ML capabilities with Azure OpenAI (production) and Ollama (development)
- Extensible plugin architecture for future growth
- WCAG-compliant accessibility throughout
- Big-bang rollout managed by stakeholder

### 1.2 Primary Business Objectives

- Provide real-time visibility into application portfolio health across the organization
- Enable proactive identification of applications requiring attention or remediation
- Streamline task management for role-based responsibilities tied to applications
- Aggregate disparate data sources into a unified view
- Facilitate compliance with organizational lifecycle management policies
- Surface and resolve data conflicts (lifecycle rot detection)
- Support low-usage application retirement reviews
- Serve as data source for other organizational systems (API-first, data feeds, webhooks)

### 1.3 Key Success Metrics

- Reduction in time to identify at-risk applications
- Improved task completion rates for lifecycle management activities
- Increased visibility into application health metrics
- User adoption rate across IT organization (~100 users, ~300 applications)
- Accuracy of health scoring based on organizational priorities
- Reduction in data conflicts between source systems
- Successful retirement or justification of low-usage applications
- External system integration adoption (API/webhook consumers)

---

## 2. Functional Requirements

### 2.1 Dashboard & Visualization

#### 2.1.1 Organization-Wide Health Heatmap

**Priority: High**

- Display a visual heatmap showing the health status of all applications in the portfolio
- Color-code applications based on aggregated health scores (e.g., green/yellow/red or gradient scale)
- Support drill-down from heatmap to individual application details
- Provide configurable grouping/organization (by department, team, technology stack, etc.)
- Enable filtering and search within the heatmap view
- Display summary statistics (total applications, applications by health status, trending indicators)

#### 2.1.2 User-Focused Task Dashboard

**Priority: High**

- Display personalized view of tasks assigned to the logged-in user
- Show tasks requiring attention based on:
  - User's assigned roles
  - Applications/capabilities the user is responsible for
  - Upcoming or overdue deadlines
- Provide task filtering by:
  - Priority/urgency
  - Application/capability
  - Task type (role validation, documentation update, security remediation, etc.)
  - Due date range
- Display task progress indicators
- Support bulk task actions where applicable
- Show task history and completion status

#### 2.1.3 Intelligent Recommendations - **UPDATED**

**Priority: Medium**

- Generate prioritized recommendations based on organizational settings
- Consider factors such as:
  - Application age
  - Vulnerability severity and count
  - Application usage metrics
  - Compliance status
  - Documentation completeness
- Display recommendation reasoning/rationale
- Allow users to dismiss, defer, or act on recommendations
- Track recommendation outcomes and effectiveness

**Low-Usage Application Recommendations:**

- **Flag applications for review and potential retirement** when usage falls below thresholds:
  - No production usage in last 90 days: High-priority retirement review
  - Very low usage (< 100 requests/month): Medium-priority retirement review
  - Low usage (< 1000 requests/month): Low-priority retirement review
- Recommendations include:
  - Usage statistics and trend charts
  - Last active usage date
  - Estimated cost of maintenance vs. business value
  - Suggested action: "Review for retirement" or "Justify retention"
- **Purpose:** Identify candidates to justify keeping or finally retire
- Workflow:
  - Application owner receives recommendation
  - Owner can provide justification for retention (low usage but critical function)
  - Owner can initiate retirement process
  - Administrator can mark recommendation as reviewed/dismissed
- Track retirement review history and outcomes

#### 2.1.4 Application Usage Visualization

**Priority: Medium**

- Display usage metrics parsed from IIS logs
- Show usage trends over time (daily, weekly, monthly views)
- Provide breakdown by:
  - Environment (dev, test, staging, production)
  - User count
  - Request volume
  - Response times
  - Error rates
- Support comparison between applications
- Integrate usage data into health scoring calculations

#### 2.1.5 Heatmap Visualization Options - **NEW**

**Priority: High**

**Multiple visualization types:**
- **Grid view:** Structured tabular organization of applications
  - Sortable columns (name, health score, owner, etc.)
  - Color-coded cells based on health status
  - Quick scanning and comparison

- **Treemap view:** Hierarchical space-efficient visualization
  - Size represents application importance/usage
  - Color represents health status
  - Grouped by capability or department
  - Interactive drill-down

**Accessibility requirements:**
- **MUST follow WCAG contrast visibility guidelines**
- High contrast mode support
- Color-blind friendly palettes
- Visual distinction beyond color alone (patterns, icons, text labels)
- Clear health status indicators

**User controls:**
- Toggle between grid and treemap views
- Saved view preferences per user (stored in IndexedDB)
- Filter and search within both view types

**Performance optimization:**
- Efficient rendering for ~300 applications
- Infinite scroll for grid view
- Progressive loading for treemap
- Smooth transitions between views

### 2.2 Application/Capability Management

#### 2.2.1 Application Browse & Search

**Priority: High**

- Provide searchable catalog of all applications/capabilities
- Support search by:
  - Name
  - Technology stack
  - Owner/responsible party
  - Health status
  - Tags/categories
- Display application summary cards with key metrics
- Enable sorting and advanced filtering

#### 2.2.2 Application Detail View

**Priority: High**

- Display comprehensive information for selected application:
  - Basic metadata (name, description, owner, teams)
  - Health score with contributing factors breakdown
  - Assigned roles and responsible individuals
  - Active and completed tasks
  - Repository information (Azure DevOps links, branch status, commit activity)
  - Security vulnerabilities (CodeQL and Advanced Security findings)
  - Dependencies (internal and external)
  - Documentation links (SharePoint)
  - IIS dashboard data integration (when applicable)
  - Linked external dashboards
  - Historical health trends

#### 2.2.3 External Dashboard Integration

**Priority: Medium**

- Support linking to existing/external dashboards related to applications
- Render external dashboards as embedded iframes where possible
- Provide direct links when iframe embedding is not supported
- Manage authentication/authorization for external resources
- Display connection status for integrated dashboards

### 2.3 Data Integration & Synchronization

#### 2.3.1 Data Source Configuration

**Priority: High**

- Provide administrative interface for configuring data sources:
  - Azure DevOps (repository data, security findings, dependencies)
  - SharePoint (documentation)
  - ServiceNow (roles, exported to CSV)
  - IIS log database (usage metrics)
- Configure connection parameters per data source:
  - Connection strings/URLs
  - Authentication credentials (secure storage)
  - API endpoints
  - Refresh frequency/schedule
- Enable/disable individual data sources
- Test connectivity and validate configuration

#### 2.3.2 Data Synchronization

**Priority: High**

- Execute scheduled data pulls from configured sources
- Support configurable refresh intervals per data source and data type
- Provide manual refresh trigger for administrators
- Display last sync timestamp for each data source
- Log sync operations and errors
- Implement incremental updates where supported by source systems
- Handle connection failures with retry logic
- Queue failed sync operations for retry

#### 2.3.3 Data Validation & Quality - **UPDATED**

**Priority: Medium**

**Data Integrity Validation:**

- Validate data integrity after each sync operation
- Detect and flag missing or incomplete data
- Provide data quality metrics and reporting
- Alert administrators to data quality issues

**Data Conflict Detection & Handling:**

- **Identify conflicts between data sources**
- **Conflict resolution strategy: Raise conflicts to user for remediation**
- **Rationale:** This is exactly the type of "lifecycle rot" the system is designed to correct
- **Flag conflicting data prominently:**
  - Visual indicator on application detail page (warning icon/badge)
  - Dedicated "Data Conflicts" section showing all conflicts
  - Include on user task dashboard if user owns the application
  - Count of conflicts displayed in application health summary

**Types of Data Conflicts:**

- **Application name mismatches:**
  - ServiceNow application name vs. SharePoint folder name
  - ServiceNow application name vs. Azure DevOps repository name

- **Role assignment conflicts:**
  - Role assigned in ServiceNow but user not found in Entra ID
  - Multiple users assigned to single-occupant role
  - Role validation contradicts ServiceNow data

- **Repository link conflicts:**
  - ServiceNow links to Azure DevOps repository that doesn't exist
  - Multiple applications claiming same repository

- **Documentation conflicts:**
  - SharePoint folder exists but no matching ServiceNow record
  - ServiceNow record exists but no SharePoint documentation folder

**Conflict Resolution Workflow:**

- System automatically detects conflicts during data synchronization
- Conflicts flagged and logged in dedicated conflict tracking table
- Notification sent to application owner and administrators
- Conflict resolution task created and assigned to application owner
- Resolution options presented based on conflict type:
  - Choose authoritative source for conflicting data
  - Manually reconcile and update source systems
  - Mark as reviewed/accepted (with justification)
- Track conflict resolution history
- Conflicts impact health score (unresolved conflicts = lower score)
- Resolved conflicts removed from active conflict list
- Recurring conflicts flagged as systemic issues requiring process improvement

### 2.4 Health Scoring System

#### 2.4.1 Configurable Scoring Weights

**Priority: High**

- Provide administrative interface for defining health scoring factors:
  - Application age
  - Vulnerability count and severity
  - Usage metrics (frequency, user count)
  - Documentation completeness
  - Role assignment status
  - Task completion rates
  - Dependency health
  - Code quality metrics
  - Compliance status
- Configure weighting/importance for each factor using sliders or numeric input
- Support different scoring profiles for different application types
- Preview impact of weight changes on existing applications

#### 2.4.2 Health Score Calculation

**Priority: High**

- Calculate composite health scores based on configured weights
- Normalize scores to consistent scale (e.g., 0-100)
- Recalculate scores when:
  - Underlying data changes
  - Scoring weights are modified
  - New factors are added
- Store historical health scores for trending analysis
- Provide detailed breakdown of score calculation for transparency

#### 2.4.3 Health Scoring Algorithm - **RECOMMENDED FORMULA**

**Priority: High**

The health scoring system uses a point-based approach on a 0-100 scale, where higher scores indicate healthier applications. The algorithm balances multiple factors with emphasis on security vulnerabilities, active maintenance, and usage patterns.

**Base Score Calculation:**

All applications start at 100 points. Deductions and bonuses are applied based on the following factors:

**Security Vulnerability Penalties:**

- Critical Severity: -15 points each (max -60 points total)
- High Severity: -8 points each (max -40 points total)
- Medium Severity: -2 points each (max -20 points total)
- Low Severity: -0.5 points each (max -10 points total)
- Maximum total deduction from vulnerabilities: -75 points

Rationale: Organization only addresses Medium/Low when time permits, so Critical/High vulnerabilities represent significant risk and should heavily impact the score. Caps prevent single categories from dominating the score entirely.

**Usage Metrics Scoring:**

Based on production usage over the last 3 months:

- No usage (0 requests): -20 points
- Very low usage (1-100 requests/month): -10 points
- Low usage (101-1000 requests/month): -5 points
- Moderate usage (1001-10000 requests/month): 0 points (neutral)
- High usage (10001+ requests/month): +5 points

Rationale: Low or no usage indicates potential retirement candidates. High usage indicates active, valuable applications deserving attention.

**Active Maintenance Bonus:**

Based on repository commit activity:

- Recent commits (within last 30 days): +10 points
- Moderate activity (31-90 days since last commit): +5 points
- Low activity (91-180 days): 0 points (neutral)
- Inactive (181-365 days): -5 points
- Stale (365+ days): -10 points

Rationale: Active maintenance indicates the application is being cared for. Age matters less than whether someone is actively maintaining the code.

**Documentation Completeness Scoring:**

Required documentation (both must exist):
- Architecture diagram present: +5 points
- System documentation present: +5 points
- Both missing: -15 points
- One missing: -10 points

Rationale: Documentation is critical for maintenance and knowledge transfer. Missing documentation creates risk and technical debt.

**Role Assignment Status:**

- All required roles assigned and validated: 0 points (neutral)
- One or more roles missing: -10 points
- Roles unvalidated (> 12 months since last validation): -5 points

Rationale: Clear ownership and accountability are essential for lifecycle management.

**Overdue Task Penalty:**

- Each overdue task: -3 points (max -15 points total)
- Critical overdue tasks (>30 days overdue): -5 points each (max -20 points total)

Rationale: Overdue tasks indicate lifecycle rot and lack of attention. This creates feedback loop encouraging task completion.

**Final Score Normalization:**

After all calculations, scores are clamped to the 0-100 range (no negative scores, no scores above 100).

**Health Score Categories:**

Based on final calculated score:

- **Healthy (80-100)**: Application is well-maintained, secure, and compliant
  - Visual indicator: Green
  - Action: Continue normal monitoring

- **Needs Attention (60-79)**: Application has some issues requiring action
  - Visual indicator: Yellow/Amber
  - Action: Review and prioritize remediation items

- **At Risk (40-59)**: Application has significant problems requiring immediate attention
  - Visual indicator: Orange
  - Action: Create remediation plan with timeline

- **Critical (0-39)**: Application is in poor health and poses organizational risk
  - Visual indicator: Red
  - Action: Immediate intervention required, executive notification

**Score Transparency:**

Each application detail view displays:
- Final health score (0-100)
- Health category (Healthy/Needs Attention/At Risk/Critical)
- Detailed breakdown showing contribution of each factor
- Point deductions and bonuses with explanations
- Comparison to organizational average
- Historical trend (improving/declining/stable)

**Configurable Weights:**

While the above represents the recommended starting formula, administrators can adjust:
- Point values for each vulnerability severity level
- Usage thresholds and corresponding point values
- Maintenance activity scoring windows and point values
- Documentation requirements and point values
- Overdue task penalties
- Health category thresholds

Changes to weights require administrator privileges and are logged in the audit trail.

#### 2.4.4 Threshold-Based Alerting

**Priority: Medium**

- Define health score thresholds for alert generation
- Notify relevant stakeholders when applications cross thresholds
- Support escalation for persistent low-health applications
- Configure alert frequency to avoid notification fatigue

### 2.5 Task & Lifecycle Management

#### 2.5.1 Task Definition & Assignment

**Priority: High**

- Define recurring lifecycle tasks:
  - Role validations
  - Data validations
  - Documentation reviews
  - Security assessments
  - Dependency updates
  - Compliance audits
- Assign tasks based on:
  - Application ownership
  - Role assignments
  - Organizational structure
- Define task completion windows (start date, due date)
- Support task templates for common lifecycle activities

#### 2.5.2 Task Scheduling & Distribution - **UPDATED**

**Priority: High**

**Organizational Task Frequency:**

Configure when routine events occur for the organization with reasonable defaults:

- **Role Validation Tasks:**
  - Frequency: Annually (default)
  - Trigger: Also triggered when role occupant leaves company (no longer discoverable in Entra ID)
  - Staggered: Yes - distribute across the year

- **Documentation Review Tasks:**
  - Frequency: Semi-annually (default recommendation)
  - Staggered: Yes - distribute across 6-month periods

- **Security Assessment Tasks:**
  - Frequency: Quarterly (default recommendation)
  - Staggered: Yes - distribute across quarters

- **Dependency Update Review:**
  - Frequency: Quarterly (default recommendation)
  - Staggered: Yes - distribute across quarters

- **Application Health Review:**
  - Frequency: Monthly for At Risk/Critical apps, Quarterly for Healthy apps
  - Staggered: Yes - intelligent distribution

**Intelligent Scheduling Algorithm:**

The system implements a three-pass scheduling approach to prevent workload clustering:

1. **First pass - Organizational Calendar:**
   - Schedule all tasks based on organizational frequency settings
   - Assign initial due dates based on task type and frequency

2. **Second pass - Workload Distribution:**
   - Analyze individual workload across all applications
   - Identify users responsible for multiple applications
   - Detect workload clustering (multiple tasks with same due date)
   - Redistribute tasks to avoid stacking 100+ tasks on one person on same date

3. **Third pass - Optimization:**
   - Apply staggering within acceptable windows (e.g., distribute annual tasks across 12 months)
   - Maintain task sequence dependencies
   - Balance team workloads
   - Preserve critical deadline requirements

**Workload Distribution Rules:**

- Maximum tasks per user per week: 10 (configurable)
- Minimum spacing between similar tasks for same user: 3 days (configurable)
- If user has multiple applications, stagger their tasks across the scheduling period
- Alert administrators when workload cannot be adequately distributed

**Manual Overrides:**

- Administrators can manually reschedule individual tasks
- Manual rescheduling preserved across automatic recalculations
- Audit trail tracks all manual schedule changes

**Task Dependencies:**

- Support task dependencies and sequencing
- Dependent tasks automatically rescheduled when predecessor changes
- Visual indication of task dependencies in task views

#### 2.5.3 Task Execution & Tracking - **UPDATED**

**Priority: High**

**Task Viewing & Updates:**

- Allow users to view assigned tasks with full details
- Support task status updates:
  - Not started
  - In progress
  - Completed
  - Blocked/deferred
- Require evidence or notes upon task completion where appropriate
- Track task completion history
- Provide task completion metrics and reporting

**Overdue Task Handling:**

- Generate alerts for approaching deadlines (7 days before due date)
- Flag tasks as overdue immediately after due date passes
- **Escalation process for overdue tasks:**
  - Initial overdue: Notify task assignee daily
  - 7 days overdue: Notify assignee's manager
  - 14 days overdue: Notify administrators
  - 30+ days overdue: Executive notification (configurable threshold)
- **Impact on health score:**
  - Each overdue task: -3 points from application health score
  - Critical overdue tasks (>30 days): -5 points each
  - See section 2.4.3 for complete health score calculation
- Display overdue task count prominently on application detail page
- Overdue tasks highlighted in red on task dashboard
- Sorting and filtering by overdue status

**Task Delegation & Reassignment:**

**Who can delegate/reassign tasks:**

- **Task assignee themselves:** Can delegate to another user
- **Privileged administrators:** User IDs defined in appsettings.json can reassign any task
- **Future enhancement:** Manager approval workflow for permanent reassignments

**Delegation capabilities:**

- Temporary delegation: Task returns to original assignee after completion
- Permanent reassignment: Task ownership transferred to new user
- Delegation includes option to add notes/rationale
- Delegated tasks display original assignee and current assignee
- Audit trail tracks all delegation activities
- Notification sent to new assignee when task delegated
- Original assignee notified when delegated task is completed

**Delegation UI:**

- "Delegate Task" button on task detail page
- User picker to select new assignee
- Delegation type selector (temporary/permanent)
- Notes field for delegation rationale
- Confirmation dialog before delegation
- Delegated tasks visually distinguished in task list

### 2.6 Role Management

#### 2.6.1 Role Assignment

**Priority: High**

- Import role assignments from ServiceNow (CSV export)
- Support multiple roles per application/capability:
  - Application Owner
  - Technical Lead
  - Security Champion
  - Documentation Maintainer
  - DevOps Engineer
  - (Other organization-specific roles)
- Associate tasks with specific roles
- Display role assignments on application detail pages
- Track role assignment history and changes

#### 2.6.2 Role Validation Workflow

**Priority: Medium**

- Generate periodic role validation tasks
- Require role holders or managers to confirm or update assignments
- Flag applications with missing or unconfirmed role assignments
- Escalate unvalidated roles after defined threshold period
- Audit trail for role validation activities

### 2.7 Reporting & Analytics

#### 2.7.1 Standard Reports

**Priority: Medium**

- Portfolio health summary report
- Task completion rates by team/individual/application
- Vulnerability aging and remediation trends
- Application usage trends
- Role assignment coverage
- Data quality and sync status
- Support export to common formats (PDF, Excel, CSV)

#### 2.7.2 Custom Reports & Analytics

**Priority: Low**

- Allow users to create custom report views
- Provide flexible filtering and grouping options
- Support scheduled report generation and distribution
- Enable sharing of custom reports across team

### 2.8 User Customization & Preferences - **NEW**

#### 2.8.1 User Settings Storage

**Priority: High**

**Client-side storage:**
- **Use IndexedDB for user settings** (simple, client-side approach)
- Store user preferences locally in browser
- Persist across browser sessions
- No complex server-side storage required for MVP

**Stored preferences:**
- Dashboard layout and widget configuration
- Saved filter combinations and search queries
- View preferences (grid vs. treemap, sort orders, column visibility)
- Theme selection (light/dark mode)
- Notification preferences
- Default landing page

**Settings backup and portability:**
- **Export/import user settings functionality**
- Download settings as JSON file
- Import settings to restore configuration
- Occasional backup prompts (e.g., every 90 days)
- Settings backup to user profile (optional)
- Survives browser cache clears if exported

#### 2.8.2 Theme Support

**Priority: Medium**

- **Light mode and dark mode themes**
- User toggle to switch between themes
- Theme preference saved to IndexedDB
- System respects OS theme preference as default
- WCAG-compliant color schemes for both themes
- Consistent styling across all pages

#### 2.8.3 Custom Dashboard Configuration

**Priority: Medium**

- **Users can configure their dashboard layout**
- Drag-and-drop widget positioning
- Show/hide dashboard sections
- Resize widgets
- Multiple saved dashboard configurations
- Reset to default layout option

#### 2.8.4 Saved Views and Filters

**Priority: Medium**

- **Save filter combinations with names**
- Quick access to frequently used filters
- Share saved views with team members (future enhancement)
- Edit and delete saved views
- Set default view for application lists and heatmaps

### 2.9 Data Feeds and Subscriptions - **NEW**

#### 2.9.1 Data Subscription Mechanism

**Priority: Medium**

**Purpose:**
- **Support data consumers** - system serves as data source for other tools
- Enable external systems to subscribe to data updates
- Push data changes rather than requiring polling

**Subscription formats:**
- **RSS/Atom feeds** for application updates
- Feed per application (health score changes, task updates)
- Organization-wide feed for all changes
- Filtered feeds (by team, health status, etc.)

**Webhook support:**
- Configure webhook endpoints for data consumers
- POST updates to external systems when data changes
- Event types: health score changed, task completed, new vulnerability discovered
- Webhook retry logic for failed deliveries
- Signature verification for security

#### 2.9.2 Real-time Data Access

**Priority: Medium**

- **API endpoints for real-time data access**
- RESTful API for all major data entities
- Query parameters for filtering and pagination
- JSON response format
- API authentication and authorization
- Rate limiting for external consumers
- Comprehensive API documentation

#### 2.9.3 Incremental Data Exports

**Priority: Low**

- **Changes-since-last-export capability**
- Timestamp-based incremental exports
- Reduce data transfer for regular consumers
- Track last export timestamp per consumer
- Combine with full exports for initial sync

### 2.10 AI/ML Integration - **NEW**

#### 2.10.1 AI/ML Infrastructure

**Priority: Medium**

**Production environment:**
- **Azure OpenAI API integration** for production AI features
- Secure API key management
- Configurable endpoints and models
- Rate limiting and cost management
- Error handling and fallback behavior

**Development/testing environment:**
- **Ollama integration** for local AI testing
- No cloud dependency during development
- Cost-free testing and experimentation
- Model compatibility layer between Ollama and Azure OpenAI

**Configuration:**
- Toggle between Ollama (dev) and Azure OpenAI (prod)
- Configurable via appsettings.json
- Environment-specific AI model selection

#### 2.10.2 Predictive Scoring - **NEW**

**Priority: Low (Future Enhancement)**

**Capability:**
- Predict future health scores based on historical trends
- Forecast health score trajectory (improving, declining, stable)
- Identify applications likely to become "At Risk" or "Critical"
- Early warning system for proactive intervention

**Implementation:**
- Analyze historical health score data
- Consider factors: vulnerability trends, task completion rates, usage patterns
- Machine learning model trained on organizational data
- Confidence intervals for predictions
- Visual representation of predicted vs. actual

#### 2.10.3 Anomaly Detection - **NEW**

**Priority: Low (Future Enhancement)**

**Capability:**
- **Detect unusual patterns** in multiple data dimensions:
  - Usage anomalies (sudden drops or spikes)
  - Security finding anomalies (unusual vulnerability patterns)
  - Task completion anomalies (sudden decline in completion rates)
  - Health score anomalies (unexpected drops)

**Implementation:**
- Statistical analysis and machine learning models
- Baseline behavior establishment per application
- Alert on significant deviations from baseline
- Provide context and potential root causes
- False positive management

#### 2.10.4 AI-Enhanced Recommendations - **NEW**

**Priority: Low (Future Enhancement)**

**Capability:**
- **Intelligent recommendation prioritization** using AI
- Natural language explanations for recommendations
- Context-aware suggestions based on application patterns
- Automated root cause analysis for health declines
- Predictive task scheduling optimization

**Implementation:**
- Enhance existing recommendation engine with AI
- Consider organizational patterns and history
- Learn from user actions and feedback
- Personalized recommendations per user role
- Continuous improvement through feedback loops

### 2.11 Extensibility and API Design - **NEW**

#### 2.11.1 API-First Architecture

**Priority: High**

**Design principle:**
- **System designed as a node in larger ecosystem**
- **API-first approach** - all functionality accessible via API
- Internal UI consumes same APIs as external consumers
- Well-documented, stable API contracts
- Versioned APIs for backward compatibility

**API coverage:**
- Applications and health scores
- Tasks and assignments
- Users and roles
- Data sources and sync status
- Notifications and alerts
- Reports and exports
- All CRUD operations where appropriate

#### 2.11.2 Plugin Architecture

**Priority: Low (Future Enhancement)**

**Capability:**
- **Support for plugins and extensions**
- Custom data source plugins
- Custom health scoring algorithm plugins
- Custom visualization plugins
- Custom report templates
- Extensible recommendation engine

**Plugin framework:**
- Plugin discovery and registration
- Dependency management
- Sandboxed execution for security
- Plugin configuration UI
- Plugin marketplace (future)

#### 2.11.3 Webhook and Event System

**Priority: Medium**

**Event-driven architecture:**
- Publish events for significant system activities
- External systems subscribe to events
- Event types: data changes, health score updates, task completions, alerts
- Configurable webhook endpoints
- Retry logic and failure handling
- Event payload with full context

**Implementation:**
- Event bus for internal event processing
- Webhook delivery queue
- Webhook configuration per external system
- Signature verification for security
- Delivery status tracking and monitoring

#### 2.11.4 Integration SDK

**Priority: Low (Future Enhancement)**

- **Developer SDK for third-party integrations**
- Client libraries for common languages (.NET, Python, JavaScript)
- Code samples and tutorials
- Integration testing tools
- Documentation for common integration patterns

### 2.12 Administration & Configuration

#### 2.12.1 System Settings

**Priority: High**

- Configure organizational preferences:
  - Health scoring weights and thresholds
  - Task scheduling parameters
  - Data refresh frequencies
  - Notification preferences
  - User interface customization (branding, themes)
- Manage data source connections
- Configure user roles and permissions
- Set up organizational structure (teams, departments)

#### 2.12.2 User Management

**Priority: High**

- Add, edit, and deactivate user accounts
- Assign user permissions and access levels
- Map users to organizational structure
- Integrate with enterprise identity provider
- Audit user access and activities

#### 2.12.3 Audit & Logging

**Priority: Medium**

- Log all significant system activities:
  - User logins and access
  - Configuration changes
  - Task completions
  - Data synchronization operations
  - Health score changes
- Provide searchable audit log interface
- Support audit log export for compliance purposes
- Retain audit logs per organizational policy

---

## 3. Non-Functional Requirements

### 3.1 Performance Requirements

#### 3.1.1 Response Time

- Dashboard page loads: < 3 seconds for initial view
- Application detail page loads: < 2 seconds
- Heatmap rendering: < 5 seconds for up to 1,000 applications
- Search results: < 1 second for typical queries
- Data refresh operations: Should not impact user experience (background processing)

#### 3.1.2 Scalability

- Support up to 5,000 applications in portfolio
- Handle up to 500 concurrent users
- Process IIS log data for applications with millions of requests per day
- Scale data synchronization to handle multiple large data sources
- Accommodate growing data volume over time without performance degradation

#### 3.1.3 Data Freshness

- Near-real-time data updates (configurable refresh intervals)
- Minimum refresh frequency: Every 15 minutes for critical data sources
- Maximum data staleness tolerance: 1 hour for non-critical sources
- Display last sync timestamp to users for transparency

### 3.2 Security Requirements

#### 3.2.1 Authentication

- Integration with enterprise identity provider (Active Directory, Azure AD, SSO)
- Multi-factor authentication support
- Session management with configurable timeout
- Secure credential storage for service accounts and API keys

#### 3.2.2 Authorization

- Role-based access control (RBAC)
- Principle of least privilege
- User permissions levels:
  - Read-only (view dashboard and application data)
  - User (complete assigned tasks, view personal data)
  - Power User (create custom reports, view all applications)
  - Administrator (configure system, manage users, modify settings)
- Data access controls based on organizational structure (team/department)
- Audit trail for all authorization changes

#### 3.2.3 Data Protection

- Encryption at rest for sensitive data
- Encryption in transit (TLS 1.2 or higher)
- Secure storage of API keys and connection strings
- Data classification and handling based on sensitivity
- Compliance with organizational data protection policies

#### 3.2.4 Vulnerability Management

- Regular security scanning of application code
- Dependency vulnerability tracking and updates
- Penetration testing prior to production deployment
- Security patch management process
- Incident response plan for security events

### 3.3 Availability & Reliability

#### 3.3.1 Uptime Requirements

- Target availability: 99.5% uptime during business hours
- Planned maintenance windows during off-peak hours
- Graceful degradation if data sources are unavailable
- Display notification when system or data sources are experiencing issues

#### 3.3.2 Backup & Recovery

- Daily automated backups of application database
- Backup retention: 30 days minimum
- Recovery Time Objective (RTO): 4 hours
- Recovery Point Objective (RPO): 24 hours
- Documented and tested disaster recovery procedures

#### 3.3.3 Error Handling

- Graceful error handling for data source connection failures
- User-friendly error messages
- Automatic retry logic for transient failures
- Error logging and alerting for administrator attention
- Fallback to cached data when real-time data is unavailable

### 3.4 Usability Requirements

#### 3.4.1 User Interface

- Intuitive, modern web-based interface
- Responsive design supporting desktop, tablet, and mobile devices
- Consistent navigation and layout across all pages
- Keyboard navigation support for accessibility
- Contextual help and tooltips

#### 3.4.2 Accessibility

- WCAG 2.1 Level AA compliance minimum
- Screen reader compatibility
- High contrast mode support
- Keyboard-only navigation capability
- Accessible data visualizations

#### 3.4.3 User Experience

- Minimal clicks to complete common tasks
- Clear visual hierarchy and information architecture
- Loading indicators for long-running operations
- Confirmation dialogs for destructive actions
- Personalization options (dashboard layout, default views)

### 3.5 Maintainability & Supportability

#### 3.5.1 Code Quality

- Comprehensive code documentation
- Adherence to coding standards and best practices
- Unit test coverage: minimum 70%
- Integration test coverage for critical workflows
- Code review process for all changes

#### 3.5.2 Monitoring & Diagnostics

- Application performance monitoring (APM)
- Error tracking and alerting
- Health check endpoints for automated monitoring
- Diagnostic logging with configurable verbosity
- Performance metrics and dashboards for operations team

#### 3.5.3 Documentation

- User guide covering all major features
- Administrator guide for configuration and management
- API documentation for integrations
- Deployment and operations runbook
- Architecture and design documentation

### 3.6 Compatibility Requirements

#### 3.6.1 Browser Support

- Modern browsers within last 2 major versions:
  - Google Chrome
  - Microsoft Edge (Chromium-based)
  - Mozilla Firefox
  - Apple Safari

#### 3.6.2 Integration Compatibility

- Azure DevOps REST API (current version)
- SharePoint Online/Server (specify version)
- ServiceNow (specify version and integration method)
- SQL Server (specify version for IIS log database)

### 3.7 Compliance & Regulatory

#### 3.7.1 Compliance Requirements

- Adherence to organizational IT policies and standards
- Data retention policies compliance
- Audit requirements for regulatory compliance
- Privacy requirements (GDPR, CCPA if applicable)

---

## 4. Data Integration Requirements

### 4.1 Azure DevOps Integration

#### 4.1.1 Repository Data

**Data Elements:**

- Repository name and URL
- Default branch
- Branch list and protection policies
- Recent commit activity (count, frequency, last commit date)
- Pull request metrics
- Build pipeline status
- Release pipeline status

**Integration Method:**

- Azure DevOps REST API
- Authentication: Personal Access Token (PAT) or OAuth 2.0
- Permissions required: Code (Read), Build (Read), Release (Read)
- Organization and project identifiers configurable via admin interface

**Migration Path:**

- Current state: One-off Node.js scripts generating CSV and JSON files
- Goal: Productionize into proper scheduled jobs with monitoring
- All configuration parameters (organization, project) exposed in admin UI

**Refresh Frequency:**

- Weekly scheduled jobs (sufficient for current needs)
- Future: Real-time updates for critical events (build failures) via webhooks
- Job execution tracked in dashboard with history and metadata

#### 4.1.2 CodeQL & Advanced Security Data

**Data Elements:**

- Security vulnerability findings
- Severity levels (Critical, High, Medium, Low)
- Vulnerability categories (SQL Injection, XSS, etc.)
- Date discovered
- Remediation status
- False positive markings
- Code scanning alerts
- Secret scanning alerts
- Dependency scanning results

**Integration Method:**

- Azure DevOps Advanced Security API
- GitHub Advanced Security API (if applicable)
- Authentication: Same as repository data

**Refresh Frequency:**

- Scheduled pulls: Every 4 hours for active vulnerabilities
- Alert webhooks for new critical/high severity findings

#### 4.1.3 Dependency Data

**Data Elements:**

- Direct and transitive dependencies
- Dependency versions
- Known vulnerabilities in dependencies
- License information
- Update availability
- Dependency graph

**Integration Method:**

- Azure Artifacts API
- Package manifest parsing (package.json, requirements.txt, pom.xml, etc.)
- Integration with vulnerability databases (NVD, GitHub Advisory Database)

**Refresh Frequency:**

- Daily dependency scans
- Weekly deep analysis

### 4.2 SharePoint Integration

#### 4.2.1 Documentation Data

**Data Elements:**

- Document library contents
- Document metadata (title, author, last modified date)
- Links to application-specific documentation
- Documentation completeness indicators
- Version history

**Integration Method:**

- SharePoint REST API or Microsoft Graph API
- Authentication: Azure AD OAuth 2.0
- Permissions required: Sites.Read.All or specific site permissions
- All configuration exposed in admin interface

**Current Documentation Structure:**

- Hierarchy: `documents/capabilities/apps/[technical, project, userfacing, support, architecture]`
- "capabilities" level contains organizational capability groupings
- "apps" level contains applications belonging to that capability
- Template folders (technical, project, userfacing, support) indicate a valid application entry
- Folder name must match the application name in ServiceNow
- ServiceNow records contain links to Azure DevOps repositories
- **Note:** This structure can be improved upon in the new implementation

**Migration Path:**

- Current state: PowerShell scripts generating CSV and JSON files
- Goal: Productionize into proper scheduled jobs
- Organization and site identifiers configurable in application

**Refresh Frequency:**

- Weekly scheduled synchronization (sufficient for current needs)
- Job execution tracked in dashboard with history and metadata
- On-demand document retrieval when viewing application details

**Data Mapping:**

- Map SharePoint folders/libraries to applications/capabilities using improved structure
- Use template folders to identify valid application entries
- Cross-reference with ServiceNow application names
- Validate Azure DevOps repository links from ServiceNow

### 4.3 ServiceNow Integration

#### 4.3.1 Role Assignment Data

**Data Elements:**

- Application/capability identifier
- Role type
- Assigned user (name, email, employee ID)
- Assignment date
- Validation status
- Manager information

**Integration Method:**

- CSV export from ServiceNow (initial approach)
- Future: ServiceNow REST API for real-time integration
- File upload interface for administrators
- Automated scheduled imports from shared location

**Refresh Frequency:**

- Weekly CSV imports (manual or automated)
- Real-time API integration (future enhancement)

**Data Validation:**

- Validate user identifiers against identity provider
- Check for duplicate role assignments
- Flag missing or incomplete data

### 4.4 IIS Log Data Integration

#### 4.4.1 Usage Metrics Data

**Data Elements:**

- Application identifier
- Environment (dev, test, staging, production)
- Timestamp
- Total requests per month
- Distinct users per month
- Usages per month (handles SPA scenarios where loading root = one usage session)
- **Note:** NO individual user data collected - privacy-focused aggregate data only

**Integration Method:**

- Direct SQL database connection to existing IIS usage database (simplest approach)
- Connection string with read-only access
- IIS data collection is a SEPARATE process - this app only connects to that database
- All connection parameters configurable in admin interface

**Refresh Frequency:**

- Weekly data synchronization (sufficient for current needs)
- Job execution tracked in dashboard
- Historical data retention: 12+ months

**Data Processing:**

- Data is pre-aggregated in IIS database
- Metrics captured: total requests, distinct user count, usage sessions
- Usage session logic handles single-page applications appropriately
- Privacy-first: only aggregate metrics, no individual user tracking

### 4.5 Job Scheduling & Monitoring Requirements

**Priority: High**

#### 4.5.1 Job Configuration

- All data integration jobs must be configurable through admin interface
- Configuration parameters include:
  - Azure DevOps organization and project identifiers
  - SharePoint site and document library locations
  - IIS database connection strings
  - ServiceNow export file locations or API endpoints
  - Any other data source identifiers or connection parameters

#### 4.5.2 Job Scheduling

- Jobs should run on a regular schedule (weekly frequency sufficient)
- Configurable schedule per job type
- Support manual job triggering by administrators
- Staggered job execution to prevent resource contention

#### 4.5.3 Job Monitoring Dashboard

**Display Requirements:**

- List of all configured data integration jobs
- Job status indicators (scheduled, running, success, failed)
- Last run timestamp for each job
- Next scheduled run time
- Job execution history with timestamps
- Success/failure history with trends
- Execution duration metrics
- Error messages and logs for failed jobs
- Job metadata (data source, configuration, frequency)

**Job History:**

- Maintain historical record of all job executions
- Track execution time, records processed, errors encountered
- Provide detailed logs for troubleshooting
- Filterable and searchable job history
- Export capability for job execution reports

#### 4.5.4 Job Alerting

- Alert administrators when jobs fail
- Notify on repeated failures or patterns
- Threshold-based alerting for job execution duration
- Email or in-app notification options

#### 4.5.5 Migration from Current State

- Current approach: Manual execution of Node.js (Azure DevOps) and PowerShell (SharePoint) scripts
- Scripts generate CSV and JSON files used as data sources
- New system productionizes this into scheduled, monitored, auditable jobs
- Maintain backwards compatibility during transition period

### 4.6 Development Mode & Mock Data Requirements

**Priority: High**

#### 4.6.1 Development Environment Constraints

- Development environment is disconnected from network - cannot test actual data source connections
- Must assume data structures based on documentation and current scripts
- All development and testing relies on comprehensive mock data

#### 4.6.2 Mock Data Requirements

**Scope:**

- Create comprehensive mock data covering all data sources:
  - Azure DevOps: repositories, commits, PRs, security findings, dependencies
  - SharePoint: document hierarchy, metadata, completeness indicators
  - ServiceNow: role assignments, application metadata
  - IIS database: usage metrics (requests, users, usage sessions)
- Mock data should represent realistic scenarios:
  - Applications with varying health scores
  - Different vulnerability severities and counts
  - Various usage patterns (high usage, low usage, inactive)
  - Complete and incomplete documentation
  - Valid and missing role assignments
  - Recent and stale data

**Mock Data Toggle:**

- Provide application setting/configuration toggle to enable "Development Mode"
- When enabled, application loads with sample/mock data
- UI should clearly indicate when viewing mock data
- Mock data should be sufficient for complete UI development and testing
- Toggle accessible via admin configuration
- Support multiple mock data sets for different scenarios (small portfolio, large portfolio, problematic data)

#### 4.6.3 Data Structure Assumptions

- Document assumed data structures for each source system
- Include data type specifications, field names, relationships
- Validate assumptions with stakeholders when network access available
- Design system to be flexible for adjustments when real data tested

#### 4.6.4 Mock Data Coverage

- All UI screens should be fully functional with mock data
- Cover edge cases: empty states, error conditions, extreme values
- Include data for testing filtering, sorting, search functionality
- Provide data that triggers all validation rules and business logic
- Support testing of health scoring algorithm with various inputs

### 4.7 Data Model & Relationships

#### 4.7.1 Core Entities

- **Application/Capability**: Central entity with unique identifier
- **User**: Individuals interacting with the system
- **Role Assignment**: Links users to applications via roles
- **Task**: Lifecycle management activities assigned to users/roles
- **Health Score**: Time-series data for application health
- **Data Source Sync**: Metadata about synchronization operations
- **Job Execution**: Record of scheduled job runs with status and metadata
- **Audit Log**: Historical record of system activities

#### 4.7.2 Entity Relationships

- Application → Role Assignments (1:many)
- Role Assignment → User (many:1)
- Application → Tasks (1:many)
- Task → User (many:1)
- Application → Repository Data (1:1)
- Application → Security Findings (1:many)
- Application → Documentation Links (1:many)
- Application → Usage Metrics (1:many)
- Application → Health Scores (1:many time-series)
- Data Source → Job Executions (1:many)
- Job Execution → Audit Log (1:many)

---

## 5. User Roles & Permissions

### 5.1 User Role Definitions

#### 5.1.1 Read-Only User

**Access Level:** View-only

**Permissions:**

- View organization-wide heatmap
- View application details (all public information)
- View public reports and analytics
- Search and browse application catalog

**Restrictions:**

- Cannot complete tasks
- Cannot modify any data
- Cannot access administrative functions
- Cannot view sensitive security details

**Use Case:** Executives, stakeholders, auditors needing visibility without operational responsibilities

#### 5.1.2 Standard User

**Access Level:** User with assigned responsibilities

**Permissions:**

- All Read-Only User permissions, plus:
- View personalized task dashboard
- Complete assigned tasks
- Update task status and add notes
- View detailed security findings for assigned applications
- Acknowledge and respond to recommendations
- View personal activity history

**Restrictions:**

- Cannot access applications/data outside their assignments
- Cannot modify system configuration
- Cannot manage users or roles
- Limited access to organization-wide analytics

**Use Case:** Application owners, developers, security champions with lifecycle responsibilities

#### 5.1.3 Power User

**Access Level:** Advanced user with broader access

**Permissions:**

- All Standard User permissions, plus:
- View all applications regardless of assignment
- Create and save custom reports
- Export data and reports
- View organization-wide analytics and trends
- Access detailed health score calculations
- View all user activity (for their team/department)

**Restrictions:**

- Cannot modify system configuration
- Cannot manage users or role assignments
- Cannot configure data sources

**Use Case:** Team leads, project managers, portfolio managers needing broader visibility

#### 5.1.4 Administrator

**Access Level:** Full system access

**Permissions:**

- All Power User permissions, plus:
- Configure health scoring weights and thresholds
- Configure task scheduling and lifecycle parameters
- Manage data source connections and refresh schedules
- Create and modify user accounts
- Assign permissions and access levels
- Configure organizational structure (teams, departments)
- View and export complete audit logs
- Trigger manual data synchronization
- Manage system settings and preferences
- Access diagnostic and monitoring tools

**Restrictions:**

- Limited by organizational policies and compliance requirements

**Use Case:** IT administrators, system owners responsible for platform management

#### 5.1.5 Security Administrator (Optional)

**Access Level:** Security-focused administrative access

**Permissions:**

- View all security findings across organization
- Configure security-related thresholds and alerts
- Manage security task definitions and assignments
- Export security reports and compliance data
- View security-related audit logs

**Restrictions:**

- Cannot modify user accounts or general system settings
- Limited to security-related configurations

**Use Case:** Security team members responsible for vulnerability management

### 5.2 Permission Model

#### 5.2.1 Data Access Controls

- **Application-level permissions**: Users can only access applications where they have explicit assignment or appropriate role
- **Team/Department filtering**: Users see data relevant to their organizational unit
- **Sensitive data masking**: Restrict access to sensitive security details based on role
- **Audit log access**: Restricted to administrators; users can view their own activity only

#### 5.2.2 Feature Access Controls

- **Task management**: Users can only complete tasks assigned to them
- **Configuration access**: Restricted to administrators
- **Reporting**: Standard users see personal reports; power users see team reports; admins see all
- **Data export**: Power users and admins only

#### 5.2.3 Integration with Identity Provider

- User roles and permissions managed within application or synced from enterprise directory
- Group-based access control for simplified management
- Just-in-time provisioning for new users
- Periodic synchronization of user attributes

---

## 6. Technical Architecture Considerations

### 6.1 Application Architecture

- Web-based application accessible via browser
- Potential technology stacks to evaluate (see Open Questions)
- RESTful or GraphQL API for data access
- Background job processing for data synchronization
- Message queue for asynchronous task processing
- Caching layer for performance optimization

### 6.2 Data Storage

- Relational database for application data, users, tasks, role assignments
- Time-series database consideration for health scores and usage metrics
- Document/blob storage for cached external data
- In-memory cache (Redis/similar) for frequently accessed data

### 6.3 Integration Architecture

- API clients for each external data source
- Webhook receivers for real-time notifications (where supported)
- ETL/data pipeline for IIS log processing
- Retry and circuit breaker patterns for resilience
- API gateway for external access (if needed)

### 6.4 Deployment Architecture

- Containerized deployment (Docker/Kubernetes consideration)
- Multi-tier architecture (web, application, data layers)
- Load balancing for high availability
- Separate environments (dev, test, staging, production)
- CI/CD pipeline for automated deployment

---

## 7. Open Questions

These questions must be answered before detailed design and implementation can begin.

### 7.1 Technology Stack & Platform

**Q1:** What is the preferred technology stack for development?

- Front-end framework: React, Angular, Vue.js, Blazor, or other? Answer: Blazor and dotnet 10
- Back-end framework: .NET Core, Node.js, Python (Django/Flask), Java (Spring Boot), or other? Dotnet 10
- UI component library preference? Vanilla as much as possible, otherwise we can use bootstrap
- State management approach? Would be nice for data to persist on page

**Q2:** What is the target deployment environment?

- On-premise servers, cloud (Azure, AWS, GCP), or hybrid? On premise windows server
- Containerization required (Docker, Kubernetes)? no
- Infrastructure as Code tools (Terraform, ARM templates)? no
- Existing infrastructure constraints? no

**Q3:** What database platform should be used?

- SQL Server, PostgreSQL, MySQL, or other relational database? sql server
- NoSQL requirements (MongoDB, Cosmos DB)?no
- Time-series database for metrics (InfluxDB, TimescaleDB)? no
- Preference for managed services vs. self-hosted?self-hosted

**Q4:** Are there organizational standards or existing platforms to leverage?

- Enterprise architecture guidelines?
- Approved technology list?
- Existing monitoring/observability platforms?
- Standard authentication providers?

### 7.2 Authentication & Authorization

**Q5:** What identity provider will be used?

- Azure Active Directory (Azure AD / Entra ID)? Entra
- Active Directory with ADFS?
- Okta, Auth0, or other third-party IDP?
- Custom user directory?

**Q6:** What authentication protocols are required?

- SAML 2.0, OAuth 2.0 / OpenID Connect, WS-Federation?
- Multi-factor authentication (MFA) required or optional?
- Certificate-based authentication needed?

**Q7:** How should user roles and permissions be managed?

- Dynamic role assignment based on ServiceNow data

**Q8:** Are there specific compliance requirements for authentication? no, just that entra sso works

### 7.3 Data Integration Specifics

**Q9:** Azure DevOps Integration - **ANSWERED**

- Organization(s) and project(s): Configurable via admin interface
- Multiple instances: Not initially, but configuration should support this
- Authentication: PAT or OAuth 2.0 with configurable credentials
- Current state: One-off Node.js scripts generating CSV/JSON files
- Goal: Productionize into scheduled jobs running weekly
- GitHub support: Not required initially

**Q10:** SharePoint Integration - **ANSWERED**

- SharePoint Online or Server: [To be confirmed - likely SharePoint Online based on Azure AD/Entra integration]
- Documentation structure: `documents/capabilities/apps/[technical, project, userfacing, support, architecture]`
  - "capabilities" level = organizational capability groupings
  - "apps" level = applications within capabilities
  - Template folders (technical, project, userfacing, support) indicate valid application
  - Folder name must match ServiceNow application name
- Current state: PowerShell scripts generating CSV/JSON files
- Goal: Productionize into scheduled jobs running weekly
- Organization/site parameters: Configurable in admin interface
- Note: Current structure can be improved in new implementation

**Q11:** ServiceNow Integration - **ANSWERED**

- Current process: CSV export (details to be confirmed)
- Real-time API: Future enhancement, CSV sufficient initially
- Authoritative identity source: [To be confirmed - likely Entra/Azure AD]
- Update frequency: Weekly sufficient
- ServiceNow also contains links to Azure DevOps repositories
- ServiceNow application names must match SharePoint folder names

**Q12:** IIS Log Data Integration - **ANSWERED**

- Current state: Separate process already captures IIS data in SQL database
- This application connects to existing IIS usage database (read-only)
- Aggregation: Pre-aggregated metrics in database
  - Total requests per month
  - Distinct users per month
  - Usages per month (handles SPA scenarios)
- Privacy: NO individual user data - aggregate only
- Connection: Direct SQL connection, configurable in admin interface
- Data retention: 12+ months (existing database)
- Simplest approach: Connect to existing database rather than re-implement collection

**Q13:** Additional Data Sources

- Are there other data sources mentioned ("others") that should be included?
- APM tools (Application Insights, Dynatrace, New Relic)?
- Ticketing systems beyond ServiceNow?
- Configuration management databases (CMDB)?
- Cost management or licensing data?

### 7.4 Data Refresh & Synchronization

**Q14: What are the specific data freshness requirements? - ANSWERED**

- **Weekly refresh is sufficient** for current needs
- No data sources currently require more frequent updates
- Data refresh can happen during off-hours (scheduled weekly jobs)
- See section 4.5.2 for job scheduling details

**Q15: What is the expected data volume? - ANSWERED**

- **Applications in portfolio: ~300 currently**
- **Users accessing system: ~100**
- **Historical data retention: As long as project lifecycle** (retain for life of application)
- Volume of security findings: Expected to vary by application, no specific limits defined
- Usage logs: Pre-aggregated in IIS database, 12+ months retention

**Q16: How should data conflicts or inconsistencies be handled? - ANSWERED**

- **Raise conflicts to user for remediation**
- This is exactly the type of "lifecycle rot" the system is designed to correct
- **Flag conflicting data prominently** with visual indicators
- See section 2.3.3 for complete conflict detection and resolution workflow
- Conflicts impact health score negatively until resolved

### 7.5 Health Scoring & Business Rules

**Q17: What are the initial default weights for health scoring factors? - ANSWERED**

**See section 2.4.3 for the complete recommended health scoring algorithm.**

Summary of recommended weights:
- **Vulnerability severity:** Critical (-15 pts each), High (-8 pts), Medium (-2 pts), Low (-0.5 pts)
- **Usage metrics:** No usage (-20 pts) to high usage (+5 pts)
- **Active maintenance:** Recent commits (+10 pts) to stale code (-10 pts)
- **Documentation completeness:** Both present (+10 pts), missing (-15 pts)
- **Role assignment:** Missing roles (-10 pts), unvalidated (-5 pts)
- **Overdue tasks:** -3 pts each (critical overdue -5 pts)

All weights are configurable by administrators.

**Q18: How should application age be scored? - ANSWERED**

- **Age is NOT as important as usage and maintenance activity** (stakeholder priority)
- Instead of directly scoring age, the system scores **maintenance activity** (commit recency)
- Recent commits (+10 pts) indicate active maintenance regardless of application age
- Old applications with recent commits are considered healthy
- Young applications without commits are penalized as stale
- See section 2.4.3 "Active Maintenance Bonus" for complete scoring details

**Q19: How should vulnerability severity be weighted? - ANSWERED**

- **Critical/High vulnerabilities should weigh heavily** (stakeholder priority)
- Organization only addresses Medium/Low when time permits
- Point-based penalty system:
  - Critical: -15 points each (max -60 total)
  - High: -8 points each (max -40 total)
  - Medium: -2 points each (max -20 total)
  - Low: -0.5 points each (max -10 total)
- Maximum total vulnerability deduction: -75 points
- Vulnerability age not factored in initial implementation (future enhancement)
- See section 2.4.3 "Security Vulnerability Penalties" for complete details

**Q20: How should low-usage applications be handled? - ANSWERED**

- **Flag for review and potential retirement**
- Purpose: Identify candidates to **justify keeping or finally retire**
- Usage-based scoring (last 3 months):
  - No usage: -20 points (high-priority retirement review)
  - Very low usage (1-100 req/month): -10 points (medium-priority review)
  - Low usage (101-1000 req/month): -5 points (low-priority review)
  - Moderate usage: 0 points (neutral)
  - High usage: +5 points (bonus for valuable applications)
- Low-usage apps flagged in recommendations with retirement review workflow
- Application owners can justify retention despite low usage
- See section 2.1.3 "Low-Usage Application Recommendations" and section 2.4.3 "Usage Metrics Scoring"

**Q21: What constitutes "documentation completeness"? - ANSWERED**

**Required documentation:**
- **Architecture diagram** (must exist)
- **System documentation** (some type of system documentation must exist)

**Scoring:**
- Both present: +10 points (+5 each)
- One missing: -10 points
- Both missing: -15 points

**Assessment method:**
- Automated assessment based on SharePoint folder structure
- Check for presence of files in "architecture" folder (architecture diagram)
- Check for presence of files in "technical" or "project" folders (system documentation)
- Manual review capability for administrators to override automated assessment
- See section 2.4.3 "Documentation Completeness Scoring" for complete details

### 7.6 Task Scheduling & Workload Management

**Q22: What are the specific lifecycle tasks that should be automated? - ANSWERED**

**All tasks should happen routinely.** Recommended default frequencies:

- **Role validations:**
  - **Annually** (default)
  - **ALSO triggered when role occupant leaves company** (no longer discoverable in Entra ID)
  - Staggered across the year

- **Documentation reviews:**
  - Semi-annually (recommended default)
  - Staggered across 6-month periods

- **Security assessments:**
  - Quarterly (recommended default)
  - Performed by application security champion role
  - Staggered across quarters

- **Dependency updates:**
  - Quarterly review (recommended default)
  - Staggered across quarters

- **Application health reviews:**
  - Monthly for At Risk/Critical applications
  - Quarterly for Healthy applications
  - Frequency adjusts dynamically based on health score

See section 2.5.2 for complete task scheduling and distribution details.

**Q23: How should task scheduling conflicts be resolved? - ANSWERED**

- **Don't stack 100 due tasks on an individual on the same due date**
- **Distribute workload intelligently** using three-pass scheduling algorithm
- Workload distribution rules:
  - Maximum tasks per user per week: 10 (configurable)
  - Minimum spacing between similar tasks: 3 days (configurable)
  - Stagger tasks for users with multiple applications
  - Alert administrators when workload cannot be adequately distributed
- Automatic load balancing with manual override capability
- Prioritization based on health score and task urgency
- See section 2.5.2 "Intelligent Scheduling Algorithm"

**Q24: What is the preferred staggering approach? - ANSWERED**

- **Intelligent distribution** based on individual workload balancing
- Three-pass scheduling algorithm:
  1. First pass: Schedule based on organizational calendar
  2. Second pass: Analyze and redistribute to prevent workload clustering
  3. Third pass: Optimize while preserving dependencies and critical deadlines
- Distribution considers:
  - User's total task count across all applications
  - Task due date clustering detection
  - Team/department workload patterns
  - Task dependencies and sequencing
- NOT purely random, alphabetical, or team-based - workload-optimized
- See section 2.5.2 "Intelligent Scheduling Algorithm" for complete details

**Q25: How should overdue tasks be handled? - ANSWERED**

- **Escalate overdue tasks**
- **Overdue tasks impact health score negatively**

**Escalation process:**
- Initial overdue: Daily notification to task assignee
- 7 days overdue: Notify assignee's manager
- 14 days overdue: Notify administrators
- 30+ days overdue: Executive notification (configurable)

**Health score impact:**
- Each overdue task: -3 points
- Critical overdue tasks (>30 days): -5 points each
- See section 2.4.3 for health score calculation

**No grace period** - tasks flagged as overdue immediately after due date

See section 2.5.3 "Overdue Task Handling" for complete workflow.

**Q26: Can tasks be delegated or reassigned? - ANSWERED**

**Yes, tasks can be delegated/reassigned by:**

- **The task assignee themselves** - can delegate to another user
- **Privileged admins** - user IDs defined in appsettings.json can reassign any task

**Delegation capabilities:**
- Temporary delegation (task returns to original assignee after completion)
- Permanent reassignment (ownership transferred)
- Delegation includes notes/rationale
- Full audit trail of all delegation activities
- Notifications sent to new assignee and original assignee
- Delegated tasks visually distinguished in UI

**Future enhancement:** Manager approval workflow for permanent reassignments

See section 2.5.3 "Task Delegation & Reassignment" for complete details.

### 7.7 User Experience & Interface

**Q27: What is the priority user persona? - ANSWERED**

- **Primary users: Individual contributors with tasks** (application owners, developers, security champions)
- The default experience should be optimized for task-focused individual contributors
- Task dashboard should be prominent default landing page
- Quick access to assigned tasks and application health for owned applications

**Q28: What devices will users primarily access the system from? - ANSWERED**

- **Desktop via browser only**
- No mobile app required
- No tablet optimization required
- Responsive design not critical (desktop-first approach acceptable)
- Focus on desktop browser experience with modern screen resolutions

**Q29: Are there specific visualization preferences? - ANSWERED**

**Heatmap style:**
- **BOTH grid and treemap views** should be supported
- Allow users to toggle between visualization types
- Grid view for structured, tabular organization
- Treemap view for hierarchical, space-efficient visualization

**Color schemes:**
- **No specific branding requirements**
- **MUST follow contrast visibility and accessibility guidelines (WCAG)**
- High contrast for color-blind users
- Clear visual distinction between health categories
- Avoid reliance on color alone for information

**Q30: What level of customization should be available to users? - ANSWERED**

**User customization features:**
- **Custom dashboards: Yes** - users can configure their dashboard layout
- **Saved views: Yes** - save filter combinations and view preferences
- **Light/dark mode: Yes** - theme selection support required

**Storage:**
- **User settings saved to IndexedDB** (keeps things simple, client-side storage)
- Avoid complex server-side user preference storage initially
- Settings persist across browser sessions

**Backup mechanism:**
- **Need backup capability for user settings** to survive browser resets
- Export/import user settings functionality
- Occasional backup prompt or automatic backup to user profile
- Allow users to download their settings configuration

**Q31: How should the system handle large portfolios? - ANSWERED**

**Scrolling approach:**
- **Infinite scroll preferred** for application lists and heatmap
- Better user experience than pagination
- Load additional items as user scrolls
- Performance optimization critical

**Portfolio size:**
- **~300 applications** currently (expected portfolio size)
- System should handle this smoothly with good performance
- Optimize for: performance, user experience, usability, functionality (in that priority order)

**Performance expectations:**
- Fast initial load with progressive enhancement
- Smooth scrolling without lag
- Efficient rendering of large datasets
- Virtual scrolling techniques where appropriate

### 7.8 Notifications & Alerts

**Q32: What notification channels should be supported? - ANSWERED**

**Primary notification channel:**
- **In-app notifications FIRST** (primary channel for all notifications)
- Notification center within the application
- Visual indicators for unread notifications
- Toast/banner notifications for real-time events

**Future enhancement:**
- **Email: Add later as enhancement** (not MVP requirement)
- Email notifications for critical events when ready
- Lower priority than in-app notifications

**Not required:**
- SMS notifications
- Push notifications
- Teams/Slack integration (future consideration)

**Q33: What events should trigger notifications? - ANSWERED**

**Standard notifications:**
- **Approaching due dates:**
  - **30 days warning** (first notice)
  - **14 days warning** (second notice)
  - 7 days warning (from existing overdue task section)
- **Overdue warnings:** Daily notifications for overdue tasks
- New task assigned
- Task delegated to user
- Data sync failures (administrators only)

**SPECIAL notification:**
- **Organizational workload warning:**
  - IF an upcoming date triggers large number of tasks (40+ tasks) for entire organization
  - **Send warning email to organization leadership**
  - Include information on what's coming and how many people are affected
  - Provide link to adjust schedules in admin app
  - Sent well in advance (e.g., 60 days before high-volume date)

**Future enhancements:**
- Application health score drops below threshold
- New critical security vulnerability discovered
- Data conflict detected

**Q34: How should notification preferences be managed? - ANSWERED**

**User-configurable preferences:**
- **Yes, users can configure their notification preferences**
- Settings for each notification type (enable/disable)
- Frequency preferences (real-time, daily digest, weekly summary)
- Notification delivery preferences (in-app only, email when available)
- Quiet hours or do-not-disturb settings

**Organizational defaults:**
- Administrators can set organizational default notification preferences
- New users inherit organizational defaults
- Users can customize from defaults

**Opt-out capabilities:**
- Users can opt out of non-critical notifications
- Critical notifications (e.g., overdue tasks, security alerts) cannot be disabled
- Administrative notifications always delivered

### 7.9 External Dashboard Integration

**Q35: What external dashboards currently exist? - ANSWERED**

**Current state:**
- **Unknown currently** - stakeholder not certain of all existing dashboards
- **Likely Power BI dashboards** based on organizational tooling
- Specific URLs and dashboard types to be catalogued during implementation
- Authentication requirements unknown at this time

**Approach:**
- Design flexible dashboard linking capability
- Support multiple dashboard types (Power BI, custom dashboards, etc.)
- Manual configuration by administrators initially
- Document discovered dashboards during rollout

**Q36: How should external dashboards be linked to applications? - ANSWERED**

**Linking method:**
- **Regular hyperlinks for now** (simple approach)
- Open external dashboards in new tab/window
- Store dashboard URLs in application metadata
- Display as clickable links in application detail view

**Future enhancements:**
- iframe embedding where supported (CORS permitting)
- Convention-based URL patterns for automation
- Dashboard type detection and appropriate rendering

**Configuration:**
- Manual configuration by administrators
- Associate dashboard URLs with applications
- Support multiple dashboards per application
- Link labels/descriptions for clarity

**Q37: Should the system support single sign-on to external dashboards? - ANSWERED**

**MVP approach:**
- **Not required for MVP** - answer not provided/not critical
- Separate authentication acceptable initially
- Users authenticate to external dashboards independently

**Future enhancement:**
- SSO integration when Power BI integration formalized
- Token passing via URL parameters where supported
- Seamless authentication experience

### 7.10 Reporting & Exports

**Q38: What standard reports are most critical? - ANSWERED**

**Report format priorities:**
- **Prioritize CSV and JSON** (open formats, maximum flexibility)
- These formats support external analysis and data pipeline integration
- Easy to consume by other tools and systems

**Lower priority formats:**
- PDF reports (nice-to-have, not critical)
- Excel exports (lower priority than CSV)
- HTML reports (can be generated from web UI)

**Distribution methods:**
- Download from web interface (primary)
- Save to user's local machine
- Future: Email delivery when email integration added

**Q39: Should reports be scheduled and automated? - ANSWERED**

**Scheduling:**
- **On-demand only** (no scheduled reports needed for MVP)
- Users generate reports when needed
- Download immediately from web interface
- No automated report generation or distribution

**Future enhancements:**
- Scheduled weekly/monthly executive summaries
- Compliance reports on fixed schedules
- Email distribution lists
- Saved report configurations that can be re-run

**Q40: What data should be exportable? - ANSWERED**

**Exportable data:**
- **ALL data should be exportable** (maximize usability)
- Portfolio-wide exports (all applications, all health data)
- Filtered subsets (specific teams, applications, date ranges)
- Individual application details
- Task lists and completion history
- Health score trends and historical data
- Audit logs and sync history

**Data feeds and subscriptions:**
- **User asked: "Is that possible?"**
- **Answer: YES - implement RSS/Atom feeds or similar subscription mechanism**
- Data consumers can subscribe to updates
- Push notifications for data changes
- Webhook support for external systems to receive updates
- API endpoints for real-time data access

**Export formats:**
- CSV for tabular data (primary)
- JSON for structured data and API responses
- Support for bulk exports
- Incremental exports (changes since last export)

### 7.11 Testing & Quality Assurance

**Q41: What testing environments are required? - ANSWERED**

**Environments:**
- **Dev** (development environment with mock data)
- **Test** (testing environment for stakeholder validation)
- **Prod** (production environment)

**Total: 3 environments**

**Test data strategy:**
- **Mock systems and mock data for now**
- Comprehensive mock data covering all scenarios (see section 4.6)
- No anonymized production data available initially
- Synthetic data representing realistic portfolio

**Environment approach:**
- Development: Disconnected from network, uses mock data exclusively
- Test: Stakeholder will test the application with mock or limited real data
- Production: Full data integration when ready

**Q42: What are the acceptance criteria for go-live? - ANSWERED**

**Current status:**
- **Still ideating, TBD** (not finalized yet)
- Stakeholder will define specific acceptance criteria later
- Basic expectations: functional application meeting core requirements

**Anticipated criteria (to be confirmed):**
- All core features functional (heatmap, task dashboard, health scoring)
- Mock data demonstrations successful
- Stakeholder testing completed satisfactorily
- Performance acceptable for ~300 applications
- No critical bugs or security issues

**Validation approach:**
- Stakeholder-led testing and validation
- Iterative feedback and refinement
- Go-live decision by stakeholder when ready

**Q43: What is the testing approach for data integrations? - ANSWERED**

**Testing approach:**
- **Mock systems and mock data for now**
- **Stakeholder will test the application** personally
- Development environment disconnected from network (cannot test real connections)
- Comprehensive mock data representing all data sources

**Mock data coverage (see section 4.6):**
- Azure DevOps mock data (repositories, security findings, dependencies)
- SharePoint mock data (documentation structure, metadata)
- ServiceNow mock data (role assignments, application metadata)
- IIS database mock data (usage metrics)

**Validation strategy:**
- Develop against assumed data structures
- Validate assumptions when network access available
- Stakeholder validates with real data in test/production
- Iterative refinement based on actual data shape

**Error scenario testing:**
- Mock error conditions (failed connections, missing data, conflicts)
- Test error handling and retry logic
- Validate user-facing error messages
- Ensure graceful degradation

### 7.12 Deployment & Operations

**Q44: What is the deployment model? - ANSWERED**

**Deployment approach:**
- **Continuous deployment (when ready)** - deploy as changes are ready
- No rigid release schedule
- Deploy updates as features are completed and tested

**Current deployment model:**
- **FOR NOW: Local development only**
- **Stakeholder will clone repo and deploy when ready**
- Self-service deployment by stakeholder
- Stakeholder controls timing of deployments

**Future deployment:**
- Automated deployment pipelines (when infrastructure ready)
- CI/CD pipeline configuration (future)
- Deployment to test and production environments

**Maintenance and rollback:**
- To be defined when formal deployment process established
- Simple rollback via git revert if needed
- No complex deployment strategies required initially

**Q45: What operational monitoring is required? - ANSWERED**

**Monitoring approach:**
- **Basic admin dashboard for now**
- Simple monitoring within the application itself
- Job execution status and history
- Data sync success/failure tracking
- Basic error logging

**Admin dashboard should display:**
- System health indicators
- Data source connection status
- Recent job execution history
- Error counts and recent errors
- User activity summary

**Future enhancements:**
- External APM tool integration (Application Insights, etc.)
- Log aggregation platform
- Advanced alerting and monitoring
- Performance metrics and SLA tracking

**Q46: Who will be responsible for ongoing operations and support? - ANSWERED**

**Operations and support:**
- **Stakeholder will handle personally** (initially)
- No formal operations team initially
- No on-call rotation required
- Stakeholder is the primary user and administrator

**Support approach:**
- Self-supported by stakeholder
- Issues tracked informally or via GitHub issues
- No formal ticketing system integration
- No escalation procedures needed initially

**Future state:**
- As system grows, formal support team may be established
- Training for dedicated team members (see Q50)
- Potential for help desk integration later

**Q47: What is the disaster recovery plan? - ANSWERED**

**Disaster recovery:**
- **Not necessary** (stakeholder response)
- No formal DR plan required
- Basic backups sufficient (see section 3.3.2 for backup requirements)

**Risk tolerance:**
- System is not mission-critical initially
- Downtime acceptable during early phases
- Data loss tolerance higher than typical production systems

**Basic resilience:**
- Regular database backups (as defined in section 3.3.2)
- Source code in version control
- Ability to rebuild from scratch if needed
- No failover or redundancy required initially

### 7.13 Migration & Rollout

**Q48: Is there an existing system to migrate from? - ANSWERED**

**Existing system:**
- **None - no existing system to migrate from**
- This is a new application serving a new purpose
- No historical data to import from previous system
- No user migration needed

**Data sources:**
- Data pulled fresh from source systems (Azure DevOps, SharePoint, ServiceNow, IIS database)
- No legacy data conversion required
- Clean start with current state of data sources

**Q49: What is the rollout strategy? - ANSWERED**

**Rollout approach:**
- **Big-bang** deployment
- **Stakeholder will do testing** - this is their brainchild
- No phased rollout or pilot program
- Launch to all users when stakeholder determines ready

**Rationale:**
- Stakeholder deeply familiar with requirements and use cases
- Personal investment in success (stakeholder's idea)
- ~100 users total - small enough for big-bang approach
- Can iterate quickly based on feedback after launch

**Rollout phases:**
1. Development with mock data (current)
2. Stakeholder testing in test environment
3. Refinement based on stakeholder feedback
4. Launch to production when stakeholder approves
5. Ongoing iteration based on user feedback

**Q50: What is the training and change management plan? - ANSWERED**

**Training approach:**
- **Stakeholder will handle later with dedicated team members**
- No formal training program for MVP
- Documentation and tutorials created as needed
- Training responsibility delegated to stakeholder

**Training delivery:**
- Stakeholder will train initial users personally
- Champions or super-users identified by stakeholder
- Knowledge transfer to dedicated team members
- Informal training sessions as needed

**Documentation:**
- User documentation created as system matures
- In-app help and tooltips for usability
- Video tutorials or walkthroughs (future enhancement)

**Change management:**
- Minimal formal change management initially
- Stakeholder drives adoption within organization
- Communication strategy managed by stakeholder
- Iterative approach based on user feedback

### 7.14 Compliance & Governance

**Q51: What compliance frameworks apply? - ANSWERED**

**Compliance requirements:**
- **None to their knowledge** (stakeholder response)
- No specific compliance frameworks identified (SOC 2, ISO 27001, NIST, FISMA, etc.)
- No industry-specific regulations known

**Data privacy:**
- **Avoid PII** (personally identifiable information)
- Use aggregate data only for usage metrics (no individual user tracking)
- User identity from Entra ID for authentication, but no sensitive personal data stored

**Internal requirements:**
- Follow organizational IT policies and standards (general)
- No specific audit requirements identified
- Standard security best practices

**Q52: What data governance policies must be followed? - ANSWERED**

**Data governance:**
- **Not specified** - no formal data governance policies defined
- Assumed standard organizational practices apply

**Data handling principles:**
- Avoid storing PII beyond authentication needs
- Aggregate usage data only (privacy-focused)
- Data retention: lifecycle-long for application data (see Q15)
- No geographic residency constraints identified

**Future consideration:**
- Formal data classification if needed
- Data retention policies as system matures
- Privacy impact assessment if requirements change

**Q53: Are there change management or approval processes for production changes? - ANSWERED**

**Change management:**
- **Regular deploy process**
- **Pipelines configured later** (not defined initially)
- No formal change advisory board (CAB) initially

**Current approach:**
- Stakeholder controls deployments (see Q44)
- Stakeholder approves changes
- Simple git-based workflow
- Standard code review practices

**Future state:**
- CI/CD pipeline with automated testing
- Pull request reviews before merge
- Deployment gates and approvals as needed
- Rollback procedures formalized

### 7.15 Budget & Resources

**Q54: What is the project budget? - ANSWERED**

**Budget status:**
- **"Let's build it right now"** (stakeholder response)
- **No budget constraints** for MVP development
- Ready to proceed immediately

**Licensing approach:**
- Prefer open-source solutions where possible (.NET 10, SQL Server, etc.)
- Existing organizational licenses leveraged (Entra ID, Azure DevOps, SharePoint)
- No restrictions on necessary third-party tools

**Infrastructure:**
- On-premise Windows Server (existing infrastructure)
- SQL Server (existing infrastructure)
- No cloud hosting costs initially

**Q55: What is the project timeline? - ANSWERED**

**Timeline:**
- **"Let's build it right now"** - no formal timeline constraints
- **No hard deadlines**
- Start immediately, iterate as needed
- Flexible timeline based on development progress

**Approach:**
- MVP-focused development
- Deliver working functionality incrementally
- Stakeholder testing determines readiness
- Go-live when stakeholder approves

**Milestones:**
- To be defined during development
- Iterative releases as features complete
- Stakeholder feedback drives priorities

**Q56: What development resources are available? - ANSWERED**

**Development approach:**
- **"Let's build it right now"** - ready to start immediately
- Resources not explicitly defined (implied: adequate resources available)
- Development starting now with available resources

**Subject matter expertise:**
- Stakeholder is deeply familiar with requirements
- Stakeholder available for questions and clarification
- Access to data source documentation and examples
- Stakeholder will test and validate

**Q57: What ongoing operational costs are acceptable? - ANSWERED**

**Operational costs:**
- **No constraints specified**
- Leverage existing infrastructure (on-premise servers)
- No new licensing costs anticipated
- Minimal ongoing costs expected

**Cost considerations:**
- SQL Server: existing license
- Windows Server: existing infrastructure
- Entra ID: existing organizational license
- Azure OpenAI: production AI/ML usage (future, acceptable)
- Ollama: local testing (free)

### 7.16 Future Enhancements & Roadmap

**Q58: What features are considered future enhancements vs. MVP? - ANSWERED**

**MVP scope:**
- **EVERYTHING discussed thus far is MVP** (stakeholder directive)
- Comprehensive initial release including all core features:
  - Organization-wide heatmap (grid and treemap views)
  - User-focused task dashboard
  - Health scoring system with configurable weights
  - Intelligent recommendations (including low-usage retirement candidates)
  - Data integration from all sources (Azure DevOps, SharePoint, ServiceNow, IIS)
  - Job scheduling and monitoring dashboard
  - Task scheduling with intelligent workload distribution
  - Role management and validation workflow
  - Data conflict detection and resolution
  - User customization (dashboards, saved views, themes)
  - In-app notifications
  - Export capabilities (CSV, JSON)
  - Data feeds/subscriptions (RSS/Atom or similar)
  - Basic admin dashboard and monitoring
  - Development mode with comprehensive mock data

**Post-MVP enhancements:**
- Email notifications (lower priority than in-app)
- PDF/Excel export formats
- Scheduled reports
- iframe embedding for external dashboards
- Advanced APM and monitoring tools
- Manager approval workflow for task delegation
- External collaboration tool integration (Teams, Slack)

**Q59: Is there a vision for AI/ML capabilities? - ANSWERED**

**AI/ML integration:**
- **YES, as much as possible!** (stakeholder enthusiastic about AI/ML)

**Production environment:**
- **Azure OpenAI via API** for production AI/ML features
- Leverage enterprise-grade AI capabilities
- API-based integration for scalability

**Local testing:**
- **Ollama** for local development and testing
- No cloud dependency during development
- Cost-effective testing approach

**AI/ML capabilities of interest:**
- **Predictive scoring:** Predict future health scores based on trends
- **Anomaly detection:** Detect unusual patterns in usage, security findings, task completion
- **Intelligent recommendations:** AI-enhanced prioritization and suggestions
- Natural language querying (future)
- Automated root cause analysis for health score declines
- Predictive task scheduling optimization

**Implementation approach:**
- Start with basic AI features in MVP if feasible
- Expand AI capabilities iteratively
- Design system to easily integrate AI enhancements
- Ollama for development, Azure OpenAI for production

**Q60: Should the system support extensibility? - ANSWERED**

**Extensibility:**
- **YES, critical requirement!** (stakeholder emphasis)

**Plugin architecture:**
- **Desired** - support for plugins and extensions
- Custom data source plugins
- Custom health scoring algorithm plugins
- Custom visualization plugins
- Extensible recommendation engine

**API design:**
- **APIs for external access** - comprehensive API layer
- RESTful API for all major functionality
- External systems can consume data via API
- Read and write APIs where appropriate
- Well-documented API for third-party integrations

**Data consumer focus:**
- **HEAVILY consider end user as DATA CONSUMER**
- System should expose data for consumption by other tools
- API-first design approach
- Support for real-time data access
- Webhook support for event notifications

**System architecture:**
- **System should serve as a NODE in a larger system** (future growth)
- **Design for interoperability and extensibility** from day one
- Not a silo - part of ecosystem
- Integration points for other organizational systems
- Event-driven architecture where appropriate

**Specific extensibility features:**
- Custom data source plugins
- Webhook support for external systems
- Data subscription feeds (RSS/Atom, webhooks)
- Comprehensive API coverage
- Extensible health scoring (custom algorithms)
- Custom report templates
- Integration SDK for third-party developers (future)

---

## 8. Assumptions & Constraints

### 8.1 Assumptions

1. Users have access to modern web browsers with JavaScript enabled and support for IndexedDB
2. External data sources (Azure DevOps, SharePoint, ServiceNow) are accessible via network connectivity in production environment
3. IIS log consolidation pipeline is operational with existing SQL database available
4. User identity information is available and consistent across integrated systems via Entra ID
5. Organizational structure (teams, departments) is relatively stable
6. Users are familiar with basic web applications and do not require extensive training
7. Initial portfolio size is ~300 applications with ~100 users accessing the system
8. Data sources provide APIs or export mechanisms that are sufficiently performant
9. Weekly data refresh frequency is sufficient for all current organizational needs
10. Development environment is network-isolated requiring comprehensive mock data for testing
11. SharePoint folder names should align with ServiceNow application names, though conflicts will be surfaced for remediation
12. IIS database schema is stable and documented for integration purposes
13. Historical data will be retained for the life of each application (as long as project lifecycle)
14. Entra ID is the authoritative source for user identity validation
15. Task assignees and privileged administrators will actively engage with task delegation features
16. Low-usage applications flagged for retirement review will receive timely owner justification or retirement decisions
17. Users primarily access system from desktop browsers (no mobile optimization required)
18. Stakeholder will handle all testing, training, and initial rollout activities
19. No existing system to migrate from - clean slate implementation
20. External dashboards likely Power BI but specific integration details discovered during implementation
21. Organization leadership receptive to AI/ML capabilities and extensibility vision
22. System designed as node in larger ecosystem with API-first approach
23. IndexedDB sufficient for user preference storage without server-side backup initially
24. Basic admin dashboard sufficient for monitoring (no enterprise APM tools required for MVP)
25. Stakeholder has authority to approve go-live criteria and deployment decisions

### 8.2 Constraints

1. Integration with external systems limited to their API capabilities and rate limits
2. Data freshness constrained to weekly refresh frequency (stakeholder-confirmed as sufficient)
3. Historical data availability dependent on retention in source systems (12+ months for IIS data, lifecycle-long for application data)
4. Authentication and authorization mechanisms must comply with organizational security policies
5. Deployment environment: On-premise Windows Server (no containerization)
6. Technology stack: .NET 10, Blazor, SQL Server (stakeholder requirements)
7. User adoption dependent on effective change management and training (stakeholder-led)
8. Development environment disconnected from network - all testing relies on mock data
9. Current data integration approach (scripts generating CSV/JSON) must be productionized
10. IIS usage data collection is external - application only connects to existing database
11. Privacy requirement: Only aggregate usage metrics, no individual user tracking
12. Privileged administrator user IDs must be defined in appsettings.json (no UI for admin management initially)
13. Health scoring algorithm complexity limited by need for transparency and user understanding
14. Task scheduling optimization constrained by need to balance automation with user control
15. Data conflict resolution requires manual user intervention (no automatic reconciliation)
16. Desktop browser-only focus (no mobile or tablet optimization required)
17. User settings stored client-side in IndexedDB (no server-side storage for MVP)
18. WCAG accessibility guidelines must be followed for all visualizations
19. MVP scope is comprehensive (all discussed features included per stakeholder directive)
20. AI/ML features use Ollama for development, Azure OpenAI for production
21. System must be designed for extensibility and API-first architecture from day one
22. No formal compliance frameworks required but must avoid storing PII
23. Stakeholder controls deployment timing and go-live decisions
24. Initial rollout is big-bang (no phased approach)
25. No formal disaster recovery plan required (basic backups sufficient)

### 8.3 Dependencies

1. Access to Azure DevOps, SharePoint, ServiceNow environments with appropriate permissions (production environment)
2. Availability of subject matter experts for each data source
3. Existing IIS log consolidation database availability and access
4. Identity provider (Entra/Azure AD) configuration and user provisioning
5. Infrastructure provisioning (on-premise Windows Server, SQL Server databases, networking)
6. Security and compliance approvals
7. User availability for requirements validation and user acceptance testing
8. Documentation of IIS database schema and data structures
9. Comprehensive mock data creation for development and testing
10. Validation of assumed data structures once network connectivity available for testing

---

## 9. Success Criteria

### 9.1 Functional Success Criteria

- Users can view personalized task dashboard with all assigned tasks
- Organization-wide heatmap displays accurate health scores for all applications
- Health scores update within defined data freshness requirements
- Data synchronization operates reliably with < 1% failure rate
- Users can drill down to application details with comprehensive integrated data
- Task completion workflows function correctly with proper status tracking
- Role assignments display accurately and validation workflow operates correctly
- Recommendations generate based on configurable organizational priorities

### 9.2 User Adoption Success Criteria

- 80% of assigned users actively access the system within first month
- 70% of assigned tasks completed within defined time windows
- Positive user satisfaction scores (> 4/5) on usability survey
- Reduction in time spent tracking application health manually
- Increase in proactive identification of at-risk applications

### 9.3 Technical Success Criteria

- System meets or exceeds defined performance benchmarks
- 99.5% uptime during business hours
- Data synchronization completes successfully within scheduled windows
- Security assessment passes with no critical or high vulnerabilities
- Scalability testing validates support for target application and user counts
- All integrations function reliably with appropriate error handling

### 9.4 Business Success Criteria

- Improved visibility into application portfolio health for leadership
- Reduction in applications with overdue lifecycle tasks
- Faster identification and remediation of security vulnerabilities
- Better compliance with organizational lifecycle management policies
- Data-driven decision making for application investment and retirement

---

## 10. Next Steps

### 10.1 Requirements Validation

1. Review this requirements document with key stakeholders
2. Address open questions through stakeholder interviews and research
3. Prioritize requirements for MVP vs. future releases
4. Validate assumptions and confirm constraints
5. Obtain sign-off on requirements from project sponsors

### 10.2 Design Phase

1. Create high-level system architecture based on finalized technology stack
2. Design database schema and data model
3. Create UI/UX mockups and wireframes for key screens
4. Design data integration architecture and workflows
5. Define API specifications for external access

### 10.3 Planning & Estimation

1. Break down requirements into user stories and technical tasks
2. Estimate effort for each component and integration
3. Identify technical risks and mitigation strategies
4. Create project schedule with milestones
5. Allocate resources and define team structure

### 10.4 Prototype & Validation

1. Consider proof-of-concept for complex integrations
2. Validate health scoring algorithm with sample data
3. Create interactive prototype for user feedback
4. Test data synchronization with actual data sources
5. Refine requirements based on prototype learnings

---

## Document Control

**Version:** 2.0 (Final)
**Date:** 2026-01-11
**Status:** COMPLETE - Ready for Implementation Planning
**Author:** Requirements Analysis Team
**Reviewers:** Stakeholder (via Q&A sessions)
**Approvers:** Stakeholder

**Revision History:**

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-01-11 | Requirements Team | Initial draft based on concept document |
| 1.1 | 2026-01-11 | Requirements Team | Added data integration specifics, job scheduling requirements, development mode/mock data requirements. Answered Q9-Q13. Updated assumptions and constraints. |
| 1.2 | 2026-01-11 | Requirements Team | **MAJOR UPDATE:** Added comprehensive health scoring algorithm (section 2.4.3) with recommended formula, scoring weights, and health categories. Updated sections 2.1.3 (low-usage recommendations), 2.3.3 (data conflict handling), 2.5.2 (task scheduling with intelligent distribution), 2.5.3 (overdue task handling and delegation). Answered Q14-Q26. Updated assumptions and constraints with new details. |
| 2.0 | 2026-01-11 | Requirements Team | **FINAL VERSION - REQUIREMENTS COMPLETE:** Answered all remaining open questions Q27-Q60. Added new functional requirement sections: Heatmap Visualization Options (2.1.5), User Customization & Preferences (2.8), Data Feeds and Subscriptions (2.9), AI/ML Integration (2.10), Extensibility and API Design (2.11). Updated all UX requirements with desktop-first approach, WCAG compliance, IndexedDB storage. Added notification requirements with organizational workload warnings. Clarified testing, deployment, and operational approaches. Defined comprehensive AI/ML vision with Azure OpenAI and Ollama. Established API-first architecture and extensibility requirements. Updated assumptions and constraints. **READY FOR IMPLEMENTATION PLANNING.** |

**Related Documents:**

- Project Ideas Document: `/Users/benjaminhoffman/Documents/code/lifecycle/docs/ideas.md`
- [Architecture Design Document - To be created]
- [UI/UX Design Specifications - To be created]
- [API Specifications - To be created]
- [Database Schema Design - To be created]
- [Mock Data Specifications - To be created]

---

## Requirements Completion Summary

### What We Accomplished

This requirements document represents the complete outcome of a comprehensive requirements gathering process:

- **60 stakeholder questions asked and answered** across all aspects of the system
- **All functional requirements defined** with priorities and detailed specifications
- **All non-functional requirements specified** including performance, security, usability, and compliance
- **Technology stack finalized:** .NET 10, Blazor, SQL Server, Entra ID authentication
- **Data integration approach defined** for all four data sources (Azure DevOps, SharePoint, ServiceNow, IIS database)
- **Health scoring algorithm specified** with detailed point-based calculation formula
- **Task scheduling algorithm defined** with intelligent workload distribution
- **User experience requirements complete** including desktop-first approach, WCAG compliance, dual visualization modes
- **AI/ML vision established** with Azure OpenAI for production and Ollama for development
- **Extensibility architecture defined** with API-first design, webhooks, data feeds, and plugin support
- **Assumptions and constraints documented** with stakeholder confirmation
- **MVP scope clarified:** Everything discussed is MVP (comprehensive initial release)

### Key Decisions Made

1. **Technology:** Blazor/.NET 10, SQL Server, vanilla UI with Bootstrap fallback
2. **Deployment:** On-premise Windows Server, stakeholder-controlled deployment, no containerization
3. **Data Refresh:** Weekly scheduled jobs sufficient for all sources
4. **User Storage:** Client-side IndexedDB for user preferences with export/import backup
5. **Notifications:** In-app primary, email future enhancement
6. **Visualization:** Both grid and treemap views with WCAG compliance
7. **AI/ML:** Azure OpenAI for production, Ollama for local development
8. **Extensibility:** API-first architecture, webhook support, RSS/Atom feeds
9. **Testing:** Mock data for development, stakeholder testing, big-bang rollout
10. **Scope:** Comprehensive MVP including all core features

### What's Next: Implementation Planning

With requirements complete, the project is ready to proceed to:

1. **Architecture Design**
   - High-level system architecture
   - Component design and interactions
   - API design and specifications
   - Database schema design
   - Integration architecture

2. **Mock Data Creation**
   - Comprehensive mock data for all data sources
   - Multiple scenarios (small portfolio, large portfolio, problematic data)
   - Development mode implementation

3. **Development Planning**
   - Break down requirements into user stories and tasks
   - Estimate effort and create project schedule
   - Prioritize features within MVP scope
   - Set up development environment and tooling

4. **UI/UX Design**
   - Wireframes and mockups for key screens
   - Heatmap visualization designs (grid and treemap)
   - Task dashboard layouts
   - Application detail page design
   - Admin interface designs

5. **Technical Prototyping**
   - Health scoring algorithm validation
   - Task scheduling algorithm testing
   - Blazor component structure
   - IndexedDB storage patterns
   - API design validation

**Stakeholder Directive:** "Let's build it right now" - ready to begin implementation immediately.

---

**End of Requirements Document**
