using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;
using Serilog;

namespace UserManagement.Services
{

    public interface IFeatureToggleService : IUserManagementScopedService
    {
        Task<bool> IsEnabledAsync(string featureName);
        Task<T> GetFeatureValueAsync<T>(string featureName, T defaultValue = default!);
        Task<Dictionary<string, bool>> GetAllFeaturesAsync();
    }
    public class FeatureToggleService(IFeatureManager featureManager, IConfiguration configuration) : IFeatureToggleService
    {
        public async Task<bool> IsEnabledAsync(string featureName)
        {
            try
            {
                var isEnabled = await featureManager.IsEnabledAsync(featureName);
                Log.Debug("Feature {FeatureName} está {Status}", featureName, isEnabled ? "habilitado" : "deshabilitado");
                return isEnabled;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error verificando feature flag: {FeatureName}", featureName);
                return false;
            }
        }

        public async Task<T> GetFeatureValueAsync<T>(string featureName, T defaultValue = default!)
        {
            try
            {
                var isEnabled = await featureManager.IsEnabledAsync(featureName);
                if (!isEnabled)
                {
                    return defaultValue;
                }

                // Intentar obtener valor específico desde configuración
                var configPath = $"FeatureManagement:{featureName}:Parameters";
                var value = configuration.GetValue<T>(configPath, defaultValue);

                return value;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error obteniendo valor de feature: {FeatureName}", featureName);
                return defaultValue;
            }
        }

