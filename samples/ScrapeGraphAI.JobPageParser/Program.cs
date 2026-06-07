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
using System.Text.RegularExpressions;

const string DefaultJobUrl = "https://example.com/careers/software-engineer";

var runAgent = args.Contains("--run", StringComparer.OrdinalIgnoreCase);
var jobUrl = GetOption(args, "--url") ?? DefaultJobUrl;
var prompt = GetOption(args, "--prompt") ?? $"""
Analyze this job posting: {jobUrl}.
Extract the role title, company, location, work mode, seniority, required skills,
preferred skills, responsibilities, hiring signals, and candidate preparation notes.
Return a concise markdown brief with these exact sections:
### Role Snapshot
### Key Analysis
### Candidate Preparation Notes
### Recommended Next Action
Use short bullets where useful and include the source URL.
""";

var endpoint = GetOption(args, "--endpoint")
    ?? Environment.GetEnvironmentVariable("OPENAI_BASE_URL")
    ?? Environment.GetEnvironmentVariable("LMSTUDIO_BASE_URL")
    ?? "http://localhost:1234/v1";

var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["OpenAI:ApiKey"] = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "lm-studio",
        ["OpenAI:Model"] = GetOption(args, "--model")
            ?? Environment.GetEnvironmentVariable("OPENAI_MODEL")
            ?? Environment.GetEnvironmentVariable("LMSTUDIO_MODEL")
            ?? "google/gemma-4-e4b",
        ["OpenAI:Endpoint"] = endpoint,
        ["ScrapeGraphAI:ApiKey"] = Environment.GetEnvironmentVariable("SGAI_API_KEY")
            ?? (runAgent ? null : "sgai-placeholder")
    })
    .Build();

var openAiApiKey = configuration["OpenAI:ApiKey"];
var model = configuration["OpenAI:Model"]!;
var openAiEndpoint = configuration["OpenAI:Endpoint"]!;
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
    options.MaxResultCharacters = 12_000;
    options.IncludedTools =
    [
       ScrapeGraphAgentToolNames.ScrapePage,
       // ScrapeGraphAgentToolNames.ExtractFromPage
    ];
});

await using var provider = services.BuildServiceProvider();
var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
var scrapeGraphTools = provider.GetRequiredService<ScrapeGraphAgentTools>();
var aiTools = scrapeGraphTools
    .AsAITools(
        ScrapeGraphAgentToolNames.ScrapePage
       // ,ScrapeGraphAgentToolNames.ExtractFromPage
        )
    .ToArray();

WriteAppHeader();
WriteRunSummary(model, openAiEndpoint, jobUrl, aiTools);

if (!runAgent)
{
    WriteInfoMessage("No API calls were made. Add --run after setting SGAI_API_KEY and starting your model endpoint.");
    return 0;
}

if (string.IsNullOrWhiteSpace(openAiApiKey))
{
    Console.Error.WriteLine("Set OPENAI_API_KEY before running with --run, or use a local endpoint placeholder such as lm-studio.");
    return 2;
}

if (string.IsNullOrWhiteSpace(scrapeGraphApiKey))
{
    Console.Error.WriteLine("Set SGAI_API_KEY before running with --run.");
    return 2;
}

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
    name: "JobPageParser",
    instructions: """
        You are a concise hiring analyst.

        For job posting analysis:
        - Use scrape_page or extract_from_page before answering.
        - Prefer structured extraction for role facts.
        - Preserve uncertainty when a field is missing or unclear.
        - Do not invent company details, compensation, seniority, or work mode.
        - Return a practical brief, not a generic summary.
        - Include the source URL.
        - End with one recommended next action.
        """,
    tools: aiTools,
    clientFactory: innerClient => new FunctionInvokingChatClient(innerClient, loggerFactory, provider)
    {
        MaximumIterationsPerRequest = 10
    },
    loggerFactory: loggerFactory,
    services: provider);

WritePrompt(prompt);
WriteSectionTitle("Response");

var response = await agent.RunAsync(prompt, cancellationToken: cancellation.Token).ConfigureAwait(false);
WriteMarkdownBrief(response.Text);

return 0;

