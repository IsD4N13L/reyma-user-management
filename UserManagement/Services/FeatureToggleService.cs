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
    public class FeatureToggleService(IFeatureManager featureManager) : IFeatureToggleService
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

                // Para features más complejas, aquí podrías obtener valores específicos
                // Por ahora, solo retornamos el valor por defecto si está habilitado
                return defaultValue;
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
                // Lista de features conocidos - esto podría venir de configuración
                var knownFeatures = new[]
                {
                    "UserPhotoUpload",
                    "AdvancedUserSearch",
                    "UserEncryption",
                    "EmailNotifications",
                    "TwoFactorAuth"
                };

                foreach (var feature in knownFeatures)
                {
                    features[feature] = await featureManager.IsEnabledAsync(feature);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error obteniendo todos los features");
            }

            return features;
        }
    }
}
