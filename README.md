# ScrapeGraphAI .NET SDK

Typed `net10.0` SDK for the ScrapeGraphAI v2 API with `IHttpClientFactory` and `Microsoft.Extensions.Http.Resilience`.

## Packages

```powershell
dotnet add package ScrapeGraphAI.Client
dotnet add package ScrapeGraphAI.AgentFramework
```

Preview builds are published automatically from `main` and can be installed with `--prerelease`.

## Register The Typed Client

```csharp
using ScrapeGraphAI;

builder.Services.AddScrapeGraphAI(options =>
{
    options.ApiKey = builder.Configuration["ScrapeGraphAI:ApiKey"];
});
```

`AddScrapeGraphAI` registers `IScrapeGraphClient` as a typed `HttpClient`, sets the `SGAI-APIKEY` header, configures the base URL, and adds a user agent.
The SDK validates configuration when the typed client is resolved; `ApiKey` must be non-empty and `BaseUrl` must be an absolute HTTP or HTTPS URI.

You can also use the normal .NET options pipeline:

```csharp
builder.Services.Configure<ScrapeGraphOptions>(
    builder.Configuration.GetSection("ScrapeGraphAI"));

builder.Services.AddScrapeGraphAI();
```

Environment variables work through your host configuration, for example `ScrapeGraphAI__ApiKey`. The SDK does not read environment variables directly.

`AddScrapeGraphAI` returns `IHttpClientBuilder`, so advanced callers can chain normal `HttpClient` customization:

```csharp
builder.Services
    .AddScrapeGraphAI()
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler());
```

To add the SDK's standard retry/timeout resilience pipeline:

```csharp
builder.Services.Configure<ScrapeGraphResilienceOptions>(
    builder.Configuration.GetSection("ScrapeGraphAI:Resilience"));

builder.Services
    .AddScrapeGraphAI()
    .AddScrapeGraphAIStandardResilience();
```

`ScrapeGraphOptions` is limited to SDK identity and endpoint settings. Retry and timeout policy lives in `ScrapeGraphResilienceOptions`, and plain `HttpClient` settings can be configured through the returned `IHttpClientBuilder`.

The SDK emits safe structured diagnostics through `ILogger` and an `ActivitySource` named `ScrapeGraphAI`. It logs endpoint names, methods, status codes, and elapsed time only; it does not log API keys, request bodies, prompts, schemas, headers, cookies, or scraped content.

## Scrape

```csharp
var sgai = serviceProvider.GetRequiredService<IScrapeGraphClient>();

var result = await sgai.ScrapeAsync(new ScrapeRequest
{
    Url = "https://example.com",
    Formats = [FormatConfig.Markdown(ScrapeContentMode.Reader)]
});

if (result.IsSuccess)
{
    Console.WriteLine(result.Data?.Results["markdown"].Data);
}
```

## Agent Framework Tools

Agent Framework integration lives in the separate `ScrapeGraphAI.AgentFramework` package so core SDK consumers do not pull agent dependencies.

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

var tools = serviceProvider.GetRequiredService<ScrapeGraphAgentTools>().AsAITools();
```

Attach `tools` to `ChatClientAgentOptions.Tools` or pass them to `AsAIAgent(...)`.

You can also choose tools per agent:

```csharp
var researchTools = serviceProvider
    .GetRequiredService<ScrapeGraphAgentTools>()
    .AsAITools(
        ScrapeGraphAgentToolNames.ScrapePage,
        ScrapeGraphAgentToolNames.SearchWeb,
        ScrapeGraphAgentToolNames.HealthCheck);
```

## Samples

- [Core SDK sample](samples/ScrapeGraphAI.CoreSample/README.md): DI typed client, health, credits, scrape, extract, and search.
- [Agent Framework sample](samples/ScrapeGraphAI.AgentFrameworkSample/README.md): registers ScrapeGraphAI Agent Framework tools, shows approval options, lists tools, and demonstrates how to attach them to an `AIAgent`.

## Configuration

`ScrapeGraphOptions`:

- `ApiKey`
- `BaseUrl`

`ScrapeGraphResilienceOptions`:

- `TotalRequestTimeout`
- `AttemptTimeout`
- `MaxRetryAttempts`
- `RetryBackoff`

Resilience options are validated when the standard resilience pipeline is used: timeout and backoff values must be positive, `AttemptTimeout` cannot exceed `TotalRequestTimeout`, and `MaxRetryAttempts` cannot be negative.

## Releases

This repository uses trunk-based release automation:

- Pull requests and pushes to `main` run restore, build, and tests.
- Every normal push to `main` publishes preview packages like `0.1.1-preview.<run>`.
- Release Please maintains stable versions, `CHANGELOG.md`, GitHub releases, and stable NuGet publishing.

NuGet Trusted Publishing must be configured for both package IDs with repository `anktsrkr/scrapegraphai-csharp-sdk` and workflow files `release.yml` and `prerelease.yml`. Set the GitHub repository variable `NUGET_USER` to the nuget.org username that owns the trusted publishing policy.
