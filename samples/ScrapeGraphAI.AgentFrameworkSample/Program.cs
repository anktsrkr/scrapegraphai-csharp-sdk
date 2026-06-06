using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenAI;
using OpenAI.Chat;
using ScrapeGraphAI;
using ScrapeGraphAI.AgentFramework;
using System.ClientModel;

const string DefaultPrompt = "Scrape Hacker News and write a short, concise summary of what people are talking about today.";

var runAgent = args.Contains("--run", StringComparer.OrdinalIgnoreCase);
var prompt = GetOption(args, "--prompt") ?? DefaultPrompt;
var endpoint = GetOption(args, "--endpoint")
    ?? Environment.GetEnvironmentVariable("LMSTUDIO_BASE_URL")
    ?? Environment.GetEnvironmentVariable("OPENAI_BASE_URL")
    ?? "http://localhost:1234/v1";
var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["OpenAI:ApiKey"] = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "lm-studio",
        ["OpenAI:Model"] = Environment.GetEnvironmentVariable("LMSTUDIO_MODEL")
            ?? Environment.GetEnvironmentVariable("OPENAI_MODEL")
            ?? "google/gemma-4-e4b",
        ["OpenAI:Endpoint"] = endpoint,
        ["ScrapeGraphAI:ApiKey"] = Environment.GetEnvironmentVariable("SGAI_API_KEY")
            ?? (runAgent ? null : "sgai-placeholder")
    })
    .Build();

var openAiApiKey = configuration["OpenAI:ApiKey"];
var model = GetOption(args, "--model") ?? configuration["OpenAI:Model"] ?? "google/gemma-4-e4b";
var openAiEndpoint = configuration["OpenAI:Endpoint"] ?? "http://localhost:1234/v1";
var scrapeGraphApiKey = configuration["ScrapeGraphAI:ApiKey"];

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

var services = new ServiceCollection();
services.Configure<ScrapeGraphOptions>(configuration.GetSection("ScrapeGraphAI"));
services.AddLogging(logging =>
{
    logging.AddSimpleConsole(options => options.SingleLine = true);
    logging.SetMinimumLevel(LogLevel.Warning);
});

services.AddScrapeGraphAI()
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(90);
    });

services.AddScrapeGraphAgentTools(options =>
{
    options.DefaultFormat = ScrapeFormatType.Markdown;
    options.MaxResultCharacters = 8_000;
    options.IncludedTools = [ScrapeGraphAgentToolNames.ScrapePage];
});

await using var provider = services.BuildServiceProvider();
var scrapeGraphTools = provider.GetRequiredService<ScrapeGraphAgentTools>();
var aiTools = scrapeGraphTools.AsAITools(ScrapeGraphAgentToolNames.ScrapePage).ToArray();

Console.WriteLine("ScrapeGraphAI Agent Framework sample");
Console.WriteLine();
Console.WriteLine($"Model: {model}");
Console.WriteLine($"OpenAI-compatible endpoint: {openAiEndpoint}");
Console.WriteLine("Registered tools:");
foreach (AITool tool in aiTools)
{
    Console.WriteLine($"- {tool.Name}: {tool.Description}");
}

Console.WriteLine();

if (!runAgent)
{
    Console.WriteLine("No API calls were made. Add --run, start LM Studio, and set SGAI_API_KEY to run the agent.");
    return 0;
}

if (string.IsNullOrWhiteSpace(openAiApiKey))
{
    Console.Error.WriteLine("Set OPENAI_API_KEY before running with --run, or use the default LM Studio placeholder key.");
    return 2;
}

if (string.IsNullOrWhiteSpace(scrapeGraphApiKey))
{
    Console.Error.WriteLine("Set SGAI_API_KEY before running with --run.");
    return 2;
}

var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
if (!Uri.TryCreate(openAiEndpoint, UriKind.Absolute, out var endpointUri))
{
    Console.Error.WriteLine($"Invalid OpenAI-compatible endpoint: {openAiEndpoint}");
    return 2;
}

var chatClient = new ChatClient(
    model,
    new ApiKeyCredential(openAiApiKey),
    new OpenAIClientOptions
    {
        Endpoint = endpointUri
    });
var agent = chatClient.AsAIAgent(
    name: "ScrapeGraphResearcher",
    instructions: """
        You research public web pages with ScrapeGraphAI.
        Use scrape_page when you need current page content.
        Keep final answers short, concrete, and concise.
        """,
    tools: aiTools,
    clientFactory: innerClient => new FunctionInvokingChatClient(innerClient, loggerFactory, provider)
    {
        MaximumIterationsPerRequest = 10
    },
    loggerFactory: loggerFactory,
    services: provider);

Console.WriteLine("Prompt:");
Console.WriteLine(prompt);
Console.WriteLine();
Console.WriteLine("Response:");

var response = await agent.RunAsync(prompt, cancellationToken: cancellation.Token).ConfigureAwait(false);
Console.WriteLine(response.Text);

return 0;

static string? GetOption(string[] args, string name)
{
    for (var i = 0; i < args.Length; i++)
    {
        if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
        {
            return args[i + 1];
        }

        var prefix = name + "=";
        if (args[i].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return args[i][prefix.Length..];
        }
    }

    return null;
}
