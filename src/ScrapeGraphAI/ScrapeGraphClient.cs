namespace ScrapeGraphAI;

internal sealed class ScrapeGraphClient(
    IScrapeGraphTransport transport,
    ICrawlResource crawl,
    IMonitorResource monitor,
    IHistoryResource history) : IScrapeGraphClient
{
    public ICrawlResource Crawl { get; } = crawl;

    public IMonitorResource Monitor { get; } = monitor;

    public IHistoryResource History { get; } = history;

    public Task<ApiResult<ScrapeResponse>> ScrapeAsync(ScrapeRequest request, CancellationToken cancellationToken = default)
        => transport.PostAsync<ScrapeRequest, ScrapeResponse>("scrape", request, cancellationToken);

    public Task<ApiResult<ExtractResponse>> ExtractAsync(ExtractRequest request, CancellationToken cancellationToken = default)
        => transport.PostAsync<ExtractRequest, ExtractResponse>("extract", request, cancellationToken);

    public Task<ApiResult<SearchResponse>> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
        => transport.PostAsync<SearchRequest, SearchResponse>("search", request, cancellationToken);

    public Task<ApiResult<CreditsResponse>> CreditsAsync(CancellationToken cancellationToken = default)
        => transport.GetAsync<CreditsResponse>("credits", cancellationToken);

    public Task<ApiResult<HealthResponse>> HealthAsync(CancellationToken cancellationToken = default)
        => transport.GetAsync<HealthResponse>("health", cancellationToken);
}
