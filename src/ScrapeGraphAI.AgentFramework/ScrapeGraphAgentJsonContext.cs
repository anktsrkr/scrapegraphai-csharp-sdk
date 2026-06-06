using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScrapeGraphAI.AgentFramework;

[JsonSourceGenerationOptions(
    JsonSerializerDefaults.Web,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    PropertyNameCaseInsensitive = true,
    WriteIndented = false)]
[JsonSerializable(typeof(ApiResult<ScrapeResponse>))]
[JsonSerializable(typeof(ApiResult<ExtractResponse>))]
[JsonSerializable(typeof(ApiResult<SearchResponse>))]
[JsonSerializable(typeof(ApiResult<CrawlResponse>))]
[JsonSerializable(typeof(ApiResult<CrawlPagesResponse>))]
[JsonSerializable(typeof(ApiResult<MonitorResponse>))]
[JsonSerializable(typeof(ApiResult<IReadOnlyList<MonitorResponse>>))]
[JsonSerializable(typeof(ApiResult<MonitorActivityResponse>))]
[JsonSerializable(typeof(ApiResult<HistoryPage>))]
[JsonSerializable(typeof(ApiResult<HistoryEntry>))]
[JsonSerializable(typeof(ApiResult<CreditsResponse>))]
[JsonSerializable(typeof(ApiResult<HealthResponse>))]
[JsonSerializable(typeof(ApiResult<object>))]
[JsonSerializable(typeof(ScrapeFormatType))]
[JsonSerializable(typeof(ScrapeFormatType?))]
[JsonSerializable(typeof(ScrapeContentMode))]
[JsonSerializable(typeof(ScrapeContentMode?))]
[JsonSerializable(typeof(SearchResultFormat))]
[JsonSerializable(typeof(SearchResultFormat?))]
[JsonSerializable(typeof(SearchTimeRange))]
[JsonSerializable(typeof(SearchTimeRange?))]
[JsonSerializable(typeof(SearchLocationGeoCode))]
[JsonSerializable(typeof(SearchLocationGeoCode?))]
[JsonSerializable(typeof(HistoryService))]
[JsonSerializable(typeof(HistoryService?))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int?))]
internal sealed partial class ScrapeGraphAgentJsonContext : JsonSerializerContext;
