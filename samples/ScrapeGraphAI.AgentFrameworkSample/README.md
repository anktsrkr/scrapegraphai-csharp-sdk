# ScrapeGraphAI Agent Framework Sample

This sample shows a Microsoft Agent Framework agent using an OpenAI-compatible chat endpoint plus the ScrapeGraphAI `scrape_page` tool. By default, it points at LM Studio's local server on `http://localhost:1234/v1`. It mirrors this AI SDK shape:

```ts
const { text } = await generateText({
  model: openai("gpt-5-nano"),
  prompt: "Scrape Hacker News and write a short, concise summary of what people are talking about today.",
  tools: {
    scrape: scrapeTool(),
  },
  stopWhen: stepCountIs(3),
});
```

## Run Without API Calls

This lists the single tool attached to the agent without calling OpenAI or ScrapeGraphAI:

```powershell
dotnet run --project samples/ScrapeGraphAI.AgentFrameworkSample
```

## Run The Agent

Start LM Studio's local server, load a tool-capable chat model, then run:

```powershell
$env:SGAI_API_KEY = "<your-scrapegraphai-api-key>"
dotnet run --project samples/ScrapeGraphAI.AgentFrameworkSample -- --run
```

LM Studio does not require a real OpenAI API key for local requests, so the sample uses `lm-studio` as the default placeholder key. Override it with `OPENAI_API_KEY` if your OpenAI-compatible endpoint requires one.

The default endpoint is `http://localhost:1234/v1`. Override it with `LMSTUDIO_BASE_URL`, `OPENAI_BASE_URL`, or `--endpoint`:

```powershell
dotnet run --project samples/ScrapeGraphAI.AgentFrameworkSample -- --run --endpoint http://localhost:1234/v1
```

The default model is `google/gemma-4-12b-qat`. Override it with `LMSTUDIO_MODEL`, `OPENAI_MODEL`, or `--model`:

```powershell
$env:LMSTUDIO_MODEL = "your-loaded-model-id"
dotnet run --project samples/ScrapeGraphAI.AgentFrameworkSample -- --run --model your-loaded-model-id
```

Override the prompt with `--prompt`:

```powershell
dotnet run --project samples/ScrapeGraphAI.AgentFrameworkSample -- --run --prompt "Scrape https://news.ycombinator.com and summarize the top discussions."
```

## Key Code

```csharp
var tools = scrapeGraphTools
    .AsAITools(ScrapeGraphAgentToolNames.ScrapePage)
    .ToArray();

var chatClient = new ChatClient(
    model,
    new ApiKeyCredential(openAiApiKey),
    new OpenAIClientOptions
    {
        Endpoint = new Uri("http://localhost:1234/v1")
    });
var agent = chatClient.AsAIAgent(
    name: "ScrapeGraphResearcher",
    instructions: "Use scrape_page when you need current page content.",
    tools: tools,
    clientFactory: innerClient => new FunctionInvokingChatClient(innerClient, loggerFactory, provider)
    {
        MaximumIterationsPerRequest = 3
    },
    loggerFactory: loggerFactory,
    services: provider);

var response = await agent.RunAsync(
    "Scrape Hacker News and write a short, concise summary of what people are talking about today.");

Console.WriteLine(response.Text);
```

`MaximumIterationsPerRequest = 3` is the Agent Framework equivalent of limiting the model/tool loop to three steps for this sample.
