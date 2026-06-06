# ScrapeGraphAI Core Sample

This sample shows direct use of the SDK typed client through dependency injection.

It demonstrates:

- `AddScrapeGraphAI(...)` typed `HttpClient` registration.
- App-level configuration binding from `SGAI_API_KEY` into `ScrapeGraphOptions`.
- Opt-in standard resilience from the SDK.
- Top-level API calls: `HealthAsync`, `CreditsAsync`, `ScrapeAsync`, `ExtractAsync`, and `SearchAsync`.
- Crawl calls: `Crawl.StartAsync`, `Crawl.GetAsync`, `Crawl.PagesAsync`, `Crawl.StopAsync`, `Crawl.ResumeAsync`, and `Crawl.DeleteAsync`.
- Monitor calls: `Monitor.CreateAsync`, `Monitor.ListAsync`, `Monitor.GetAsync`, `Monitor.UpdateAsync`, `Monitor.PauseAsync`, `Monitor.ResumeAsync`, `Monitor.ActivityAsync`, and `Monitor.DeleteAsync`.
- History calls: `History.ListAsync` and `History.GetAsync`.
- Request cancellation with Ctrl+C.
- Preserving structured JSON extraction output.

The sample creates temporary crawl and monitor resources and attempts to clean them up before exiting.

## Run

```powershell
$env:SGAI_API_KEY = "<your-api-key>"
dotnet run --project samples/ScrapeGraphAI.CoreSample -- https://example.com
```

The sample maps `SGAI_API_KEY` to `ScrapeGraphAI:ApiKey`, binds `ScrapeGraphAI` with `services.Configure<ScrapeGraphOptions>(...)`, and binds `ScrapeGraphAI:Resilience` with `services.Configure<ScrapeGraphResilienceOptions>(...)`. The SDK itself does not read environment variables directly.

Enable SDK request logging:

```powershell
dotnet run --project samples/ScrapeGraphAI.CoreSample -- https://example.com --debug
```

## What To Change

- Change `targetUrl` by passing a different URL as the first argument.
- Change `FormatConfig.Markdown(ScrapeContentMode.Reader)` to `FormatConfig.Html()`, `FormatConfig.Links()`, `FormatConfig.Images()`, or `FormatConfig.Screenshot()`.
- Change the `ExtractRequest.Schema` JSON schema to match your desired output.
- Change the monitor cron expression if you want a different recurring schedule.
- Tune `ScrapeGraphAI:Resilience` configuration for `TotalRequestTimeout`, `AttemptTimeout`, `MaxRetryAttempts`, and `RetryBackoff`.
