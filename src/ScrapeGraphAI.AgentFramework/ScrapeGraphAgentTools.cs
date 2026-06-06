using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace ScrapeGraphAI.AgentFramework;

/// <summary>
/// Microsoft Agent Framework tools backed by ScrapeGraphAI.
/// </summary>
public sealed class ScrapeGraphAgentTools(IScrapeGraphClient client, IOptions<ScrapeGraphAgentToolOptions> options)
{
    private static readonly JsonSerializerOptions AgentJsonOptions = ScrapeGraphAgentJsonContext.Default.Options;

    private readonly ScrapeGraphAgentToolOptions _options = options.Value;
    private readonly HashSet<string>? _approvalRequiredTools = CreateNameSet(options.Value.ApprovalRequiredTools);

    /// <summary>
    /// Returns the ScrapeGraphAI tools as Agent Framework AI tools.
    /// </summary>
    /// <returns>The configured ScrapeGraphAI tools.</returns>
    public IEnumerable<AITool> AsAITools()
        => this.FilterTools(this.CreateAllTools(), this._options.IncludedTools);

    /// <summary>
    /// Returns only the requested ScrapeGraphAI tools as Agent Framework AI tools.
    /// </summary>
    /// <param name="toolNames">The tool names to include.</param>
    /// <returns>The requested ScrapeGraphAI tools.</returns>
    public IEnumerable<AITool> AsAITools(params string[] toolNames)
        => this.AsAITools((IEnumerable<string>)toolNames);

    /// <summary>
    /// Returns only the requested ScrapeGraphAI tools as Agent Framework AI tools.
    /// </summary>
    /// <param name="toolNames">The tool names to include.</param>
    /// <returns>The requested ScrapeGraphAI tools.</returns>
    public IEnumerable<AITool> AsAITools(IEnumerable<string> toolNames)
        => this.FilterTools(this.CreateAllTools(), toolNames);

    private IEnumerable<AITool> CreateAllTools()
    {
        yield return this.CreateTool(this.ScrapePageAsync, ScrapeGraphAgentToolNames.ScrapePage, "Fetch a URL and return content in markdown, HTML, links, images, summary, JSON, branding, or screenshot format.");
        yield return this.CreateTool(this.ExtractFromPageAsync, ScrapeGraphAgentToolNames.ExtractFromPage, "Extract structured JSON from a URL, raw HTML, or markdown using a natural-language prompt.");
        yield return this.CreateTool(this.SearchWebAsync, ScrapeGraphAgentToolNames.SearchWeb, "Search the web, fetch top results, and optionally extract structured JSON across the results.");
        yield return this.CreateTool(this.StartCrawlAsync, ScrapeGraphAgentToolNames.StartCrawl, "Start an async multi-page crawl job.");
        yield return this.CreateTool(this.GetCrawlStatusAsync, ScrapeGraphAgentToolNames.GetCrawlStatus, "Get crawl job status and discovered pages.");
        yield return this.CreateTool(this.GetCrawlPagesAsync, ScrapeGraphAgentToolNames.GetCrawlPages, "Get paginated crawl pages.");
        yield return this.CreateTool(this.StopCrawlAsync, ScrapeGraphAgentToolNames.StopCrawl, "Stop a running crawl job.");
        yield return this.CreateTool(this.ResumeCrawlAsync, ScrapeGraphAgentToolNames.ResumeCrawl, "Resume a stopped crawl job.");
        yield return this.CreateTool(this.DeleteCrawlAsync, ScrapeGraphAgentToolNames.DeleteCrawl, "Delete a crawl job.");
        yield return this.CreateTool(this.CreateMonitorAsync, ScrapeGraphAgentToolNames.CreateMonitor, "Create a scheduled page monitor.");
        yield return this.CreateTool(this.ListMonitorsAsync, ScrapeGraphAgentToolNames.ListMonitors, "List configured monitors.");
        yield return this.CreateTool(this.GetMonitorAsync, ScrapeGraphAgentToolNames.GetMonitor, "Get one monitor by cron id.");
        yield return this.CreateTool(this.UpdateMonitorAsync, ScrapeGraphAgentToolNames.UpdateMonitor, "Update a monitor.");
        yield return this.CreateTool(this.PauseMonitorAsync, ScrapeGraphAgentToolNames.PauseMonitor, "Pause a monitor.");
        yield return this.CreateTool(this.ResumeMonitorAsync, ScrapeGraphAgentToolNames.ResumeMonitor, "Resume a monitor.");
        yield return this.CreateTool(this.DeleteMonitorAsync, ScrapeGraphAgentToolNames.DeleteMonitor, "Delete a monitor.");
        yield return this.CreateTool(this.GetMonitorActivityAsync, ScrapeGraphAgentToolNames.GetMonitorActivity, "Get monitor tick activity.");
        yield return this.CreateTool(this.ListHistoryAsync, ScrapeGraphAgentToolNames.ListHistory, "List request history.");
        yield return this.CreateTool(this.GetHistoryAsync, ScrapeGraphAgentToolNames.GetHistory, "Get one history entry.");
        yield return this.CreateTool(this.GetCreditsAsync, ScrapeGraphAgentToolNames.GetCredits, "Get account credits and job quotas.");
        yield return this.CreateTool(this.HealthCheckAsync, ScrapeGraphAgentToolNames.HealthCheck, "Check API health.");
    }

