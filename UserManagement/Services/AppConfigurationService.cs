using Microsoft.Extensions.Configuration;
using Serilog;
using System.Text.Json;

namespace UserManagement.Services
{

    public interface IAppConfigurationService : IUserManagementScopedService
    {
        Task<T> GetValueAsync<T>(string key, T defaultValue = default!);
        Task<string> GetStringAsync(string key, string defaultValue = "");
        Task<bool> GetBoolAsync(string key, bool defaultValue = false);
        Task<int> GetIntAsync(string key, int defaultValue = 0);
        Task UpdateConfigurationAsync(string key, object value);
        Task<Dictionary<string, object>> GetAllConfigurationsAsync(string prefix = "");
        Task<List<string>> GetAllFeatureNamesAsync();
        Task<Dictionary<string, object>> GetFeatureParametersAsync(string featureName);
    }
    public class AppConfigurationService(IConfiguration configuration) : IAppConfigurationService
    {
        public Task<T> GetValueAsync<T>(string key, T defaultValue = default!)
        {
            try
            {
                var value = configuration[key];
                if (string.IsNullOrEmpty(value))
                {
                    Log.Debug("Configuración no encontrada: {Key}, usando valor por defecto", key);
                    return Task.FromResult(defaultValue);
                }

                if (typeof(T) == typeof(string))
                {
                    return Task.FromResult((T)(object)value);
                }

                var convertedValue = JsonSerializer.Deserialize<T>(value);
                return Task.FromResult(convertedValue ?? defaultValue);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error obteniendo configuración: {Key}", key);
                return Task.FromResult(defaultValue);
            }
        }

        public async Task<string> GetStringAsync(string key, string defaultValue = "")
        {
            return await GetValueAsync(key, defaultValue);
        }

        public async Task<bool> GetBoolAsync(string key, bool defaultValue = false)
        {
            var value = await GetStringAsync(key);
            return bool.TryParse(value, out var result) ? result : defaultValue;
        }

        public async Task<int> GetIntAsync(string key, int defaultValue = 0)
        {
            var value = await GetStringAsync(key);
            return int.TryParse(value, out var result) ? result : defaultValue;
        }

        public Task UpdateConfigurationAsync(string key, object value)
        {
            // Esta operación requeriría Azure App Configuration Data API
            // Por ahora, solo loggeamos el intento
            Log.Information("Solicitud de actualización de configuración: {Key} = {Value}", key, value);

            // TODO: Implementar con Azure App Configuration Data Client
            throw new NotImplementedException("Actualización de configuración requiere Azure App Configuration Data API");
        }

        public Task<Dictionary<string, object>> GetAllConfigurationsAsync(string prefix = "")
        {
            var configurations = new Dictionary<string, object>();

            try
            {
                var configurationRoot = configuration as IConfigurationRoot;
                if (configurationRoot != null)
                {
                    foreach (var child in configurationRoot.GetChildren())
                    {
                        if (string.IsNullOrEmpty(prefix) || child.Key.StartsWith(prefix))
                        {
                            configurations[child.Key] = child.Value ?? "";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error obteniendo todas las configuraciones");
            }

            return Task.FromResult(configurations);
        }

        public Task<List<string>> GetAllFeatureNamesAsync()
        {
            var features = new List<string>();

            try
            {
                var featureSection = configuration.GetSection("FeatureManagement");

                foreach (var child in featureSection.GetChildren())
                {
                    if (!IsSystemConfiguration(child.Key))
                    {
                        features.Add(child.Key);
                    }
                }

                Log.Information("Features encontrados en configuración: {Features}", string.Join(", ", features));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error obteniendo nombres de features");
            }

            return Task.FromResult(features);
        }

        public Task<Dictionary<string, object>> GetFeatureParametersAsync(string featureName)
        {
            var parameters = new Dictionary<string, object>();

            try
            {
                var featureSection = configuration.GetSection($"FeatureManagement:{featureName}");

                foreach (var child in featureSection.GetChildren())
                {
                    parameters[child.Key] = child.Value ?? "";
                }

                Log.Debug("Parámetros obtenidos para feature {FeatureName}: {ParameterCount}",
                    featureName, parameters.Count);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error obteniendo parámetros del feature: {FeatureName}", featureName);
            }

            return Task.FromResult(parameters);
        }

        private bool IsSystemConfiguration(string key)
        {
            var systemKeys = new[]
            {
            "RefreshSentinel",
            "CacheExpiration",
            "Label",
            "Environment"
        };

            return systemKeys.Contains(key, StringComparer.OrdinalIgnoreCase);
        }
    }
}
