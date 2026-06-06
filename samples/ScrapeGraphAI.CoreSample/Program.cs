using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScrapeGraphAI;

var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["ScrapeGraphAI:ApiKey"] = Environment.GetEnvironmentVariable("SGAI_API_KEY"),
        ["ScrapeGraphAI:Resilience:TotalRequestTimeout"] = "00:01:30",
        ["ScrapeGraphAI:Resilience:AttemptTimeout"] = "00:00:30",
        ["ScrapeGraphAI:Resilience:MaxRetryAttempts"] = "3",
        ["ScrapeGraphAI:Resilience:RetryBackoff"] = "00:00:02"
    })
    .Build();

var apiKey = configuration["ScrapeGraphAI:ApiKey"];

if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.Error.WriteLine("Set SGAI_API_KEY before running this sample.");
    Console.Error.WriteLine("PowerShell: $env:SGAI_API_KEY = '<your-api-key>'");
    return 2;
}

var targetUrl = args.FirstOrDefault(arg => !arg.StartsWith("--", StringComparison.Ordinal)) ?? "https://docs.scrapegraphai.com/api-reference/introduction";
var debug = args.Contains("--debug", StringComparer.OrdinalIgnoreCase);

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

var services = new ServiceCollection();
services.Configure<ScrapeGraphOptions>(configuration.GetSection("ScrapeGraphAI"));
services.Configure<ScrapeGraphResilienceOptions>(configuration.GetSection("ScrapeGraphAI:Resilience"));
services.AddLogging(logging =>
{
    logging.AddSimpleConsole(options => options.SingleLine = true);
    logging.SetMinimumLevel(debug ? LogLevel.Debug : LogLevel.Warning);
});

services.AddScrapeGraphAI()
    .AddScrapeGraphAIStandardResilience();

await using var provider = services.BuildServiceProvider();
var client = provider.GetRequiredService<IScrapeGraphClient>();
var requestSpacing = TimeSpan.FromSeconds(7);
var lastRequestStarted = DateTimeOffset.MinValue;

Console.WriteLine($"ScrapeGraphAI core sample against {targetUrl}");
Console.WriteLine("This sample creates temporary crawl and monitor resources, then cleans them up.");
Console.WriteLine();

await PrintResultAsync("health", () => client.HealthAsync(cancellation.Token));
await PrintResultAsync("credits", () => client.CreditsAsync(cancellation.Token));

var scrape = await PrintResultAsync("scrape markdown", () => client.ScrapeAsync(new ScrapeRequest
{
    Url = targetUrl,
    Formats = [FormatConfig.Markdown(ScrapeContentMode.Reader)],
    FetchConfig = new FetchConfig
    {
        Timeout = 30_000,
        Wait = 1_000
    }
}, cancellation.Token));

using var schemaDocument = JsonDocument.Parse("""
    {
      "type": "object",
      "properties": {
        "title": { "type": "string" },
        "summary": { "type": "string" },
        "links": {
          "type": "array",
          "items": { "type": "string" }
        }
      }
    }
    """);

await PrintResultAsync("extract json", () => client.ExtractAsync(new ExtractRequest
{
    Url = targetUrl,
    Prompt = "Extract the page title, a short summary, and important links.",
    Schema = schemaDocument.RootElement.Clone()
}, cancellation.Token));

await PrintResultAsync("search", () => client.SearchAsync(new SearchRequest
{
    Query = $"site:{new Uri(targetUrl).Host} documentation",
    NumResults = 3,
    Format = SearchResultFormat.Markdown
}, cancellation.Token));

var crawl = await PrintResultAsync("crawl start", () => client.Crawl.StartAsync(new CrawlRequest
{
    Url = targetUrl,
    Formats = [FormatConfig.Markdown(ScrapeContentMode.Reader)],
    MaxDepth = 0,
    MaxPages = 1,
    MaxLinksPerPage = 1,
    AllowExternal = false,
    FetchConfig = new FetchConfig
    {
        Timeout = 30_000,
        Wait = 1_000
    }
}, cancellation.Token));

