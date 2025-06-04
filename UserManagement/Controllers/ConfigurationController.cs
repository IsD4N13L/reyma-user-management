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
                var features = await featureToggleService.GetAllFeaturesAsync();
                return Ok(new { features, timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error obteniendo features");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("settings")]
        public async Task<ActionResult> GetSettings()
        {
            try
            {
                var settings = new
                {
                    MaxPhotoSizeMB = await appConfigurationService.GetIntAsync("UserManagement:MaxPhotoSizeMB", 5),
                    AllowedPhotoTypes = await appConfigurationService.GetStringAsync("UserManagement:AllowedPhotoTypes", "jpg,png,gif"),
                    TokenExpirationHours = await appConfigurationService.GetIntAsync("UserManagement:TokenExpirationHours", 24),
                    MaxLoginAttempts = await appConfigurationService.GetIntAsync("UserManagement:MaxLoginAttempts", 5),
                    EmailNotificationsEnabled = await featureToggleService.IsEnabledAsync("EmailNotifications"),
                    TwoFactorAuthEnabled = await featureToggleService.IsEnabledAsync("TwoFactorAuth")
                };

                return Ok(new { settings, timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error obteniendo configuraciones");
                return StatusCode(500, new { message = "Error interno del servidor" });
            }
        }

        [HttpGet("health")]
        [AllowAnonymous]
        public async Task<ActionResult> GetHealth()
        {
            try
            {
                var health = new
                {
                    Status = "Healthy",
                    Timestamp = DateTime.UtcNow,
                    ConfigurationSources = new
                    {
                        AppConfiguration = await appConfigurationService.GetStringAsync("UserManagement:RefreshSentinel", "") != "",
                        FeatureFlags = await featureToggleService.IsEnabledAsync("UserPhotoUpload") ||
                                       await featureToggleService.IsEnabledAsync("UserEncryption")
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
