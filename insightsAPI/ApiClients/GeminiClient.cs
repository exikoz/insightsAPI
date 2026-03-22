using System.Text;
using System.Text.Json;
using insightsAPI.Models.Options;
using Microsoft.Extensions.Options;

namespace insightsAPI.ApiClients
{
    public interface IGeminiClient
    {
        Task<string> GenerateInsightAsync(string prompt);
    }

    public class GeminiClient : IGeminiClient
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiOptions _options;
        private readonly ILogger<GeminiClient> _logger;

        public GeminiClient(HttpClient httpClient, IOptions<GeminiOptions> options, ILogger<GeminiClient> logger)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<string> GenerateInsightAsync(string prompt)
        {
            if (string.IsNullOrEmpty(_options.ApiKey))
            {
                throw new InvalidOperationException("GEMINI_API_KEY är inte konfigurerad.");
            }

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, _options.GenerateContentUrl);
            requestMessage.Headers.Add("x-goog-api-key", _options.ApiKey);

            var payload = new
            {
                contents = new[] { new { parts = new[] { new { text = prompt } } } },
                generationConfig = new
                {
                    maxOutputTokens = 800,
                    temperature = 0.2
                }
            };

            requestMessage.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            // Assuming Polly Handles 503 retries at the HttpClient level in Program.cs
            var response = await _httpClient.SendAsync(requestMessage);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Gemini API Error: {StatusCode} - {Body}", response.StatusCode, errorBody);
                throw new HttpRequestException($"Gemini API-fel: {response.StatusCode} - Försök igen om en stund.");
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(responseContent);

            var candidates = document.RootElement.GetProperty("candidates");
            if (candidates.GetArrayLength() == 0) return "Inget svar från AI.";

            var firstCandidate = candidates[0];
            var parts = firstCandidate.GetProperty("content").GetProperty("parts");
            string insightText = parts[0].GetProperty("text").GetString() ?? "Inget svar från AI.";

            // Check if truncated
            var finishReason = firstCandidate.TryGetProperty("finishReason", out var fr) ? fr.GetString() : "UNKNOWN";
            if (finishReason == "MAX_TOKENS")
            {
                insightText += "\n\n[VARNING: Svaret trunkerades på grund av token-gräns]";
            }

            return insightText;
        }
    }
}
