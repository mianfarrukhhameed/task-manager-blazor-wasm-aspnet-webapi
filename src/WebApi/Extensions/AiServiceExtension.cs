using Fistix.TaskManager.AiLayer.Implementations;
using Fistix.TaskManager.AiLayer.Abstractions;
using Fistix.TaskManager.AiLayer.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fistix.TaskManager.WebApi.Extensions;

/// <summary>
/// Extension methods for registering AI services with dependency injection.
/// </summary>
public static class AiServiceExtension
{
    /// <summary>
    /// Adds AI Layer services to the DI container.
    /// </summary>
    public static IServiceCollection AddAiServices(this IServiceCollection services)
    {
        // Register AI configuration
        services.AddSingleton(provider =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            var aiConfig = new AiConfiguration();
            config.GetSection("Ai").Bind(aiConfig);
            return aiConfig;
        });

        services.AddSingleton(provider =>
        {
            var aiConfig = provider.GetRequiredService<AiConfiguration>();            
            return aiConfig.GoogleAI;
        });

        // Register SemanticKernelOrchestrator as singleton (create kernel once)
        services.AddSingleton<SemanticKernelOrchestrator>();
        
        // Register Kernel factory that uses the orchestrator
        services.AddSingleton(provider =>
        {
            var orchestrator = provider.GetRequiredService<SemanticKernelOrchestrator>();
            return orchestrator.CreateKernelAsync().GetAwaiter().GetResult();
        });

        // Register GoogleAIService for Google Gemini support
        services.AddHttpClient<GoogleAIService>();

        // Register pipelines as scoped (create new instance per request)
        services.AddScoped<SummarizationPipeline>();

        return services;
    }
}
