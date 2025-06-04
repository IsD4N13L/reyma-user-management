using Hellang.Middleware.ProblemDetails;
using Hellang.Middleware.ProblemDetails.Mvc;
using Serilog;
using UserManagement.Middleware;
using UserManagement.Services;
using System.Reflection;
using System.Text.Json.Serialization;
using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.FeatureManagement;

namespace UserManagement.Extensions.Services
{
    public static class WebAppServiceConfiguration
    {
        public static void ConfigureServices(this WebApplicationBuilder builder)
        {
            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddFeatureManagement();
            //builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();
            builder.Services.AddProblemDetails(ProblemDetailsConfigurationExtension.ConfigureProblemDetails)
            .AddProblemDetailsConventions();

            builder.OpenTelemetryRegistration(builder.Configuration, "UserManagement");

            builder.Services.AddBoundaryServices(Assembly.GetExecutingAssembly());

            builder.Services.AddInfraestructure(builder.Environment, builder.Configuration);

            builder.Services.AddControllers()
            .AddJsonOptions(o => o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

            builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
            builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

            builder.Services.AddHealthChecks();
            //builder.Services.AddSwaggerExtension(builder.Configuration);



            builder.Services.AddHealthChecks();

            var appConfigConnectionString = Environment.GetEnvironmentVariable("AzureAppConfiguration__ConnectionString") ?? builder.Configuration["AzureAppConfiguration:ConnectionString"];
            var appConfigEndpoint = Environment.GetEnvironmentVariable("AzureAppConfiguration__Endpoint") ?? builder.Configuration["AzureAppConfiguration:Endpoint"];

            if (!string.IsNullOrEmpty(appConfigConnectionString) || !string.IsNullOrEmpty(appConfigEndpoint))
            {
                try
                {
                    builder.Configuration.AddAzureAppConfiguration(options =>
                    {
                        // Opción 1: Usar Connection String
                        if (!string.IsNullOrEmpty(appConfigConnectionString))
                        {
                            options.Connect(appConfigConnectionString);
                            Console.WriteLine("App Configuration conectado via Connection String");
                        }
                        // Opción 2: Usar Managed Identity
                        else if (!string.IsNullOrEmpty(appConfigEndpoint))
                        {
                            options.Connect(new Uri(appConfigEndpoint), new DefaultAzureCredential());
                            Console.WriteLine($"App Configuration conectado via Managed Identity: {appConfigEndpoint}");
                        }

                        // Configurar qué keys cargar
                        options.Select(KeyFilter.Any, LabelFilter.Null)
                               .Select(KeyFilter.Any, Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production");

                        // Configurar feature flags
                        options.UseFeatureFlags(featureFlagOptions =>
                        {
                            featureFlagOptions.SetRefreshInterval(TimeSpan.FromMinutes(5));
                            featureFlagOptions.Label = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
                        });

                        // Configurar refresh automático
                        options.ConfigureRefresh(refresh =>
                        {
                            refresh.Register("UserManagement:RefreshSentinel", refreshAll: true)
                                   .SetRefreshInterval(TimeSpan.FromMinutes(1));
                        });

                        // Configurar Key Vault integration
                        options.ConfigureKeyVault(kv =>
                        {
                            kv.SetCredential(new DefaultAzureCredential());
                        });
                    });

                    // Agregar Azure App Configuration middleware
                    builder.Services.AddAzureAppConfiguration();

                    Console.WriteLine("Azure App Configuration configurado exitosamente");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: No se pudo conectar a Azure App Configuration: {ex.Message}");
                    Console.WriteLine("Continuando con configuración local...");
                }
            }

            var keyVaultUri = Environment.GetEnvironmentVariable("AzureKeyVault__VaultUri") ?? builder.Configuration["AzureKeyVault:VaultUri"];
            if (!string.IsNullOrEmpty(keyVaultUri))
            {
                var credential = new DefaultAzureCredential();

                builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), credential,
                    new AzureKeyVaultConfigurationOptions
                    {
                        ReloadInterval = TimeSpan.FromMinutes(30),
                    });
            }

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ClockSkew = TimeSpan.FromMinutes(5)
                    };

                    // Configurar la validación asíncrona usando nuestro servicio
                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var configService = context.HttpContext.RequestServices.GetRequiredService<IConfigurationService>();

                            try
                            {
                                var issuer = await configService.GetJwtIssuerAsync();
                                var audience = await configService.GetJwtAudienceAsync();
                                var secretKey = await configService.GetJwtSecretKeyAsync();

                                // Validar manualmente el token con los valores obtenidos
                                var tokenHandler = new JwtSecurityTokenHandler();
                                var validationParameters = new TokenValidationParameters
                                {
                                    ValidateIssuer = true,
                                    ValidateAudience = true,
                                    ValidateLifetime = true,
                                    ValidateIssuerSigningKey = true,
                                    ValidIssuer = issuer,
                                    ValidAudience = audience,
                                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                                    ClockSkew = TimeSpan.FromMinutes(5)
                                };

                                // El token ya fue validado por el middleware, solo loggeamos
                                Log.Debug("Token JWT validado exitosamente para usuario: {UserId}",
                                    context.Principal?.Identity?.Name);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Error durante validación adicional del token");
                                context.Fail("Token validation failed");
                            }
                        }
                    };
                });

            builder.Services.AddAuthorization();
        }


        private static void AddBoundaryServices(this IServiceCollection services, params Assembly[] assemblies)
        {
            if (!assemblies.Any())
                throw new ArgumentException("No assemblies found to scan. Supply at least one assembly to scan for handlers.");

            foreach (var assembly in assemblies)
            {
                var rules = assembly.GetTypes()
                    .Where(x => !x.IsAbstract && x.IsClass && x.GetInterface(nameof(IUserManagementScopedService)) == typeof(IUserManagementScopedService));

                foreach (var rule in rules)
                {
                    foreach (var @interface in rule.GetInterfaces())
                    {
                        services.Add(new ServiceDescriptor(@interface, rule, ServiceLifetime.Scoped));
                    }
                }
            }
        }
    }
}

