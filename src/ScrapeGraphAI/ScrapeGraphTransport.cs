using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ScrapeGraphAI;

internal interface IScrapeGraphTransport
{
    Task<ApiResult<TResponse>> GetAsync<TResponse>(string path, CancellationToken cancellationToken);

    Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(string path, TRequest request, CancellationToken cancellationToken);

    Task<ApiResult<TResponse>> PatchAsync<TRequest, TResponse>(string path, TRequest request, CancellationToken cancellationToken);

    Task<ApiResult<TResponse>> PostEmptyAsync<TResponse>(string path, CancellationToken cancellationToken);

    Task<ApiResult<TResponse>> DeleteAsync<TResponse>(string path, CancellationToken cancellationToken);
}

internal sealed class ScrapeGraphTransport(HttpClient httpClient, ILogger<ScrapeGraphTransport> logger) : IScrapeGraphTransport
{
    internal static readonly ActivitySource ActivitySource = new("ScrapeGraphAI", typeof(ScrapeGraphTransport).Assembly.GetName().Version?.ToString());
    internal static readonly JsonSerializerOptions JsonOptions = ScrapeGraphJsonContext.Default.Options;
    private static readonly object EmptyResponse = new();

    public Task<ApiResult<TResponse>> GetAsync<TResponse>(string path, CancellationToken cancellationToken)
        => this.SendAsync<TResponse>(HttpMethod.Get, path, null, cancellationToken);

    public Task<ApiResult<TResponse>> PostAsync<TRequest, TResponse>(string path, TRequest request, CancellationToken cancellationToken)
        => this.SendAsync<TResponse>(HttpMethod.Post, path, JsonContent.Create(request, options: JsonOptions), cancellationToken);

    public Task<ApiResult<TResponse>> PatchAsync<TRequest, TResponse>(string path, TRequest request, CancellationToken cancellationToken)
        => this.SendAsync<TResponse>(HttpMethod.Patch, path, JsonContent.Create(request, options: JsonOptions), cancellationToken);

    public Task<ApiResult<TResponse>> PostEmptyAsync<TResponse>(string path, CancellationToken cancellationToken)
        => this.SendAsync<TResponse>(HttpMethod.Post, path, null, cancellationToken);

    public Task<ApiResult<TResponse>> DeleteAsync<TResponse>(string path, CancellationToken cancellationToken)
        => this.SendAsync<TResponse>(HttpMethod.Delete, path, null, cancellationToken);

    internal static string BuildQueryString(params (string Name, object? Value)[] values)
    {
        var builder = new StringBuilder();
        foreach (var (name, value) in values)
        {
            if (value is null)
            {
                continue;
            }

            builder.Append(builder.Length == 0 ? '?' : '&');
            builder.Append(Uri.EscapeDataString(name));
            builder.Append('=');
            builder.Append(Uri.EscapeDataString(GetQueryValue(value)));
        }

        return builder.ToString();
    }

    private static string GetQueryValue(object value)
    {
        if (value is Enum enumValue)
        {
            return enumValue switch
            {
                HistoryService historyService => ScrapeGraphEnumWireValues.ToWireValue(historyService),
                _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty
            };
        }

        return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private async Task<ApiResult<TResponse>> SendAsync<TResponse>(HttpMethod method, string path, HttpContent? content, CancellationToken cancellationToken)
    {
        var startTimestamp = Stopwatch.GetTimestamp();
        var endpoint = GetEndpointName(method, path);
        var methodName = method.Method;
        using var activity = StartActivity(methodName, endpoint);

        try
        {
            using var request = new HttpRequestMessage(method, path) { Content = content };
            logger.RequestStarted(methodName, endpoint);

            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            var elapsedMs = GetElapsedMilliseconds(startTimestamp);

            if (!response.IsSuccessStatusCode)
            {
                CompleteActivity(activity, methodName, endpoint, response.StatusCode, elapsedMs, success: false);
                logger.RequestFailed(methodName, endpoint, (int)response.StatusCode, elapsedMs);
                return ApiResult<TResponse>.Failure(await ReadErrorAsync(response, cancellationToken).ConfigureAwait(false), elapsedMs, response.StatusCode);
            }

            if (typeof(TResponse) == typeof(object))
            {
                CompleteActivity(activity, methodName, endpoint, response.StatusCode, elapsedMs, success: true);
                logger.RequestCompleted(methodName, endpoint, (int)response.StatusCode, elapsedMs);
                return ApiResult<TResponse>.Success((TResponse)EmptyResponse, elapsedMs, response.StatusCode);
            }

            TResponse? payload;
            try
            {
                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                payload = await JsonSerializer.DeserializeAsync<TResponse>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
            }
            catch (JsonException ex)
            {
                CompleteActivity(activity, methodName, endpoint, response.StatusCode, elapsedMs, success: false);
                logger.SdkError(methodName, endpoint, elapsedMs, ex);
                return ApiResult<TResponse>.Failure(new ScrapeGraphError("invalid_response", "The API returned malformed JSON."), elapsedMs, response.StatusCode);
            }

            if (payload is null)
            {
                CompleteActivity(activity, methodName, endpoint, response.StatusCode, elapsedMs, success: false);
                logger.EmptyResponse(methodName, endpoint, elapsedMs);
                return ApiResult<TResponse>.Failure(new ScrapeGraphError("empty_response", "The API returned an empty response."), elapsedMs, response.StatusCode);
            }

            CompleteActivity(activity, methodName, endpoint, response.StatusCode, elapsedMs, success: true);
            logger.RequestCompleted(methodName, endpoint, (int)response.StatusCode, elapsedMs);
            return ApiResult<TResponse>.Success(payload, elapsedMs, response.StatusCode);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            var elapsedMs = GetElapsedMilliseconds(startTimestamp);
            CompleteActivity(activity, methodName, endpoint, null, elapsedMs, success: false);
            logger.RequestTimedOut(methodName, endpoint, elapsedMs);
            return ApiResult<TResponse>.Failure(new ScrapeGraphError("timeout", "The request timed out."), elapsedMs);
        }
        catch (OperationCanceledException)
        {
            var elapsedMs = GetElapsedMilliseconds(startTimestamp);
            CompleteActivity(activity, methodName, endpoint, null, elapsedMs, success: false);
            logger.RequestCanceled(methodName, endpoint, elapsedMs);
            return ApiResult<TResponse>.Failure(new ScrapeGraphError("canceled", "The request was canceled."), elapsedMs);
        }
        catch (HttpRequestException ex)
        {
            var elapsedMs = GetElapsedMilliseconds(startTimestamp);
            CompleteActivity(activity, methodName, endpoint, ex.StatusCode, elapsedMs, success: false);
            logger.HttpRequestFailed(methodName, endpoint, elapsedMs, ex);
            return ApiResult<TResponse>.Failure(new ScrapeGraphError("http_request_error", ex.Message), elapsedMs, ex.StatusCode);
        }
        catch (Exception ex)
        {
            var elapsedMs = GetElapsedMilliseconds(startTimestamp);
            CompleteActivity(activity, methodName, endpoint, null, elapsedMs, success: false);
            logger.SdkError(methodName, endpoint, elapsedMs, ex);
            return ApiResult<TResponse>.Failure(new ScrapeGraphError("sdk_error", ex.Message), elapsedMs);
        }
    }

    private static async Task<ScrapeGraphError> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var envelope = await JsonSerializer.DeserializeAsync<ScrapeGraphErrorEnvelope>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
            if (envelope?.Error is { } error && (!string.IsNullOrWhiteSpace(error.Type) || !string.IsNullOrWhiteSpace(error.Message)))
            {
                return new ScrapeGraphError(
                    string.IsNullOrWhiteSpace(error.Type) ? "api_error" : error.Type,
                    string.IsNullOrWhiteSpace(error.Message) ? "Request failed." : error.Message,
                    error.Details);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
        }

        var fallback = response.StatusCode switch
        {
            HttpStatusCode.BadRequest => "Validation error.",
            HttpStatusCode.Unauthorized => "Missing API key.",
            HttpStatusCode.PaymentRequired => "Insufficient credits.",
            HttpStatusCode.Forbidden => "Invalid or deprecated API key.",
            HttpStatusCode.NotFound => "Resource not found.",
            (HttpStatusCode)429 => "Rate limit exceeded.",
            _ => response.ReasonPhrase ?? "Request failed."
        };

        return new ScrapeGraphError(((int)response.StatusCode).ToString(), fallback);
    }

