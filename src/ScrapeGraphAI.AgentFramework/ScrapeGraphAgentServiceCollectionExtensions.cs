using Microsoft.Extensions.DependencyInjection;
namespace ScrapeGraphAI.AgentFramework;

/// <summary>
/// Dependency injection helpers for ScrapeGraphAI Agent Framework tools.
/// </summary>
public static class ScrapeGraphAgentServiceCollectionExtensions
{
    /// <summary>
    /// Registers ScrapeGraphAI Agent Framework tools.
    /// </summary>
    /// <param name="services">The service collection to register with.</param>
    /// <param name="configure">An optional callback for configuring Agent Framework tool options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddScrapeGraphAgentTools(this IServiceCollection services, Action<ScrapeGraphAgentToolOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<ScrapeGraphAgentToolOptions>()
            .Validate(options => options.MaxResultCharacters >= 0, "ScrapeGraphAgentToolOptions.MaxResultCharacters must be greater than or equal to zero.");

        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddSingleton<ScrapeGraphAgentTools>();

        return services;
    }
}