    /// <summary>Fetches a URL and returns content in a selected format.</summary>
    /// <param name="url">The public URL to scrape.</param>
    /// <param name="format">The requested output format.</param>
    /// <param name="mode">The optional processing mode for markdown or HTML output.</param>
    /// <param name="prompt">The optional extraction prompt when JSON output is requested.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A JSON string containing the scrape result.</returns>
    [Description("Fetch a URL and return content in a selected format.")]
    public async Task<string> ScrapePageAsync(
        [Description("Public URL to scrape.")] string url,
        [Description("Requested output format.")] ScrapeFormatType? format = null,
        [Description("Optional processing mode for markdown/html.")] ScrapeContentMode? mode = null,
        [Description("Optional extraction prompt when format is json.")] string? prompt = null,
        CancellationToken cancellationToken = default)
    {
        var request = new ScrapeRequest
        {
            Url = url,
            Formats = [CreateFormat(format ?? this._options.DefaultFormat, mode, prompt, null)]
        };

        return await this.SerializeAsync(client.ScrapeAsync(request, cancellationToken)).ConfigureAwait(false);
    }

    /// <summary>Extracts structured JSON from a URL, HTML, or markdown.</summary>
    /// <param name="prompt">The natural-language extraction prompt.</param>
    /// <param name="url">The optional public URL to fetch.</param>
    /// <param name="html">The optional raw HTML input.</param>
    /// <param name="markdown">The optional raw markdown input.</param>
    /// <param name="schemaJson">The optional JSON schema as a string.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A JSON string containing the extraction result.</returns>
    [Description("Extract structured JSON from a URL, HTML, or markdown.")]
    public async Task<string> ExtractFromPageAsync(
        [Description("Natural-language extraction prompt.")] string prompt,
        [Description("Public URL to fetch.")] string? url = null,
        [Description("Raw HTML source.")] string? html = null,
        [Description("Raw markdown source.")] string? markdown = null,
        [Description("Optional JSON schema as a JSON string.")] string? schemaJson = null,
        CancellationToken cancellationToken = default)
    {
        var request = new ExtractRequest
        {
            Prompt = prompt,
            Url = url,
            Html = html,
            Markdown = markdown,
            Schema = ParseJsonElement(schemaJson)
        };

        return await this.SerializeAsync(client.ExtractAsync(request, cancellationToken)).ConfigureAwait(false);
    }

    /// <summary>Searches the web and fetches top results.</summary>
    /// <param name="query">The search query.</param>
    /// <param name="numResults">The number of results to fetch.</param>
    /// <param name="format">The requested content format.</param>
    /// <param name="mode">The optional processing mode for result content.</param>
    /// <param name="prompt">The optional extraction prompt across fetched results.</param>
    /// <param name="schemaJson">The optional JSON schema as a string.</param>
    /// <param name="timeRange">The optional recency filter.</param>
    /// <param name="locationGeoCode">The optional two-letter country code.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A JSON string containing the search result.</returns>
    [Description("Search the web and fetch top results.")]
    public async Task<string> SearchWebAsync(
        [Description("Search query.")] string query,
        [Description("Number of results to fetch, 1 to 20.")] int? numResults = null,
        [Description("Content format.")] SearchResultFormat? format = null,
        [Description("Optional processing mode for result content.")] ScrapeContentMode? mode = null,
        [Description("Optional extraction prompt across fetched results.")] string? prompt = null,
        [Description("Optional JSON schema as a JSON string.")] string? schemaJson = null,
        [Description("Optional recency filter.")] SearchTimeRange? timeRange = null,
        [Description("Optional localized search country code.")] SearchLocationGeoCode? locationGeoCode = null,
        CancellationToken cancellationToken = default)
    {
        var request = new SearchRequest
        {
            Query = query,
            NumResults = numResults,
            Format = format ?? ToSearchResultFormat(this._options.DefaultFormat),
            Mode = mode,
            Prompt = prompt,
            Schema = ParseJsonElement(schemaJson),
            TimeRange = timeRange,
            LocationGeoCode = locationGeoCode
        };

        return await this.SerializeAsync(client.SearchAsync(request, cancellationToken)).ConfigureAwait(false);
    }

