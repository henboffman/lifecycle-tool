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

        // Use LIKE without ToLower - SQL Server LIKE is case-insensitive by default
        return await dbContext.EntraUsers
            .Where(u => EF.Functions.Like(u.DisplayName, $"%{normalizedSearch}%") ||
                        EF.Functions.Like(u.UserPrincipalName, $"%{normalizedSearch}%") ||
                        (u.Mail != null && EF.Functions.Like(u.Mail, $"%{normalizedSearch}%")))
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
        // Use case-insensitive collation for SQL Server
        return await dbContext.EntraUsers
            .Include(u => u.Aliases)
            .FirstOrDefaultAsync(u =>
                EF.Functions.Collate(u.UserPrincipalName, "Latin1_General_CI_AS") == email ||
                (u.Mail != null && EF.Functions.Collate(u.Mail, "Latin1_General_CI_AS") == email));
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
        // Use case-insensitive collation for SQL Server
        return await dbContext.EntraUsers
            .Include(u => u.Aliases)
            .FirstOrDefaultAsync(u => EF.Functions.Collate(u.DisplayName, "Latin1_General_CI_AS") == name);
    }

    private async Task<EntraUserEntity?> TryNamePermutationMatchAsync(LifecycleDbContext dbContext, string name)
    {
        // Generate permutations
        var permutations = GenerateNamePermutations(name);

        foreach (var permutation in permutations)
        {
            // First try DisplayName match with case-insensitive collation
            var match = await dbContext.EntraUsers
                .Include(u => u.Aliases)
                .FirstOrDefaultAsync(u =>
                    EF.Functions.Collate(u.DisplayName, "Latin1_General_CI_AS") == permutation);

            if (match != null)
                return match;

            // Try GivenName + Surname match - load candidates and filter in memory
            // to avoid string concatenation in SQL
            var candidates = await dbContext.EntraUsers
                .Include(u => u.Aliases)
                .Where(u => u.GivenName != null && u.Surname != null)
                .ToListAsync();

            var givenSurnameMatch = candidates.FirstOrDefault(u =>
                $"{u.GivenName} {u.Surname}".Equals(permutation, StringComparison.OrdinalIgnoreCase));

            if (givenSurnameMatch != null)
                return givenSurnameMatch;

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

        // Parse the input name (from ServiceNow - typically "First Last")
        var (inputFirst, inputLast) = ParseName(name);
        var normalizedInput = NormalizeString(name);

        _logger.LogDebug("Fuzzy matching '{Name}' - parsed as First='{First}', Last='{Last}'",
            name, inputFirst, inputLast);

        foreach (var user in allUsers)
        {
            // Clean AD display name (remove parenthetical suffix like "(J)")
            var cleanDisplayName = CleanAdDisplayName(user.DisplayName);

            // Parse the user's name from AD (typically "Last, First" format)
            var (adFirst, adLast) = ParseName(cleanDisplayName);

            // Also use GivenName/Surname if available (more reliable)
            var adGivenName = !string.IsNullOrEmpty(user.GivenName) ? user.GivenName : adFirst;
            var adSurname = !string.IsNullOrEmpty(user.Surname) ? user.Surname : adLast;

            double bestSimilarity = 0;
            MatchConfidence confidence = MatchConfidence.NoMatch;

            // Strategy 1: Last names match exactly, check first names with nickname support
            // This handles "Jeff Jones" matching "Jones, Jeffery (J)"
            if (!string.IsNullOrEmpty(inputLast) && !string.IsNullOrEmpty(adSurname) &&
                LastNamesMatch(inputLast, adSurname))
            {
                if (!string.IsNullOrEmpty(inputFirst) && !string.IsNullOrEmpty(adGivenName) &&
                    FirstNamesMatch(inputFirst, adGivenName))
                {
                    // Last name exact + first name match (including nicknames) = High confidence
                    bestSimilarity = 0.95;
                    confidence = MatchConfidence.High;

                    _logger.LogDebug("High match: '{Input}' -> '{AD}' (last name exact, first name/nickname match)",
                        name, user.DisplayName);
                }
                else if (!string.IsNullOrEmpty(inputFirst) && !string.IsNullOrEmpty(adGivenName))
                {
                    // Last name exact, first names don't match but could be partial
                    var firstNameSimilarity = CalculateSimilarity(
                        NormalizeString(inputFirst),
                        NormalizeString(adGivenName));

                    if (firstNameSimilarity >= 0.5)
                    {
                        bestSimilarity = 0.7 + (firstNameSimilarity * 0.2);
                        confidence = MatchConfidence.Medium;

                        _logger.LogDebug("Medium match: '{Input}' -> '{AD}' (last name exact, first name {Sim:P0} similar)",
                            name, user.DisplayName, firstNameSimilarity);
                    }
                }
            }

            // Strategy 2: Traditional similarity comparison on full names
            if (bestSimilarity < 0.6)
            {
                // Compare with cleaned display name
                var cleanedNormalized = NormalizeString(cleanDisplayName);
                var displaySimilarity = CalculateSimilarity(normalizedInput, cleanedNormalized);

                // Compare with first+last name combinations
                double nameSimilarity = 0;
                if (!string.IsNullOrEmpty(adGivenName) && !string.IsNullOrEmpty(adSurname))
                {
                    // Try "First Last" format
                    var fullName = NormalizeString($"{adGivenName} {adSurname}");
                    nameSimilarity = CalculateSimilarity(normalizedInput, fullName);

                    // Also try "Last First" format (without comma)
                    var reverseFullName = NormalizeString($"{adSurname} {adGivenName}");
                    var reverseSimilarity = CalculateSimilarity(normalizedInput, reverseFullName);
                    nameSimilarity = Math.Max(nameSimilarity, reverseSimilarity);
                }

                var traditionalBest = Math.Max(displaySimilarity, nameSimilarity);
                if (traditionalBest > bestSimilarity)
                {
                    bestSimilarity = traditionalBest;
                    confidence = bestSimilarity switch
                    {
                        >= 0.95 => MatchConfidence.High,
                        >= 0.85 => MatchConfidence.Medium,
                        >= 0.7 => MatchConfidence.Low,
                        _ => MatchConfidence.Low
                    };
                }
            }

            if (bestSimilarity >= 0.6) // At least 60% similar
            {
                matches.Add((user, confidence, bestSimilarity));
            }
        }

        var result = matches.OrderByDescending(m => m.Similarity).ToList();

        if (result.Count > 0)
        {
            _logger.LogDebug("Found {Count} fuzzy matches for '{Name}', best: '{Best}' ({Similarity:P0})",
                result.Count, name, result[0].User.DisplayName, result[0].Similarity);
        }
        else
        {
            _logger.LogDebug("No fuzzy matches found for '{Name}'", name);
        }

        return result;
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

    /// <summary>
    /// Cleans AD display names by removing parenthetical suffixes like "(J)" or "(JJ)"
    /// "Jones, Jeffery (J)" becomes "Jones, Jeffery"
    /// </summary>
    private static string CleanAdDisplayName(string displayName)
    {
        if (string.IsNullOrEmpty(displayName))
            return "";

        // Remove parenthetical suffix like (J), (JJ), (Jr), etc.
        var parenIndex = displayName.LastIndexOf('(');
        if (parenIndex > 0 && displayName.EndsWith(')'))
        {
            displayName = displayName.Substring(0, parenIndex).Trim();
        }

        return displayName;
    }

    /// <summary>
    /// Extracts first and last name from various formats.
    /// Handles: "First Last", "Last, First", "Last, First (J)"
    /// </summary>
    private static (string FirstName, string LastName) ParseName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ("", "");

        // Clean any parenthetical suffix first
        name = CleanAdDisplayName(name);

        // Handle "Last, First" format
        if (name.Contains(','))
        {
            var parts = name.Split(',', 2).Select(p => p.Trim()).ToArray();
            if (parts.Length == 2)
            {
                return (parts[1], parts[0]); // First is after comma, Last is before
            }
        }

        // Handle "First Last" or "First Middle Last" format
        var spaceParts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (spaceParts.Length >= 2)
        {
            var firstName = spaceParts[0];
            var lastName = spaceParts[^1]; // Last element
            return (firstName, lastName);
        }

        // Single name - assume it's a last name
        return ("", name);
    }

    /// <summary>
    /// Common nickname mappings (both directions).
    /// </summary>
    private static readonly Dictionary<string, HashSet<string>> NicknameMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["jeff"] = ["jeffrey", "jeffery", "geoffrey", "geoff"],
        ["jeffrey"] = ["jeff", "jeffery", "geoffrey", "geoff"],
        ["jeffery"] = ["jeff", "jeffrey", "geoffrey", "geoff"],
        ["geoff"] = ["geoffrey", "jeff", "jeffrey", "jeffery"],
        ["geoffrey"] = ["geoff", "jeff", "jeffrey", "jeffery"],
        ["john"] = ["jonathan", "jon", "johnny", "jack"],
        ["jonathan"] = ["john", "jon", "johnny"],
        ["jon"] = ["john", "jonathan", "johnny"],
        ["mike"] = ["michael", "mick", "mickey"],
        ["michael"] = ["mike", "mick", "mickey"],
        ["bill"] = ["william", "will", "billy", "willy"],
        ["william"] = ["bill", "will", "billy", "willy"],
        ["will"] = ["william", "bill", "billy"],
        ["bob"] = ["robert", "rob", "robbie", "bobby"],
        ["robert"] = ["bob", "rob", "robbie", "bobby"],
        ["rob"] = ["robert", "bob", "robbie"],
        ["jim"] = ["james", "jimmy", "jamie"],
        ["james"] = ["jim", "jimmy", "jamie"],
        ["jimmy"] = ["james", "jim", "jamie"],
        ["joe"] = ["joseph", "joey"],
        ["joseph"] = ["joe", "joey"],
        ["tom"] = ["thomas", "tommy"],
        ["thomas"] = ["tom", "tommy"],
        ["dan"] = ["daniel", "danny"],
        ["daniel"] = ["dan", "danny"],
        ["dave"] = ["david", "davey"],
        ["david"] = ["dave", "davey"],
        ["steve"] = ["steven", "stephen", "stevie"],
        ["steven"] = ["steve", "stephen", "stevie"],
        ["stephen"] = ["steve", "steven", "stevie"],
        ["chris"] = ["christopher", "kristopher"],
        ["christopher"] = ["chris"],
        ["matt"] = ["matthew", "matty"],
        ["matthew"] = ["matt", "matty"],
        ["nick"] = ["nicholas", "nicky"],
        ["nicholas"] = ["nick", "nicky"],
        ["tony"] = ["anthony", "anton"],
        ["anthony"] = ["tony", "anton"],
        ["rick"] = ["richard", "ricky", "dick"],
        ["richard"] = ["rick", "ricky", "dick"],
        ["dick"] = ["richard", "rick"],
        ["ben"] = ["benjamin", "benny"],
        ["benjamin"] = ["ben", "benny"],
        ["sam"] = ["samuel", "sammy", "samantha"],
        ["samuel"] = ["sam", "sammy"],
        ["samantha"] = ["sam", "sammy"],
        ["alex"] = ["alexander", "alexandra", "alexis"],
        ["alexander"] = ["alex"],
        ["alexandra"] = ["alex"],
        ["liz"] = ["elizabeth", "beth", "betty", "eliza"],
        ["elizabeth"] = ["liz", "beth", "betty", "eliza"],
        ["beth"] = ["elizabeth", "liz", "betty"],
        ["kate"] = ["katherine", "catherine", "kathy", "cathy", "katie"],
        ["katherine"] = ["kate", "kathy", "katie"],
        ["catherine"] = ["kate", "cathy", "katie"],
        ["jen"] = ["jennifer", "jenny"],
        ["jennifer"] = ["jen", "jenny"],
        ["sue"] = ["susan", "susie", "suzanne"],
        ["susan"] = ["sue", "susie", "suzanne"],
        ["pat"] = ["patricia", "patrick", "patty"],
        ["patricia"] = ["pat", "patty", "tricia"],
        ["patrick"] = ["pat", "paddy"],
        ["ed"] = ["edward", "eddie", "ted", "teddy"],
        ["edward"] = ["ed", "eddie", "ted", "teddy"],
        ["ted"] = ["edward", "theodore", "teddy"],
        ["theodore"] = ["ted", "teddy", "theo"],
        ["larry"] = ["lawrence", "laurence"],
        ["lawrence"] = ["larry"],
        ["charlie"] = ["charles", "chuck"],
        ["charles"] = ["charlie", "chuck"],
        ["chuck"] = ["charles", "charlie"],
        ["harry"] = ["harold", "henry"],
        ["harold"] = ["harry", "hal"],
        ["henry"] = ["harry", "hank"],
        ["hank"] = ["henry"],
        ["greg"] = ["gregory"],
        ["gregory"] = ["greg"],
        ["andy"] = ["andrew", "drew"],
        ["andrew"] = ["andy", "drew"],
        ["drew"] = ["andrew", "andy"],
        ["pete"] = ["peter"],
        ["peter"] = ["pete"],
        ["tim"] = ["timothy", "timmy"],
        ["timothy"] = ["tim", "timmy"],
        ["ron"] = ["ronald", "ronnie"],
        ["ronald"] = ["ron", "ronnie"],
        ["phil"] = ["philip", "phillip"],
        ["philip"] = ["phil"],
        ["phillip"] = ["phil"],
        ["doug"] = ["douglas"],
        ["douglas"] = ["doug"],
        ["ray"] = ["raymond"],
        ["raymond"] = ["ray"],
        ["jerry"] = ["gerald", "jerome"],
        ["gerald"] = ["jerry", "gerry"],
        ["gerry"] = ["gerald", "jerry"],
        ["ken"] = ["kenneth", "kenny"],
        ["kenneth"] = ["ken", "kenny"],
        ["don"] = ["donald", "donnie"],
        ["donald"] = ["don", "donnie"],
        ["frank"] = ["francis", "franklin", "frankie"],
        ["francis"] = ["frank", "frankie"],
        ["roger"] = ["rodger"],
        ["rodger"] = ["roger"],
        ["al"] = ["albert", "alan", "allen", "alfred"],
        ["albert"] = ["al", "bert"],
        ["alan"] = ["al"],
        ["allen"] = ["al"],
        ["alfred"] = ["al", "fred", "alfie"],
        ["fred"] = ["frederick", "alfred", "freddy"],
        ["frederick"] = ["fred", "freddy"],
        ["wes"] = ["wesley"],
        ["wesley"] = ["wes"],
        ["stan"] = ["stanley"],
        ["stanley"] = ["stan"],
        ["marge"] = ["margaret", "maggie", "peggy"],
        ["margaret"] = ["marge", "maggie", "peggy", "meg"],
        ["maggie"] = ["margaret", "marge"],
        ["peggy"] = ["margaret", "marge"],
        ["nancy"] = ["ann", "anne", "anna"],
        ["ann"] = ["anna", "anne", "annie"],
        ["anna"] = ["ann", "anne", "annie"],
        ["anne"] = ["ann", "anna", "annie"],
        ["debbie"] = ["deborah", "deb"],
        ["deborah"] = ["debbie", "deb"],
        ["deb"] = ["deborah", "debbie"],
        ["linda"] = ["lynn", "lindy"],
        ["lynn"] = ["linda", "lynne"],
        ["barb"] = ["barbara"],
        ["barbara"] = ["barb", "barbie"],
        ["carol"] = ["caroline", "carolyn"],
        ["caroline"] = ["carol"],
        ["carolyn"] = ["carol"],
        ["cindy"] = ["cynthia"],
        ["cynthia"] = ["cindy"],
        ["donna"] = ["don"],
        ["jane"] = ["janet", "janice"],
        ["janet"] = ["jane", "jan"],
        ["janice"] = ["jane", "jan"],
        ["jan"] = ["janet", "janice", "jane"],
        ["judy"] = ["judith", "judi"],
        ["judith"] = ["judy", "judi"],
        ["julie"] = ["julia", "juliet"],
        ["julia"] = ["julie"],
        ["kathy"] = ["katherine", "catherine", "kate", "katie"],
        ["katie"] = ["katherine", "catherine", "kate", "kathy"],
        ["mary"] = ["marie", "maria"],
        ["marie"] = ["mary", "maria"],
        ["maria"] = ["mary", "marie"],
        ["pam"] = ["pamela"],
        ["pamela"] = ["pam"],
        ["sandy"] = ["sandra"],
        ["sandra"] = ["sandy"],
        ["sharon"] = ["shari"],
        ["shari"] = ["sharon"],
        ["steph"] = ["stephanie", "stephany"],
        ["stephanie"] = ["steph", "stephany"],
        ["terri"] = ["teresa", "theresa"],
        ["teresa"] = ["terri", "theresa"],
        ["theresa"] = ["terri", "teresa"],
        ["vicky"] = ["victoria"],
        ["victoria"] = ["vicky", "vicki"],
        ["vicki"] = ["victoria", "vicky"]
    };

    /// <summary>
    /// Checks if two first names could be the same person (exact match or nickname variation).
    /// </summary>
    private static bool FirstNamesMatch(string name1, string name2)
    {
        if (string.IsNullOrWhiteSpace(name1) || string.IsNullOrWhiteSpace(name2))
            return false;

        name1 = name1.ToLowerInvariant().Trim();
        name2 = name2.ToLowerInvariant().Trim();

        // Exact match
        if (name1 == name2)
            return true;

        // Check nickname mappings
        if (NicknameMappings.TryGetValue(name1, out var nicknames1) && nicknames1.Contains(name2))
            return true;

        if (NicknameMappings.TryGetValue(name2, out var nicknames2) && nicknames2.Contains(name1))
            return true;

        // Check if one starts with the other (e.g., "Rob" matches "Robert")
        if (name1.Length >= 3 && name2.StartsWith(name1))
            return true;
        if (name2.Length >= 3 && name1.StartsWith(name2))
            return true;

        return false;
    }

    /// <summary>
    /// Checks if two last names match (case-insensitive).
    /// </summary>
    private static bool LastNamesMatch(string name1, string name2)
    {
        if (string.IsNullOrWhiteSpace(name1) || string.IsNullOrWhiteSpace(name2))
            return false;

        return string.Equals(name1.Trim(), name2.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsEmail(string input)
    {
        return input.Contains('@') && input.Contains('.');
    }

    private static List<string> GenerateNamePermutations(string name)
    {
        var permutations = new List<string>();

        // Clean the name first (remove parenthetical suffixes)
        name = CleanAdDisplayName(name);

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
