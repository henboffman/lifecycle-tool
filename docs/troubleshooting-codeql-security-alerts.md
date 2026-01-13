# Troubleshooting Azure DevOps Advanced Security (CodeQL) Integration

This guide provides comprehensive debugging steps for the CodeQL/Advanced Security API integration in the Lifecycle Dashboard.

## Overview

The application fetches security alerts from Azure DevOps Advanced Security using this endpoint:
```
https://advsec.dev.azure.com/{organization}/{project}/_apis/alert/repositories/{repositoryId}/alerts?api-version=7.2-preview.1
```

**Important:** This is a DIFFERENT base URL than the standard Azure DevOps API (`dev.azure.com`). The Advanced Security API uses `advsec.dev.azure.com`.

## Current Implementation

The relevant code is in `src/LifecycleDashboard/Services/DataIntegration/AzureDevOpsService.cs`, method `GetSecurityAlertsAsync()` (lines 768-909).

### How Authentication Works

The same PAT (Personal Access Token) used for regular Azure DevOps API calls is used for the Advanced Security API. The authentication is Basic Auth with format:
```
Authorization: Basic base64({username}:{PAT})
```

### Configuration Values Retrieved

The code retrieves these secrets from secure storage:
- `AzureDevOpsOrganization` - The organization name (NOT full URL)
- `AzureDevOpsProject` - The project name
- `AzureDevOpsPat` - The Personal Access Token
- `AzureDevOpsUsername` - The username for Basic auth

## Step-by-Step Debugging

### Step 1: Verify Your Configuration Values

First, check what values are stored in the secure storage. Run this in a .NET REPL or create a quick test endpoint:

```csharp
// In a Blazor page or test, inject ISecureStorageService and check:
var org = await _secureStorage.GetSecretAsync("AzureDevOps:Organization");
var project = await _secureStorage.GetSecretAsync("AzureDevOps:Project");
var pat = await _secureStorage.GetSecretAsync("AzureDevOps:Pat");
var username = await _secureStorage.GetSecretAsync("AzureDevOps:Username");

Console.WriteLine($"Organization: {org}");
Console.WriteLine($"Project: {project}");
Console.WriteLine($"Username: {username}");
Console.WriteLine($"PAT length: {pat?.Length ?? 0}");
```

### Step 2: Test the Standard Azure DevOps API First

Before testing Advanced Security, verify the standard API works:

```bash
# Replace these values with your actual values
ORG="your-organization"
PROJECT="your-project"
USERNAME="your-username"
PAT="your-pat-token"

# Create the Basic auth header
AUTH=$(echo -n "${USERNAME}:${PAT}" | base64)

# Test the standard API - list repositories
curl -s -H "Authorization: Basic ${AUTH}" \
  "https://dev.azure.com/${ORG}/${PROJECT}/_apis/git/repositories?api-version=7.1" | jq '.count'
```

**Expected:** Should return a number (count of repositories)

If this fails with 401/403:
- Your PAT may be expired
- Your PAT may not have the right scopes
- Your username may be incorrect (try your email address)

### Step 3: Test the Advanced Security API Directly

```bash
# Get a repository ID first (from step 2, pick any repo)
REPO_ID="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"

# Test the Advanced Security endpoint
curl -v -H "Authorization: Basic ${AUTH}" \
  "https://advsec.dev.azure.com/${ORG}/${PROJECT}/_apis/alert/repositories/${REPO_ID}/alerts?api-version=7.2-preview.1"
```

**Possible Responses:**

| Status Code | Meaning | Solution |
|-------------|---------|----------|
| **200 OK** | Success - alerts returned | API is working |
| **200 OK with empty `value: []`** | Success but no alerts | Advanced Security enabled but no findings |
| **401 Unauthorized** | Auth failed | Check PAT, username, ensure PAT has `Advanced Security` scope |
| **403 Forbidden** | Permission denied | PAT needs "Advanced Security (read)" scope |
| **404 Not Found** | Resource not found | Advanced Security not enabled for this repo/project, OR wrong org/project/repo ID |
| **500 Internal Server Error** | Server-side issue | Try again later, check Azure DevOps status |

### Step 4: Check PAT Scopes

Your PAT needs these scopes for security alerts:
- **Code (Read)** - Basic repository access
- **Advanced Security (Read)** - Required for security alerts

To verify/update PAT scopes:
1. Go to Azure DevOps > User Settings > Personal Access Tokens
2. Find your PAT and click "Edit"
3. Ensure "Advanced Security" > "Read" is checked
4. If you need to modify scopes, you'll need to regenerate the PAT

### Step 5: Verify Advanced Security is Enabled

Advanced Security must be enabled at the organization and/or repository level. To check:

