using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using UserManagement.Services;

namespace UserManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConfigurationController(IAppConfigurationService appConfigurationService,
        IFeatureToggleService featureToggleService) : ControllerBase
    {
        [HttpGet("features")]
        public async Task<ActionResult> GetFeatures()
        {
            try
            {
                // Obtener features dinámicamente desde App Configuration
                var features = await featureToggleService.GetAllFeaturesAsync();
                var featureNames = await appConfigurationService.GetAllFeatureNamesAsync();

                var result = new
                {
                    features,
                    discoveredFeatures = featureNames,
                    totalCount = features.Count,
                    enabledCount = features.Count(f => f.Value),
                    timestamp = DateTime.UtcNow
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error obteniendo features");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("features/{featureName}")]
        public async Task<ActionResult> GetFeatureDetails(string featureName)
        {
            try
            {
                var isEnabled = await featureToggleService.IsEnabledAsync(featureName);
                var parameters = await appConfigurationService.GetFeatureParametersAsync(featureName);

                var result = new
                {
                    featureName,
                    isEnabled,
                    parameters,
                    timestamp = DateTime.UtcNow
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error obteniendo detalles del feature: {FeatureName}", featureName);
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("health")]
        [AllowAnonymous]
        public async Task<ActionResult> GetHealth()
        {
            try
            {
                var allFeatures = await featureToggleService.GetAllFeaturesAsync();
                var discoveredFeatures = await appConfigurationService.GetAllFeatureNamesAsync();

                var health = new
                {
                    Status = "Healthy",
                    Timestamp = DateTime.UtcNow,
                    Configuration = new
                    {
                        TotalFeatures = allFeatures.Count,
                        EnabledFeatures = allFeatures.Count(f => f.Value),
                        DiscoveredFromConfig = discoveredFeatures.Count,
                        FeaturesFromAppConfig = discoveredFeatures.Count > 0
                    }
                };

                return Ok(health);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error en health check");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }
    }
}