    /// <summary>Starts an async crawl.</summary>
    /// <param name="url">The crawl start URL.</param>
    /// <param name="maxPages">The maximum number of pages to crawl.</param>
    /// <param name="maxDepth">The maximum crawl depth.</param>
    /// <param name="format">The requested output format.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A JSON string containing the crawl start result.</returns>
    [Description("Start an async crawl.")]
    public async Task<string> StartCrawlAsync(
        string url,
        int? maxPages = null,
        int? maxDepth = null,
        ScrapeFormatType? format = null,
        CancellationToken cancellationToken = default)
    {
        var request = new CrawlRequest
        {
            Url = url,
            MaxPages = maxPages,
            MaxDepth = maxDepth,
            Formats = [CreateFormat(format ?? this._options.DefaultFormat, null, null, null)]
        };

        return await this.SerializeAsync(client.Crawl.StartAsync(request, cancellationToken)).ConfigureAwait(false);
    }

    /// <summary>Gets crawl job status and discovered pages.</summary>
    /// <param name="id">The crawl id.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A JSON string containing the crawl status result.</returns>
    public async Task<string> GetCrawlStatusAsync(string id, CancellationToken cancellationToken = default)
        => await this.SerializeAsync(client.Crawl.GetAsync(id, cancellationToken)).ConfigureAwait(false);

    /// <summary>Gets paginated crawl pages.</summary>
    /// <param name="id">The crawl id.</param>
    /// <param name="cursor">The optional page cursor.</param>
    /// <param name="limit">The optional page size.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A JSON string containing the crawl pages result.</returns>
    public async Task<string> GetCrawlPagesAsync(string id, string? cursor = null, int? limit = null, CancellationToken cancellationToken = default)
        => await this.SerializeAsync(client.Crawl.PagesAsync(id, cursor, limit, cancellationToken)).ConfigureAwait(false);

    /// <summary>Stops a running crawl job.</summary>
    /// <param name="id">The crawl id.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A JSON string containing the stop result.</returns>
    public async Task<string> StopCrawlAsync(string id, CancellationToken cancellationToken = default)
        => await this.SerializeAsync(client.Crawl.StopAsync(id, cancellationToken)).ConfigureAwait(false);

    /// <summary>Resumes a stopped crawl job.</summary>
    /// <param name="id">The crawl id.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A JSON string containing the resume result.</returns>
    public async Task<string> ResumeCrawlAsync(string id, CancellationToken cancellationToken = default)
        => await this.SerializeAsync(client.Crawl.ResumeAsync(id, cancellationToken)).ConfigureAwait(false);

    /// <summary>Deletes a crawl job.</summary>
    /// <param name="id">The crawl id.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A JSON string containing the delete result.</returns>
    public async Task<string> DeleteCrawlAsync(string id, CancellationToken cancellationToken = default)
        => await this.SerializeAsync(client.Crawl.DeleteAsync(id, cancellationToken)).ConfigureAwait(false);

    /// <summary>Creates a scheduled page monitor.</summary>
    /// <param name="url">The URL to monitor.</param>
    /// <param name="name">The monitor name.</param>
    /// <param name="interval">The monitor schedule interval.</param>
    /// <param name="format">The requested output format.</param>
    /// <param name="webhookUrl">The optional webhook URL.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A JSON string containing the monitor creation result.</returns>
    public async Task<string> CreateMonitorAsync(string url, string name, string interval, ScrapeFormatType? format = null, string? webhookUrl = null, CancellationToken cancellationToken = default)
    {
        var request = new MonitorCreateRequest
        {
            Url = url,
            Name = name,
            Interval = interval,
            Formats = [CreateFormat(format ?? this._options.DefaultFormat, null, null, null)],
            WebhookUrl = webhookUrl
        };

        return await this.SerializeAsync(client.Monitor.CreateAsync(request, cancellationToken)).ConfigureAwait(false);
    }