1. **Via Azure DevOps UI:**
   - Go to Project Settings > Repos > Repositories
   - Select a repository
   - Look for "Advanced Security" section
   - It should show "Enabled"

2. **Via API (check if endpoint exists):**
```bash
# This will return 404 if Advanced Security isn't enabled
curl -s -o /dev/null -w "%{http_code}" -H "Authorization: Basic ${AUTH}" \
  "https://advsec.dev.azure.com/${ORG}/${PROJECT}/_apis/alert/repositories/${REPO_ID}/alerts?api-version=7.2-preview.1"
```

### Step 6: Check the Full Request URL Being Built

The code builds the URL like this:
```csharp
var organization = await _secureStorage.GetSecretAsync(SecretKeys.AzureDevOpsOrganization) ?? "";
var advSecBaseUrl = $"https://advsec.dev.azure.com/{Uri.EscapeDataString(organization)}/";
var alertsUrl = $"{advSecBaseUrl}{Uri.EscapeDataString(projectName)}/_apis/alert/repositories/{repositoryId}/alerts?api-version=7.2-preview.1";
```

**CRITICAL:** The `organization` value should be JUST the organization name, NOT the full URL.

| Correct | Incorrect |
|---------|-----------|
| `mycompany` | `https://dev.azure.com/mycompany` |
| `mycompany` | `dev.azure.com/mycompany` |
| `mycompany` | `mycompany/` |

### Step 7: Enable Debug Logging

To see what's happening, enable debug logging. In `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "LifecycleDashboard.Services.DataIntegration.AzureDevOpsService": "Debug"
    }
  }
}
```

Then run the sync and look for these log messages:
```
Fetching security alerts from: https://advsec.dev.azure.com/...
```

The log will show:
- The exact URL being called
- The HTTP status code returned
- The error content if it fails

### Step 8: Test with a Specific Repository

From the DataJobs page, you can see which repositories are being scanned. Get a repository ID and project name from the logs, then test directly:

```bash
# Example with specific repo
curl -v -H "Authorization: Basic ${AUTH}" \
  "https://advsec.dev.azure.com/myorg/MyProject/_apis/alert/repositories/a1b2c3d4-e5f6-7890-abcd-ef1234567890/alerts?api-version=7.2-preview.1"
```

## Common Issues and Solutions

### Issue 1: "Security alerts failed" for all repositories

**Symptoms:** Every repo shows security alerts failed in the sync results.

**Likely Causes:**
1. **Advanced Security not enabled** - The most common cause. Enable it in Azure DevOps settings.
2. **PAT missing scope** - Add "Advanced Security (Read)" scope to your PAT.
3. **Wrong organization format** - Organization should be just the name, not a URL.

**Debug Steps:**
```bash
# Check if you can reach the Advanced Security API at all
curl -v -H "Authorization: Basic ${AUTH}" \
  "https://advsec.dev.azure.com/${ORG}/_apis/projects?api-version=7.1"
```

### Issue 2: 404 Not Found

**Symptoms:** API returns 404 for security alerts endpoint.

**Likely Causes:**
1. Advanced Security is not enabled for the repository
2. The repository ID is incorrect
3. The project name is incorrect or not URL-encoded properly

**Debug Steps:**
```bash
# First verify the repository exists via standard API
curl -s -H "Authorization: Basic ${AUTH}" \
  "https://dev.azure.com/${ORG}/${PROJECT}/_apis/git/repositories/${REPO_ID}?api-version=7.1" | jq '.name'

# If that works but Advanced Security 404s, AS isn't enabled for this repo
```

### Issue 3: 401/403 Authentication Errors

**Symptoms:** API returns 401 Unauthorized or 403 Forbidden.

**Likely Causes:**
1. PAT expired
2. PAT doesn't have Advanced Security scope
3. User doesn't have permission to view security alerts

**Debug Steps:**
```bash
# Test if PAT works for regular API
curl -s -H "Authorization: Basic ${AUTH}" \
  "https://dev.azure.com/${ORG}/_apis/projects?api-version=7.1" | jq '.count'

