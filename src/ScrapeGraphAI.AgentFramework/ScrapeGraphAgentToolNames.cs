namespace ScrapeGraphAI.AgentFramework;

/// <summary>
/// Well-known ScrapeGraphAI Agent Framework tool names.
/// </summary>
public static class ScrapeGraphAgentToolNames
{
    /// <summary>Tool name for scraping one page.</summary>
    public const string ScrapePage = "scrape_page";

    /// <summary>Tool name for extracting structured data from a page.</summary>
    public const string ExtractFromPage = "extract_from_page";

    /// <summary>Tool name for searching the web.</summary>
    public const string SearchWeb = "search_web";

    /// <summary>Tool name for starting a crawl job.</summary>
    public const string StartCrawl = "start_crawl";

    /// <summary>Tool name for getting crawl status.</summary>
    public const string GetCrawlStatus = "get_crawl_status";

    /// <summary>Tool name for getting crawl pages.</summary>
    public const string GetCrawlPages = "get_crawl_pages";

    /// <summary>Tool name for stopping a crawl job.</summary>
    public const string StopCrawl = "stop_crawl";

    /// <summary>Tool name for resuming a crawl job.</summary>
    public const string ResumeCrawl = "resume_crawl";

    /// <summary>Tool name for deleting a crawl job.</summary>
    public const string DeleteCrawl = "delete_crawl";

    /// <summary>Tool name for creating a monitor.</summary>
    public const string CreateMonitor = "create_monitor";

    /// <summary>Tool name for listing monitors.</summary>
    public const string ListMonitors = "list_monitors";

    /// <summary>Tool name for getting a monitor.</summary>
    public const string GetMonitor = "get_monitor";

    /// <summary>Tool name for updating a monitor.</summary>
    public const string UpdateMonitor = "update_monitor";

    /// <summary>Tool name for pausing a monitor.</summary>
    public const string PauseMonitor = "pause_monitor";

    /// <summary>Tool name for resuming a monitor.</summary>
    public const string ResumeMonitor = "resume_monitor";

    /// <summary>Tool name for deleting a monitor.</summary>
    public const string DeleteMonitor = "delete_monitor";

    /// <summary>Tool name for getting monitor activity.</summary>
    public const string GetMonitorActivity = "get_monitor_activity";

    /// <summary>Tool name for listing request history.</summary>
    public const string ListHistory = "list_history";

    /// <summary>Tool name for getting one request history entry.</summary>
    public const string GetHistory = "get_history";

    /// <summary>Tool name for getting account credits.</summary>
    public const string GetCredits = "get_credits";

    /// <summary>Tool name for checking API health.</summary>
    public const string HealthCheck = "health_check";

    /// <summary>Gets all well-known ScrapeGraphAI Agent Framework tool names.</summary>
    public static readonly IReadOnlyList<string> All =
    [
        ScrapePage,
        ExtractFromPage,
        SearchWeb,
        StartCrawl,
        GetCrawlStatus,
        GetCrawlPages,
        StopCrawl,
        ResumeCrawl,
        DeleteCrawl,
        CreateMonitor,
        ListMonitors,
        GetMonitor,
        UpdateMonitor,
        PauseMonitor,
        ResumeMonitor,
        DeleteMonitor,
        GetMonitorActivity,
        ListHistory,
        GetHistory,
        GetCredits,
        HealthCheck
    ];
}
