namespace ScrapeGraphAI;

/// <summary>
/// ScrapeGraphAI v2 client abstraction.
/// </summary>
public interface IScrapeGraphClient
{
    /// <summary>Gets crawl operations.</summary>
    ICrawlResource Crawl { get; }

    /// <summary>Gets monitor operations.</summary>
    IMonitorResource Monitor { get; }

    /// <summary>Gets history operations.</summary>
    IHistoryResource History { get; }

    /// <summary>Scrapes a web page and returns the requested output formats.</summary>
    /// <param name="request">The scrape request payload.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The scrape result, including output data or an SDK/API error.</returns>
    Task<ApiResult<ScrapeResponse>> ScrapeAsync(ScrapeRequest request, CancellationToken cancellationToken = default);

    /// <summary>Extracts structured data from a URL, HTML, or markdown input.</summary>
    /// <param name="request">The extraction request payload.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The extraction result, including structured JSON or an SDK/API error.</returns>
    Task<ApiResult<ExtractResponse>> ExtractAsync(ExtractRequest request, CancellationToken cancellationToken = default);

    /// <summary>Searches the web and optionally extracts structured data from fetched results.</summary>
    /// <param name="request">The search request payload.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The search result, including search hits, optional structured JSON, or an SDK/API error.</returns>
    Task<ApiResult<SearchResponse>> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default);

    /// <summary>Gets account credit and quota information.</summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The credits result, including quota data or an SDK/API error.</returns>
    Task<ApiResult<CreditsResponse>> CreditsAsync(CancellationToken cancellationToken = default);

    /// <summary>Checks ScrapeGraphAI API health.</summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The health result, including API health data or an SDK/API error.</returns>
    Task<ApiResult<HealthResponse>> HealthAsync(CancellationToken cancellationToken = default);
}

/// <summary>Crawl operation group.</summary>
public interface ICrawlResource
{
    /// <summary>Starts a crawl job.</summary>
    /// <param name="request">The crawl start request payload.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The crawl result, including job details or an SDK/API error.</returns>
    Task<ApiResult<CrawlResponse>> StartAsync(CrawlRequest request, CancellationToken cancellationToken = default);

    /// <summary>Gets a crawl job by id.</summary>
    /// <param name="id">The crawl id.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The crawl result, including job details or an SDK/API error.</returns>
    Task<ApiResult<CrawlResponse>> GetAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Gets paginated pages discovered by a crawl job.</summary>
    /// <param name="id">The crawl id.</param>
    /// <param name="cursor">The optional pagination cursor.</param>
    /// <param name="limit">The optional page size.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The crawl pages result, including page data or an SDK/API error.</returns>
    Task<ApiResult<CrawlPagesResponse>> PagesAsync(string id, string? cursor = null, int? limit = null, CancellationToken cancellationToken = default);

    /// <summary>Stops a running crawl job.</summary>
    /// <param name="id">The crawl id.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The stop result, including success status or an SDK/API error.</returns>
    Task<ApiResult<object>> StopAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Resumes a stopped crawl job.</summary>
    /// <param name="id">The crawl id.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The resume result, including success status or an SDK/API error.</returns>
    Task<ApiResult<object>> ResumeAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Deletes a crawl job.</summary>
    /// <param name="id">The crawl id.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The delete result, including success status or an SDK/API error.</returns>
    Task<ApiResult<object>> DeleteAsync(string id, CancellationToken cancellationToken = default);
}

/// <summary>Monitor operation group.</summary>
public interface IMonitorResource
{
    /// <summary>Creates a scheduled monitor.</summary>
    /// <param name="request">The monitor creation request payload.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The monitor result, including monitor details or an SDK/API error.</returns>
    Task<ApiResult<MonitorResponse>> CreateAsync(MonitorCreateRequest request, CancellationToken cancellationToken = default);

    /// <summary>Lists configured monitors.</summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The monitor list result, including configured monitors or an SDK/API error.</returns>
    Task<ApiResult<IReadOnlyList<MonitorResponse>>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>Gets a monitor by cron id.</summary>
    /// <param name="cronId">The monitor cron id.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The monitor result, including monitor details or an SDK/API error.</returns>
    Task<ApiResult<MonitorResponse>> GetAsync(string cronId, CancellationToken cancellationToken = default);

    /// <summary>Updates a monitor.</summary>
    /// <param name="cronId">The monitor cron id.</param>
    /// <param name="request">The monitor update request payload.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The monitor result, including updated monitor details or an SDK/API error.</returns>
    Task<ApiResult<MonitorResponse>> UpdateAsync(string cronId, MonitorUpdateRequest request, CancellationToken cancellationToken = default);

    /// <summary>Pauses a monitor.</summary>
    /// <param name="cronId">The monitor cron id.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The monitor result, including updated monitor details or an SDK/API error.</returns>
    Task<ApiResult<MonitorResponse>> PauseAsync(string cronId, CancellationToken cancellationToken = default);

    /// <summary>Resumes a paused monitor.</summary>
    /// <param name="cronId">The monitor cron id.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The monitor result, including updated monitor details or an SDK/API error.</returns>
    Task<ApiResult<MonitorResponse>> ResumeAsync(string cronId, CancellationToken cancellationToken = default);

    /// <summary>Deletes a monitor.</summary>
    /// <param name="cronId">The monitor cron id.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The delete result, including success status or an SDK/API error.</returns>
    Task<ApiResult<object>> DeleteAsync(string cronId, CancellationToken cancellationToken = default);

    /// <summary>Gets monitor activity ticks.</summary>
    /// <param name="cronId">The monitor cron id.</param>
    /// <param name="limit">The optional page size.</param>
    /// <param name="cursor">The optional pagination cursor.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The monitor activity result, including ticks or an SDK/API error.</returns>
    Task<ApiResult<MonitorActivityResponse>> ActivityAsync(string cronId, int? limit = null, string? cursor = null, CancellationToken cancellationToken = default);
}

/// <summary>History operation group.</summary>
public interface IHistoryResource
{
    /// <summary>Lists request history entries.</summary>
    /// <param name="page">The optional page number.</param>
    /// <param name="limit">The optional page size.</param>
    /// <param name="service">The optional service filter.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The history page result, including entries and pagination or an SDK/API error.</returns>
    Task<ApiResult<HistoryPage>> ListAsync(int? page = null, int? limit = null, HistoryService? service = null, CancellationToken cancellationToken = default);

    /// <summary>Gets a request history entry by id.</summary>
    /// <param name="id">The history entry id.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The history entry result, including entry details or an SDK/API error.</returns>
    Task<ApiResult<HistoryEntry>> GetAsync(string id, CancellationToken cancellationToken = default);
}
