using System.Net.Http.Headers;

namespace ScrapeGraphAI;

internal static class ScrapeGraphHttpClientDefaults
{
    private const string ApiKeyHeaderName = "SGAI-APIKEY";
    private const string JsonMediaType = "application/json";
    private const string UserAgentName = "ScrapeGraphAI-DotNet";
    private static readonly string UserAgent = $"{UserAgentName}/{typeof(ScrapeGraphHttpClientDefaults).Assembly.GetName().Version}";

    public static void Apply(HttpClient client, string? apiKey)
    {
        if (!client.DefaultRequestHeaders.UserAgent.Any(product => string.Equals(product.Product?.Name, UserAgentName, StringComparison.Ordinal)))
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
        }

        if (!client.DefaultRequestHeaders.Accept.Any(header => string.Equals(header.MediaType, JsonMediaType, StringComparison.OrdinalIgnoreCase)))
        {
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(JsonMediaType));
        }

        if (!client.DefaultRequestHeaders.Contains(ApiKeyHeaderName) && !string.IsNullOrWhiteSpace(apiKey))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation(ApiKeyHeaderName, apiKey);
        }
    }
}
