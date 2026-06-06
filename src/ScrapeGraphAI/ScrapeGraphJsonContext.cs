using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScrapeGraphAI;

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNameCaseInsensitive = true,
    WriteIndented = false)]
[JsonSerializable(typeof(ScrapeRequest))]
[JsonSerializable(typeof(ExtractRequest))]
[JsonSerializable(typeof(SearchRequest))]
[JsonSerializable(typeof(CrawlRequest))]
[JsonSerializable(typeof(MonitorCreateRequest))]
[JsonSerializable(typeof(MonitorUpdateRequest))]
[JsonSerializable(typeof(ScrapeFormatType))]
[JsonSerializable(typeof(ScrapeContentMode))]
[JsonSerializable(typeof(FetchMode))]
[JsonSerializable(typeof(SearchResultFormat))]
[JsonSerializable(typeof(SearchTimeRange))]
[JsonSerializable(typeof(SearchLocationGeoCode))]
[JsonSerializable(typeof(HistoryService))]
[JsonSerializable(typeof(ScrapeResponse))]
[JsonSerializable(typeof(ExtractResponse))]
[JsonSerializable(typeof(SearchResponse))]
[JsonSerializable(typeof(CrawlResponse))]
[JsonSerializable(typeof(CrawlPagesResponse))]
[JsonSerializable(typeof(MonitorResponse))]
[JsonSerializable(typeof(IReadOnlyList<MonitorResponse>))]
[JsonSerializable(typeof(MonitorActivityResponse))]
[JsonSerializable(typeof(HistoryPage))]
[JsonSerializable(typeof(HistoryEntry))]
[JsonSerializable(typeof(CreditsResponse))]
[JsonSerializable(typeof(HealthResponse))]
[JsonSerializable(typeof(ScrapeGraphErrorEnvelope))]
[JsonSerializable(typeof(IReadOnlyList<ScrapeGraphErrorDetail>))]
internal sealed partial class ScrapeGraphJsonContext : JsonSerializerContext;
