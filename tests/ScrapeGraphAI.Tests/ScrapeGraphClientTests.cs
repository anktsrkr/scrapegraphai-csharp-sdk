using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScrapeGraphAI.AgentFramework;

namespace ScrapeGraphAI.Tests;

public sealed class ScrapeGraphClientTests
{
    [Fact]
    public async Task ScrapeAsync_SendsExpectedRequest()
    {
        var handler = new RecordingHandler(_ => JsonResponse("""
            {"id":"abc","results":{"markdown":{"data":["# Example"]}},"metadata":{"contentType":"text/html"}}
            """));
        using var provider = CreateProvider(handler, options => options.ApiKey = "test-key");
        var client = provider.GetRequiredService<IScrapeGraphClient>();

        var result = await client.ScrapeAsync(new ScrapeRequest
        {
            Url = "https://example.com",
            Formats = [FormatConfig.Markdown(ScrapeContentMode.Reader)]
        }, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Post, handler.LastRequest?.Method);
        Assert.Equal("https://unit.test/api/scrape", handler.LastRequest?.RequestUri?.ToString());
        Assert.True(handler.LastRequest?.Headers.Contains("SGAI-APIKEY"));
        Assert.Contains("\"mode\":\"reader\"", handler.LastBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AddScrapeGraphAI_DoesNotDuplicateDefaultHeaders()
    {
        var handler = new RecordingHandler(_ => JsonResponse("""
            {"id":"abc","results":{"markdown":{"data":["# Example"]}},"metadata":{}}
            """));
        using var provider = CreateProvider(handler, options => options.ApiKey = "test-key");
        var client = provider.GetRequiredService<IScrapeGraphClient>();

        await client.ScrapeAsync(new ScrapeRequest
        {
            Url = "https://example.com",
            Formats = [FormatConfig.Markdown()]
        }, TestContext.Current.CancellationToken);

        Assert.Single(handler.LastHeaders["Accept"]);
        Assert.Single(handler.LastHeaders["SGAI-APIKEY"]);
        Assert.Single(handler.LastHeaders["User-Agent"]);
        Assert.StartsWith("ScrapeGraphAI-DotNet/", handler.LastHeaders["User-Agent"][0], StringComparison.Ordinal);
        Assert.False(handler.LastHeaders["User-Agent"][0].EndsWith("/0.1.0", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AddScrapeGraphAI_AddsDefaultHeaders()
    {
        var handler = new RecordingHandler(_ => JsonResponse("""
            {"id":"abc","results":{"markdown":{"data":["# Example"]}},"metadata":{}}
            """));
        using var provider = CreateProvider(handler, options => options.ApiKey = "test-key");
        var client = provider.GetRequiredService<IScrapeGraphClient>();

        await client.ScrapeAsync(new ScrapeRequest
        {
            Url = "https://example.com",
            Formats = [FormatConfig.Markdown()]
        }, TestContext.Current.CancellationToken);

        Assert.Single(handler.LastHeaders["Accept"]);
        Assert.Single(handler.LastHeaders["SGAI-APIKEY"]);
    }

    [Fact]
    public void AddScrapeGraphAI_RejectsMissingApiKey()
    {
        var handler = new RecordingHandler(_ => JsonResponse("""{"remaining":10,"used":1,"plan":"Free","jobs":{}}"""));
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScrapeGraphAI(options =>
        {
            options.BaseUrl = new Uri("https://unit.test/api");
        })
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IScrapeGraphClient>());
    }

    [Fact]
    public void AddScrapeGraphAI_RejectsBlankApiKey()
    {
        var handler = new RecordingHandler(_ => JsonResponse("""{"remaining":10,"used":1,"plan":"Free","jobs":{}}"""));
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScrapeGraphAI(options =>
        {
            options.ApiKey = " ";
            options.BaseUrl = new Uri("https://unit.test/api");
        })
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IScrapeGraphClient>());
    }

    [Fact]
    public void AddScrapeGraphAI_RejectsInvalidBaseUrlScheme()
    {
        var handler = new RecordingHandler(_ => JsonResponse("""{"remaining":10,"used":1,"plan":"Free","jobs":{}}"""));
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScrapeGraphAI(options =>
        {
            options.ApiKey = "test-key";
            options.BaseUrl = new Uri("ftp://unit.test/api");
        })
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IScrapeGraphClient>());
    }

    [Fact]
    public void AddScrapeGraphAIStandardResilience_RejectsInvalidOptions()
    {
        var handler = new RecordingHandler(_ => JsonResponse("""{"status":"ok","uptime":123456789}"""));
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScrapeGraphAI(options =>
        {
            options.ApiKey = "test-key";
            options.BaseUrl = new Uri("https://unit.test/api");
        })
            .AddScrapeGraphAIStandardResilience(options =>
            {
                options.TotalRequestTimeout = TimeSpan.FromSeconds(1);
                options.AttemptTimeout = TimeSpan.FromSeconds(2);
            })
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<ScrapeGraphResilienceOptions>>().Value);
    }

    [Fact]
    public void AddScrapeGraphAIStandardResilience_RejectsZeroRetryAttempts()
    {
        var handler = new RecordingHandler(_ => JsonResponse("""{"status":"ok","uptime":123456789}"""));
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScrapeGraphAI(options =>
        {
            options.ApiKey = "test-key";
            options.BaseUrl = new Uri("https://unit.test/api");
        })
            .AddScrapeGraphAIStandardResilience(options =>
            {
                options.MaxRetryAttempts = 0;
            })
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<ScrapeGraphResilienceOptions>>().Value);
    }

