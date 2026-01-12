using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace LifecycleDashboard.Services;

/// <summary>
/// Secure storage implementation using .NET Data Protection API.
/// Secrets are encrypted and stored in a JSON file outside the web root.
/// </summary>
public class SecureStorageService : ISecureStorageService
{
    private readonly IDataProtector _protector;
    private readonly string _storagePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private const string Purpose = "LifecycleDashboard.SecureStorage.v1";
    private const string FileName = "secure-credentials.json";

    public SecureStorageService(IDataProtectionProvider dataProtectionProvider, IWebHostEnvironment environment)
    {
        _protector = dataProtectionProvider.CreateProtector(Purpose);

        // Store outside web root in a data directory
        var dataDir = Path.Combine(environment.ContentRootPath, "..", "data");
        Directory.CreateDirectory(dataDir);
        _storagePath = Path.Combine(dataDir, FileName);
    }

    public async Task StoreSecretAsync(string key, string value)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentNullException(nameof(key));

        await _lock.WaitAsync();
        try
        {
            var secrets = await LoadSecretsAsync();

            // Encrypt the value
            var encrypted = _protector.Protect(value);

            secrets[key] = new StoredSecret
            {
                EncryptedValue = encrypted,
                LastUpdated = DateTimeOffset.UtcNow
            };

            await SaveSecretsAsync(secrets);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<string?> GetSecretAsync(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        await _lock.WaitAsync();
        try
        {
            var secrets = await LoadSecretsAsync();

            if (!secrets.TryGetValue(key, out var stored))
                return null;

            try
            {
                return _protector.Unprotect(stored.EncryptedValue);
            }
            catch (CryptographicException)
            {
                // Key may have changed, secret is unreadable
                return null;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> HasSecretAsync(string key)
    {
        if (string.IsNullOrEmpty(key))
            return false;

        await _lock.WaitAsync();
        try
        {
            var secrets = await LoadSecretsAsync();
            return secrets.ContainsKey(key);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RemoveSecretAsync(string key)
    {
        if (string.IsNullOrEmpty(key))
            return;

        await _lock.WaitAsync();
        try
        {
            var secrets = await LoadSecretsAsync();

            if (secrets.Remove(key))
            {
                await SaveSecretsAsync(secrets);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<string>> GetSecretKeysAsync(string prefix)
    {
        await _lock.WaitAsync();
        try
        {
            var secrets = await LoadSecretsAsync();
            return secrets.Keys
                .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<CredentialStatus> GetCredentialStatusAsync(string dataSourceId)
    {
        var prefix = SecretKeys.GetDataSourcePrefix(dataSourceId);
        var keys = await GetSecretKeysAsync(prefix);

        if (keys.Count == 0)
        {
            return new CredentialStatus
            {
                HasCredentials = false,
                ConfiguredFields = []
            };
        }

        await _lock.WaitAsync();
        try
        {
            var secrets = await LoadSecretsAsync();
            var lastUpdated = secrets
                .Where(kvp => kvp.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Select(kvp => kvp.Value.LastUpdated)
                .DefaultIfEmpty(DateTimeOffset.MinValue)
                .Max();

            var fields = keys
                .Select(k => k.Replace(prefix, ""))
                .ToList();

            return new CredentialStatus
            {
                HasCredentials = true,
                LastUpdated = lastUpdated,
                ConfiguredFields = fields
            };
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<Dictionary<string, StoredSecret>> LoadSecretsAsync()
    {
        if (!File.Exists(_storagePath))
            return new Dictionary<string, StoredSecret>();

        try
        {
            var json = await File.ReadAllTextAsync(_storagePath);
            return JsonSerializer.Deserialize<Dictionary<string, StoredSecret>>(json)
                   ?? new Dictionary<string, StoredSecret>();
        }
        catch
        {
            return new Dictionary<string, StoredSecret>();
        }
    }

    private async Task SaveSecretsAsync(Dictionary<string, StoredSecret> secrets)
    {
        var json = JsonSerializer.Serialize(secrets, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(_storagePath, json);
    }

    private class StoredSecret
    {
        public string EncryptedValue { get; set; } = "";
        public DateTimeOffset LastUpdated { get; set; }
    }
}
