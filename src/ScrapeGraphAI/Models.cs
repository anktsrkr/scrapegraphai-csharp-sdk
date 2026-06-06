using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScrapeGraphAI;

/// <summary>Known content processing modes for markdown and HTML formats.</summary>
[JsonConverter(typeof(ScrapeGraphEnumJsonConverter<ScrapeContentMode>))]
public enum ScrapeContentMode
{
    /// <summary>Uses the default page processing mode.</summary>
    Normal,

    /// <summary>Extracts reader-oriented page content.</summary>
    Reader,

    /// <summary>Prunes low-value page content.</summary>
    Prune
}

/// <summary>Known output format types for scrape-like endpoints.</summary>
[JsonConverter(typeof(ScrapeGraphEnumJsonConverter<ScrapeFormatType>))]
public enum ScrapeFormatType
{
    /// <summary>Clean markdown conversion.</summary>
    Markdown,

    /// <summary>Raw or processed HTML.</summary>
    Html,

    /// <summary>Outgoing links.</summary>
    Links,

    /// <summary>Image URLs.</summary>
    Images,

    /// <summary>AI-generated summary.</summary>
    Summary,

    /// <summary>Structured JSON extraction.</summary>
    Json,

    /// <summary>Brand colors, typography, and logos.</summary>
    Branding,

    /// <summary>Screenshot output.</summary>
    Screenshot
}

/// <summary>Known fetch render modes.</summary>
[JsonConverter(typeof(ScrapeGraphEnumJsonConverter<FetchMode>))]
public enum FetchMode
{
    /// <summary>Lets the API choose the fetch mode.</summary>
    Auto,

    /// <summary>Uses fast non-JavaScript fetching.</summary>
    Fast,

    /// <summary>Uses JavaScript rendering.</summary>
    Js
}

/// <summary>Known inline content formats for search results.</summary>
[JsonConverter(typeof(ScrapeGraphEnumJsonConverter<SearchResultFormat>))]
public enum SearchResultFormat
{
    /// <summary>Markdown content.</summary>
    Markdown,

    /// <summary>HTML content.</summary>
    Html
}

/// <summary>Known search recency filters.</summary>
[JsonConverter(typeof(ScrapeGraphEnumJsonConverter<SearchTimeRange>))]
public enum SearchTimeRange
{
    /// <summary>Results from the past hour.</summary>
    PastHour,

    /// <summary>Results from the past 24 hours.</summary>
    Past24Hours,

    /// <summary>Results from the past week.</summary>
    PastWeek,

    /// <summary>Results from the past month.</summary>
    PastMonth,

    /// <summary>Results from the past year.</summary>
    PastYear
}

/// <summary>Documented localized search country codes.</summary>
[JsonConverter(typeof(ScrapeGraphEnumJsonConverter<SearchLocationGeoCode>))]
#pragma warning disable CS1591
public enum SearchLocationGeoCode
{
    Ae, Ar, At, Au, Be, Br, Ca, Ch, Cl, Cn, Co, Cz, De, Dk, Eg, Es, Fi, Fr, Gb, Gr, Hk, Hu,
    Id, Ie, Il, In, It, Jp, Kr, Mx, My, Ng, Nl, No, Nz, Pe, Ph, Pk, Pl, Pt, Ro, Ru, Sa, Se,
    Sg, Th, Tr, Tw, Ua, Us, Vn, Za
}
#pragma warning restore CS1591

/// <summary>Known history service filters.</summary>
[JsonConverter(typeof(ScrapeGraphEnumJsonConverter<HistoryService>))]
public enum HistoryService
{
    /// <summary>Scrape requests.</summary>
    Scrape,

    /// <summary>Extract requests.</summary>
    Extract,

    /// <summary>Search requests.</summary>
    Search,

    /// <summary>Monitor requests and ticks.</summary>
    Monitor,

    /// <summary>Crawl jobs.</summary>
    Crawl,

    /// <summary>Schema generation requests.</summary>
    Schema
}

/// <summary>Fetch configuration shared by scrape, extract, search, crawl, and monitor calls.</summary>
public sealed record FetchConfig
{
    /// <summary>Gets the fetch mode.</summary>
    public FetchMode? Mode { get; init; }

    /// <summary>Gets whether stealth fetching is requested.</summary>
    public bool? Stealth { get; init; }

    /// <summary>Gets the fetch timeout in milliseconds.</summary>
    public int? Timeout { get; init; }

    /// <summary>Gets the wait time in milliseconds before reading content.</summary>
    public int? Wait { get; init; }

