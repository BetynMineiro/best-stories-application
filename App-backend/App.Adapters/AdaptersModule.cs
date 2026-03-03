using System.Net;
using App.Adapters.HackerNews;
using App.Adapters.MemoryCache;
using App.CrossCutting.Configurations;
using App.Domain.Contracts;
using App.Domain.Entities;
using App.Domain.Specifications;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace App.Adapters;

public static class AdaptersModule
{
    private const string UserAgent = "App-BestStories/1.0";

    /// <summary>
    /// HttpClient overall timeout (backstop). Per-attempt timeout is controlled by Polly (HttpClientTimeoutSeconds).
    /// </summary>
    private static readonly TimeSpan HttpClientOverallTimeout = TimeSpan.FromMinutes(2);

    public static void AddAdapters(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<HackerNewsConfig>(configuration.GetSection("App").GetSection("HackerNews"));
        services.Configure<CacheConfig>(configuration.GetSection("App").GetSection("Cache"));

        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheAdapter>();
        services.AddSingleton<ISpecification<BestStory>, DisplayableBestStorySpecification>();

        services.AddHttpClient<IBestStoryService, HackerNewsBestStoryAdapter>((client, sp) =>
        {
            var options = sp.GetRequiredService<IOptions<HackerNewsConfig>>().Value;
            client.BaseAddress = new Uri(options.BaseAddress);
            client.Timeout = HttpClientOverallTimeout;
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", UserAgent);
            return new HackerNewsBestStoryAdapter(
                client,
                sp.GetRequiredService<ILogger<HackerNewsBestStoryAdapter>>(),
                sp.GetRequiredService<ISpecification<BestStory>>());
        })
            .AddPolicyHandler((sp, _) => GetTimeoutPolicy(sp.GetRequiredService<IOptions<HackerNewsConfig>>().Value))
            .AddPolicyHandler((sp, _) => GetRetryPolicy(sp.GetRequiredService<ILogger<HackerNewsBestStoryAdapter>>()));
    }

    /// <summary>
    /// Per-attempt timeout (inner). Should be innermost so each retry has its own limit.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy(HackerNewsConfig config)
    {
        var timeout = TimeSpan.FromSeconds(config.HttpClientTimeoutSeconds);
        return Policy.TimeoutAsync<HttpResponseMessage>(timeout);
    }

    /// <summary>
    /// Retry with exponential backoff (outer). Handles transient errors and network failures.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger<HackerNewsBestStoryAdapter> logger)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, span, attempt, _) =>
                {
                    var reason = outcome.Exception != null
                        ? outcome.Exception.Message
                        : outcome.Result?.StatusCode.ToString() ?? "Unknown";
                    logger.LogWarning(
                        "Retry {Attempt}/3 after {Delay:F1}s. Reason: {Reason}",
                        attempt, span.TotalSeconds, reason);
                });
    }
}
