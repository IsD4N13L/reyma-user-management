using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Azure.Security.KeyVault.Secrets;
using Serilog;
using System.Text;

namespace UserManagement.Services
{
    public interface IConfigurationService : IUserManagementScopedService
    {
        Task<string> GetValueAsync(string key, string? defaultValue = null);
        Task<T> GetSectionAsync<T>(string sectionName) where T : class, new();
        Task<string> GetConnectionStringAsync(string name = "UserManagementApi");
        Task<string> GetJwtIssuerAsync();
        Task<string> GetJwtAudienceAsync();
        Task<string> GetJwtSecretKeyAsync();
    }
    public class ConfigurationService(SecretClient secretClient, IConfiguration configuration) : IConfigurationService
    {

        private readonly Dictionary<string, string> cache = new();
        private readonly SemaphoreSlim cacheLock = new(1, 1);
        public async Task<string> GetValueAsync(string key, string? defaultValue = null)
        {
            // Verificar cache primero
            await cacheLock.WaitAsync();
            try
            {
                if (cache.TryGetValue(key, out var cachedValue))
                {
                    return cachedValue;
                }
            }
            finally
            {
                cacheLock.Release();
            }

            string? value = null;

            // 1. PRIORIDAD: Variables de entorno (App Service)
            value = Environment.GetEnvironmentVariable(key.Replace(":", "__"));
            if (!string.IsNullOrEmpty(value))
            {
                Log.Debug("Valor obtenido de variable de entorno: {Key}", key);
                await CacheValueAsync(key, value);
                return value;
            }

            // 2. PRIORIDAD: Key Vault
            if (secretClient != null)
            {
                try
                {
                    var keyVaultKey = key.Replace(":", "--");
                    var secret = await secretClient.GetSecretAsync(keyVaultKey);
                    value = secret.Value.Value;

                    if (!string.IsNullOrEmpty(value))
                    {
                        Log.Debug("Valor obtenido de Key Vault: {Key}", key);
                        await CacheValueAsync(key, value);
                        return value;
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "No se pudo obtener valor de Key Vault: {Key}", key);
                }
            }

            // 3. PRIORIDAD: appsettings.json
            value = configuration[key];
            if (!string.IsNullOrEmpty(value))
            {
                Log.Debug("Valor obtenido de configuración local: {Key}", key);
                await CacheValueAsync(key, value);
                return value;
            }

            // 4. FALLBACK: Valor por defecto
            if (defaultValue != null)
            {
                Log.Debug("Usando valor por defecto para: {Key}", key);
                return defaultValue;
            }

            throw new InvalidOperationException($"Configuración '{key}' no encontrada en ningún proveedor");
        }

        public async Task<T> GetSectionAsync<T>(string sectionName) where T : class, new()
        {
            var result = new T();
            var properties = typeof(T).GetProperties();

            foreach (var property in properties)
            {
                var key = $"{sectionName}:{property.Name}";
                try
                {
                    var value = await GetValueAsync(key);
                    if (!string.IsNullOrEmpty(value))
                    {
                        // Conversión básica de tipos
                        object convertedValue = property.PropertyType.Name switch
                        {
                            "Boolean" => bool.Parse(value),
                            "Int32" => int.Parse(value),
                            "TimeSpan" => TimeSpan.Parse(value),
                            _ => value
                        };

                        property.SetValue(result, convertedValue);
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug(ex, "No se pudo obtener valor para: {Key}", key);
                }
            }

            return result;
        }

        public async Task<string> GetConnectionStringAsync(string name = "DefaultConnection")
        {
            return await GetValueAsync($"ConnectionStrings:{name}");
        }

        public async Task<string> GetJwtIssuerAsync()
        {
            return await GetValueAsync("Jwt:Issuer", "users-management-api");
        }

        public async Task<string> GetJwtAudienceAsync()
        {
            return await GetValueAsync("Jwt:Audience", "users-management-api-client");
        }

        public async Task<string> GetJwtSecretKeyAsync()
        {
            return await GetValueAsync("Jwt:SecretKey");
        }

        private async Task CacheValueAsync(string key, string value)
        {
            await cacheLock.WaitAsync();
            try
            {
                cache[key] = value;
            }
            finally
            {
                cacheLock.Release();
            }
        }
    }
}
