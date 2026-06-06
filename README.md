# ScrapeGraphAI .NET SDK

Typed .NET SDK for the ScrapeGraphAI v2 API.

The SDK is built around `IHttpClientFactory`, dependency injection, typed request and response models, optional resilience policies, structured diagnostics, and optional Microsoft Agent Framework tool integration.

## Packages

```powershell
dotnet add package ScrapeGraphAI.Client
dotnet add package ScrapeGraphAI.AgentFramework
```

Preview packages are available with:

```powershell
dotnet add package ScrapeGraphAI.Client --prerelease
dotnet add package ScrapeGraphAI.AgentFramework --prerelease
```

## Requirements

- .NET 10
- A ScrapeGraphAI API key

## Quick Start

Register the typed client:

```csharp
using ScrapeGraphAI;

builder.Services.AddScrapeGraphAI(options =>
{
    options.ApiKey = builder.Configuration["ScrapeGraphAI:ApiKey"];
});
```

Call the API:

```csharp
var client = serviceProvider.GetRequiredService<IScrapeGraphClient>();

var result = await client.ScrapeAsync(new ScrapeRequest
{
    Url = "https://example.com",
    Formats = [FormatConfig.Markdown(ScrapeContentMode.Reader)]
});

if (result.IsSuccess)
{
    Console.WriteLine(result.Data?.Results["markdown"].Data);
}
else
{
    Console.Error.WriteLine(result.Error?.Message);
}
```

## Configuration

`AddScrapeGraphAI` registers `IScrapeGraphClient` as a typed `HttpClient`, configures the ScrapeGraphAI base URL, sets the `SGAI-APIKEY` header, and adds a SDK user agent.

The default base URL is:

```text
https://v2-api.scrapegraphai.com/api/
```

You can bind configuration through the normal .NET options pipeline:

```csharp
builder.Services.Configure<ScrapeGraphOptions>(
    builder.Configuration.GetSection("ScrapeGraphAI"));

builder.Services.AddScrapeGraphAI();
```

Example environment variable for typical .NET configuration providers:

```powershell
$env:ScrapeGraphAI__ApiKey = "<your-api-key>"
```

`ScrapeGraphOptions` contains:

- `ApiKey`
- `BaseUrl`

The SDK validates options when the typed client is resolved. `ApiKey` must be non-empty, and `BaseUrl` must be an absolute HTTP or HTTPS URI.

## HttpClient Customization

`AddScrapeGraphAI` returns `IHttpClientBuilder`, so you can use normal `HttpClient` customization:

```csharp
builder.Services
    .AddScrapeGraphAI()
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(90);
    });
```

## Resilience

The SDK includes an opt-in retry and timeout pipeline built on `Microsoft.Extensions.Http.Resilience`:

```csharp
builder.Services
    .AddScrapeGraphAI()
    .AddScrapeGraphAIStandardResilience();
```

You can configure resilience separately from API identity settings:

```csharp
builder.Services.Configure<ScrapeGraphResilienceOptions>(
    builder.Configuration.GetSection("ScrapeGraphAI:Resilience"));
```

`ScrapeGraphResilienceOptions` contains:

- `TotalRequestTimeout`
- `AttemptTimeout`
- `MaxRetryAttempts`
- `RetryBackoff`

Resilience options are validated when the pipeline is used. Timeouts and backoff must be positive, `AttemptTimeout` cannot exceed `TotalRequestTimeout`, and `MaxRetryAttempts` must be greater than zero.

## API Surface

The core client exposes:

- `HealthAsync`
- `CreditsAsync`
- `ScrapeAsync`
- `ExtractAsync`
- `SearchAsync`
- `Crawl.StartAsync`, `Crawl.GetAsync`, `Crawl.PagesAsync`, `Crawl.StopAsync`, `Crawl.ResumeAsync`, `Crawl.DeleteAsync`
- `Monitor.CreateAsync`, `Monitor.ListAsync`, `Monitor.GetAsync`, `Monitor.UpdateAsync`, `Monitor.PauseAsync`, `Monitor.ResumeAsync`, `Monitor.ActivityAsync`, `Monitor.DeleteAsync`
- `History.ListAsync`, `History.GetAsync`

All API calls return `ApiResult<T>`, so callers can handle successful responses and API errors without relying on exceptions for normal error payloads.

## Agent Framework Tools

Agent integration lives in `ScrapeGraphAI.AgentFramework`, keeping the core SDK free of agent dependencies.

```csharp
using ScrapeGraphAI.AgentFramework;

builder.Services.AddScrapeGraphAI();
builder.Services.AddScrapeGraphAgentTools(options =>
{
    options.IncludedTools =
    [
        ScrapeGraphAgentToolNames.ScrapePage,
        ScrapeGraphAgentToolNames.ExtractFromPage,
        ScrapeGraphAgentToolNames.GetCredits,
        ScrapeGraphAgentToolNames.HealthCheck
    ];

    options.ApprovalRequiredTools =
    [
        ScrapeGraphAgentToolNames.ScrapePage,
        ScrapeGraphAgentToolNames.ExtractFromPage
    ];
});

var tools = serviceProvider
    .GetRequiredService<ScrapeGraphAgentTools>()
    .AsAITools();
```

Attach the returned tools to a Microsoft Agent Framework agent, or choose a smaller tool set per agent:

```csharp
var researchTools = serviceProvider
    .GetRequiredService<ScrapeGraphAgentTools>()
    .AsAITools(
        ScrapeGraphAgentToolNames.ScrapePage,
        ScrapeGraphAgentToolNames.SearchWeb,
        ScrapeGraphAgentToolNames.HealthCheck);
```

## Diagnostics

The SDK emits structured logs through `ILogger` and client spans through an `ActivitySource` named `ScrapeGraphAI`.

Diagnostics are designed to be safe by default. They include endpoint names, HTTP methods, status codes, and elapsed time. They do not log API keys, prompts, schemas, request bodies, response bodies, cookies, headers, or scraped content.

## Samples

- [Core SDK sample](samples/ScrapeGraphAI.CoreSample/README.md): dependency injection, configuration, resilience, scrape, extract, search, crawl, monitor, and history APIs.
- [Agent Framework sample](samples/ScrapeGraphAI.AgentFrameworkSample/README.md): registers ScrapeGraphAI tools and runs them from a Microsoft Agent Framework agent through an OpenAI-compatible chat endpoint.

## Releases

Stable packages are published to NuGet from GitHub releases. Preview packages are published from the `main` branch and can be installed with `--prerelease`.

Versions follow semantic versioning. Patch releases include fixes, minor releases add features, and major releases may include breaking changes.
