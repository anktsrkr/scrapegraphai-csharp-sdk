using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;

namespace ScrapeGraphAI;

/// <summary>
/// Dependency injection helpers for the ScrapeGraphAI typed client.
/// </summary>
public static class ScrapeGraphServiceCollectionExtensions
{
    /// <summary>
    /// Registers the ScrapeGraphAI typed HttpClient.
    /// </summary>
    /// <param name="services">The service collection to register with.</param>
    /// <param name="configure">An optional callback for configuring ScrapeGraphAI client options.</param>
    /// <returns>The typed client builder for further HttpClient configuration.</returns>
    public static IHttpClientBuilder AddScrapeGraphAI(this IServiceCollection services, Action<ScrapeGraphOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<ScrapeGraphOptions>()
            .Validate(options => !string.IsNullOrWhiteSpace(options.ApiKey), "ScrapeGraphOptions.ApiKey must be configured.")
            .Validate(options => options.BaseUrl is { IsAbsoluteUri: true }
                && (string.Equals(options.BaseUrl.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(options.BaseUrl.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)),
                "ScrapeGraphOptions.BaseUrl must be an absolute HTTP or HTTPS URI.");

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.PostConfigure<ScrapeGraphOptions>(configureOptions =>
        {
            if (configureOptions.BaseUrl is { IsAbsoluteUri: true })
            {
                configureOptions.BaseUrl = ScrapeGraphOptions.EnsureTrailingSlash(configureOptions.BaseUrl);
            }
        });

        var builder = services.AddHttpClient<ScrapeGraphTransport>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<ScrapeGraphOptions>>().Value;
            client.BaseAddress = options.BaseUrl;
            ScrapeGraphHttpClientDefaults.Apply(client, options.ApiKey);
        });

        services.AddTransient<IScrapeGraphClient>(provider =>
        {
            var transport = provider.GetRequiredService<ScrapeGraphTransport>();
            return new ScrapeGraphClient(
                transport,
                new CrawlResource(transport),
                new MonitorResource(transport),
                new HistoryResource(transport));
        });

        return builder;
    }

    /// <summary>
    /// Adds the ScrapeGraphAI standard resilience pipeline to the typed HttpClient.
    /// </summary>
    /// <param name="builder">The ScrapeGraphAI typed client builder.</param>
    /// <param name="configure">An optional callback for configuring resilience options.</param>
    /// <returns>The typed client builder for further HttpClient configuration.</returns>
    public static IHttpClientBuilder AddScrapeGraphAIStandardResilience(
        this IHttpClientBuilder builder,
        Action<ScrapeGraphResilienceOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddOptions<ScrapeGraphResilienceOptions>()
            .Validate(options => options.TotalRequestTimeout > TimeSpan.Zero, "ScrapeGraphResilienceOptions.TotalRequestTimeout must be positive.")
            .Validate(options => options.AttemptTimeout is null || options.AttemptTimeout > TimeSpan.Zero, "ScrapeGraphResilienceOptions.AttemptTimeout must be positive when configured.")
            .Validate(options => options.AttemptTimeout is null || options.AttemptTimeout <= options.TotalRequestTimeout, "ScrapeGraphResilienceOptions.AttemptTimeout must not exceed TotalRequestTimeout.")
            .Validate(options => options.MaxRetryAttempts > 0, "ScrapeGraphResilienceOptions.MaxRetryAttempts must be greater than zero.")
            .Validate(options => options.RetryBackoff > TimeSpan.Zero, "ScrapeGraphResilienceOptions.RetryBackoff must be positive.");

        if (configure is not null)
        {
            builder.Services.Configure(configure);
        }

        builder.ConfigureHttpClient((_, client) =>
        {
            client.Timeout = Timeout.InfiniteTimeSpan;
        });

        builder.AddStandardResilienceHandler()
            .Configure((resilience, provider) =>
            {
                var options = provider.GetRequiredService<IOptions<ScrapeGraphResilienceOptions>>().Value;
                var attemptTimeout = options.AttemptTimeout ?? (options.TotalRequestTimeout <= TimeSpan.FromSeconds(15)
                    ? options.TotalRequestTimeout
                    : TimeSpan.FromSeconds(10));

                resilience.TotalRequestTimeout.Timeout = options.TotalRequestTimeout;
                resilience.AttemptTimeout.Timeout = attemptTimeout;
                if (resilience.CircuitBreaker.SamplingDuration < attemptTimeout * 2)
                {
                    resilience.CircuitBreaker.SamplingDuration = attemptTimeout * 2;
                }

                resilience.Retry.MaxRetryAttempts = options.MaxRetryAttempts;
                resilience.Retry.Delay = options.RetryBackoff;
                resilience.Retry.BackoffType = DelayBackoffType.Exponential;
                resilience.Retry.UseJitter = true;
                resilience.Retry.ShouldHandle = static args =>
                {
                    if (args.Outcome.Exception is HttpRequestException or TimeoutException)
                    {
                        return PredicateResult.True();
                    }

                    var response = args.Outcome.Result;
                    if (response is null)
                    {
                        return PredicateResult.False();
                    }

                    var statusCode = (int)response.StatusCode;
                    var shouldRetry = response.StatusCode == System.Net.HttpStatusCode.RequestTimeout
                        || statusCode >= 500;
                    return shouldRetry ? PredicateResult.True() : PredicateResult.False();
                };
            });

        return builder;
    }
}
