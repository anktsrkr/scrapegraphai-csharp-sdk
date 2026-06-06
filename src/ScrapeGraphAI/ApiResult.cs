using System.Net;
using System.Text.Json.Serialization;

namespace ScrapeGraphAI;

/// <summary>
/// Represents the status of an SDK call.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ApiResultStatus>))]
public enum ApiResultStatus
{
    /// <summary>The request completed successfully.</summary>
    Success,

    /// <summary>The request failed or could not be completed.</summary>
    Error
}

/// <summary>
/// Non-throwing wrapper returned by ScrapeGraphAI SDK methods.
/// </summary>
/// <typeparam name="T">The successful response payload type.</typeparam>
public sealed record ApiResult<T>(
    ApiResultStatus Status,
    T? Data,
    ScrapeGraphError? Error,
    long ElapsedMs,
    HttpStatusCode? StatusCode)
{
    /// <summary>Gets whether the request succeeded.</summary>
    public bool IsSuccess => this.Status == ApiResultStatus.Success;

    /// <summary>Creates a success result.</summary>
    public static ApiResult<T> Success(T data, long elapsedMs, HttpStatusCode? statusCode)
        => new(ApiResultStatus.Success, data, null, elapsedMs, statusCode);

    /// <summary>Creates an error result.</summary>
    public static ApiResult<T> Failure(ScrapeGraphError error, long elapsedMs, HttpStatusCode? statusCode = null)
        => new(ApiResultStatus.Error, default, error, elapsedMs, statusCode);
}
