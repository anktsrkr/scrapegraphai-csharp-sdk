# ScrapeGraphAI Core Sample

This sample is the fastest way to see the core SDK in a real .NET application. It registers the typed client through dependency injection, binds configuration, enables the standard resilience pipeline, and calls the main ScrapeGraphAI API groups.

## What It Demonstrates

- `AddScrapeGraphAI(...)` typed `HttpClient` registration
- Configuration binding from `SGAI_API_KEY` into `ScrapeGraphOptions`
- Optional retry and timeout resilience
- Health and credit checks
- Scrape, extract, and search requests
- Crawl lifecycle calls
- Monitor lifecycle calls
- History listing and lookup
- Ctrl+C cancellation
- Structured JSON output for responses and errors

The sample creates temporary crawl and monitor resources and attempts to clean them up before it exits.

## Prerequisites

- .NET 10
- A ScrapeGraphAI API key

## Run

From the repository root:

```powershell
$env:SGAI_API_KEY = "<your-api-key>"
dotnet run --project samples/ScrapeGraphAI.CoreSample -- https://example.com
```

If no URL is provided, the sample uses the ScrapeGraphAI API documentation URL.

## Enable SDK Logs

```powershell
dotnet run --project samples/ScrapeGraphAI.CoreSample -- https://example.com --debug
```

Debug logging prints SDK request diagnostics. It does not log API keys, request bodies, prompts, schemas, or scraped content.

## Configuration

The sample maps:

```text
SGAI_API_KEY -> ScrapeGraphAI:ApiKey
```

It also configures:

```text
ScrapeGraphAI:Resilience:TotalRequestTimeout = 00:01:30
ScrapeGraphAI:Resilience:AttemptTimeout = 00:00:30
ScrapeGraphAI:Resilience:MaxRetryAttempts = 3
ScrapeGraphAI:Resilience:RetryBackoff = 00:00:02
```

The SDK itself does not read environment variables directly. Environment variables are mapped by the sample's .NET configuration setup.

## Useful Edits

- Change the target URL by passing a different first argument.
- Change `FormatConfig.Markdown(ScrapeContentMode.Reader)` to `FormatConfig.Html()`, `FormatConfig.Links()`, `FormatConfig.Images()`, or `FormatConfig.Screenshot()`.
- Change the `ExtractRequest.Schema` JSON schema to match your desired output.
- Change the monitor cron expression for a different recurring schedule.
- Tune `ScrapeGraphAI:Resilience` values for your application's timeout and retry policy.

## Notes

The sample spaces API requests by a few seconds to be polite to rate limits while demonstrating many endpoints in one run.