static void WriteAppHeader()
{
    Console.WriteLine();
    if (UseAnsi())
    {
        Console.WriteLine($"{Ansi.BrightCyan}{Ansi.Bold}ScrapeGraphAI job page parser{Ansi.Reset}");
    }
    else
    {
        Console.WriteLine("ScrapeGraphAI job page parser");
    }

    WriteRule();
}

static void WriteRunSummary(string model, string endpoint, string jobUrl, IReadOnlyCollection<AITool> tools)
{
    WriteSectionTitle("Run Configuration");
    WriteKeyValue("Model", model);
    WriteKeyValue("Endpoint", endpoint);
    WriteKeyValue("Job URL", jobUrl);
    Console.WriteLine();

    WriteSectionTitle("Registered Tools");
    foreach (var tool in tools)
    {
        WriteWrappedInline($"**{tool.Name}:** {tool.Description}", "  - ", "    ", visibleFirstIndentLength: 4);
    }

    Console.WriteLine();
}

static void WritePrompt(string prompt)
{
    WriteSectionTitle("Prompt");
    foreach (var line in prompt.ReplaceLineEndings("\n").Split('\n'))
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            Console.WriteLine();
            continue;
        }

        WriteWrappedInline(line.Trim(), "  ", "  ");
    }

    Console.WriteLine();
}

static void WriteSectionTitle(string title)
{
    if (UseAnsi())
    {
        Console.WriteLine($"{Ansi.Cyan}{Ansi.Bold}{title}{Ansi.Reset}");
    }
    else
    {
        Console.WriteLine(title);
    }

    WriteRule();
}

static void WriteKeyValue(string key, string value)
{
    WriteWrappedInline($"**{key}:** {value}", "  ", "  ");
}

static void WriteInfoMessage(string message)
{
    Console.WriteLine();
    WriteWrappedInline(message, "  ", "  ");
}

static void WriteMarkdownBrief(string? markdown)
{
    if (string.IsNullOrWhiteSpace(markdown))
    {
        Console.WriteLine("(No response text returned.)");
        return;
    }

    if (!UseAnsi())
    {
        Console.WriteLine(markdown);
        return;
    }

    Console.WriteLine();
    WriteRule();

    foreach (var rawLine in markdown.ReplaceLineEndings("\n").Split('\n'))
    {
        var line = rawLine.TrimEnd();
        var trimmed = line.Trim();

        if (trimmed.Length == 0)
        {
            Console.WriteLine();
            continue;
        }

        if (IsHorizontalRule(trimmed))
        {
            WriteRule();
            continue;
        }

        if (TryWriteHeading(trimmed))
        {
            continue;
        }

        if (TryWriteBullet(trimmed))
        {
            continue;
        }

        if (TryWriteNumberedItem(trimmed))
        {
            continue;
        }

        WriteWrappedInline(trimmed, "  ", "  ");
    }

    WriteRule();
}

static bool TryWriteHeading(string line)
{
    var heading = Regex.Match(line, @"^(?<marks>#{1,6})\s+(?<text>.+)$");
    if (!heading.Success)
    {
        return false;
    }

    var text = StripInlineMarkdown(heading.Groups["text"].Value);
    var color = heading.Groups["marks"].Value.Length == 1 ? Ansi.BrightCyan : Ansi.Cyan;
    Console.WriteLine($"{color}{Ansi.Bold}{text}{Ansi.Reset}");
    WriteRule();
    return true;
}

static bool TryWriteBullet(string line)
{
    var bullet = Regex.Match(line, @"^(?<marker>[-*])\s+(?<text>.+)$");
    if (!bullet.Success)
    {
        return false;
    }

    WriteWrappedInline(bullet.Groups["text"].Value, $"{Ansi.Dim}  - {Ansi.Reset}", "    ", visibleFirstIndentLength: 4);
    return true;
}

static bool TryWriteNumberedItem(string line)
{
    var numberedItem = Regex.Match(line, @"^(?<number>\d+)\.\s+(?<text>.+)$");
    if (!numberedItem.Success)
    {
        return false;
    }

    var marker = $"{numberedItem.Groups["number"].Value}.";
    var firstIndent = $"{Ansi.Dim}{marker.PadLeft(4)} {Ansi.Reset}";
    WriteWrappedInline(numberedItem.Groups["text"].Value, firstIndent, "     ", visibleFirstIndentLength: 5);
    return true;
}

