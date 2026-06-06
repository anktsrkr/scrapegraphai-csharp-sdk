using Microsoft.Extensions.Logging;

namespace ScrapeGraphAI;

internal static partial class ScrapeGraphClientLog
{
    [LoggerMessage(EventId = 1, EventName = "RequestStarted", Level = LogLevel.Debug, Message = "ScrapeGraphAI request started {Method} {Endpoint}")]
    internal static partial void RequestStarted(this ILogger logger, string method, string endpoint);

    [LoggerMessage(EventId = 2, EventName = "RequestCompleted", Level = LogLevel.Debug, Message = "ScrapeGraphAI request completed {Method} {Endpoint} with status {StatusCode} in {ElapsedMs}ms")]
    internal static partial void RequestCompleted(this ILogger logger, string method, string endpoint, int statusCode, long elapsedMs);

    [LoggerMessage(EventId = 3, EventName = "RequestFailed", Level = LogLevel.Warning, Message = "ScrapeGraphAI request failed {Method} {Endpoint} with status {StatusCode} in {ElapsedMs}ms")]
    internal static partial void RequestFailed(this ILogger logger, string method, string endpoint, int statusCode, long elapsedMs);

    [LoggerMessage(EventId = 4, EventName = "RequestTimedOut", Level = LogLevel.Warning, Message = "ScrapeGraphAI request timed out {Method} {Endpoint} in {ElapsedMs}ms")]
    internal static partial void RequestTimedOut(this ILogger logger, string method, string endpoint, long elapsedMs);

    [LoggerMessage(EventId = 5, EventName = "RequestCanceled", Level = LogLevel.Warning, Message = "ScrapeGraphAI request canceled {Method} {Endpoint} in {ElapsedMs}ms")]
    internal static partial void RequestCanceled(this ILogger logger, string method, string endpoint, long elapsedMs);

    [LoggerMessage(EventId = 6, EventName = "HttpRequestFailed", Level = LogLevel.Warning, Message = "ScrapeGraphAI HTTP request error {Method} {Endpoint} in {ElapsedMs}ms")]
    internal static partial void HttpRequestFailed(this ILogger logger, string method, string endpoint, long elapsedMs, Exception exception);

    [LoggerMessage(EventId = 7, EventName = "SdkError", Level = LogLevel.Error, Message = "ScrapeGraphAI SDK error {Method} {Endpoint} in {ElapsedMs}ms")]
    internal static partial void SdkError(this ILogger logger, string method, string endpoint, long elapsedMs, Exception exception);

    [LoggerMessage(EventId = 8, EventName = "EmptyResponse", Level = LogLevel.Warning, Message = "ScrapeGraphAI empty response {Method} {Endpoint} in {ElapsedMs}ms")]
    internal static partial void EmptyResponse(this ILogger logger, string method, string endpoint, long elapsedMs);
}