    /// <summary>Lists configured monitors.</summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A JSON string containing the monitor list result.</returns>
    public async Task<string> ListMonitorsAsync(CancellationToken cancellationToken = default)
        => await this.SerializeAsync(client.Monitor.ListAsync(cancellationToken)).ConfigureAwait(false);

    /// <summary>Gets one monitor by cron id.</summary>
    /// <param name="cronId">The monitor cron id.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A JSON string containing the monitor result.</returns>
    public async Task<string> GetMonitorAsync(string cronId, CancellationToken cancellationToken = default)
        => await this.SerializeAsync(client.Monitor.GetAsync(cronId, cancellationToken)).ConfigureAwait(false);

    /// <summary>Updates a monitor.</summary>
    /// <param name="cronId">The monitor cron id.</param>
    /// <param name="name">The updated monitor name.</param>
    /// <param name="interval">The updated schedule interval.</param>
    /// <param name="webhookUrl">The updated webhook URL.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A JSON string containing the monitor update result.</returns>
    public async Task<string> UpdateMonitorAsync(string cronId, string? name = null, string? interval = null, string? webhookUrl = null, CancellationToken cancellationToken = default)
    {
        var request = new MonitorUpdateRequest { Name = name, Interval = interval, WebhookUrl = webhookUrl };
        return await this.SerializeAsync(client.Monitor.UpdateAsync(cronId, request, cancellationToken)).ConfigureAwait(false);
    }

    /// <summary>Pauses a monitor.</summary>
    /// <param name="cronId">The monitor cron id.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A JSON string containing the pause result.</returns>
    public async Task<string> PauseMonitorAsync(string cronId, CancellationToken cancellationToken = default)
        => await this.SerializeAsync(client.Monitor.PauseAsync(cronId, cancellationToken)).ConfigureAwait(false);

    /// <summary>Resumes a monitor.</summary>
    /// <param name="cronId">The monitor cron id.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A JSON string containing the resume result.</returns>
    public async Task<string> ResumeMonitorAsync(string cronId, CancellationToken cancellationToken = default)
        => await this.SerializeAsync(client.Monitor.ResumeAsync(cronId, cancellationToken)).ConfigureAwait(false);

    /// <summary>Deletes a monitor.</summary>
    /// <param name="cronId">The monitor cron id.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A JSON string containing the delete result.</returns>
    public async Task<string> DeleteMonitorAsync(string cronId, CancellationToken cancellationToken = default)
        => await this.SerializeAsync(client.Monitor.DeleteAsync(cronId, cancellationToken)).ConfigureAwait(false);

    /// <summary>Gets monitor tick activity.</summary>
    /// <param name="cronId">The monitor cron id.</param>
    /// <param name="limit">The optional page size.</param>
    /// <param name="cursor">The optional page cursor.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A JSON string containing the monitor activity result.</returns>
    public async Task<string> GetMonitorActivityAsync(string cronId, int? limit = null, string? cursor = null, CancellationToken cancellationToken = default)
        => await this.SerializeAsync(client.Monitor.ActivityAsync(cronId, limit, cursor, cancellationToken)).ConfigureAwait(false);

    /// <summary>Lists request history.</summary>
    /// <param name="page">The optional page number.</param>
    /// <param name="limit">The optional page size.</param>
    /// <param name="service">The optional service filter.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A JSON string containing the history list result.</returns>
    public async Task<string> ListHistoryAsync(int? page = null, int? limit = null, HistoryService? service = null, CancellationToken cancellationToken = default)
        => await this.SerializeAsync(client.History.ListAsync(page, limit, service, cancellationToken)).ConfigureAwait(false);

    /// <summary>Gets one request history entry.</summary>
    /// <param name="id">The history entry id.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A JSON string containing the history entry result.</returns>
    public async Task<string> GetHistoryAsync(string id, CancellationToken cancellationToken = default)
        => await this.SerializeAsync(client.History.GetAsync(id, cancellationToken)).ConfigureAwait(false);

    /// <summary>Gets account credits and job quotas.</summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A JSON string containing the credits result.</returns>
    public async Task<string> GetCreditsAsync(CancellationToken cancellationToken = default)
        => await this.SerializeAsync(client.CreditsAsync(cancellationToken)).ConfigureAwait(false);

    /// <summary>Checks API health.</summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A JSON string containing the health result.</returns>
    public async Task<string> HealthCheckAsync(CancellationToken cancellationToken = default)
        => await this.SerializeAsync(client.HealthAsync(cancellationToken)).ConfigureAwait(false);

