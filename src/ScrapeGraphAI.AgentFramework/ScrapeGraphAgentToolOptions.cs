namespace ScrapeGraphAI.AgentFramework;

/// <summary>
/// Configures ScrapeGraphAI tools exposed to Microsoft Agent Framework agents.
/// </summary>
public sealed class ScrapeGraphAgentToolOptions
{
    /// <summary>Maximum number of characters returned to the agent by a tool.</summary>
    public int MaxResultCharacters { get; set; } = 24_000;

    /// <summary>Default scrape/search content format used by simple tools.</summary>
    public ScrapeFormatType DefaultFormat { get; set; } = ScrapeFormatType.Markdown;

    /// <summary>
    /// Optional allow-list of tool names returned by AsAITools. Null or empty means all tools are allowed.
    /// </summary>
    public IReadOnlyCollection<string>? IncludedTools { get; set; }

    /// <summary>
    /// Optional list of tool names wrapped with Agent Framework approval.
    /// </summary>
    public IReadOnlyCollection<string>? ApprovalRequiredTools { get; set; }

}
