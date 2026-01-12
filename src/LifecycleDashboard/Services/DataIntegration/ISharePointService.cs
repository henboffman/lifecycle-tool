using LifecycleDashboard.Models;

namespace LifecycleDashboard.Services.DataIntegration;

/// <summary>
/// Service for integrating with SharePoint to discover application documentation folders
/// and check documentation completeness.
///
/// SharePoint Structure:
/// Documents > General > Offerings > [capabilities] > [apps]
///
/// Each application folder should contain these 4 template subfolders:
/// - Project Documents
/// - Promotional Content
/// - Technical Documentation
/// - User Documentation
/// </summary>
public interface ISharePointService
{
    /// <summary>
    /// Tests the connection to SharePoint using configured credentials.
    /// </summary>
    Task<ConnectionTestResult> TestConnectionAsync();

    /// <summary>
    /// Discovers all application folders by navigating the document hierarchy.
    /// Looks for folders containing the 4 template subfolders to identify applications.
    /// </summary>
    Task<DataSyncResult<List<ApplicationFolder>>> DiscoverApplicationFoldersAsync();

    /// <summary>
    /// Checks documentation completeness for a specific application folder.
    /// </summary>
    Task<DataSyncResult<DocumentationStatus>> CheckDocumentationCompletenessAsync(string folderPath);

    /// <summary>
    /// Gets all documents within an application folder.
    /// </summary>
    Task<DataSyncResult<List<SharePointDocument>>> GetDocumentsAsync(string folderPath);

    /// <summary>
    /// Gets capabilities (top-level organizational folders).
    /// </summary>
    Task<DataSyncResult<List<string>>> GetCapabilitiesAsync();

    /// <summary>
    /// Gets all application folders within a capability.
    /// </summary>
    Task<DataSyncResult<List<ApplicationFolder>>> GetApplicationsInCapabilityAsync(string capability);

    /// <summary>
    /// Syncs documentation status for all discovered applications.
    /// </summary>
    Task<DataSyncResult> SyncDocumentationStatusAsync();
}

/// <summary>
/// Represents an application folder discovered in SharePoint.
/// </summary>
public record ApplicationFolder
{
    /// <summary>Folder name (should match application name).</summary>
    public required string Name { get; init; }

    /// <summary>Full path to the folder in SharePoint.</summary>
    public required string Path { get; init; }

    /// <summary>URL to the folder in SharePoint.</summary>
    public string? Url { get; init; }

    /// <summary>Parent capability folder.</summary>
    public string? Capability { get; init; }

    /// <summary>Template subfolders found in this application folder.</summary>
    public List<string> TemplateFoldersFound { get; init; } = [];

    /// <summary>Whether all 4 template folders exist.</summary>
    public bool HasAllTemplateFolders => TemplateFoldersFound.Count >= 4;

    /// <summary>When the folder was last modified.</summary>
    public DateTimeOffset? LastModified { get; init; }

    /// <summary>Who created the folder.</summary>
    public string? CreatedBy { get; init; }

    /// <summary>Documentation completeness status based on folder contents.</summary>
    public FolderDocumentationStatus DocumentationStatus { get; init; } = new();
}

/// <summary>
/// Documentation status based on SharePoint folder structure.
/// </summary>
public record FolderDocumentationStatus
{
    /// <summary>Whether Project Documents folder has content.</summary>
    public bool HasProjectDocuments { get; init; }

    /// <summary>Number of files in Project Documents.</summary>
    public int ProjectDocumentCount { get; init; }

    /// <summary>Whether Technical Documentation folder has content.</summary>
    public bool HasTechnicalDocumentation { get; init; }

    /// <summary>Number of files in Technical Documentation.</summary>
    public int TechnicalDocumentCount { get; init; }

    /// <summary>Whether User Documentation folder has content.</summary>
    public bool HasUserDocumentation { get; init; }

    /// <summary>Number of files in User Documentation.</summary>
    public int UserDocumentCount { get; init; }

    /// <summary>Whether Promotional Content folder has content.</summary>
    public bool HasPromotionalContent { get; init; }

    /// <summary>Number of files in Promotional Content.</summary>
    public int PromotionalContentCount { get; init; }

    /// <summary>Total file count across all folders.</summary>
    public int TotalFileCount => ProjectDocumentCount + TechnicalDocumentCount + UserDocumentCount + PromotionalContentCount;

    /// <summary>Completeness percentage (0-100).</summary>
    public int CompletenessPercentage
    {
        get
        {
            var score = 0;
            if (HasProjectDocuments) score += 25;
            if (HasTechnicalDocumentation) score += 25;
            if (HasUserDocumentation) score += 25;
            if (HasPromotionalContent) score += 25;
            return score;
        }
    }
}

/// <summary>
/// Represents a document in SharePoint.
/// </summary>
public record SharePointDocument
{
    /// <summary>Document name.</summary>
    public required string Name { get; init; }

    /// <summary>Full path to the document.</summary>
    public required string Path { get; init; }

    /// <summary>URL to access the document.</summary>
    public string? Url { get; init; }

    /// <summary>File size in bytes.</summary>
    public long? SizeBytes { get; init; }

    /// <summary>File extension.</summary>
    public string? Extension { get; init; }

    /// <summary>MIME type.</summary>
    public string? ContentType { get; init; }

    /// <summary>When the document was created.</summary>
    public DateTimeOffset? CreatedDate { get; init; }

    /// <summary>When the document was last modified.</summary>
    public DateTimeOffset? LastModified { get; init; }

    /// <summary>Who created the document.</summary>
    public string? CreatedBy { get; init; }

    /// <summary>Who last modified the document.</summary>
    public string? ModifiedBy { get; init; }

    /// <summary>Which template folder the document is in.</summary>
    public string? FolderCategory { get; init; }
}

/// <summary>
/// Constants for SharePoint folder structure.
/// </summary>
public static class SharePointFolders
{
    /// <summary>Root path for offerings documentation.</summary>
    public const string OfferingsRoot = "Documents/General/Offerings";

    /// <summary>Project Documents template folder name.</summary>
    public const string ProjectDocuments = "Project Documents";

    /// <summary>Promotional Content template folder name.</summary>
    public const string PromotionalContent = "Promotional Content";

    /// <summary>Technical Documentation template folder name.</summary>
    public const string TechnicalDocumentation = "Technical Documentation";

    /// <summary>User Documentation template folder name.</summary>
    public const string UserDocumentation = "User Documentation";

    /// <summary>All template folder names that indicate a valid application folder.</summary>
    public static readonly string[] TemplateFolders =
    [
        ProjectDocuments,
        PromotionalContent,
        TechnicalDocumentation,
        UserDocumentation
    ];
}
