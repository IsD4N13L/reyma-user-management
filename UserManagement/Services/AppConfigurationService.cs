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
    }
}