    /// <summary>Gets the number of page scrolls to perform.</summary>
    public int? Scrolls { get; init; }

    /// <summary>Gets the country code used for fetching.</summary>
    public string? Country { get; init; }

    /// <summary>Gets custom request headers.</summary>
    public IDictionary<string, string>? Headers { get; init; }

    /// <summary>Gets custom request cookies.</summary>
    public IDictionary<string, string>? Cookies { get; init; }

    /// <summary>Gets whether mock fetching is requested.</summary>
    public bool? Mock { get; init; }
}

/// <summary>Output format configuration for scrape-like endpoints.</summary>
public sealed record FormatConfig
{
    /// <summary>Gets the output format type.</summary>
    public required ScrapeFormatType Type { get; init; }

    /// <summary>Gets the format-specific content mode.</summary>
    public ScrapeContentMode? Mode { get; init; }

    /// <summary>Gets the extraction prompt for JSON output.</summary>
    public string? Prompt { get; init; }

    /// <summary>Gets the optional JSON schema for structured output.</summary>
    public JsonElement? Schema { get; init; }

    /// <summary>Gets whether screenshots should capture the full page.</summary>
    public bool? FullPage { get; init; }

    /// <summary>Gets the screenshot viewport width.</summary>
    public int? Width { get; init; }

    /// <summary>Gets the screenshot viewport height.</summary>
    public int? Height { get; init; }

    /// <summary>Gets the screenshot quality.</summary>
    public int? Quality { get; init; }

    /// <summary>Creates a markdown output format.</summary>
    /// <param name="mode">The optional markdown processing mode.</param>
    /// <returns>A markdown format configuration.</returns>
    public static FormatConfig Markdown(ScrapeContentMode? mode = null) => new() { Type = ScrapeFormatType.Markdown, Mode = mode };

    /// <summary>Creates an HTML output format.</summary>
    /// <param name="mode">The optional HTML processing mode.</param>
    /// <returns>An HTML format configuration.</returns>
    public static FormatConfig Html(ScrapeContentMode? mode = null) => new() { Type = ScrapeFormatType.Html, Mode = mode };

    /// <summary>Creates a links output format.</summary>
    /// <returns>A links format configuration.</returns>
    public static FormatConfig Links() => new() { Type = ScrapeFormatType.Links };

    /// <summary>Creates an images output format.</summary>
    /// <returns>An images format configuration.</returns>
    public static FormatConfig Images() => new() { Type = ScrapeFormatType.Images };

    /// <summary>Creates a summary output format.</summary>
    /// <returns>A summary format configuration.</returns>
    public static FormatConfig Summary() => new() { Type = ScrapeFormatType.Summary };

    /// <summary>Creates a branding output format.</summary>
    /// <returns>A branding format configuration.</returns>
    public static FormatConfig Branding() => new() { Type = ScrapeFormatType.Branding };

    /// <summary>Creates a JSON extraction output format.</summary>
    /// <param name="prompt">The extraction prompt.</param>
    /// <param name="schema">The optional JSON schema.</param>
    /// <returns>A JSON extraction format configuration.</returns>
    public static FormatConfig Json(string prompt, JsonElement? schema = null) => new() { Type = ScrapeFormatType.Json, Prompt = prompt, Schema = schema };

    /// <summary>Creates a screenshot output format.</summary>
    /// <param name="fullPage">Whether to capture the full page.</param>
    /// <param name="width">The optional viewport width.</param>
    /// <param name="height">The optional viewport height.</param>
    /// <param name="quality">The optional screenshot quality.</param>
    /// <returns>A screenshot format configuration.</returns>
    public static FormatConfig Screenshot(bool? fullPage = null, int? width = null, int? height = null, int? quality = null)
        => new() { Type = ScrapeFormatType.Screenshot, FullPage = fullPage, Width = width, Height = height, Quality = quality };
}

/// <summary>Request payload for scraping one page.</summary>
public sealed record ScrapeRequest
{
    /// <summary>Gets the public URL to scrape.</summary>
    public required string Url { get; init; }

    /// <summary>Gets the requested output formats.</summary>
    public IReadOnlyList<FormatConfig>? Formats { get; init; }

    /// <summary>Gets the optional content type hint.</summary>
    public string? ContentType { get; init; }

    /// <summary>Gets fetch behavior for the request.</summary>
    public FetchConfig? FetchConfig { get; init; }
}

