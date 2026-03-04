using Microsoft.AspNetCore.Mvc;
using App.Api.Middleware;
using App.Adapters;
using App.Application;
using App.CrossCutting;

namespace App.Api;

public static class ApiModule
{
    public static void ConfigureFilters(this MvcOptions options)
    {
        options.Filters.Add<AcceptHeaderMiddleware>();
        options.Filters.Add<NotificationServiceMiddleware>();
        options.Filters.Add(new ProducesAttribute("application/json"));
    }

    extension(IServiceCollection services)
    {
        private void ConfigureAppSettingsApi(IConfiguration configuration)
        {
            services.AddSingleton(configuration.GetAppSettingsApiConfig());
        }

        public void ConfigureApiServicesLayer(IConfiguration configuration, IHostEnvironment environment)
        {
            services.ConfigureAppSettingsApi(configuration);
            services.ConfigureLogging(environment);
            services.ConfigureMetrics();
            services.AddAdapters(configuration);
            services.AddApplication();
        }
    }
}