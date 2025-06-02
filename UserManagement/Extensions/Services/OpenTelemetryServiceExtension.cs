namespace UserManagement.Extensions.Services
{

    using OpenTelemetry;
    using OpenTelemetry.Metrics;
    using OpenTelemetry.Resources;
    using OpenTelemetry.Trace;
    public static class OpenTelemetryServiceExtension
    {
        public static void OpenTelemetryRegistration(this WebApplicationBuilder builder, IConfiguration configuration, string serviceName)
        {
            var resourceBuilder = ResourceBuilder.CreateDefault().AddService(serviceName)
                .AddTelemetrySdk()
                .AddEnvironmentVariableDetector();

            builder.Logging.AddOpenTelemetry(o =>
            {
                // TODO: Setup an exporter here
                o.SetResourceBuilder(resourceBuilder);
            });

            builder.Services.AddOpenTelemetry()
                .WithMetrics(metricsBuilder =>
                    metricsBuilder.SetResourceBuilder(resourceBuilder)
                        .AddAspNetCoreInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddHttpClientInstrumentation())
                .WithTracing(tracerBuilder =>
                    tracerBuilder.SetResourceBuilder(resourceBuilder)
                        .AddSource("MassTransit")
                        .AddSource("Microsoft.EntityFrameworkCore.SqlServer")
                        //.AddSqlClientInstrumentation(opt => opt.SetDbStatementForText = true)
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        //.AddEntityFrameworkCoreInstrumentation()
                        );
        }
    }
}
