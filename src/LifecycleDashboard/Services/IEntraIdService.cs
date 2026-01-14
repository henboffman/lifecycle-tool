using LifecycleDashboard.Data.Entities;

namespace LifecycleDashboard.Services;

/// <summary>
/// Service interface for Microsoft Entra ID (Azure AD) operations.
/// Handles user lookup, matching, and caching.
/// </summary>
public interface IEntraIdService
{
    /// <summary>
    /// Gets the currently authenticated user from the cache or Entra.
    /// </summary>
    Task<EntraUserEntity?> GetCurrentUserAsync();

    /// <summary>
    /// Looks up a user by their Entra Object ID.
    /// </summary>
    Task<EntraUserEntity?> GetUserByIdAsync(string entraId);

    /// <summary>
    /// Looks up a user by their User Principal Name (email).
    /// </summary>
    Task<EntraUserEntity?> GetUserByUpnAsync(string userPrincipalName);

    /// <summary>
    /// Attempts to match a name or email to an Entra user.
    /// Uses fuzzy matching with configurable confidence thresholds.
    /// </summary>
    Task<UserMatchResult> MatchUserAsync(string nameOrEmail, MatchContext context);

    /// <summary>
    /// Attempts to match multiple names/emails in batch for efficiency.
    /// </summary>
    Task<IReadOnlyList<UserMatchResult>> MatchUsersAsync(IEnumerable<string> namesOrEmails, MatchContext context);

    /// <summary>
    /// Syncs users from Entra ID to local cache.
    /// </summary>
    Task<SyncUsersResult> SyncUsersFromEntraAsync(int? maxUsers = null);

    /// <summary>
    /// Gets a user's profile photo from Entra.
    /// </summary>
    Task<UserPhotoResult> GetUserPhotoAsync(string entraId);

    /// <summary>
    /// Searches for users by name or email.
    /// </summary>
    Task<IReadOnlyList<EntraUserEntity>> SearchUsersAsync(string searchTerm, int maxResults = 10);

    /// <summary>
    /// Gets all cached Entra users.
    /// </summary>
    Task<IReadOnlyList<EntraUserEntity>> GetAllCachedUsersAsync();

    /// <summary>
    /// Adds or updates a user alias for improved matching.
    /// </summary>
    Task AddUserAliasAsync(string entraUserId, AliasType type, string value, string discoveredFrom);

    /// <summary>
    /// Gets all aliases for a user.
    /// </summary>
    Task<IReadOnlyList<UserAliasEntity>> GetUserAliasesAsync(string entraUserId);

    /// <summary>
    /// Checks if Entra ID integration is properly configured.
    /// </summary>
    Task<bool> IsConfiguredAsync();
}

/// <summary>
/// Result of attempting to match a user.
/// </summary>
public record UserMatchResult
{
    /// <summary>
    /// The original input that was matched.
    /// </summary>
    public required string OriginalInput { get; init; }

    /// <summary>
    /// Whether a match was found.
    /// </summary>
    public bool IsMatched => MatchedUser != null;

    /// <summary>
    /// The matched Entra user, if found.
    /// </summary>
    public EntraUserEntity? MatchedUser { get; init; }

    /// <summary>
    /// Confidence level of the match.
    /// </summary>
    public MatchConfidence Confidence { get; init; } = MatchConfidence.NoMatch;

    /// <summary>
    /// How the match was made.
    /// </summary>
    public MatchMethod Method { get; init; } = MatchMethod.None;

    /// <summary>
    /// Explanation of how/why the match was made.
    /// </summary>
    public string? MatchExplanation { get; init; }

    /// <summary>
    /// Alternative matches that were considered.
    /// </summary>
    public IReadOnlyList<AlternativeMatch> Alternatives { get; init; } = Array.Empty<AlternativeMatch>();
}

/// <summary>
/// An alternative match that was considered but not selected.
/// </summary>
public record AlternativeMatch
{
    public required EntraUserEntity User { get; init; }
    public required MatchConfidence Confidence { get; init; }
    public required MatchMethod Method { get; init; }
    public string? Reason { get; init; }
}

/// <summary>
/// Confidence level of a user match.
/// </summary>
public enum MatchConfidence
{
    /// <summary>
    /// No match found.
    /// </summary>
    NoMatch = 0,

    /// <summary>
    /// Low confidence - manual review recommended.
    /// </summary>
    Low = 1,

    /// <summary>
    /// Medium confidence - likely correct but verify.
    /// </summary>
    Medium = 2,

    /// <summary>
    /// High confidence - very likely correct.
    /// </summary>
    High = 3,

    /// <summary>
    /// Exact match - UPN, email, or ID match.
    /// </summary>
    Exact = 4
}

/// <summary>
/// How a match was made.
/// </summary>
public enum MatchMethod
{
    None,
    ExactUpn,
    ExactEmail,
    ExactAlias,
    DisplayNameExact,
    DisplayNameFuzzy,
    NamePermutation,
    EmailDomainMatch,
    EmployeeId
}

/// <summary>
/// Context for user matching to help determine confidence.
/// </summary>
public record MatchContext
{
    /// <summary>
    /// The data source being imported from.
    /// </summary>
    public required string DataSource { get; init; }

    /// <summary>
    /// The role type being matched (Owner, FunctionalArchitect, etc.).
    /// </summary>
    public string? RoleType { get; init; }

    /// <summary>
    /// The application ID if matching for a specific application.
    /// </summary>
    public string? ApplicationId { get; init; }

    /// <summary>
    /// The application name for context.
    /// </summary>
    public string? ApplicationName { get; init; }

    /// <summary>
    /// Whether to automatically create departed user alerts for no-matches.
    /// </summary>
    public bool CreateAlertsForNoMatch { get; init; } = true;

    /// <summary>
    /// Minimum confidence level to accept as a match.
    /// </summary>
    public MatchConfidence MinConfidence { get; init; } = MatchConfidence.Medium;
}

/// <summary>
/// Result of syncing users from Entra.
/// </summary>
public record SyncUsersResult
{
    public bool Success { get; init; }
    public int UsersAdded { get; init; }
    public int UsersUpdated { get; init; }
    public int UsersTotal { get; init; }
    public int PhotosUpdated { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTimeOffset SyncedAt { get; init; }
}

/// <summary>
/// Result of fetching a user's photo.
/// </summary>
public record UserPhotoResult
{
    public bool Success { get; init; }
    public byte[]? PhotoData { get; init; }
    public string? ContentType { get; init; }
    public string? ErrorMessage { get; init; }
}
