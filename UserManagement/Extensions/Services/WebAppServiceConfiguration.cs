using Hellang.Middleware.ProblemDetails;
using Hellang.Middleware.ProblemDetails.Mvc;
using Serilog;
using UserManagement.Middleware;
using UserManagement.Services;
using System.Reflection;
using System.Text.Json.Serialization;

namespace UserManagement.Extensions.Services
{
    public static class WebAppServiceConfiguration
    {
        public static void ConfigureServices(this WebApplicationBuilder builder)
        {
            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddProblemDetails(ProblemDetailsConfigurationExtension.ConfigureProblemDetails)
            .AddProblemDetailsConventions();

            builder.OpenTelemetryRegistration(builder.Configuration, "UserManagement");
            builder.Services.AddInfraestructure(builder.Environment, builder.Configuration);

            builder.Services.AddControllers()
            .AddJsonOptions(o => o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

            builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
            builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

            builder.Services.AddHealthChecks();
            //builder.Services.AddSwaggerExtension(builder.Configuration);

            builder.Services.AddBoundaryServices(Assembly.GetExecutingAssembly());

            builder.Services.AddHealthChecks();
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

