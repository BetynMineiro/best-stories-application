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
        options.Filters.Add(new ProducesAttribute("application/json"));
    }

    public static void ConfigureAppSettingsApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(_ => configuration.GetAppSettingsApiConfig());
    }

    public static void ConfigureApiServicesLayer(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.ConfigureAppSettingsApi(configuration);
        services.ConfigureLogging(environment);
        services.ConfigureMetrics();
        services.AddAdapters(configuration);
        services.AddApplication();
    }
}