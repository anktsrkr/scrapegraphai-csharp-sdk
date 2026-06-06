namespace ScrapeGraphAI;

internal sealed class CrawlResource(IScrapeGraphTransport transport) : ICrawlResource
{
    public Task<ApiResult<CrawlResponse>> StartAsync(CrawlRequest request, CancellationToken cancellationToken = default)
        => transport.PostAsync<CrawlRequest, CrawlResponse>("crawl", request, cancellationToken);

    public Task<ApiResult<CrawlResponse>> GetAsync(string id, CancellationToken cancellationToken = default)
        => transport.GetAsync<CrawlResponse>($"crawl/{Uri.EscapeDataString(id)}", cancellationToken);

    public Task<ApiResult<CrawlPagesResponse>> PagesAsync(string id, string? cursor = null, int? limit = null, CancellationToken cancellationToken = default)
        => transport.GetAsync<CrawlPagesResponse>($"crawl/{Uri.EscapeDataString(id)}/pages{ScrapeGraphTransport.BuildQueryString(("cursor", cursor), ("limit", limit))}", cancellationToken);

    public Task<ApiResult<object>> StopAsync(string id, CancellationToken cancellationToken = default)
        => transport.PostEmptyAsync<object>($"crawl/{Uri.EscapeDataString(id)}/stop", cancellationToken);

    public Task<ApiResult<object>> ResumeAsync(string id, CancellationToken cancellationToken = default)
        => transport.PostEmptyAsync<object>($"crawl/{Uri.EscapeDataString(id)}/resume", cancellationToken);

    public Task<ApiResult<object>> DeleteAsync(string id, CancellationToken cancellationToken = default)
        => transport.DeleteAsync<object>($"crawl/{Uri.EscapeDataString(id)}", cancellationToken);
}

internal sealed class MonitorResource(IScrapeGraphTransport transport) : IMonitorResource
{
    public Task<ApiResult<MonitorResponse>> CreateAsync(MonitorCreateRequest request, CancellationToken cancellationToken = default)
        => transport.PostAsync<MonitorCreateRequest, MonitorResponse>("monitor", request, cancellationToken);

    public Task<ApiResult<IReadOnlyList<MonitorResponse>>> ListAsync(CancellationToken cancellationToken = default)
        => transport.GetAsync<IReadOnlyList<MonitorResponse>>("monitor", cancellationToken);

    public Task<ApiResult<MonitorResponse>> GetAsync(string cronId, CancellationToken cancellationToken = default)
        => transport.GetAsync<MonitorResponse>($"monitor/{Uri.EscapeDataString(cronId)}", cancellationToken);

    public Task<ApiResult<MonitorResponse>> UpdateAsync(string cronId, MonitorUpdateRequest request, CancellationToken cancellationToken = default)
        => transport.PatchAsync<MonitorUpdateRequest, MonitorResponse>($"monitor/{Uri.EscapeDataString(cronId)}", request, cancellationToken);

    public Task<ApiResult<MonitorResponse>> PauseAsync(string cronId, CancellationToken cancellationToken = default)
        => transport.PostEmptyAsync<MonitorResponse>($"monitor/{Uri.EscapeDataString(cronId)}/pause", cancellationToken);

    public Task<ApiResult<MonitorResponse>> ResumeAsync(string cronId, CancellationToken cancellationToken = default)
        => transport.PostEmptyAsync<MonitorResponse>($"monitor/{Uri.EscapeDataString(cronId)}/resume", cancellationToken);

    public Task<ApiResult<object>> DeleteAsync(string cronId, CancellationToken cancellationToken = default)
        => transport.DeleteAsync<object>($"monitor/{Uri.EscapeDataString(cronId)}", cancellationToken);

    public Task<ApiResult<MonitorActivityResponse>> ActivityAsync(string cronId, int? limit = null, string? cursor = null, CancellationToken cancellationToken = default)
        => transport.GetAsync<MonitorActivityResponse>($"monitor/{Uri.EscapeDataString(cronId)}/activity{ScrapeGraphTransport.BuildQueryString(("limit", limit), ("cursor", cursor))}", cancellationToken);
}

internal sealed class HistoryResource(IScrapeGraphTransport transport) : IHistoryResource
{
    public Task<ApiResult<HistoryPage>> ListAsync(int? page = null, int? limit = null, HistoryService? service = null, CancellationToken cancellationToken = default)
        => transport.GetAsync<HistoryPage>($"history{ScrapeGraphTransport.BuildQueryString(("page", page), ("limit", limit), ("service", service))}", cancellationToken);

    public Task<ApiResult<HistoryEntry>> GetAsync(string id, CancellationToken cancellationToken = default)
        => transport.GetAsync<HistoryEntry>($"history/{Uri.EscapeDataString(id)}", cancellationToken);
}
