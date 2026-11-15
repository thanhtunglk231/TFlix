using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CoreLib.Config;
using CoreLib.Dtos.Chat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Server.Services.OpenAI
{
    public sealed class OpenAiClient : IOpenAiClient
    {
        private const string ResponsesEndpoint = "https://api.openai.com/v1/responses";
        private readonly HttpClient _httpClient;
        private readonly OpenAiOptions _options;
        private readonly ILogger<OpenAiClient> _logger;
        private readonly string _apiKey;

        public OpenAiClient(IHttpClientFactory httpClientFactory, IOptions<OpenAiOptions> options,
            ILogger<OpenAiClient> logger)
        {
            _httpClient = httpClientFactory.CreateClient("OpenAIClient");
            _options = options.Value;
            _logger = logger;
            _apiKey = string.IsNullOrWhiteSpace(_options.ApiKey)
                ? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                  ?? Environment.GetEnvironmentVariable("OpenAI__ApiKey") ?? string.Empty
                : _options.ApiKey;
            _httpClient.Timeout = TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 10, 120));
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

        public async Task<string?> GenerateContentAsync(string systemInstruction, IEnumerable<ChatMessageDto> history,
            string currentPrompt, CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
            {
                _logger.LogWarning("OpenAI API key is not configured. Chatbot will use the local fallback.");
                return null;
            }

            var input = new List<object>();
            foreach (var message in history ?? Enumerable.Empty<ChatMessageDto>())
            {
                if (!string.IsNullOrWhiteSpace(message.Content))
                    input.Add(new { role = string.Equals(message.Role, "user", StringComparison.OrdinalIgnoreCase) ? "user" : "assistant", content = message.Content });
            }
            input.Add(new { role = "user", content = currentPrompt });

            var payload = new { model = _options.Model, instructions = systemInstruction, input, max_output_tokens = 900 };
            using var request = new HttpRequestMessage(HttpMethod.Post, ResponsesEndpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            try
            {
                using var response = await _httpClient.SendAsync(request, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OpenAI chatbot request failed with status {StatusCode}.", (int)response.StatusCode);
                    return null;
                }

                using var document = JsonDocument.Parse(body);
                if (document.RootElement.TryGetProperty("output_text", out var outputText) && outputText.ValueKind == JsonValueKind.String)
                    return outputText.GetString();
                return ExtractOutputText(document.RootElement);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("OpenAI chatbot request timed out.");
                return null;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "OpenAI chatbot request could not be completed.");
                return null;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "OpenAI chatbot response could not be read.");
                return null;
            }
        }

        private static string? ExtractOutputText(JsonElement root)
        {
            if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array) return null;
            var chunks = new List<string>();
            foreach (var item in output.EnumerateArray())
            {
                if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array) continue;
                foreach (var part in content.EnumerateArray())
                    if (part.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String) chunks.Add(text.GetString()!);
            }
            return chunks.Count == 0 ? null : string.Join("\n", chunks);
        }
    }
}
