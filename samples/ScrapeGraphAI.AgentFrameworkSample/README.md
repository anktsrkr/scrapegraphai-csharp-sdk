# ScrapeGraphAI Agent Framework Sample

This sample shows how to expose ScrapeGraphAI as Microsoft Agent Framework tools and attach those tools to an agent using an OpenAI-compatible chat endpoint.

By default, the sample targets LM Studio's local OpenAI-compatible server at:

```text
http://localhost:1234/v1
```

## What It Demonstrates

- `AddScrapeGraphAI(...)` typed client registration
- `AddScrapeGraphAgentTools(...)` tool registration
- Converting ScrapeGraphAI tools to `AITool` instances with `AsAITools(...)`
- Attaching tools to an Agent Framework agent
- Running tool calls through `FunctionInvokingChatClient`
- Using a local OpenAI-compatible endpoint such as LM Studio

## Prerequisites

- .NET 10
- A ScrapeGraphAI API key
- LM Studio or another OpenAI-compatible chat endpoint
- A tool-capable chat model loaded in that endpoint

## List Registered Tools

This command does not call OpenAI or ScrapeGraphAI. It only prints the model, endpoint, and registered tools:

```powershell
dotnet run --project samples/ScrapeGraphAI.AgentFrameworkSample
```

## Run The Agent

Start your OpenAI-compatible endpoint, set your ScrapeGraphAI API key, then run:

```powershell
$env:SGAI_API_KEY = "<your-scrapegraphai-api-key>"
dotnet run --project samples/ScrapeGraphAI.AgentFrameworkSample -- --run
```

LM Studio does not require a real OpenAI API key for local requests, so the sample uses `lm-studio` as the default placeholder key. If your endpoint requires an API key, set:

```powershell
$env:OPENAI_API_KEY = "<your-openai-compatible-api-key>"
```

## Configure Endpoint, Model, And Prompt

Override the endpoint:

```powershell
dotnet run --project samples/ScrapeGraphAI.AgentFrameworkSample -- --run --endpoint http://localhost:1234/v1
```

Override the model:

```powershell
$env:LMSTUDIO_MODEL = "your-loaded-model-id"
dotnet run --project samples/ScrapeGraphAI.AgentFrameworkSample -- --run --model your-loaded-model-id
```

Override the prompt:

```powershell
dotnet run --project samples/ScrapeGraphAI.AgentFrameworkSample -- --run --prompt "Scrape https://news.ycombinator.com and summarize the top discussions."
```

Configuration precedence:

- Endpoint: `--endpoint`, `LMSTUDIO_BASE_URL`, `OPENAI_BASE_URL`, then `http://localhost:1234/v1`
- Model: `--model`, `LMSTUDIO_MODEL`, `OPENAI_MODEL`, then `google/gemma-4-e4b`
- Prompt: `--prompt`, then the sample's default Hacker News prompt

## Key Code

```csharp
var aiTools = scrapeGraphTools
    .AsAITools(ScrapeGraphAgentToolNames.ScrapePage)
    .ToArray();

var chatClient = new ChatClient(
    model,
    new ApiKeyCredential(openAiApiKey),
    new OpenAIClientOptions
    {
        Endpoint = endpointUri
    });

var agent = chatClient.AsAIAgent(
    name: "ScrapeGraphResearcher",
    instructions: "Use scrape_page when you need current page content.",
    tools: aiTools,
    clientFactory: innerClient => new FunctionInvokingChatClient(innerClient, loggerFactory, provider)
    {
        MaximumIterationsPerRequest = 10
    },
    loggerFactory: loggerFactory,
    services: provider);

var response = await agent.RunAsync(prompt, cancellationToken: cancellation.Token);
Console.WriteLine(response.Text);
```

`MaximumIterationsPerRequest = 10` limits the model and tool-call loop for this sample.
