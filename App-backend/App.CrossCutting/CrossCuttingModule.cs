using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using App.CrossCutting.Configurations;

namespace App.CrossCutting;

public static class CrossCuttingModule
{
    public static AppSettingsConfig GetAppSettingsApiConfig(this IConfiguration configuration)
    {
        var settingsSection = configuration.GetSection("App");
        var settingsApi = settingsSection.Get<AppSettingsConfig>();
        return settingsApi!;
    }

    extension(IServiceCollection services)
    {
        /// <summary>
        /// Configures Serilog with OpenTelemetry sink (OTLP gRPC) for logs in tracing context.
        /// </summary>
        public void ConfigureLogging(IHostEnvironment environment)
        {
            var applicationName = Assembly.GetEntryAssembly()?.GetName().Name ?? "UnknownApp";
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId()
                .Enrich.WithProperty("Application", applicationName)
                .Enrich.WithExceptionDetails()
                .WriteTo.Console()
                .MinimumLevel.Is(LogEventLevel.Information)
                .WriteTo.OpenTelemetry(options =>
                {
                    options.Protocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc;
                    options.ResourceAttributes["service.name"] = applicationName;
                    options.ResourceAttributes["deployment.environment"] = environment.EnvironmentName;
                })
                .CreateLogger();

            services.AddSingleton(Log.Logger);
        }

        /// <summary>
        /// Configures OpenTelemetry: distributed tracing (ASP.NET Core + HttpClient) and metrics (AspNetCore, Runtime, HttpClient).
        /// Exports via OTLP gRPC to a collector (Jaeger, Tempo, etc.).
        /// </summary>
        public void ConfigureMetrics()
        {
            var applicationName = Assembly.GetEntryAssembly()?.GetName().Name ?? "UnknownApp";

            services.AddOpenTelemetry()
                .WithTracing(tracer =>
                {
                    tracer
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .SetResourceBuilder(
                            ResourceBuilder.CreateDefault()
                                .AddService(serviceName: applicationName))
                        .AddOtlpExporter(otlp => { otlp.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc; });
                })
                .WithMetrics(metrics =>
                {
                    metrics
                        .AddAspNetCoreInstrumentation()
                        .AddRuntimeInstrumentation()
                        .AddHttpClientInstrumentation()
                        .SetResourceBuilder(
                            ResourceBuilder.CreateDefault()
                                .AddService(applicationName))
                        .AddOtlpExporter(otlp => { otlp.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc; });
                });
        }
    }
}