/// <summary>Request payload for extracting structured data.</summary>
public sealed record ExtractRequest
{
    /// <summary>Gets the URL to fetch.</summary>
    public string? Url { get; init; }

    /// <summary>Gets raw HTML input.</summary>
    public string? Html { get; init; }

    /// <summary>Gets raw markdown input.</summary>
    public string? Markdown { get; init; }

    /// <summary>Gets the natural-language extraction prompt.</summary>
    public required string Prompt { get; init; }

    /// <summary>Gets the optional JSON schema for extraction.</summary>
    public JsonElement? Schema { get; init; }

    /// <summary>Gets the optional extraction mode.</summary>
    public ScrapeContentMode? Mode { get; init; }

    /// <summary>Gets the optional content type hint.</summary>
    public string? ContentType { get; init; }

    /// <summary>Gets fetch behavior for the request.</summary>
    public FetchConfig? FetchConfig { get; init; }
}

/// <summary>Request payload for web search.</summary>
public sealed record SearchRequest
{
    /// <summary>Gets the search query.</summary>
    public required string Query { get; init; }

    /// <summary>Gets the number of search results to fetch.</summary>
    public int? NumResults { get; init; }

    /// <summary>Gets the optional prompt for extracting data across results.</summary>
    public string? Prompt { get; init; }

    /// <summary>Gets the optional JSON schema for extraction.</summary>
    public JsonElement? Schema { get; init; }

    /// <summary>Gets the requested content format.</summary>
    public SearchResultFormat? Format { get; init; }

    /// <summary>Gets the optional content processing mode.</summary>
    public ScrapeContentMode? Mode { get; init; }

    /// <summary>Gets the optional recency filter.</summary>
    public SearchTimeRange? TimeRange { get; init; }

    /// <summary>Gets the optional geographic location code.</summary>
    public SearchLocationGeoCode? LocationGeoCode { get; init; }

    /// <summary>Gets fetch behavior for the request.</summary>
    public FetchConfig? FetchConfig { get; init; }
}

/// <summary>Request payload for starting a crawl job.</summary>
public sealed record CrawlRequest
{
    /// <summary>Gets the URL where crawling starts.</summary>
    public required string Url { get; init; }

    /// <summary>Gets the requested output formats for crawled pages.</summary>
    public IReadOnlyList<FormatConfig>? Formats { get; init; }

    /// <summary>Gets the maximum crawl depth.</summary>
    public int? MaxDepth { get; init; }

    /// <summary>Gets the maximum number of pages to crawl.</summary>
    public int? MaxPages { get; init; }

    /// <summary>Gets the maximum links followed from each page.</summary>
    public int? MaxLinksPerPage { get; init; }

    /// <summary>Gets whether external links may be crawled.</summary>
    public bool? AllowExternal { get; init; }

    /// <summary>Gets URL patterns to include.</summary>
    public IReadOnlyList<string>? IncludePatterns { get; init; }

    /// <summary>Gets URL patterns to exclude.</summary>
    public IReadOnlyList<string>? ExcludePatterns { get; init; }

    /// <summary>Gets content types to crawl.</summary>
    public IReadOnlyList<string>? ContentTypes { get; init; }

    /// <summary>Gets fetch behavior for the request.</summary>
    public FetchConfig? FetchConfig { get; init; }
}

/// <summary>Request payload for creating a page monitor.</summary>
public sealed record MonitorCreateRequest
{
    /// <summary>Gets the URL to monitor.</summary>
    public required string Url { get; init; }

    /// <summary>Gets the monitor display name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the monitor schedule interval.</summary>
    public required string Interval { get; init; }

    /// <summary>Gets the requested output formats.</summary>
    public IReadOnlyList<FormatConfig>? Formats { get; init; }

    /// <summary>Gets the optional webhook URL for monitor notifications.</summary>
    public string? WebhookUrl { get; init; }

    /// <summary>Gets fetch behavior for the monitor.</summary>
    public FetchConfig? FetchConfig { get; init; }
}

/// <summary>Request payload for updating a page monitor.</summary>
public sealed record MonitorUpdateRequest
{
    /// <summary>Gets the updated URL.</summary>
    public string? Url { get; init; }

    /// <summary>Gets the updated monitor name.</summary>
    public string? Name { get; init; }

    /// <summary>Gets the updated schedule interval.</summary>
    public string? Interval { get; init; }

    /// <summary>Gets the updated output formats.</summary>
    public IReadOnlyList<FormatConfig>? Formats { get; init; }

    /// <summary>Gets the updated webhook URL.</summary>
    public string? WebhookUrl { get; init; }