static void WriteWrappedInline(
    string text,
    string firstIndent,
    string continuationIndent,
    int? visibleFirstIndentLength = null)
{
    var firstWidth = AvailableContentWidth(visibleFirstIndentLength ?? firstIndent.Length);
    var continuationWidth = AvailableContentWidth(continuationIndent.Length);
    var wrappedLines = WrapText(text, firstWidth, continuationWidth);

    for (var i = 0; i < wrappedLines.Count; i++)
    {
        Console.Write(i == 0 ? firstIndent : continuationIndent);
        WriteInline(wrappedLines[i]);
        Console.WriteLine();
    }
}

static void WriteInline(string text)
{
    if (!UseAnsi())
    {
        Console.Write(StripInlineMarkdown(text));
        return;
    }

    var cursor = 0;
    foreach (Match match in Regex.Matches(text, @"(\*\*(?<bold>.+?)\*\*)|(`(?<code>.+?)`)"))
    {
        Console.Write(text[cursor..match.Index]);

        if (match.Groups["bold"].Success)
        {
            Console.Write($"{Ansi.Bold}{Ansi.Yellow}{match.Groups["bold"].Value}{Ansi.Reset}");
        }
        else
        {
            Console.Write($"{Ansi.Dim}{match.Groups["code"].Value}{Ansi.Reset}");
        }

        cursor = match.Index + match.Length;
    }

    Console.Write(text[cursor..]);
}

static string StripInlineMarkdown(string text)
    => text
        .Replace("**", string.Empty, StringComparison.Ordinal)
        .Replace("`", string.Empty, StringComparison.Ordinal)
        .Trim();

static List<string> WrapText(string text, int firstWidth, int continuationWidth)
{
    var words = Regex.Split(text.Trim(), @"\s+")
        .Where(static word => word.Length > 0)
        .ToArray();

    var lines = new List<string>();
    var current = string.Empty;
    var width = firstWidth;

    foreach (var word in words)
    {
        var candidate = current.Length == 0 ? word : $"{current} {word}";
        if (VisibleLength(candidate) <= width)
        {
            current = candidate;
            continue;
        }

        if (current.Length > 0)
        {
            lines.Add(current);
            width = continuationWidth;
            current = word;
            continue;
        }

        foreach (var chunk in SplitLongWord(word, width))
        {
            lines.Add(chunk);
        }

        width = continuationWidth;
        current = string.Empty;
    }

    if (current.Length > 0)
    {
        lines.Add(current);
    }

    return lines.Count == 0 ? [string.Empty] : lines;
}

static IEnumerable<string> SplitLongWord(string word, int width)
{
    width = Math.Max(width, 8);
    for (var i = 0; i < word.Length; i += width)
    {
        yield return word[i..Math.Min(i + width, word.Length)];
    }
}

static int VisibleLength(string text)
    => StripInlineMarkdown(text).Length;

static int AvailableContentWidth(int indentLength)
{
    var width = Console.IsOutputRedirected ? 100 : GetConsoleWidth();
    return Math.Max(32, width - indentLength - 2);
}

static bool IsHorizontalRule(string line)
    => line is "---" or "***" or "___";

static void WriteRule()
{
    var width = Math.Max(40, GetConsoleWidth() - 4);
    if (UseAnsi())
    {
        Console.WriteLine($"{Ansi.Dim}  {new string('-', width)}{Ansi.Reset}");
    }
    else
    {
        Console.WriteLine($"  {new string('-', width)}");
    }
}

static bool UseAnsi()
    => !Console.IsOutputRedirected
        && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("NO_COLOR"));

static int GetConsoleWidth()
{
    try
    {
        return Console.WindowWidth > 0 ? Console.WindowWidth : 100;
    }
    catch (IOException)
    {
        return 100;
    }
}

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

static class Ansi
{
    public const string Reset = "\u001b[0m";
    public const string Bold = "\u001b[1m";
    public const string Dim = "\u001b[2m";
    public const string Cyan = "\u001b[36m";
    public const string BrightCyan = "\u001b[96m";
    public const string Yellow = "\u001b[33m";
}
