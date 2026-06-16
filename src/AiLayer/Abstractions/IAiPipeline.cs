namespace Fistix.TaskManager.AiLayer.Abstractions;

/// <summary>
/// Base interface for all AI processing pipelines.
/// Defines the contract for executing AI operations.
/// </summary>
public interface IAiPipeline
{
    /// <summary>
    /// Execute an AI pipeline operation asynchronously.
    /// </summary>
    /// <typeparam name="TRequest">Type of the request/input</typeparam>
    /// <typeparam name="TResponse">Type of the response/output</typeparam>
    /// <param name="request">The request object containing input data</param>
    /// <returns>The pipeline response</returns>
    Task<TResponse> ExecuteAsync<TRequest, TResponse>(TRequest request) 
        where TRequest : class 
        where TResponse : class;
}

/// <summary>
/// Unified interface for all LLM provider implementations.
/// </summary>
public interface ILlmProviderService
{
    /// <summary>
    /// Executes a prompt against the configured provider.
    /// </summary>
    Task<string> GetCompletionAsync(string prompt, CancellationToken cancellationToken = default);
}
