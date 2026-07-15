using System;
using Fistix.TaskManager.AiLayer.Abstractions;
using Fistix.TaskManager.AiLayer.Implementations;
using Fistix.TaskManager.AiLayer.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fistix.TaskManager.WebApi.Extensions;

/// <summary>
/// Extension methods for registering AI services with dependency injection.
/// </summary>
public static class AiServiceExtension
{
    public static IServiceCollection AddAiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(provider =>
        {
            var config = provider.GetRequiredService<IConfiguration>();
            var aiConfig = new AiConfiguration();
            config.GetSection("Ai").Bind(aiConfig);
            return aiConfig;
        });

        services.AddSingleton<SemanticKernelOrchestrator>();

        services.AddSingleton(provider =>
        {
            var orchestrator = provider.GetRequiredService<SemanticKernelOrchestrator>();
            return orchestrator.CreateKernelAsync().GetAwaiter().GetResult();
        });

        services.AddSingleton<ILlmProviderService, SemanticKernelLlmProvider>();
        services.AddScoped<SummarizationPipeline>();
        services.AddScoped<ClassificationPipeline>();
        services.AddScoped<SemanticSearchPipeline>();
        services.AddHttpClient(nameof(SemanticKernelEmbeddingService));

        var embeddingProvider = configuration["Ai:Embedding:Provider"] ?? "Onnx";
        if (string.Equals(embeddingProvider, "onnx", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<OnnxBgeEmbeddingService>();
            services.AddSingleton<IEmbeddingService>(sp => sp.GetRequiredService<OnnxBgeEmbeddingService>());
        }
        else
        {
            services.AddScoped<IEmbeddingService, SemanticKernelEmbeddingService>();
        }

        return services;
    }
}
