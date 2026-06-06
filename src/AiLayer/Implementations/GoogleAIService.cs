using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Fistix.TaskManager.AiLayer.Shared;
using System.Net.Http;
using System.Threading.Tasks;

namespace Fistix.TaskManager.AiLayer.Implementations;

/// <summary>
/// Service for calling Google Gemini AI models directly via REST API.
/// This is a complement to Semantic Kernel for Google AI support.
/// </summary>
public class GoogleAIService
{
    private readonly HttpClient _httpClient;
    private readonly GoogleAISettings _settings;
    private readonly ILogger<GoogleAIService> _logger;

    private const string GoogleAIEndpoint = "https://generativelanguage.googleapis.com/v1beta/models";

    public GoogleAIService(
        HttpClient httpClient,
        GoogleAISettings settings,
        ILogger<GoogleAIService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates content using Google Gemini AI.
    /// </summary>
    public async Task<string> GenerateContentAsync(string prompt)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new InvalidOperationException("Google AI API key is not configured");
        }

        try
        {
            var url = $"{GoogleAIEndpoint}/{_settings.Model}:generateContent?key={_settings.ApiKey}";

            var request = new GoogleAIRequest
            {
                Contents = new[]
                {
                    new GoogleAIContent
                    {
                        Parts = new[]
                        {
                            new GoogleAIPart { Text = prompt }
                        }
                    }
                }
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(request),
                System.Text.Encoding.UTF8,
                "application/json");

            _logger.LogDebug("Calling Google AI endpoint: {Url}", url);

            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Google AI API error: {StatusCode} - {Error}", response.StatusCode, errorContent);
                throw new HttpRequestException($"Google AI API returned {response.StatusCode}: {errorContent}");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var aiResponse = JsonConvert.DeserializeObject<GoogleAIResponse>(responseContent);

            return aiResponse?.Candidates?[0]?.Content?.Parts?[0]?.Text ?? "No response generated";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Google AI API");
            throw;
        }
    }

    // Google AI API request/response models
    private class GoogleAIRequest
    {
        [JsonProperty("contents")]
        public GoogleAIContent[] Contents { get; set; }
    }

    private class GoogleAIContent
    {
        [JsonProperty("parts")]
        public GoogleAIPart[] Parts { get; set; }
    }

    private class GoogleAIPart
    {
        [JsonProperty("text")]
        public string Text { get; set; }
    }

    private class GoogleAIResponse
    {
        [JsonProperty("candidates")]
        public GoogleAICandidate[] Candidates { get; set; }
    }

    private class GoogleAICandidate
    {
        [JsonProperty("content")]
        public GoogleAIContent Content { get; set; }
    }
}