    private AITool CreateTool(Delegate method, string name, string description)
    {
        var function = AIFunctionFactory.Create(method, new AIFunctionFactoryOptions
        {
            Name = name,
            Description = description,
            SerializerOptions = AgentJsonOptions
        });

        return this._approvalRequiredTools is not null && this._approvalRequiredTools.Contains(name)
            ? new ApprovalRequiredAIFunction(function)
            : function;
    }

    private IEnumerable<AITool> FilterTools(IEnumerable<AITool> tools, IEnumerable<string>? includedTools)
    {
        var include = CreateNameSet(includedTools);
        var known = CreateNameSet(ScrapeGraphAgentToolNames.All)!;

        ValidateToolNames(include, known);
        ValidateToolNames(this._approvalRequiredTools, known);

        foreach (var tool in tools)
        {
            if (include is not null && !include.Contains(tool.Name))
            {
                continue;
            }

            yield return tool;
        }
    }

    private static HashSet<string>? CreateNameSet(IEnumerable<string>? names)
    {
        if (names is null)
        {
            return null;
        }

        var set = names
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return set.Count == 0 ? null : set;
    }

    private static void ValidateToolNames(HashSet<string>? requested, HashSet<string> known)
    {
        if (requested is null)
        {
            return;
        }

        var unknown = requested.Where(name => !known.Contains(name)).ToArray();
        if (unknown.Length > 0)
        {
            throw new ArgumentException($"Unknown ScrapeGraphAI tool name: {string.Join(", ", unknown)}");
        }
    }

    private async Task<string> SerializeAsync<T>(Task<ApiResult<T>> task)
    {
        var value = await task.ConfigureAwait(false);
        var maxCharacters = Math.Max(0, this._options.MaxResultCharacters);
        if (maxCharacters == 0)
        {
            return "...";
        }

        using var stream = new CappedBufferStream(GetUtf8BufferLimit(maxCharacters));
        using (var writer = new Utf8JsonWriter(stream))
        {
            var typeInfo = ScrapeGraphAgentJsonContext.Default.GetTypeInfo(typeof(ApiResult<T>));
            if (typeInfo is null)
            {
                JsonSerializer.Serialize(writer, value, AgentJsonOptions);
            }
            else
            {
                JsonSerializer.Serialize(writer, value, typeInfo);
            }
        }

        var json = stream.GetString();
        if (!stream.Truncated && json.Length <= maxCharacters)
        {
            return json;
        }

        return json[..Math.Min(json.Length, maxCharacters)] + "...";
    }

    private static int GetUtf8BufferLimit(int maxCharacters)
    {
        var limit = ((long)maxCharacters * 4) + 16;
        return limit > int.MaxValue ? int.MaxValue : (int)limit;
    }

    private static FormatConfig CreateFormat(ScrapeFormatType format, ScrapeContentMode? mode, string? prompt, string? schemaJson)
        => format switch
        {
            ScrapeFormatType.Html => FormatConfig.Html(mode),
            ScrapeFormatType.Links => FormatConfig.Links(),
            ScrapeFormatType.Images => FormatConfig.Images(),
            ScrapeFormatType.Summary => FormatConfig.Summary(),
            ScrapeFormatType.Json => FormatConfig.Json(prompt ?? "Extract structured data from the page.", ParseJsonElement(schemaJson)),
            ScrapeFormatType.Branding => FormatConfig.Branding(),
            ScrapeFormatType.Screenshot => FormatConfig.Screenshot(),
            _ => FormatConfig.Markdown(mode)
        };

    private static SearchResultFormat ToSearchResultFormat(ScrapeFormatType format)
        => format == ScrapeFormatType.Html ? SearchResultFormat.Html : SearchResultFormat.Markdown;

    private static JsonElement? ParseJsonElement(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private sealed class CappedBufferStream(int capacity) : Stream
    {
        private readonly byte[] _buffer = new byte[capacity];
        private int _length;

        public bool Truncated { get; private set; }

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => this._length;

        public override long Position
        {
            get => this._length;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();

        public override void SetLength(long value)
            => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
            => this.Write(buffer.AsSpan(offset, count));

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            var remaining = this._buffer.Length - this._length;
            if (remaining > 0)
            {
                var written = Math.Min(buffer.Length, remaining);
                buffer[..written].CopyTo(this._buffer.AsSpan(this._length));
                this._length += written;
            }

            if (buffer.Length > remaining)
            {
                this.Truncated = true;
            }
        }

        public string GetString()
            => Encoding.UTF8.GetString(this._buffer, 0, this._length);
    }
}
