﻿using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.EntityFrameworkCore;
using UserManagement.Configurations;
using UserManagement.Databases;
using UserManagement.Resources;
using UserManagement.Resources.HangfireUtilities;

namespace UserManagement.Extensions.Services
{
    public static class InfraestructureServiceExtension
    {
        public static void AddInfraestructure(this IServiceCollection services, IWebHostEnvironment env, IConfiguration configuration)
        {
            var connectionstring = configuration.GetConnectionString(ConnectionStringOptions.UserManagementKey);
            
            services.AddDbContext<UserManagementDbContext>(options => options.UseSqlServer(connectionstring));
            services.SetupHangfire(env);
        }
    }

    public static class HangfireConfig
    {
        public static void SetupHangfire(this IServiceCollection services, IWebHostEnvironment env)
        {
            services.AddScoped<IJobContextAccessor, JobContextAccessor>();
            services.AddScoped<IJobWithUserContext, JobWithUserContext>();
            // if you want tags with sql server
            // var tagOptions = new TagsOptions() { TagsListStyle = TagsListStyle.Dropdown };

            // var hangfireConfig = new MemoryStorageOptions() { };
            services.AddHangfire(config =>
            {
                config
                    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                    .UseMemoryStorage()
                    .UseColouredConsoleLogProvider()
                    .UseSimpleAssemblyNameTypeSerializer()
                    .UseRecommendedSerializerSettings()
                    // if you want tags with sql server
                    // .UseTagsWithSql(tagOptions, hangfireConfig)
                    .UseActivator(new JobWithUserContextActivator(services.BuildServiceProvider()
                        .GetRequiredService<IServiceScopeFactory>()));
            });
            services.AddHangfireServer(options =>
            {
                options.WorkerCount = 10;
                options.ServerName = $"PeakLims-{env.EnvironmentName}";

                if (Consts.HangfireQueues.List().Length > 0)
                {
                    options.Queues = Consts.HangfireQueues.List();
                }
            });

        }
    }
}