        public async Task<Dictionary<string, bool>> GetAllFeaturesAsync()
        {
            var features = new Dictionary<string, bool>();

            try
            {
                // 1. Obtener todos los features desde la sección FeatureManagement
                var featureSection = configuration.GetSection("FeatureManagement");
                var discoveredFeatures = GetFeaturesFromConfiguration(featureSection);

                Log.Information("Features descubiertos desde configuración: {FeatureCount}", discoveredFeatures.Count);

                // 2. Evaluar cada feature encontrado
                foreach (var featureName in discoveredFeatures)
                {
                    try
                    {
                        features[featureName] = await featureManager.IsEnabledAsync(featureName);
                        Log.Debug("Feature evaluado: {FeatureName} = {IsEnabled}", featureName, features[featureName]);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Error evaluando feature específico: {FeatureName}", featureName);
                        features[featureName] = false;
                    }
                }

                // 3. Si no se encontraron features en configuración, usar lista de fallback
                if (features.Count == 0)
                {
                    Log.Warning("No se encontraron features en configuración, usando lista de fallback");
                    await AddFallbackFeatures(features);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error obteniendo todos los features");

                // Fallback en caso de error total
                await AddFallbackFeatures(features);
            }

            Log.Information("Total de features cargados: {FeatureCount}", features.Count);
            return features;
        }

        // Método para obtener features desde cualquier fuente de configuración
        private List<string> GetFeaturesFromConfiguration(IConfigurationSection featureSection)
        {
            var features = new List<string>();

            try
            {
                // Obtener todos los children de la sección FeatureManagement
                foreach (var child in featureSection.GetChildren())
                {
                    var featureName = child.Key;

                    // Filtrar configuraciones que no son features
                    if (!IsSystemConfiguration(featureName))
                    {
                        features.Add(featureName);
                        Log.Debug("Feature descubierto: {FeatureName}", featureName);
                    }
                }

                // También buscar en otras posibles ubicaciones de App Configuration
                var appConfigFeatures = GetFeaturesFromAppConfiguration();
                features.AddRange(appConfigFeatures);

                // Remover duplicados
                features = features.Distinct().ToList();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error extrayendo features de configuración");
            }

            return features;
        }

        // Método para obtener features específicamente de App Configuration
        private List<string> GetFeaturesFromAppConfiguration()
        {
            var features = new List<string>();

            try
            {
                // Buscar en posibles ubicaciones de App Configuration
                var appConfigPaths = new[]
                {
                "AppConfiguration:Features",
                "Features",
                "UserManagement:Features"
            };

                foreach (var path in appConfigPaths)
                {
                    var section = configuration.GetSection(path);
                    if (section.Exists())
                    {
                        foreach (var child in section.GetChildren())
                        {
                            if (!features.Contains(child.Key))
                            {
                                features.Add(child.Key);
                                Log.Debug("Feature desde App Configuration: {FeatureName}", child.Key);
                            }
                        }
                    }
                }

                // Buscar features con prefijo específico
                var allKeys = GetAllConfigurationKeys();
                var featureKeys = allKeys
                    .Where(key => key.StartsWith("FeatureManagement:", StringComparison.OrdinalIgnoreCase))
                    .Select(key => ExtractFeatureName(key))
                    .Where(name => !string.IsNullOrEmpty(name))
                    .Distinct()
                    .ToList();

                features.AddRange(featureKeys);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error obteniendo features desde App Configuration");
            }

            return features.Distinct().ToList();
        }

        // Método para obtener todas las claves de configuración
        private List<string> GetAllConfigurationKeys()
        {
            var keys = new List<string>();

            try
            {
                if (configuration is IConfigurationRoot configRoot)
                {
                    foreach (var provider in configRoot.Providers)
                    {
                        if (provider.TryGet("FeatureManagement", out _))
                        {
                            // Intentar obtener todas las claves del proveedor
                            // Esto es específico para cada proveedor
                            ExtractKeysFromProvider(provider, keys);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error obteniendo claves de configuración");
            }

            return keys;
        }

        private void ExtractKeysFromProvider(IConfigurationProvider provider, List<string> keys)
        {
            try
            {
                // Método genérico para extraer claves
                // Esto podría necesitar implementación específica según el proveedor
                var data = new Dictionary<string, string>();
                provider.Load();

                // Usar reflexión para obtener datos del proveedor si es necesario
                var dataProperty = provider.GetType().GetProperty("Data",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (dataProperty?.GetValue(provider) is IDictionary<string, string> providerData)
                {
                    foreach (var key in providerData.Keys)
                    {
                        if (key.StartsWith("FeatureManagement:", StringComparison.OrdinalIgnoreCase))
                        {
                            keys.Add(key);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error extrayendo claves del proveedor {ProviderType}", provider.GetType().Name);
            }
        }

        private string ExtractFeatureName(string configKey)
        {
            try
            {
                // Extraer nombre del feature de claves como:
                // "FeatureManagement:UserPhotoUpload" -> "UserPhotoUpload"
                // "FeatureManagement:UserPhotoUpload:Enabled" -> "UserPhotoUpload"

                if (configKey.StartsWith("FeatureManagement:", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = configKey.Split(':');
                    if (parts.Length >= 2)
                    {
                        return parts[1];
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Error extrayendo nombre de feature de clave: {ConfigKey}", configKey);
            }

            return string.Empty;
        }

        private bool IsSystemConfiguration(string key)
        {
            // Filtrar configuraciones del sistema que no son features
            var systemKeys = new[]
            {
            "RefreshSentinel",
            "CacheExpiration",
            "Label",
            "Environment"
        };

            return systemKeys.Contains(key, StringComparer.OrdinalIgnoreCase);
        }

        private async Task AddFallbackFeatures(Dictionary<string, bool> features)
        {
            // Lista de fallback cuando no se pueden obtener desde configuración
            var fallbackFeatures = new[]
            {
            "UserPhotoUpload",
            "AdvancedUserSearch",
            "UserEncryption",
            "EmailNotifications",
            "TwoFactorAuth",
            "UserAuditLogging",
            "BulkUserOperations",
            "TwoFactorAuthentication"
        };

            Log.Information("Cargando {Count} features de fallback", fallbackFeatures.Length);

            foreach (var feature in fallbackFeatures)
            {
                try
                {
                    if (!features.ContainsKey(feature))
                    {
                        features[feature] = await featureManager.IsEnabledAsync(feature);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error evaluando feature de fallback: {FeatureName}", feature);
                    features[feature] = false;
                }
            }
        }
    }
}