    /// <summary>Gets updated fetch behavior.</summary>
    public FetchConfig? FetchConfig { get; init; }
}

/// <summary>Response returned by scrape operations.</summary>
/// <param name="Id">The request or job id.</param>
/// <param name="Results">The output results keyed by format name.</param>
/// <param name="Metadata">Response metadata returned by the API.</param>
public sealed record ScrapeResponse(string Id, IReadOnlyDictionary<string, FormatResult> Results, JsonElement? Metadata)
{
    /// <summary>Gets additional response fields not modeled by the SDK.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}

/// <summary>Result payload for one requested output format.</summary>
/// <param name="Data">The format data returned by the API.</param>
/// <param name="Metadata">Format-specific metadata.</param>
public sealed record FormatResult(JsonElement Data, JsonElement? Metadata)
{
    /// <summary>Gets additional result fields not modeled by the SDK.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}

/// <summary>Response returned by extraction operations.</summary>
/// <param name="Id">The request or job id.</param>
/// <param name="Raw">Raw extracted content, when returned.</param>
/// <param name="Json">Structured JSON extracted by the API.</param>
/// <param name="Usage">Token or usage metadata.</param>
/// <param name="Metadata">Response metadata returned by the API.</param>
public sealed record ExtractResponse(string Id, string? Raw, JsonElement Json, JsonElement? Usage, JsonElement? Metadata)
{
    /// <summary>Gets additional response fields not modeled by the SDK.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}

/// <summary>Response returned by search operations.</summary>
/// <param name="Id">The request or job id.</param>
/// <param name="Results">Search results returned by the API.</param>
/// <param name="Json">Structured JSON extracted across results, when requested.</param>
/// <param name="Metadata">Response metadata returned by the API.</param>
public sealed record SearchResponse(string Id, IReadOnlyList<SearchResult> Results, JsonElement? Json, JsonElement? Metadata)
{
    /// <summary>Gets raw extracted content, when returned.</summary>
    public string? Raw { get; init; }

    /// <summary>Gets token or usage metadata.</summary>
    public JsonElement? Usage { get; init; }

    /// <summary>Gets additional response fields not modeled by the SDK.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}

/// <summary>One search result.</summary>
/// <param name="Url">The result URL.</param>
/// <param name="Title">The result title.</param>
/// <param name="Content">The fetched or summarized result content.</param>
public sealed record SearchResult(string Url, string? Title, string? Content)
{
    /// <summary>Gets additional result fields not modeled by the SDK.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}

/// <summary>Response returned for crawl job operations.</summary>
/// <param name="Id">The crawl id.</param>
/// <param name="Status">The crawl status.</param>
/// <param name="Total">The total page count, when known.</param>
/// <param name="Finished">The number of finished pages, when known.</param>
/// <param name="Pages">Pages included in the response.</param>
public sealed record CrawlResponse(string Id, string Status, int? Total, int? Finished, IReadOnlyList<CrawlPage>? Pages)
{
    /// <summary>Gets additional response fields not modeled by the SDK.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}

/// <summary>One page discovered or processed by a crawl.</summary>
/// <param name="Url">The page URL.</param>
/// <param name="Depth">The page depth from the crawl root.</param>
/// <param name="Title">The page title.</param>
/// <param name="Status">The page processing status.</param>
/// <param name="ParentUrl">The parent page URL.</param>
/// <param name="ContentType">The page content type.</param>
/// <param name="Links">Links discovered on the page.</param>
/// <param name="ScrapeRefId">The related scrape reference id.</param>
public sealed record CrawlPage(string Url, int? Depth, string? Title, string? Status, string? ParentUrl, string? ContentType, IReadOnlyList<string>? Links, string? ScrapeRefId)
{
    /// <summary>Gets output results keyed by format name.</summary>
    public IReadOnlyDictionary<string, FormatResult>? Results { get; init; }

    /// <summary>Gets page metadata returned by the API.</summary>
    public JsonElement? Metadata { get; init; }

    /// <summary>Gets a raw page result payload, when returned.</summary>
    public JsonElement? Result { get; init; }

    /// <summary>Gets a page-level error payload, when returned.</summary>
    public JsonElement? Error { get; init; }

    /// <summary>Gets elapsed processing time in milliseconds.</summary>
    public long? ElapsedMs { get; init; }