var crawlId = crawl.Data?.Id;
if (!string.IsNullOrWhiteSpace(crawlId))
{
    await PrintResultAsync("crawl get", () => client.Crawl.GetAsync(crawlId, cancellation.Token));
    await PrintResultAsync("crawl pages", () => client.Crawl.PagesAsync(crawlId, limit: 5, cancellationToken: cancellation.Token));
    await PrintResultAsync("crawl stop", () => client.Crawl.StopAsync(crawlId, cancellation.Token));
    await PrintResultAsync("crawl resume", () => client.Crawl.ResumeAsync(crawlId, cancellation.Token));
    await PrintResultAsync("crawl delete", () => client.Crawl.DeleteAsync(crawlId, cancellation.Token));
}

var monitorName = $"Core sample {DateTimeOffset.UtcNow:yyyyMMddHHmmss}";
var monitor = await PrintResultAsync("monitor create", () => client.Monitor.CreateAsync(new MonitorCreateRequest
{
    Url = targetUrl,
    Name = monitorName,
    Interval = "0 */12 * * *",
    Formats = [FormatConfig.Markdown(ScrapeContentMode.Reader)],
    FetchConfig = new FetchConfig
    {
        Timeout = 30_000,
        Wait = 1_000
    }
}, cancellation.Token));

await PrintResultAsync("monitor list", () => client.Monitor.ListAsync(cancellation.Token));

var cronId = monitor.Data?.CronId ?? monitor.Data?.ScheduleId;
if (!string.IsNullOrWhiteSpace(cronId))
{
    await PrintResultAsync("monitor get", () => client.Monitor.GetAsync(cronId, cancellation.Token));
    await PrintResultAsync("monitor update", () => client.Monitor.UpdateAsync(cronId, new MonitorUpdateRequest
    {
        Name = $"{monitorName} updated",
        Interval = "0 */6 * * *"
    }, cancellation.Token));
    await PrintResultAsync("monitor pause", () => client.Monitor.PauseAsync(cronId, cancellation.Token));
    await PrintResultAsync("monitor resume", () => client.Monitor.ResumeAsync(cronId, cancellation.Token));
    await PrintResultAsync("monitor activity", () => client.Monitor.ActivityAsync(cronId, limit: 5, cancellationToken: cancellation.Token));
    await PrintResultAsync("monitor delete", () => client.Monitor.DeleteAsync(cronId, cancellation.Token));
}

var history = await PrintResultAsync("history list", () => client.History.ListAsync(page: 1, limit: 5, service: HistoryService.Scrape, cancellationToken: cancellation.Token));
var historyId = scrape.Data?.Id ?? history.Data?.Data.FirstOrDefault()?.Id;
if (!string.IsNullOrWhiteSpace(historyId))
{
    await PrintResultAsync("history get", () => client.History.GetAsync(historyId, cancellation.Token));
}

return 0;

async Task<ApiResult<T>> PrintResultAsync<T>(string label, Func<Task<ApiResult<T>>> operation)
{
    Console.WriteLine($"--- {label} ---");

    await WaitForRequestSlotAsync().ConfigureAwait(false);
    lastRequestStarted = DateTimeOffset.UtcNow;

    var result = await operation().ConfigureAwait(false);
    Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    }));

    Console.WriteLine();
    return result;
}

async Task WaitForRequestSlotAsync()
{
    var nextRequestAt = lastRequestStarted + requestSpacing;
    var now = DateTimeOffset.UtcNow;
    if (nextRequestAt <= now)
    {
        return;
    }

    var delay = nextRequestAt - now;
    Console.WriteLine($"Waiting {delay.TotalSeconds:N0}s to stay under the sample rate limit...");
    await Task.Delay(delay, cancellation.Token).ConfigureAwait(false);
}
