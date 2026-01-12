using LifecycleDashboard.Models;

namespace LifecycleDashboard.Services.DataIntegration;

/// <summary>
/// Service for integrating with ServiceNow CMDB data via CSV exports.
/// ServiceNow is the system of record for application metadata and role assignments.
/// </summary>
public interface IServiceNowService
{
    /// <summary>
    /// Tests the connection/accessibility to ServiceNow CSV file location.
    /// </summary>
    Task<ConnectionTestResult> TestConnectionAsync();

    /// <summary>
    /// Imports application data from a CSV export stream.
    /// </summary>
    Task<DataSyncResult<List<ServiceNowApplication>>> ImportApplicationsAsync(Stream csvStream);

    /// <summary>
    /// Imports application data from a CSV file path.
    /// </summary>
    Task<DataSyncResult<List<ServiceNowApplication>>> ImportApplicationsAsync(string csvFilePath);

    /// <summary>
    /// Imports role assignments from a CSV export stream.
    /// </summary>
    Task<DataSyncResult<List<RoleAssignment>>> ImportRoleAssignmentsAsync(Stream csvStream);

    /// <summary>
    /// Imports role assignments from a CSV file path.
    /// </summary>
    Task<DataSyncResult<List<RoleAssignment>>> ImportRoleAssignmentsAsync(string csvFilePath);

    /// <summary>
    /// Gets the last successful import timestamp.
    /// </summary>
    Task<DateTimeOffset?> GetLastImportDateAsync();

    /// <summary>
    /// Syncs all ServiceNow data from the configured CSV source.
    /// </summary>
    Task<DataSyncResult> SyncFromCsvAsync(string applicationsFilePath, string? roleAssignmentsFilePath = null);

    /// <summary>
    /// Validates imported users against the user directory.
    /// Returns conflicts for users not found.
    /// </summary>
    Task<List<DataConflict>> ValidateUsersAsync(List<RoleAssignment> assignments);

    /// <summary>
    /// Gets column mapping configuration for CSV import.
    /// </summary>
    ServiceNowCsvMapping GetCsvMapping();

    /// <summary>
    /// Updates column mapping configuration for CSV import.
    /// </summary>
    Task SetCsvMappingAsync(ServiceNowCsvMapping mapping);
}

/// <summary>
/// Application data imported from ServiceNow.
/// </summary>
public record ServiceNowApplication
{
    /// <summary>ServiceNow CI (Configuration Item) ID - unique identifier.</summary>
    public required string ServiceNowId { get; init; }

    /// <summary>Application display name.</summary>
    public required string Name { get; init; }

    /// <summary>Application description.</summary>
    public string? Description { get; init; }

    /// <summary>Business capability/area.</summary>
    public string? Capability { get; init; }

    /// <summary>Application status in ServiceNow.</summary>
    public string? Status { get; init; }

    /// <summary>Application owner user ID.</summary>
    public string? OwnerId { get; init; }

    /// <summary>Application owner name.</summary>
    public string? OwnerName { get; init; }

    /// <summary>Technical lead user ID.</summary>
    public string? TechnicalLeadId { get; init; }

    /// <summary>Technical lead name.</summary>
    public string? TechnicalLeadName { get; init; }

    /// <summary>Business owner user ID.</summary>
    public string? BusinessOwnerId { get; init; }

    /// <summary>Business owner name.</summary>
    public string? BusinessOwnerName { get; init; }

    /// <summary>Link to Azure DevOps repository.</summary>
    public string? RepositoryUrl { get; init; }

    /// <summary>Link to SharePoint documentation.</summary>
    public string? DocumentationUrl { get; init; }

    /// <summary>Environment (Production, Development, etc.).</summary>
    public string? Environment { get; init; }

    /// <summary>Criticality level.</summary>
    public string? Criticality { get; init; }

    /// <summary>Support group.</summary>
    public string? SupportGroup { get; init; }

    /// <summary>Date application was created in ServiceNow.</summary>
    public DateTimeOffset? CreatedDate { get; init; }

    /// <summary>Date application record was last updated.</summary>
    public DateTimeOffset? LastUpdated { get; init; }

    /// <summary>Additional custom fields from CSV.</summary>
    public Dictionary<string, string> CustomFields { get; init; } = [];
}

/// <summary>
/// Column mapping configuration for ServiceNow CSV import.
/// Maps CSV column names to expected fields.
/// </summary>
public record ServiceNowCsvMapping
{
    // Application fields
    public string ServiceNowIdColumn { get; init; } = "u_application_id";
    public string NameColumn { get; init; } = "name";
    public string DescriptionColumn { get; init; } = "short_description";
    public string CapabilityColumn { get; init; } = "u_capability";
    public string StatusColumn { get; init; } = "install_status";
    public string OwnerIdColumn { get; init; } = "owned_by.employee_number";
    public string OwnerNameColumn { get; init; } = "owned_by.name";
    public string TechnicalLeadIdColumn { get; init; } = "u_technical_lead.employee_number";
    public string TechnicalLeadNameColumn { get; init; } = "u_technical_lead.name";
    public string BusinessOwnerIdColumn { get; init; } = "u_business_owner.employee_number";
    public string BusinessOwnerNameColumn { get; init; } = "u_business_owner.name";
    public string RepositoryUrlColumn { get; init; } = "u_repository_url";
    public string DocumentationUrlColumn { get; init; } = "u_documentation_url";
    public string EnvironmentColumn { get; init; } = "u_environment";
    public string CriticalityColumn { get; init; } = "u_criticality";
    public string SupportGroupColumn { get; init; } = "support_group.name";
    public string CreatedDateColumn { get; init; } = "sys_created_on";
    public string LastUpdatedColumn { get; init; } = "sys_updated_on";

    // Role assignment fields (if in separate CSV)
    public string RoleAppIdColumn { get; init; } = "u_application.u_application_id";
    public string RoleUserIdColumn { get; init; } = "u_user.employee_number";
    public string RoleUserNameColumn { get; init; } = "u_user.name";
    public string RoleUserEmailColumn { get; init; } = "u_user.email";
    public string RoleTypeColumn { get; init; } = "u_role";
    public string RoleAssignedDateColumn { get; init; } = "sys_created_on";

    /// <summary>Custom field columns to import (column name -> field name).</summary>
    public Dictionary<string, string> CustomFieldMappings { get; init; } = [];

    /// <summary>Date format used in the CSV (default: ServiceNow format).</summary>
    public string DateFormat { get; init; } = "yyyy-MM-dd HH:mm:ss";
}

/// <summary>
/// CSV import result with detailed statistics.
/// </summary>
public record CsvImportResult
{
    /// <summary>Total rows in the CSV file.</summary>
    public int TotalRows { get; init; }

    /// <summary>Rows successfully parsed.</summary>
    public int SuccessfulRows { get; init; }

    /// <summary>Rows that failed to parse.</summary>
    public int FailedRows { get; init; }

    /// <summary>Rows skipped (e.g., duplicates, invalid status).</summary>
    public int SkippedRows { get; init; }

    /// <summary>Details of parsing errors.</summary>
    public List<CsvRowError> Errors { get; init; } = [];
}

/// <summary>
/// Error encountered parsing a specific CSV row.
/// </summary>
public record CsvRowError
{
    /// <summary>Row number in the CSV (1-based).</summary>
    public int RowNumber { get; init; }

    /// <summary>Raw row content (truncated if long).</summary>
    public string? RawContent { get; init; }

    /// <summary>Error message.</summary>
    public required string ErrorMessage { get; init; }

    /// <summary>Column that caused the error (if applicable).</summary>
    public string? ColumnName { get; init; }
}
