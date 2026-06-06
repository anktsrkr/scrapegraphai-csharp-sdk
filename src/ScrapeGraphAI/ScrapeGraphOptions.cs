namespace ScrapeGraphAI;

/// <summary>
/// Configures ScrapeGraphAI client behavior.
/// </summary>
public sealed class ScrapeGraphOptions
{
    /// <summary>Default ScrapeGraphAI v2 API base URL.</summary>
    public static readonly Uri DefaultBaseUrl = new("https://v2-api.scrapegraphai.com/api/");

    /// <summary>Gets or sets the API base URL.</summary>
    public Uri BaseUrl { get; set; } = DefaultBaseUrl;

    /// <summary>Gets or sets the API key.</summary>
    public string? ApiKey { get; set; }

    internal static Uri EnsureTrailingSlash(Uri uri)
    {
        var value = uri.AbsoluteUri;
        return value.EndsWith('/') ? uri : new Uri(value + "/");
    }
}

/// <summary>
/// Configures the optional ScrapeGraphAI standard resilience pipeline.
/// </summary>
public sealed class ScrapeGraphResilienceOptions
{
    /// <summary>Gets or sets the total request timeout used by the resilience pipeline.</summary>
    public TimeSpan TotalRequestTimeout { get; set; } = TimeSpan.FromSeconds(120);

    /// <summary>Gets or sets the timeout for each request attempt. When null, the SDK chooses a value from the total timeout.</summary>
    public TimeSpan? AttemptTimeout { get; set; }

    /// <summary>Gets or sets the maximum retry attempts for resilient clients.</summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>Gets or sets the retry backoff base delay for resilient clients.</summary>
    public TimeSpan RetryBackoff { get; set; } = TimeSpan.FromSeconds(2);
}
