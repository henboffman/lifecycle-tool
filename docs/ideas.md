# Goal

A dashboard that can be used by an IT organization to track the state of their application portfolio lifecycle health, using data aggregated from numerous sources to ensure accuracy in nearly-real time. From the tool, users can quickly see where they need to take action, as tasks have target windows of completion or repetition defined, and application/capabilities have roles assigned to them, and those roles have tasks that must be performed. Users may also use the tool to browse information about all applications/capabilities in the system.

# Ideas

1. Data sources with regular pulls (AzDo, SharePoint, ServiceNow, others)

- configurable per data source and action

1. Heatmap that shows health of applications across the organization. Configurable weighting scores to determine importance (heavily used app is more important than old and rarely used one)
2. User-focused dashboard- approach it such that people only want to see what they need to get done
3. The dashboard can make suggestions based on priorities of organization (defined in settings sliders (age, vulnerability remediation, use, etc.))
4. Also need a way to show the application usage information. This can be information that we parse from IIS logs and write to a SQL database
5. It would be nice if the lifecycle dashboard, when viewing information for a given application, could also pull information from teh iis dashboard, so that we could see the state of the dev/test/prod/other resources for that given application or capability.
6. We should also be able to link to existing/external dashboards that might be related to the application/capability. Would be great if they were rendered as iframes in the dashboard, but even links would be better than nothing
7. The organization should be able to configure when the routine events occur (e.g. role validations, data validations, etc.), how frequently they occur, if they should be staggered throughout the org or all be due simultaneously for the org. It would also be good if the scheduler/recommender could take two passes through the system, first collecting everything and scheduling initial plans, but then revising to ensure that some individuals also have their work staggered out (if they're responsible for several things).

# Data and sources

1. Repository data - Azure DevOps
2. CodeQL & advanced security data - Azure DevOps
3. Documentation - SharePoint
4. Roles - ServiceNow, exported to CSV
5. IIS data - various windows servers, to be consolidated to single SQL database