# If regular API works but advsec doesn't, you need Advanced Security scope
```

### Issue 4: Empty Results (Success but no alerts)

**Symptoms:** API returns 200 OK with `{ "value": [] }`.

**This is not an error!** It means:
- Advanced Security IS enabled
- No security alerts have been found (or all are resolved)

The code correctly handles this case and returns `AdvancedSecurityEnabled = true` with zero counts.

## API Response Format

### Successful Response with Alerts

```json
{
  "value": [
    {
      "alertId": 1,
      "alertType": "code",
      "severity": "high",
      "state": "active",
      "title": "SQL Injection vulnerability",
      "firstSeenDate": "2024-01-15T10:30:00Z",
      "lastSeenDate": "2024-01-20T14:00:00Z",
      "repositoryUrl": "https://dev.azure.com/org/project/_git/repo",
      "rule": {
        "id": "sql-injection",
        "name": "SQL Injection",
        "description": "..."
      }
    }
  ],
  "count": 1
}
```

### Fields Parsed by the Application

| Field | Used For |
|-------|----------|
| `state` | Counting open vs closed (`active`, `open`, `new` = open; `fixed`, `closed`, `dismissed` = closed) |
| `severity` | Counting by severity (`critical`, `high`, `medium`, `low`) |
| `alertType` | Identifying secrets (`secret`, `secretscanning`) and dependencies (`dependency`, `dependencyscanning`) |
| `lastSeenDate` | Tracking last scan date |

## Testing Checklist

Run through this checklist to diagnose the issue:

```bash
# Set your variables
export ORG="your-organization"
export PROJECT="your-project"
export USERNAME="your-username"
export PAT="your-pat"
export AUTH=$(echo -n "${USERNAME}:${PAT}" | base64)

# 1. Test standard API (should return project count)
echo "=== Test 1: Standard API ==="
curl -s -H "Authorization: Basic ${AUTH}" \
  "https://dev.azure.com/${ORG}/_apis/projects?api-version=7.1" | jq '.count'

# 2. Get repositories (note down a repo ID)
echo "=== Test 2: List Repositories ==="
curl -s -H "Authorization: Basic ${AUTH}" \
  "https://dev.azure.com/${ORG}/${PROJECT}/_apis/git/repositories?api-version=7.1" | jq '.value[0] | {id, name}'

# 3. Test Advanced Security API (replace REPO_ID with actual ID from step 2)
echo "=== Test 3: Advanced Security API ==="
REPO_ID="paste-repo-id-here"
curl -w "\nHTTP Status: %{http_code}\n" -s -H "Authorization: Basic ${AUTH}" \
  "https://advsec.dev.azure.com/${ORG}/${PROJECT}/_apis/alert/repositories/${REPO_ID}/alerts?api-version=7.2-preview.1" | head -100

# 4. Check if the alerts endpoint returns valid JSON
echo "=== Test 4: Parse Response ==="
curl -s -H "Authorization: Basic ${AUTH}" \
  "https://advsec.dev.azure.com/${ORG}/${PROJECT}/_apis/alert/repositories/${REPO_ID}/alerts?api-version=7.2-preview.1" | jq '.count // .message // "No count or message field"'
```

## If All Else Fails

1. **Create a minimal test case:**
   ```bash
   # The simplest possible test
   curl -u "${USERNAME}:${PAT}" \
     "https://advsec.dev.azure.com/${ORG}/${PROJECT}/_apis/alert/repositories/${REPO_ID}/alerts?api-version=7.2-preview.1"
   ```

2. **Check Azure DevOps service status:** https://status.dev.azure.com/

3. **Try a different API version:**
   ```bash
   # Try older API version
   curl -H "Authorization: Basic ${AUTH}" \
     "https://advsec.dev.azure.com/${ORG}/${PROJECT}/_apis/alert/repositories/${REPO_ID}/alerts?api-version=7.1-preview.1"
   ```

4. **Check if there's an IP allowlist** that might be blocking the `advsec.dev.azure.com` domain but allowing `dev.azure.com`.

5. **Contact Azure DevOps support** if you've verified:
   - Advanced Security is enabled
   - PAT has correct scopes
   - Standard API works
   - But Advanced Security API consistently fails

## Code Location Reference

| File | Purpose |
|------|---------|
| `Services/DataIntegration/AzureDevOpsService.cs:768-909` | `GetSecurityAlertsAsync()` method |
| `Services/DataIntegration/IAzureDevOpsService.cs` | Interface with `SecurityAlertSummary` record |
| `Services/DataIntegration/DataSyncOrchestrator.cs` | Calls `GetSecurityAlertsAsync()` during sync |
| `Services/IMockDataService.cs` | `SyncedRepository` record with security fields |
| `Data/Entities/SyncedRepositoryEntity.cs` | Database entity with security columns |

## Expected Behavior After Fix

Once working, the sync should:
1. Call the Advanced Security API for each repository
2. Count alerts by severity and state
3. Store counts in `SyncedRepository`:
   - `OpenCriticalVulns`, `OpenHighVulns`, `OpenMediumVulns`, `OpenLowVulns`
   - `ClosedCriticalVulns`, `ClosedHighVulns`, `ClosedMediumVulns`, `ClosedLowVulns`
   - `ExposedSecretsCount`, `DependencyAlertCount`
   - `AdvancedSecurityEnabled`, `LastSecurityScanDate`
4. Report success in the sync step results for "Security Alerts"
