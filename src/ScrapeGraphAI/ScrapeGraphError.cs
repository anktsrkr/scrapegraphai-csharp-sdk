using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScrapeGraphAI;

/// <summary>
/// Error payload returned by the ScrapeGraphAI API or produced by the SDK.
/// </summary>
public sealed record ScrapeGraphError(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("details")] IReadOnlyList<ScrapeGraphErrorDetail>? Details = null);

/// <summary>
/// Field-level validation detail.
/// </summary>
public sealed record ScrapeGraphErrorDetail(
    [property: JsonPropertyName("code")] string? Code,
    [property: JsonPropertyName("format")] string? Format,
    [property: JsonPropertyName("path")] IReadOnlyList<string>? Path,
    [property: JsonPropertyName("message")] string? Message);

internal sealed class ScrapeGraphErrorEnvelope
{
    [JsonPropertyName("error")]
    public ScrapeGraphError? Error { get; init; }

    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}