    private static Activity? StartActivity(string method, string endpoint)
    {
        var activity = ActivitySource.StartActivity("ScrapeGraphAI request", ActivityKind.Client);
        activity?.SetTag("scrapegraphai.endpoint", endpoint);
        activity?.SetTag("http.request.method", method);
        return activity;
    }

    private static void CompleteActivity(Activity? activity, string method, string endpoint, HttpStatusCode? statusCode, long elapsedMs, bool success)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag("scrapegraphai.endpoint", endpoint);
        activity.SetTag("http.request.method", method);
        activity.SetTag("scrapegraphai.elapsed_ms", elapsedMs);
        activity.SetTag("scrapegraphai.success", success);
        if (statusCode is not null)
        {
            activity.SetTag("http.response.status_code", (int)statusCode.Value);
        }

        if (!success)
        {
            activity.SetStatus(ActivityStatusCode.Error);
        }
    }

    private static long GetElapsedMilliseconds(long startTimestamp)
        => (long)Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;

    private static string GetEndpointName(HttpMethod method, string path)
    {
        var queryIndex = path.IndexOf('?', StringComparison.Ordinal);
        var pathOnly = queryIndex < 0 ? path : path[..queryIndex];
        var segments = pathOnly.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return "unknown";
        }

        return segments[0] switch
        {
            "scrape" or "extract" or "search" or "credits" or "health" => segments[0],
            "crawl" => GetCrawlEndpoint(method, segments),
            "monitor" => GetMonitorEndpoint(method, segments),
            "history" => segments.Length == 1 ? "history.list" : "history.get",
            _ => segments[0]
        };
    }

    private static string GetCrawlEndpoint(HttpMethod method, string[] segments)
        => segments.Length switch
        {
            1 => method == HttpMethod.Post ? "crawl.start" : "crawl",
            2 => method == HttpMethod.Delete ? "crawl.delete" : "crawl.get",
            _ when string.Equals(segments[2], "pages", StringComparison.Ordinal) => "crawl.pages",
            _ when string.Equals(segments[2], "stop", StringComparison.Ordinal) => "crawl.stop",
            _ when string.Equals(segments[2], "resume", StringComparison.Ordinal) => "crawl.resume",
            _ => "crawl"
        };

    private static string GetMonitorEndpoint(HttpMethod method, string[] segments)
        => segments.Length switch
        {
            1 => method == HttpMethod.Post ? "monitor.create" : "monitor.list",
            2 => method == HttpMethod.Delete ? "monitor.delete" : method == HttpMethod.Patch ? "monitor.update" : "monitor.get",
            _ when string.Equals(segments[2], "activity", StringComparison.Ordinal) => "monitor.activity",
            _ when string.Equals(segments[2], "pause", StringComparison.Ordinal) => "monitor.pause",
            _ when string.Equals(segments[2], "resume", StringComparison.Ordinal) => "monitor.resume",
            _ => "monitor"
        };
}