    [Fact]
    public async Task ExtractAsync_ParsesApiError()
    {
        var handler = new RecordingHandler(_ => JsonResponse("""
            {"error":{"type":"validation","message":"Validation failed","details":[{"code":"invalid_format","path":["url"],"message":"Invalid URL"}]}}
            """, HttpStatusCode.BadRequest));
        using var provider = CreateProvider(handler);
        var client = provider.GetRequiredService<IScrapeGraphClient>();

        var result = await client.ExtractAsync(
            new ExtractRequest { Url = "not-a-url", Prompt = "extract title" },
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Equal("validation", result.Error?.Type);
        Assert.Equal("invalid_format", result.Error?.Details?[0].Code);
    }

    [Fact]
    public async Task ExtractAsync_MalformedApiErrorFallsBackToStatusMessage()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{not-json", Encoding.UTF8, "application/json")
        });
        using var provider = CreateProvider(handler);
        var client = provider.GetRequiredService<IScrapeGraphClient>();

        var result = await client.ExtractAsync(
            new ExtractRequest { Url = "https://example.com", Prompt = "secret prompt" },
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("400", result.Error?.Type);
        Assert.Equal("Validation error.", result.Error?.Message);
    }

    [Fact]
    public async Task ExtractAsync_EmptyApiErrorFallsBackToStatusMessage()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage((HttpStatusCode)429)
        {
            Content = new StringContent(string.Empty, Encoding.UTF8, "application/json")
        });
        using var provider = CreateProvider(handler);
        var client = provider.GetRequiredService<IScrapeGraphClient>();

        var result = await client.ExtractAsync(
            new ExtractRequest { Url = "https://example.com", Prompt = "secret prompt" },
            TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal("429", result.Error?.Type);
        Assert.Equal("Rate limit exceeded.", result.Error?.Message);
    }

    [Fact]
    public async Task SuccessResponse_WithMalformedJsonReturnsInvalidResponseError()
    {
        var handler = new RecordingHandler(_ => JsonResponse("{not-json"));
        using var provider = CreateProvider(handler);
        var client = provider.GetRequiredService<IScrapeGraphClient>();

        var result = await client.CreditsAsync(TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal("invalid_response", result.Error?.Type);
        Assert.Equal("The API returned malformed JSON.", result.Error?.Message);
    }

    [Fact]
    public async Task Diagnostics_LogsEndpointWithoutSensitiveValues()
    {
        var loggerProvider = new RecordingLoggerProvider();
        var handler = new RecordingHandler(_ => JsonResponse("""
            {"error":{"type":"validation","message":"Validation failed"}}
            """, HttpStatusCode.BadRequest));
        using var provider = CreateProvider(handler, options => options.ApiKey = "secret-key", loggerProvider);
        var client = provider.GetRequiredService<IScrapeGraphClient>();

        await client.ExtractAsync(new ExtractRequest
        {
            Url = "https://sensitive.example/private",
            Prompt = "secret prompt"
        }, TestContext.Current.CancellationToken);

        var combined = string.Join(
            Environment.NewLine,
            loggerProvider.Entries
                .Where(entry => entry.Category == "ScrapeGraphAI.ScrapeGraphTransport")
                .Select(entry => entry.Message));
        Assert.Contains("extract", combined, StringComparison.Ordinal);
        Assert.Contains("400", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-key", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("secret prompt", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("sensitive.example", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("https://unit.test", combined, StringComparison.Ordinal);
        Assert.Contains(loggerProvider.Entries, entry => entry.EventId == 3 && entry.EventName == "RequestFailed");
    }

    [Fact]
    public async Task Diagnostics_ActivityUsesSafeTags()
    {
        var activities = new List<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == "ScrapeGraphAI",
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => activities.Add(activity)
        };
        ActivitySource.AddActivityListener(listener);

        var handler = new RecordingHandler(_ => JsonResponse("""
            {"remaining":10,"used":1,"plan":"Free","jobs":{}}
            """));
        using var provider = CreateProvider(handler, options => options.ApiKey = "secret-key");
        var client = provider.GetRequiredService<IScrapeGraphClient>();

        await client.CreditsAsync(TestContext.Current.CancellationToken);

        var activity = Assert.Single(activities);
        var tags = activity.TagObjects.ToDictionary(tag => tag.Key, tag => tag.Value?.ToString());
        Assert.Equal("credits", tags["scrapegraphai.endpoint"]);
        Assert.Equal("GET", tags["http.request.method"]);
        Assert.Equal("200", tags["http.response.status_code"]);
        Assert.Equal("True", tags["scrapegraphai.success"]);
        Assert.DoesNotContain(tags, tag => tag.Value?.Contains("secret-key", StringComparison.Ordinal) == true);
        Assert.DoesNotContain(tags, tag => tag.Value?.Contains("unit.test", StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task TopLevelEndpoints_SendDocumentedRoutes()
    {
        var handler = new RecordingHandler(request => JsonResponse(request.RequestUri?.AbsolutePath switch
        {
            "/api/scrape" => """
                {"id":"scrape-id","results":{"markdown":{"data":["# Example"]}},"metadata":{"contentType":"text/html"}}
                """,
            "/api/extract" => """
                {"id":"extract-id","raw":null,"json":{"title":"Example Domain"},"usage":{"promptTokens":1},"metadata":{"fetch":{}}}
                """,
            "/api/search" => """
                {"id":"search-id","results":[{"url":"https://example.com","title":"Example","content":"Example content"}],"json":{"answer":"ok"},"raw":null,"usage":{"promptTokens":1},"metadata":{"pages":{"requested":1,"scraped":1}}}
                """,
            "/api/credits" => """
                {"remaining":750000,"used":287,"plan":"Pro Plan","jobs":{"crawl":{"used":0,"limit":50},"monitor":{"used":0,"limit":100}}}
                """,
            _ => throw new InvalidOperationException($"Unexpected path: {request.RequestUri}")
        }));
        using var provider = CreateProvider(handler, options => options.ApiKey = "test-key");
        var client = provider.GetRequiredService<IScrapeGraphClient>();

        var scrape = await client.ScrapeAsync(new ScrapeRequest
        {
            Url = "https://example.com",
            Formats = [FormatConfig.Markdown()]
        }, TestContext.Current.CancellationToken);
        var extract = await client.ExtractAsync(new ExtractRequest
        {
            Url = "https://example.com",
            Prompt = "Extract the title."
        }, TestContext.Current.CancellationToken);
        var search = await client.SearchAsync(new SearchRequest
        {
            Query = "scrapegraphai pricing",
            NumResults = 1,
            Prompt = "Extract the answer.",
            Format = SearchResultFormat.Markdown
        }, TestContext.Current.CancellationToken);
        var credits = await client.CreditsAsync(TestContext.Current.CancellationToken);

        Assert.True(scrape.IsSuccess);
        Assert.True(extract.IsSuccess);
        Assert.True(search.IsSuccess);
        Assert.Equal("ok", search.Data?.Json?.GetProperty("answer").GetString());
        Assert.Equal(750000, credits.Data?.Remaining);
        Assert.Equal(
            [
                "POST https://unit.test/api/scrape",
                "POST https://unit.test/api/extract",
                "POST https://unit.test/api/search",
                "GET https://unit.test/api/credits"
            ],
            handler.Requests.Select(request => $"{request.Method} {request.Uri}"));
    }

    [Fact]
    public async Task RequestEnums_SerializeDocumentedWireValues()
    {
        var handler = new RecordingHandler(request => JsonResponse(request.RequestUri?.AbsolutePath switch
        {
            "/api/scrape" => """
                {"id":"scrape-id","results":{"markdown":{"data":["# Example"]}},"metadata":{}}
                """,
            "/api/extract" => """
                {"id":"extract-id","raw":null,"json":{"title":"Example"},"metadata":{}}
                """,
            "/api/search" => """
                {"id":"search-id","results":[],"metadata":{}}
                """,
            "/api/crawl" => """
                {"id":"crawl-id","status":"running","total":1,"finished":0,"pages":[]}
                """,
            "/api/monitor" => """
                {"cronId":"cron-id","status":"active","config":{}}
                """,
            _ => throw new InvalidOperationException($"Unexpected path: {request.RequestUri}")
        }));
        using var provider = CreateProvider(handler, options => options.ApiKey = "test-key");
        var client = provider.GetRequiredService<IScrapeGraphClient>();

        await client.ScrapeAsync(new ScrapeRequest
        {
            Url = "https://example.com",
            Formats = [FormatConfig.Markdown(ScrapeContentMode.Reader)],
            FetchConfig = new FetchConfig { Mode = FetchMode.Js }
        }, TestContext.Current.CancellationToken);
        await client.ExtractAsync(new ExtractRequest
        {
            Url = "https://example.com",
            Prompt = "Extract the title.",
            Mode = ScrapeContentMode.Reader
        }, TestContext.Current.CancellationToken);
        await client.SearchAsync(new SearchRequest
        {
            Query = "scrapegraphai",
            Format = SearchResultFormat.Html,
            Mode = ScrapeContentMode.Prune,
            TimeRange = SearchTimeRange.PastWeek,
            LocationGeoCode = SearchLocationGeoCode.Us
        }, TestContext.Current.CancellationToken);
        await client.Crawl.StartAsync(new CrawlRequest
        {
            Url = "https://example.com",
            Formats = [FormatConfig.Markdown()]
        }, TestContext.Current.CancellationToken);
        await client.Monitor.CreateAsync(new MonitorCreateRequest
        {
            Url = "https://example.com",
            Name = "Homepage",
            Interval = "*/30 * * * *",
            Formats = [FormatConfig.Markdown()]
        }, TestContext.Current.CancellationToken);

        Assert.Contains("\"type\":\"markdown\"", handler.Requests[0].Body, StringComparison.Ordinal);
        Assert.Contains("\"mode\":\"reader\"", handler.Requests[0].Body, StringComparison.Ordinal);
        Assert.Contains("\"fetchConfig\":{\"mode\":\"js\"}", handler.Requests[0].Body, StringComparison.Ordinal);
        Assert.Contains("\"mode\":\"reader\"", handler.Requests[1].Body, StringComparison.Ordinal);
        Assert.Contains("\"format\":\"html\"", handler.Requests[2].Body, StringComparison.Ordinal);
        Assert.Contains("\"mode\":\"prune\"", handler.Requests[2].Body, StringComparison.Ordinal);
        Assert.Contains("\"timeRange\":\"past_week\"", handler.Requests[2].Body, StringComparison.Ordinal);
        Assert.Contains("\"locationGeoCode\":\"us\"", handler.Requests[2].Body, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"markdown\"", handler.Requests[3].Body, StringComparison.Ordinal);
        Assert.Contains("\"type\":\"markdown\"", handler.Requests[4].Body, StringComparison.Ordinal);
    }

    [Fact]
    public void RequestEnums_DeserializeDocumentedWireValues()
    {
        Assert.Equal(ScrapeFormatType.Markdown, JsonSerializer.Deserialize<ScrapeFormatType?>("\"markdown\""));
        Assert.Equal(ScrapeContentMode.Reader, JsonSerializer.Deserialize<ScrapeContentMode?>("\"reader\""));
        Assert.Equal(SearchTimeRange.PastWeek, JsonSerializer.Deserialize<SearchTimeRange?>("\"past_week\""));
        Assert.Equal(SearchLocationGeoCode.Us, JsonSerializer.Deserialize<SearchLocationGeoCode?>("\"us\""));
    }

    [Fact]
    public void RequestEnums_DeserializeUnknownWireValueThrows()
        => Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ScrapeFormatType?>("\"not_a_format\""));

    [Fact]
    public async Task CrawlResource_SendDocumentedRoutesAndDeserializesResolvedPages()
    {
        var handler = new RecordingHandler(request => JsonResponse(request.RequestUri?.AbsolutePath.EndsWith("/pages", StringComparison.Ordinal) == true
            ? """
                {"pages":[{"url":"https://example.com","depth":0,"title":"Example","status":"completed","parentUrl":null,"contentType":"text/html","links":["https://iana.org/domains/example"],"scrapeRefId":"scrape-id","results":{"markdown":{"data":["# Example"]}},"metadata":{"contentType":"text/html"},"elapsedMs":42}],"nextCursor":"next"}
                """
            : """
                {"id":"crawl-id","status":"running","total":3,"finished":1,"pages":[{"url":"https://example.com","depth":0,"status":"completed","scrapeRefId":"scrape-id"}]}
                """));
        using var provider = CreateProvider(handler, options => options.ApiKey = "test-key");
        var client = provider.GetRequiredService<IScrapeGraphClient>();

        await client.Crawl.StartAsync(new CrawlRequest
        {
            Url = "https://example.com",
            Formats = [FormatConfig.Markdown()],
            MaxPages = 5,
            MaxDepth = 2
        }, TestContext.Current.CancellationToken);
        await client.Crawl.GetAsync("crawl id", TestContext.Current.CancellationToken);
        var pages = await client.Crawl.PagesAsync("crawl id", "next cursor", 10, TestContext.Current.CancellationToken);
        await client.Crawl.StopAsync("crawl id", TestContext.Current.CancellationToken);
        await client.Crawl.ResumeAsync("crawl id", TestContext.Current.CancellationToken);
        await client.Crawl.DeleteAsync("crawl id", TestContext.Current.CancellationToken);

        Assert.True(pages.IsSuccess);
        Assert.Equal("next", pages.Data?.NextCursor);
        Assert.Equal("# Example", pages.Data?.Pages[0].Results?["markdown"].Data[0].GetString());
        Assert.Equal(
            [
                "POST https://unit.test/api/crawl",
                "GET https://unit.test/api/crawl/crawl%20id",
                "GET https://unit.test/api/crawl/crawl%20id/pages?cursor=next%20cursor&limit=10",
                "POST https://unit.test/api/crawl/crawl%20id/stop",
                "POST https://unit.test/api/crawl/crawl%20id/resume",
                "DELETE https://unit.test/api/crawl/crawl%20id"
            ],
            handler.Requests.Select(request => $"{request.Method} {request.Uri}"));
    }

    [Fact]
    public async Task MonitorResource_SendsDocumentedRoutes()
    {
        var handler = new RecordingHandler(request => JsonResponse(request.RequestUri?.AbsolutePath switch
        {
            "/api/monitor" when request.Method == HttpMethod.Get => """
                [{"cronId":"cron-id","status":"active","config":{"url":"https://example.com"},"createdAt":"2026-04-23T11:11:37.487Z"}]
                """,
            "/api/monitor/cron%20id/activity" => """
                {"ticks":[{"id":"tick-id","status":"completed","createdAt":"2026-04-23T11:11:37.619Z","elapsedMs":14,"changed":true,"diffs":{}}],"nextCursor":null}
                """,
            _ => """
                {"cronId":"cron-id","scheduleId":"schedule-id","interval":"*/30 * * * *","status":"active","config":{"url":"https://example.com","name":"Homepage watch"},"createdAt":"2026-04-23T11:11:37.487Z","updatedAt":"2026-04-23T11:11:37.487Z"}
                """
        }));
        using var provider = CreateProvider(handler, options => options.ApiKey = "test-key");
        var client = provider.GetRequiredService<IScrapeGraphClient>();

        var created = await client.Monitor.CreateAsync(new MonitorCreateRequest
        {
            Url = "https://example.com",
            Name = "Homepage watch",
            Interval = "*/30 * * * *",
            Formats = [FormatConfig.Markdown()]
        }, TestContext.Current.CancellationToken);
        await client.Monitor.ListAsync(TestContext.Current.CancellationToken);
        await client.Monitor.GetAsync("cron id", TestContext.Current.CancellationToken);
        await client.Monitor.UpdateAsync("cron id", new MonitorUpdateRequest { Interval = "0 */6 * * *" }, TestContext.Current.CancellationToken);
        await client.Monitor.PauseAsync("cron id", TestContext.Current.CancellationToken);
        await client.Monitor.ResumeAsync("cron id", TestContext.Current.CancellationToken);
        await client.Monitor.ActivityAsync("cron id", 20, "older cursor", TestContext.Current.CancellationToken);
        await client.Monitor.DeleteAsync("cron id", TestContext.Current.CancellationToken);

        Assert.True(created.IsSuccess);
        Assert.Equal("schedule-id", created.Data?.ScheduleId);
        Assert.Equal(
            [
                "POST https://unit.test/api/monitor",
                "GET https://unit.test/api/monitor",
                "GET https://unit.test/api/monitor/cron%20id",
                "PATCH https://unit.test/api/monitor/cron%20id",
                "POST https://unit.test/api/monitor/cron%20id/pause",
                "POST https://unit.test/api/monitor/cron%20id/resume",
                "GET https://unit.test/api/monitor/cron%20id/activity?limit=20&cursor=older%20cursor",
                "DELETE https://unit.test/api/monitor/cron%20id"
            ],
            handler.Requests.Select(request => $"{request.Method} {request.Uri}"));
    }

    [Fact]
    public async Task HistoryResource_SendsDocumentedRoutes()
    {
        var handler = new RecordingHandler(request => JsonResponse(request.RequestUri?.AbsolutePath.EndsWith("/history", StringComparison.Ordinal) == true
            ? """
                {"data":[{"id":"history-id","userId":"user-id","service":"scrape","status":"completed","params":{"url":"https://example.com"},"result":{"results":{"markdown":{"data":["# Example"]}}},"error":null,"elapsedMs":533,"requestParentId":"parent-id","createdAt":"2026-04-28T09:00:02.907Z"}],"pagination":{"page":2,"limit":5,"total":178}}
                """
            : """
                {"id":"history-id","userId":"user-id","service":"scrape","status":"completed","params":{"url":"https://example.com"},"result":{"results":{"markdown":{"data":["# Example"]}}},"error":null,"elapsedMs":533,"requestParentId":"parent-id","createdAt":"2026-04-28T09:00:02.907Z"}
                """));
        using var provider = CreateProvider(handler, options => options.ApiKey = "test-key");
        var client = provider.GetRequiredService<IScrapeGraphClient>();

        var list = await client.History.ListAsync(2, 5, HistoryService.Scrape, TestContext.Current.CancellationToken);
        var entry = await client.History.GetAsync("history id", TestContext.Current.CancellationToken);

        Assert.True(list.IsSuccess);
        Assert.Equal(178, list.Data?.Pagination?.Total);
        Assert.Equal("history-id", entry.Data?.Id);
        Assert.Equal(
            [
                "GET https://unit.test/api/history?page=2&limit=5&service=scrape",
                "GET https://unit.test/api/history/history%20id"
            ],
            handler.Requests.Select(request => $"{request.Method} {request.Uri}"));
    }

    [Fact]
    public async Task AddScrapeGraphAI_RegistersTypedClientWithFactory()
    {
        var handler = new RecordingHandler(_ => JsonResponse("""
            {"remaining":10,"used":1,"plan":"Free","jobs":{"crawl":{"used":0,"limit":1}}}
            """));
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScrapeGraphAI(options =>
        {
            options.ApiKey = "di-key";
            options.BaseUrl = new Uri("https://unit.test/api");
        })
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IScrapeGraphClient>();

        var result = await client.CreditsAsync(TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://unit.test/api/credits", handler.LastRequest?.RequestUri?.ToString());
        Assert.Contains("SGAI-APIKEY", handler.LastRequest?.Headers.Select(h => h.Key) ?? []);
    }

    [Fact]
    public async Task AddScrapeGraphAI_DefaultResiliencePipelineResolves()
    {
        var handler = new RecordingHandler(_ => JsonResponse("""
            {"status":"ok","uptime":123456789}
            """));
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScrapeGraphAI(options =>
        {
            options.ApiKey = "di-key";
            options.BaseUrl = new Uri("https://unit.test/api");
        })
            .AddScrapeGraphAIStandardResilience(options =>
            {
                options.TotalRequestTimeout = TimeSpan.FromSeconds(90);
                options.AttemptTimeout = TimeSpan.FromSeconds(30);
            })
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IScrapeGraphClient>();

        var result = await client.HealthAsync(TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://unit.test/api/health", handler.LastRequest?.RequestUri?.ToString());
    }

    [Fact]
    public async Task AddScrapeGraphAI_UsesRegisteredOptions()
    {
        var handler = new RecordingHandler(_ => JsonResponse("""
            {"remaining":10,"used":1,"plan":"Free","jobs":{}}
            """));
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<ScrapeGraphOptions>(options =>
        {
            options.ApiKey = "configured-key";
            options.BaseUrl = new Uri("https://unit.test/configured-api");
        });
        services.AddScrapeGraphAI()
            .ConfigurePrimaryHttpMessageHandler(() => handler);

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IScrapeGraphClient>();

        var result = await client.CreditsAsync(TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://unit.test/configured-api/credits", handler.LastRequest?.RequestUri?.ToString());
        Assert.Contains("SGAI-APIKEY", handler.LastRequest?.Headers.Select(header => header.Key) ?? []);
    }

    [Fact]
    public async Task AgentTools_TruncateResults()
    {
        var fakeClient = new FakeScrapeGraphClient();
        var tools = new ScrapeGraphAgentTools(fakeClient, Options.Create(new ScrapeGraphAgentToolOptions { MaxResultCharacters = 40 }));

        var output = await tools.HealthCheckAsync(TestContext.Current.CancellationToken);

        Assert.True(output.Length <= 43);
        Assert.EndsWith("...", output);
    }

    [Fact]
    public async Task AgentTools_TruncateLargeResults()
    {
        var fakeClient = new FakeScrapeGraphClient { SearchContent = new string('x', 50_000) };
        var tools = new ScrapeGraphAgentTools(fakeClient, Options.Create(new ScrapeGraphAgentToolOptions { MaxResultCharacters = 128 }));

        var output = await tools.SearchWebAsync("scrapegraphai", cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(output.Length <= 131);
        Assert.EndsWith("...", output);
    }

    [Fact]
    public void AgentTools_ReturnToolSet()
    {
        var fakeClient = new FakeScrapeGraphClient();
        var tools = new ScrapeGraphAgentTools(fakeClient, Options.Create(new ScrapeGraphAgentToolOptions()));

        Assert.True(tools.AsAITools().Count() >= 20);
    }

    [Fact]
    public void AgentTools_CanSelectToolSubsetWithOverload()
    {
        var fakeClient = new FakeScrapeGraphClient();
        var tools = new ScrapeGraphAgentTools(fakeClient, Options.Create(new ScrapeGraphAgentToolOptions()));

        var selected = tools.AsAITools(
            ScrapeGraphAgentToolNames.ScrapePage,
            ScrapeGraphAgentToolNames.GetCredits,
            ScrapeGraphAgentToolNames.HealthCheck).ToArray();

        Assert.Equal(
            [ScrapeGraphAgentToolNames.ScrapePage, ScrapeGraphAgentToolNames.GetCredits, ScrapeGraphAgentToolNames.HealthCheck],
            selected.Select(tool => tool.Name));
    }

    [Fact]
    public void AgentTools_CanSelectToolSubsetWithOptions()
    {
        var fakeClient = new FakeScrapeGraphClient();
        var tools = new ScrapeGraphAgentTools(fakeClient, Options.Create(new ScrapeGraphAgentToolOptions
        {
            IncludedTools =
            [
                ScrapeGraphAgentToolNames.SearchWeb,
                ScrapeGraphAgentToolNames.HealthCheck
            ]
        }));

        var selected = tools.AsAITools().ToArray();

        Assert.Equal([ScrapeGraphAgentToolNames.SearchWeb, ScrapeGraphAgentToolNames.HealthCheck], selected.Select(tool => tool.Name));
    }

    [Fact]
    public void AgentTools_RejectUnknownToolNames()
    {
        var fakeClient = new FakeScrapeGraphClient();
        var tools = new ScrapeGraphAgentTools(fakeClient, Options.Create(new ScrapeGraphAgentToolOptions()));

        var exception = Assert.Throws<ArgumentException>(() => tools.AsAITools("not_a_tool").ToArray());

        Assert.Contains("not_a_tool", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AgentTools_RejectUnknownIncludedToolNames()
    {
        var fakeClient = new FakeScrapeGraphClient();
        var tools = new ScrapeGraphAgentTools(fakeClient, Options.Create(new ScrapeGraphAgentToolOptions
        {
            IncludedTools = ["not_a_tool"]
        }));

        var exception = Assert.Throws<ArgumentException>(() => tools.AsAITools().ToArray());

        Assert.Contains("not_a_tool", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AgentTools_RejectUnknownApprovalToolNames()
    {
        var fakeClient = new FakeScrapeGraphClient();
        var tools = new ScrapeGraphAgentTools(fakeClient, Options.Create(new ScrapeGraphAgentToolOptions
        {
            ApprovalRequiredTools = ["not_a_tool"]
        }));

        var exception = Assert.Throws<ArgumentException>(() => tools.AsAITools().ToArray());

        Assert.Contains("not_a_tool", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AgentTools_WrapsOnlyApprovalRequiredTools()
    {
        var fakeClient = new FakeScrapeGraphClient();
        var tools = new ScrapeGraphAgentTools(fakeClient, Options.Create(new ScrapeGraphAgentToolOptions
        {
            ApprovalRequiredTools =
            [
                ScrapeGraphAgentToolNames.SearchWeb
            ]
        }));

        var selected = tools.AsAITools(
            ScrapeGraphAgentToolNames.SearchWeb,
            ScrapeGraphAgentToolNames.HealthCheck).ToArray();

        Assert.Equal("ApprovalRequiredAIFunction", selected[0].GetType().Name);
        Assert.NotEqual("ApprovalRequiredAIFunction", selected[1].GetType().Name);
    }

    [Fact]
    public void AgentTools_IgnoresApprovalForUnselectedTools()
    {
        var fakeClient = new FakeScrapeGraphClient();
        var tools = new ScrapeGraphAgentTools(fakeClient, Options.Create(new ScrapeGraphAgentToolOptions
        {
            IncludedTools =
            [
                ScrapeGraphAgentToolNames.HealthCheck
            ],
            ApprovalRequiredTools =
            [
                ScrapeGraphAgentToolNames.SearchWeb
            ]
        }));

        var selected = tools.AsAITools().ToArray();

        var tool = Assert.Single(selected);
        Assert.Equal(ScrapeGraphAgentToolNames.HealthCheck, tool.Name);
        Assert.NotEqual("ApprovalRequiredAIFunction", tool.GetType().Name);
    }

    [Fact]
    public void AgentTools_UsesExternallyConfiguredOptions()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IScrapeGraphClient>(new FakeScrapeGraphClient());
        services.Configure<ScrapeGraphAgentToolOptions>(options =>
        {
            options.IncludedTools =
            [
                ScrapeGraphAgentToolNames.HealthCheck
            ];
        });
        services.AddScrapeGraphAgentTools();

        using var provider = services.BuildServiceProvider();
        var tools = provider.GetRequiredService<ScrapeGraphAgentTools>().AsAITools().ToArray();

        Assert.Single(tools);
        Assert.Equal(ScrapeGraphAgentToolNames.HealthCheck, tools[0].Name);
    }

    [Fact]
    public void Samples_DoNotContainRealLookingApiKey()
    {
        var sampleSource = File.ReadAllText(Path.Combine(GetRepositoryRoot(), "samples", "ScrapeGraphAI.CoreSample", "Program.cs"));

        Assert.DoesNotContain("sgai-", sampleSource, StringComparison.OrdinalIgnoreCase);
    }

    private static ServiceProvider CreateProvider(
        RecordingHandler handler,
        Action<ScrapeGraphOptions>? configure = null,
        ILoggerProvider? loggerProvider = null,
        Action<HttpClient>? configureHttpClient = null)
    {
        var services = new ServiceCollection();
        services.AddLogging(logging =>
        {
            if (loggerProvider is not null)
            {
                logging.AddProvider(loggerProvider);
            }
        });

        services.AddScrapeGraphAI(options =>
        {
            options.ApiKey = "test-key";
            options.BaseUrl = new Uri("https://unit.test/api");
            configure?.Invoke(options);
        })
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .ConfigureHttpClient(client => configureHttpClient?.Invoke(client));

        return services.BuildServiceProvider();
    }

    private static HttpResponseMessage JsonResponse(string json, HttpStatusCode statusCode = HttpStatusCode.OK)
        => new(statusCode) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static string GetRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Directory.Build.props")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastBody { get; private set; }
        public Dictionary<string, string[]> LastHeaders { get; private set; } = [];
        public List<RecordedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.LastRequest = request;
            this.LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            this.LastHeaders = request.Headers.ToDictionary(header => header.Key, header => header.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
            this.Requests.Add(new RecordedRequest(request.Method.Method, request.RequestUri?.AbsoluteUri ?? string.Empty, this.LastBody));
            return responder(request);
        }
    }

    private sealed record RecordedRequest(string Method, string Uri, string? Body);

    private sealed record LogEntry(string Category, LogLevel Level, int EventId, string? EventName, string Message);

    private sealed class RecordingLoggerProvider : ILoggerProvider
    {
        public List<LogEntry> Entries { get; } = [];

        public ILogger CreateLogger(string categoryName)
            => new RecordingLogger(categoryName, this.Entries);

        public void Dispose()
        {
        }
    }

    private sealed class RecordingLogger(string categoryName, List<LogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel)
            => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => entries.Add(new LogEntry(categoryName, logLevel, eventId.Id, eventId.Name, formatter(state, exception)));
    }

    private sealed class FakeScrapeGraphClient : IScrapeGraphClient
    {
        public string? SearchContent { get; init; }

        public ICrawlResource Crawl { get; } = new FakeCrawlResource();
        public IMonitorResource Monitor { get; } = new FakeMonitorResource();
        public IHistoryResource History { get; } = new FakeHistoryResource();

        public Task<ApiResult<ScrapeResponse>> ScrapeAsync(ScrapeRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(ApiResult<ScrapeResponse>.Success(new ScrapeResponse("id", new Dictionary<string, FormatResult>(), null), 1, HttpStatusCode.OK));

        public Task<ApiResult<ExtractResponse>> ExtractAsync(ExtractRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(ApiResult<ExtractResponse>.Failure(new ScrapeGraphError("not_used", "not used"), 1));

        public Task<ApiResult<SearchResponse>> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default)
        {
            if (this.SearchContent is not null)
            {
                var response = new SearchResponse(
                    "search-id",
                    [new SearchResult("https://example.com", "Example", this.SearchContent)],
                    null,
                    null);
                return Task.FromResult(ApiResult<SearchResponse>.Success(response, 1, HttpStatusCode.OK));
            }

            return Task.FromResult(ApiResult<SearchResponse>.Failure(new ScrapeGraphError("not_used", "not used"), 1));
        }

        public Task<ApiResult<CreditsResponse>> CreditsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(ApiResult<CreditsResponse>.Success(new CreditsResponse(1, 0, "Free", null), 1, HttpStatusCode.OK));

        public Task<ApiResult<HealthResponse>> HealthAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(ApiResult<HealthResponse>.Success(new HealthResponse("ok", 123456789), 1, HttpStatusCode.OK));
    }

    private sealed class FakeCrawlResource : ICrawlResource
    {
        public Task<ApiResult<CrawlResponse>> StartAsync(CrawlRequest request, CancellationToken cancellationToken = default) => NotUsed<CrawlResponse>();
        public Task<ApiResult<CrawlResponse>> GetAsync(string id, CancellationToken cancellationToken = default) => NotUsed<CrawlResponse>();
        public Task<ApiResult<CrawlPagesResponse>> PagesAsync(string id, string? cursor = null, int? limit = null, CancellationToken cancellationToken = default) => NotUsed<CrawlPagesResponse>();
        public Task<ApiResult<object>> StopAsync(string id, CancellationToken cancellationToken = default) => NotUsed<object>();
        public Task<ApiResult<object>> ResumeAsync(string id, CancellationToken cancellationToken = default) => NotUsed<object>();
        public Task<ApiResult<object>> DeleteAsync(string id, CancellationToken cancellationToken = default) => NotUsed<object>();
    }

    private sealed class FakeMonitorResource : IMonitorResource
    {
        public Task<ApiResult<MonitorResponse>> CreateAsync(MonitorCreateRequest request, CancellationToken cancellationToken = default) => NotUsed<MonitorResponse>();
        public Task<ApiResult<IReadOnlyList<MonitorResponse>>> ListAsync(CancellationToken cancellationToken = default) => NotUsed<IReadOnlyList<MonitorResponse>>();
        public Task<ApiResult<MonitorResponse>> GetAsync(string cronId, CancellationToken cancellationToken = default) => NotUsed<MonitorResponse>();
        public Task<ApiResult<MonitorResponse>> UpdateAsync(string cronId, MonitorUpdateRequest request, CancellationToken cancellationToken = default) => NotUsed<MonitorResponse>();
        public Task<ApiResult<MonitorResponse>> PauseAsync(string cronId, CancellationToken cancellationToken = default) => NotUsed<MonitorResponse>();
        public Task<ApiResult<MonitorResponse>> ResumeAsync(string cronId, CancellationToken cancellationToken = default) => NotUsed<MonitorResponse>();
        public Task<ApiResult<object>> DeleteAsync(string cronId, CancellationToken cancellationToken = default) => NotUsed<object>();
        public Task<ApiResult<MonitorActivityResponse>> ActivityAsync(string cronId, int? limit = null, string? cursor = null, CancellationToken cancellationToken = default) => NotUsed<MonitorActivityResponse>();
    }

    private sealed class FakeHistoryResource : IHistoryResource
    {
        public Task<ApiResult<HistoryPage>> ListAsync(int? page = null, int? limit = null, HistoryService? service = null, CancellationToken cancellationToken = default) => NotUsed<HistoryPage>();
        public Task<ApiResult<HistoryEntry>> GetAsync(string id, CancellationToken cancellationToken = default) => NotUsed<HistoryEntry>();
    }

    private static Task<ApiResult<T>> NotUsed<T>()
        => Task.FromResult(ApiResult<T>.Failure(new ScrapeGraphError("not_used", "not used"), 1));
}

