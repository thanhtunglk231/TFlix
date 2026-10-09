using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using CoreLib.Config;
using CoreLib.Dtos.Chat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Server.Services.Gemini
{
    public class GeminiClient : IGeminiClient
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiOptions _options;
        private readonly ILogger<GeminiClient> _logger;
        private readonly string _apiKey;

        public GeminiClient(
            IHttpClientFactory httpClientFactory,
            IOptions<GeminiOptions> options,
            ILogger<GeminiClient> logger)
        {
            _httpClient = httpClientFactory.CreateClient("GeminiClient");
            _options = options.Value;
            _logger = logger;

            // Đọc API Key từ Options -> Fallback sang Environment Variable
            var key = _options.ApiKey;
            if (string.IsNullOrWhiteSpace(key))
            {
                key = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                      ?? Environment.GetEnvironmentVariable("Gemini__ApiKey")
                      ?? string.Empty;
            }
            _apiKey = key.Trim();

            if (_options.TimeoutSeconds > 0)
            {
                _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
            }
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

        public async Task<string?> GenerateContentAsync(
            string systemInstruction,
            IEnumerable<ChatMessageDto> history,
            string currentPrompt,
            CancellationToken cancellationToken = default)
        {
            if (!IsConfigured)
            {
                _logger.LogWarning("Gemini API Key is not configured. Falling back to local heuristic assistance.");
                return null;
            }

            try
            {
                var baseUrl = string.IsNullOrWhiteSpace(_options.BaseUrl)
                    ? "https://generativelanguage.googleapis.com/v1beta"
                    : _options.BaseUrl.TrimEnd('/');

                var model = string.IsNullOrWhiteSpace(_options.Model) ? "gemini-2.5-flash" : _options.Model;
                var endpoint = $"{baseUrl}/models/{model}:generateContent?key={_apiKey}";

                // Xây dựng payload contents
                var contents = new List<object>();

                if (history != null)
                {
                    foreach (var h in history)
                    {
                        if (string.IsNullOrWhiteSpace(h.Content)) continue;
                        var geminiRole = string.Equals(h.Role, "model", StringComparison.OrdinalIgnoreCase) ||
                                         string.Equals(h.Role, "assistant", StringComparison.OrdinalIgnoreCase)
                            ? "model"
                            : "user";

                        contents.Add(new
                        {
                            role = geminiRole,
                            parts = new object[] { new { text = h.Content } }
                        });
                    }
                }

                // Thêm prompt hiện tại
                contents.Add(new
                {
                    role = "user",
                    parts = new object[] { new { text = currentPrompt } }
                });

                var requestBody = new
                {
                    system_instruction = string.IsNullOrWhiteSpace(systemInstruction)
                        ? null
                        : new { parts = new object[] { new { text = systemInstruction } } },
                    contents = contents,
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens = 1024,
                        topP = 0.95
                    }
                };

                var jsonPayload = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                });

                using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                {
                    Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
                };

                using var response = await _httpClient.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var statusCode = (int)response.StatusCode;
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Gemini API call failed with StatusCode {StatusCode}: {ErrorBody}", statusCode, errorBody);
                    return null;
                }

                var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(responseString);

                if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                    candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var contentElem) &&
                        contentElem.TryGetProperty("parts", out var parts) &&
                        parts.GetArrayLength() > 0)
                    {
                        var text = parts[0].GetProperty("text").GetString();
                        return text;
                    }
                }

                _logger.LogWarning("Gemini API response did not contain text candidates.");
                return null;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Gemini API call was cancelled or timed out after {Timeout} seconds.", _options.TimeoutSeconds);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected exception during Gemini API call.");
                return null;
            }
        }
    }
}
