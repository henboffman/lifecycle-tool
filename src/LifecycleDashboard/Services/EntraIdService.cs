using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using LifecycleDashboard.Data;
using LifecycleDashboard.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace LifecycleDashboard.Services;

/// <summary>
/// Service for Microsoft Entra ID (Azure AD) operations.
/// Handles user lookup, matching, and caching.
/// </summary>
public partial class EntraIdService : IEntraIdService
{
    private readonly IDbContextFactory<LifecycleDbContext> _dbContextFactory;
    private readonly GraphServiceClient? _graphClient;
    private readonly ILogger<EntraIdService> _logger;
    private readonly IConfiguration _configuration;

    // Cache for current user during request
    private EntraUserEntity? _currentUserCache;

    public EntraIdService(
        IDbContextFactory<LifecycleDbContext> dbContextFactory,
        ILogger<EntraIdService> logger,
        IConfiguration configuration,
        GraphServiceClient? graphClient = null)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
        _configuration = configuration;
        _graphClient = graphClient;
    }

    public async Task<bool> IsConfiguredAsync()
    {
        var tenantId = _configuration["AzureAd:TenantId"];
        var clientId = _configuration["AzureAd:ClientId"];
        return !string.IsNullOrEmpty(tenantId) && !string.IsNullOrEmpty(clientId) && _graphClient != null;
    }

    public async Task<EntraUserEntity?> GetCurrentUserAsync()
    {
        if (_currentUserCache != null)
            return _currentUserCache;

        if (_graphClient == null)
        {
            _logger.LogWarning("Graph client not configured, cannot get current user");
            return null;
        }

        try
        {
            var me = await _graphClient.Me.GetAsync(config =>
            {
                config.QueryParameters.Select = new[]
                {
                    "id", "userPrincipalName", "displayName", "givenName", "surname",
                    "mail", "employeeId", "department", "jobTitle", "officeLocation",
                    "accountEnabled"
                };
            });

            if (me == null)
                return null;

            _currentUserCache = await GetOrCreateUserFromGraphAsync(me);
            return _currentUserCache;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current user from Graph API");
            return null;
        }
    }

    public async Task<EntraUserEntity?> GetUserByIdAsync(string entraId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var user = await dbContext.EntraUsers
            .Include(u => u.Aliases)
            .FirstOrDefaultAsync(u => u.Id == entraId);

        if (user != null)
            return user;

        // Try to fetch from Graph API if not in cache
        if (_graphClient == null)
            return null;

        try
        {
            var graphUser = await _graphClient.Users[entraId].GetAsync(config =>
            {
                config.QueryParameters.Select = new[]
                {
                    "id", "userPrincipalName", "displayName", "givenName", "surname",
                    "mail", "employeeId", "department", "jobTitle", "officeLocation",
                    "accountEnabled"
                };
            });

            if (graphUser != null)
                return await GetOrCreateUserFromGraphAsync(graphUser);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch user {EntraId} from Graph API", entraId);
        }

        return null;
    }

    public async Task<EntraUserEntity?> GetUserByUpnAsync(string userPrincipalName)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var user = await dbContext.EntraUsers
            .Include(u => u.Aliases)
            .FirstOrDefaultAsync(u => u.UserPrincipalName == userPrincipalName);

        if (user != null)
            return user;

        // Try to fetch from Graph API if not in cache
        if (_graphClient == null)
            return null;

        try
        {
            var graphUser = await _graphClient.Users[userPrincipalName].GetAsync(config =>
            {
                config.QueryParameters.Select = new[]
                {
                    "id", "userPrincipalName", "displayName", "givenName", "surname",
                    "mail", "employeeId", "department", "jobTitle", "officeLocation",
                    "accountEnabled"
                };
            });

            if (graphUser != null)
                return await GetOrCreateUserFromGraphAsync(graphUser);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch user {UPN} from Graph API", userPrincipalName);
        }

        return null;
    }

    public async Task<UserMatchResult> MatchUserAsync(string nameOrEmail, MatchContext context)
    {
        if (string.IsNullOrWhiteSpace(nameOrEmail))
        {
            return new UserMatchResult
            {
                OriginalInput = nameOrEmail ?? "",
                Confidence = MatchConfidence.NoMatch,
                Method = MatchMethod.None,
                MatchExplanation = "Input was empty or whitespace"
            };
        }

        var normalizedInput = NormalizeString(nameOrEmail);
        _logger.LogDebug("Matching user: '{Input}' (normalized: '{Normalized}')", nameOrEmail, normalizedInput);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        // Strategy 1: Exact email/UPN match
        if (IsEmail(normalizedInput))
        {
            var emailMatch = await TryExactEmailMatchAsync(dbContext, normalizedInput);
            if (emailMatch != null)
            {
                return new UserMatchResult
                {
                    OriginalInput = nameOrEmail,
                    MatchedUser = emailMatch,
                    Confidence = MatchConfidence.Exact,
                    Method = normalizedInput.Contains('@') ? MatchMethod.ExactUpn : MatchMethod.ExactEmail,
                    MatchExplanation = $"Exact email/UPN match: {emailMatch.UserPrincipalName}"
                };
            }
        }

        // Strategy 2: Exact alias match
        var aliasMatch = await TryExactAliasMatchAsync(dbContext, normalizedInput);
        if (aliasMatch != null)
        {
            return new UserMatchResult
            {
                OriginalInput = nameOrEmail,
                MatchedUser = aliasMatch,
                Confidence = MatchConfidence.High,
                Method = MatchMethod.ExactAlias,
                MatchExplanation = $"Matched via known alias: {nameOrEmail}"
            };
        }

        // Strategy 3: Display name exact match
        var displayNameMatch = await TryDisplayNameMatchAsync(dbContext, normalizedInput);
        if (displayNameMatch != null)
        {
            return new UserMatchResult
            {
                OriginalInput = nameOrEmail,
                MatchedUser = displayNameMatch,
                Confidence = MatchConfidence.High,
                Method = MatchMethod.DisplayNameExact,
                MatchExplanation = $"Exact display name match: {displayNameMatch.DisplayName}"
            };
        }

        // Strategy 4: Name permutation match (e.g., "Smith, John" vs "John Smith")
        var permutationMatch = await TryNamePermutationMatchAsync(dbContext, normalizedInput);
        if (permutationMatch != null)
        {
            return new UserMatchResult
            {
                OriginalInput = nameOrEmail,
                MatchedUser = permutationMatch,
                Confidence = MatchConfidence.High,
                Method = MatchMethod.NamePermutation,
                MatchExplanation = $"Matched via name permutation: {permutationMatch.DisplayName}"
            };
        }

        // Strategy 5: Fuzzy name matching
        var fuzzyMatches = await TryFuzzyNameMatchAsync(dbContext, normalizedInput);
        if (fuzzyMatches.Count > 0)
        {
            var bestMatch = fuzzyMatches[0];
            var alternatives = fuzzyMatches.Skip(1).Take(3).Select(m => new AlternativeMatch
            {
                User = m.User,
                Confidence = m.Confidence,
                Method = MatchMethod.DisplayNameFuzzy,
                Reason = $"Similarity: {m.Similarity:P0}"
            }).ToList();

            return new UserMatchResult
            {
                OriginalInput = nameOrEmail,
                MatchedUser = bestMatch.Confidence >= context.MinConfidence ? bestMatch.User : null,
                Confidence = bestMatch.Confidence,
                Method = MatchMethod.DisplayNameFuzzy,
                MatchExplanation = bestMatch.Confidence >= context.MinConfidence
                    ? $"Fuzzy name match ({bestMatch.Similarity:P0} similar): {bestMatch.User.DisplayName}"
                    : $"Best fuzzy match below threshold ({bestMatch.Similarity:P0}): {bestMatch.User.DisplayName}",
                Alternatives = alternatives
            };
        }

        // No match found - create departed user alert if configured
        if (context.CreateAlertsForNoMatch && !string.IsNullOrEmpty(context.ApplicationId))
        {
            await CreateDepartedUserAlertAsync(dbContext, nameOrEmail, context);
        }

        return new UserMatchResult
        {
            OriginalInput = nameOrEmail,
            Confidence = MatchConfidence.NoMatch,
            Method = MatchMethod.None,
            MatchExplanation = "No matching user found in Entra ID"
        };
    }

    public async Task<IReadOnlyList<UserMatchResult>> MatchUsersAsync(IEnumerable<string> namesOrEmails, MatchContext context)
    {
        var results = new List<UserMatchResult>();
        foreach (var input in namesOrEmails)
        {
            results.Add(await MatchUserAsync(input, context));
        }
        return results;
    }

    public async Task<SyncUsersResult> SyncUsersFromEntraAsync(int? maxUsers = null)
    {
        if (_graphClient == null)
        {
            return new SyncUsersResult
            {
                Success = false,
                ErrorMessage = "Graph client not configured",
                SyncedAt = DateTimeOffset.UtcNow
            };
        }

        var startTime = DateTimeOffset.UtcNow;
        var usersAdded = 0;
        var usersUpdated = 0;
        var usersTotal = 0;

        try
        {
            _logger.LogInformation("Starting Entra user sync{MaxUsers}", maxUsers.HasValue ? $" (max: {maxUsers})" : "");

            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var usersResponse = await _graphClient.Users.GetAsync(config =>
            {
                config.QueryParameters.Select = new[]
                {
                    "id", "userPrincipalName", "displayName", "givenName", "surname",
                    "mail", "employeeId", "department", "jobTitle", "officeLocation",
                    "accountEnabled"
                };
                config.QueryParameters.Filter = "accountEnabled eq true";
                if (maxUsers.HasValue)
                    config.QueryParameters.Top = maxUsers.Value;
            });

            if (usersResponse?.Value != null)
            {
                foreach (var graphUser in usersResponse.Value)
                {
                    if (string.IsNullOrEmpty(graphUser.Id))
                        continue;

                    usersTotal++;
                    var existingUser = await dbContext.EntraUsers.FindAsync(graphUser.Id);

                    if (existingUser == null)
                    {
                        var newUser = MapGraphUserToEntity(graphUser);
                        dbContext.EntraUsers.Add(newUser);
                        usersAdded++;

                        // Add default aliases
                        AddDefaultAliases(dbContext, newUser, graphUser);
                    }
                    else
                    {
                        UpdateUserFromGraph(existingUser, graphUser);
                        usersUpdated++;
                    }
                }

                await dbContext.SaveChangesAsync();
            }

            _logger.LogInformation("Entra user sync complete: {Added} added, {Updated} updated, {Total} total",
                usersAdded, usersUpdated, usersTotal);

            return new SyncUsersResult
            {
                Success = true,
                UsersAdded = usersAdded,
                UsersUpdated = usersUpdated,
                UsersTotal = usersTotal,
                SyncedAt = startTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to sync users from Entra");
            return new SyncUsersResult
            {
                Success = false,
                UsersAdded = usersAdded,
                UsersUpdated = usersUpdated,
                UsersTotal = usersTotal,
                ErrorMessage = ex.Message,
                SyncedAt = startTime
            };
        }
    }

    public async Task<UserPhotoResult> GetUserPhotoAsync(string entraId)
    {
        if (_graphClient == null)
        {
            return new UserPhotoResult
            {
                Success = false,
                ErrorMessage = "Graph client not configured"
            };
        }

        try
        {
            var photoStream = await _graphClient.Users[entraId].Photo.Content.GetAsync();
            if (photoStream == null)
            {
                return new UserPhotoResult
                {
                    Success = false,
                    ErrorMessage = "No photo available"
                };
            }

            using var memoryStream = new MemoryStream();
            await photoStream.CopyToAsync(memoryStream);
            var photoData = memoryStream.ToArray();

            // Update cached photo in database
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var user = await dbContext.EntraUsers.FindAsync(entraId);
            if (user != null)
            {
                user.PhotoData = photoData;
                user.PhotoContentType = "image/jpeg"; // Graph API returns JPEG
                user.PhotoLastUpdated = DateTimeOffset.UtcNow;
                await dbContext.SaveChangesAsync();
            }

            return new UserPhotoResult
            {
                Success = true,
                PhotoData = photoData,
                ContentType = "image/jpeg"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get photo for user {EntraId}", entraId);
            return new UserPhotoResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<IReadOnlyList<EntraUserEntity>> SearchUsersAsync(string searchTerm, int maxResults = 10)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return Array.Empty<EntraUserEntity>();

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var normalizedSearch = NormalizeString(searchTerm);

        return await dbContext.EntraUsers
            .Where(u => EF.Functions.Like(u.DisplayName.ToLower(), $"%{normalizedSearch}%") ||
                        EF.Functions.Like(u.UserPrincipalName.ToLower(), $"%{normalizedSearch}%") ||
                        (u.Mail != null && EF.Functions.Like(u.Mail.ToLower(), $"%{normalizedSearch}%")))
            .OrderBy(u => u.DisplayName)
            .Take(maxResults)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<EntraUserEntity>> GetAllCachedUsersAsync()
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.EntraUsers
            .Include(u => u.Aliases)
            .OrderBy(u => u.DisplayName)
            .ToListAsync();
    }

    public async Task AddUserAliasAsync(string entraUserId, AliasType type, string value, string discoveredFrom)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        var normalizedValue = NormalizeString(value);

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        // Check if alias already exists
        var existingAlias = await dbContext.UserAliases
            .FirstOrDefaultAsync(a => a.EntraUserId == entraUserId &&
                                      a.Type == type &&
                                      a.Value == normalizedValue);

        if (existingAlias != null)
            return;

        var alias = new UserAliasEntity
        {
            EntraUserId = entraUserId,
            Type = type,
            Value = normalizedValue,
            OriginalValue = value,
            DiscoveredFrom = discoveredFrom,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.UserAliases.Add(alias);
        await dbContext.SaveChangesAsync();

        _logger.LogDebug("Added alias '{Value}' (type: {Type}) for user {UserId} from {Source}",
            value, type, entraUserId, discoveredFrom);
    }

    public async Task<IReadOnlyList<UserAliasEntity>> GetUserAliasesAsync(string entraUserId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.UserAliases
            .Where(a => a.EntraUserId == entraUserId)
            .OrderBy(a => a.Type)
            .ThenBy(a => a.Value)
            .ToListAsync();
    }

    #region Private Helper Methods

    private async Task<EntraUserEntity> GetOrCreateUserFromGraphAsync(User graphUser)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var existingUser = await dbContext.EntraUsers.FindAsync(graphUser.Id);
        if (existingUser != null)
        {
            UpdateUserFromGraph(existingUser, graphUser);
            await dbContext.SaveChangesAsync();
            return existingUser;
        }

        var newUser = MapGraphUserToEntity(graphUser);
        dbContext.EntraUsers.Add(newUser);

        // Add default aliases
        AddDefaultAliases(dbContext, newUser, graphUser);

        await dbContext.SaveChangesAsync();
        return newUser;
    }

    private static EntraUserEntity MapGraphUserToEntity(User graphUser)
    {
        return new EntraUserEntity
        {
            Id = graphUser.Id!,
            UserPrincipalName = graphUser.UserPrincipalName ?? "",
            DisplayName = graphUser.DisplayName ?? "",
            GivenName = graphUser.GivenName,
            Surname = graphUser.Surname,
            Mail = graphUser.Mail,
            EmployeeId = graphUser.EmployeeId,
            Department = graphUser.Department,
            JobTitle = graphUser.JobTitle,
            OfficeLocation = graphUser.OfficeLocation,
            AccountEnabled = graphUser.AccountEnabled ?? true,
            EntraLastSyncedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private static void UpdateUserFromGraph(EntraUserEntity entity, User graphUser)
    {
        entity.UserPrincipalName = graphUser.UserPrincipalName ?? entity.UserPrincipalName;
        entity.DisplayName = graphUser.DisplayName ?? entity.DisplayName;
        entity.GivenName = graphUser.GivenName;
        entity.Surname = graphUser.Surname;
        entity.Mail = graphUser.Mail;
        entity.EmployeeId = graphUser.EmployeeId;
        entity.Department = graphUser.Department;
        entity.JobTitle = graphUser.JobTitle;
        entity.OfficeLocation = graphUser.OfficeLocation;
        entity.AccountEnabled = graphUser.AccountEnabled ?? true;
        entity.EntraLastSyncedAt = DateTimeOffset.UtcNow;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void AddDefaultAliases(LifecycleDbContext dbContext, EntraUserEntity user, User graphUser)
    {
        var now = DateTimeOffset.UtcNow;

        // Add email as alias if different from UPN
        if (!string.IsNullOrEmpty(graphUser.Mail) &&
            !graphUser.Mail.Equals(graphUser.UserPrincipalName, StringComparison.OrdinalIgnoreCase))
        {
            dbContext.UserAliases.Add(new UserAliasEntity
            {
                EntraUserId = user.Id,
                Type = AliasType.Email,
                Value = NormalizeString(graphUser.Mail),
                OriginalValue = graphUser.Mail,
                DiscoveredFrom = "Entra",
                CreatedAt = now
            });
        }

        // Add name variations
        if (!string.IsNullOrEmpty(graphUser.GivenName) && !string.IsNullOrEmpty(graphUser.Surname))
        {
            // "Last, First" format
            var lastFirst = $"{graphUser.Surname}, {graphUser.GivenName}";
            dbContext.UserAliases.Add(new UserAliasEntity
            {
                EntraUserId = user.Id,
                Type = AliasType.Name,
                Value = NormalizeString(lastFirst),
                OriginalValue = lastFirst,
                DiscoveredFrom = "Entra",
                CreatedAt = now
            });

            // "First Last" format (if different from display name)
            var firstLast = $"{graphUser.GivenName} {graphUser.Surname}";
            if (!firstLast.Equals(graphUser.DisplayName, StringComparison.OrdinalIgnoreCase))
            {
                dbContext.UserAliases.Add(new UserAliasEntity
                {
                    EntraUserId = user.Id,
                    Type = AliasType.Name,
                    Value = NormalizeString(firstLast),
                    OriginalValue = firstLast,
                    DiscoveredFrom = "Entra",
                    CreatedAt = now
                });
            }
        }

        // Add employee ID as alias
        if (!string.IsNullOrEmpty(graphUser.EmployeeId))
        {
            dbContext.UserAliases.Add(new UserAliasEntity
            {
                EntraUserId = user.Id,
                Type = AliasType.EmployeeId,
                Value = NormalizeString(graphUser.EmployeeId),
                OriginalValue = graphUser.EmployeeId,
                DiscoveredFrom = "Entra",
                CreatedAt = now
            });
        }
    }

    private async Task<EntraUserEntity?> TryExactEmailMatchAsync(LifecycleDbContext dbContext, string email)
    {
        return await dbContext.EntraUsers
            .Include(u => u.Aliases)
            .FirstOrDefaultAsync(u =>
                u.UserPrincipalName.ToLower() == email ||
                (u.Mail != null && u.Mail.ToLower() == email));
    }

    private async Task<EntraUserEntity?> TryExactAliasMatchAsync(LifecycleDbContext dbContext, string value)
    {
        var alias = await dbContext.UserAliases
            .Include(a => a.EntraUser)
            .ThenInclude(u => u.Aliases)
            .FirstOrDefaultAsync(a => a.Value == value);

        return alias?.EntraUser;
    }

    private async Task<EntraUserEntity?> TryDisplayNameMatchAsync(LifecycleDbContext dbContext, string name)
    {
        return await dbContext.EntraUsers
            .Include(u => u.Aliases)
            .FirstOrDefaultAsync(u => u.DisplayName.ToLower() == name);
    }

    private async Task<EntraUserEntity?> TryNamePermutationMatchAsync(LifecycleDbContext dbContext, string name)
    {
        // Generate permutations
        var permutations = GenerateNamePermutations(name);

        foreach (var permutation in permutations)
        {
            var match = await dbContext.EntraUsers
                .Include(u => u.Aliases)
                .FirstOrDefaultAsync(u =>
                    u.DisplayName.ToLower() == permutation ||
                    (u.GivenName != null && u.Surname != null &&
                     $"{u.GivenName} {u.Surname}".ToLower() == permutation));

            if (match != null)
                return match;

            // Also check aliases
            var aliasMatch = await dbContext.UserAliases
                .Include(a => a.EntraUser)
                .ThenInclude(u => u.Aliases)
                .FirstOrDefaultAsync(a => a.Type == AliasType.Name && a.Value == permutation);

            if (aliasMatch != null)
                return aliasMatch.EntraUser;
        }

        return null;
    }

    private async Task<List<(EntraUserEntity User, MatchConfidence Confidence, double Similarity)>> TryFuzzyNameMatchAsync(
        LifecycleDbContext dbContext, string name)
    {
        var allUsers = await dbContext.EntraUsers
            .Include(u => u.Aliases)
            .ToListAsync();

        var matches = new List<(EntraUserEntity User, MatchConfidence Confidence, double Similarity)>();

        foreach (var user in allUsers)
        {
            // Compare with display name
            var displaySimilarity = CalculateSimilarity(name, NormalizeString(user.DisplayName));

            // Compare with first+last name
            double nameSimilarity = 0;
            if (!string.IsNullOrEmpty(user.GivenName) && !string.IsNullOrEmpty(user.Surname))
            {
                var fullName = NormalizeString($"{user.GivenName} {user.Surname}");
                nameSimilarity = CalculateSimilarity(name, fullName);
            }

            var bestSimilarity = Math.Max(displaySimilarity, nameSimilarity);

            if (bestSimilarity >= 0.6) // At least 60% similar
            {
                var confidence = bestSimilarity switch
                {
                    >= 0.95 => MatchConfidence.High,
                    >= 0.85 => MatchConfidence.Medium,
                    >= 0.7 => MatchConfidence.Low,
                    _ => MatchConfidence.Low
                };

                matches.Add((user, confidence, bestSimilarity));
            }
        }

        return matches.OrderByDescending(m => m.Similarity).ToList();
    }

    private async Task CreateDepartedUserAlertAsync(LifecycleDbContext dbContext, string unmatchedValue, MatchContext context)
    {
        // Check if alert already exists
        var existingAlert = await dbContext.DepartedUserAlerts
            .FirstOrDefaultAsync(a =>
                a.UnmatchedValue == unmatchedValue &&
                a.ApplicationId == context.ApplicationId &&
                a.RoleType == (context.RoleType ?? "Unknown") &&
                a.Status == DepartedUserAlertStatus.Open);

        if (existingAlert != null)
            return;

        var alert = new DepartedUserAlertEntity
        {
            UnmatchedValue = unmatchedValue,
            ValueType = IsEmail(NormalizeString(unmatchedValue)) ? AliasType.Email : AliasType.Name,
            ApplicationId = context.ApplicationId!,
            ApplicationName = context.ApplicationName ?? "Unknown",
            RoleType = context.RoleType ?? "Unknown",
            DataSource = context.DataSource,
            Status = DepartedUserAlertStatus.Open,
            DetectedAt = DateTimeOffset.UtcNow
        };

        dbContext.DepartedUserAlerts.Add(alert);
        await dbContext.SaveChangesAsync();

        _logger.LogWarning("Created departed user alert for '{Value}' (role: {Role}) on application {AppName}",
            unmatchedValue, context.RoleType, context.ApplicationName);
    }

    private static string NormalizeString(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "";

        return input.Trim().ToLowerInvariant();
    }

    private static bool IsEmail(string input)
    {
        return input.Contains('@') && input.Contains('.');
    }

    private static List<string> GenerateNamePermutations(string name)
    {
        var permutations = new List<string>();

        // Handle "Last, First" format
        if (name.Contains(','))
        {
            var parts = name.Split(',').Select(p => p.Trim()).ToArray();
            if (parts.Length == 2)
            {
                permutations.Add($"{parts[1]} {parts[0]}"); // First Last
                permutations.Add($"{parts[0]} {parts[1]}"); // Last First (without comma)
            }
        }
        // Handle "First Last" format
        else
        {
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                permutations.Add($"{parts[1]}, {parts[0]}"); // Last, First
                permutations.Add($"{parts[1]} {parts[0]}"); // Last First
            }
            else if (parts.Length > 2)
            {
                // Handle names like "John Paul Smith"
                var first = string.Join(" ", parts.Take(parts.Length - 1));
                var last = parts.Last();
                permutations.Add($"{last}, {first}");
                permutations.Add($"{last} {first}");
            }
        }

        return permutations;
    }

    private static double CalculateSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
            return 0;

        if (s1 == s2)
            return 1;

        // Use Levenshtein distance for similarity
        var distance = LevenshteinDistance(s1, s2);
        var maxLength = Math.Max(s1.Length, s2.Length);
        return 1.0 - (double)distance / maxLength;
    }

    private static int LevenshteinDistance(string s1, string s2)
    {
        var n = s1.Length;
        var m = s2.Length;
        var d = new int[n + 1, m + 1];

        if (n == 0) return m;
        if (m == 0) return n;

        for (var i = 0; i <= n; i++) d[i, 0] = i;
        for (var j = 0; j <= m; j++) d[0, j] = j;

        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = s2[j - 1] == s1[i - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }

    #endregion
}