    /// <summary>Gets additional page fields not modeled by the SDK.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}

/// <summary>Paginated crawl page response.</summary>
/// <param name="Pages">The returned crawl pages.</param>
/// <param name="NextCursor">The cursor for the next page.</param>
public sealed record CrawlPagesResponse(IReadOnlyList<CrawlPage> Pages, string? NextCursor)
{
    /// <summary>Gets additional response fields not modeled by the SDK.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}

/// <summary>Monitor details returned by the API.</summary>
/// <param name="CronId">The monitor cron id.</param>
/// <param name="Status">The monitor status.</param>
/// <param name="Config">The monitor configuration payload.</param>
public sealed record MonitorResponse(string? CronId, string? Status, JsonElement? Config)
{
    /// <summary>Gets the monitor schedule id.</summary>
    public string? ScheduleId { get; init; }

    /// <summary>Gets the monitor schedule interval.</summary>
    public string? Interval { get; init; }

    /// <summary>Gets when the monitor was created.</summary>
    public DateTimeOffset? CreatedAt { get; init; }

    /// <summary>Gets when the monitor was last updated.</summary>
    public DateTimeOffset? UpdatedAt { get; init; }

    /// <summary>Gets additional response fields not modeled by the SDK.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}

/// <summary>Paginated monitor activity response.</summary>
/// <param name="Ticks">The returned activity ticks.</param>
/// <param name="NextCursor">The cursor for the next page.</param>
public sealed record MonitorActivityResponse(IReadOnlyList<MonitorTick> Ticks, string? NextCursor)
{
    /// <summary>Gets additional response fields not modeled by the SDK.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}

/// <summary>One monitor activity tick.</summary>
/// <param name="Id">The activity tick id.</param>
/// <param name="Status">The tick status.</param>
/// <param name="CreatedAt">When the tick was created.</param>
/// <param name="ElapsedMs">Elapsed processing time in milliseconds.</param>
/// <param name="Changed">Whether the monitored page changed.</param>
/// <param name="Diffs">The change diff payload.</param>
public sealed record MonitorTick(string Id, string? Status, DateTimeOffset? CreatedAt, long? ElapsedMs, bool? Changed, JsonElement? Diffs)
{
    /// <summary>Gets additional tick fields not modeled by the SDK.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}

/// <summary>Paginated request history response.</summary>
/// <param name="Data">The returned history entries.</param>
/// <param name="Pagination">Pagination metadata.</param>
public sealed record HistoryPage(IReadOnlyList<HistoryEntry> Data, Pagination? Pagination)
{
    /// <summary>Gets additional response fields not modeled by the SDK.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}

/// <summary>Pagination metadata.</summary>
/// <param name="Page">The current page number.</param>
/// <param name="Limit">The page size.</param>
/// <param name="Total">The total number of records.</param>
public sealed record Pagination(int? Page, int? Limit, int? Total)
{
    /// <summary>Gets additional pagination fields not modeled by the SDK.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}

/// <summary>One request history entry.</summary>
/// <param name="Id">The history entry id.</param>
/// <param name="UserId">The user id associated with the request.</param>
/// <param name="Service">The ScrapeGraphAI service used.</param>
/// <param name="Status">The request status.</param>
/// <param name="Params">Request parameters recorded by the API.</param>
/// <param name="Result">The recorded result payload.</param>
/// <param name="Error">The recorded error payload.</param>
/// <param name="ElapsedMs">Elapsed request time in milliseconds.</param>
/// <param name="RequestParentId">The parent request id.</param>
/// <param name="CreatedAt">When the history entry was created.</param>
public sealed record HistoryEntry(string Id, string? UserId, string? Service, string? Status, JsonElement? Params, JsonElement? Result, JsonElement? Error, long? ElapsedMs, string? RequestParentId, DateTimeOffset? CreatedAt)
{
    /// <summary>Gets additional history fields not modeled by the SDK.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}

/// <summary>Account credit and quota response.</summary>
/// <param name="Remaining">The remaining credit count.</param>
/// <param name="Used">The used credit count.</param>
/// <param name="Plan">The account plan name.</param>
/// <param name="Jobs">Job quota metadata.</param>
public sealed record CreditsResponse(long? Remaining, long? Used, string? Plan, JsonElement? Jobs)
{
    /// <summary>Gets additional response fields not modeled by the SDK.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}

/// <summary>API health response.</summary>
/// <param name="Status">The API health status.</param>
/// <param name="Uptime">The API uptime value.</param>
public sealed record HealthResponse(string? Status, long? Uptime)
{
    /// <summary>Gets additional response fields not modeled by the SDK.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? ExtensionData { get; set; }
}
