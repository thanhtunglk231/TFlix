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

        public async Task<string?> GenerateWithAudioAsync(string prompt, byte[] audio, string mimeType,
            CancellationToken cancellationToken = default)
        {
            if (!IsConfigured || audio.Length == 0) return null;
            try
            {
                var baseUrl = string.IsNullOrWhiteSpace(_options.BaseUrl)
                    ? "https://generativelanguage.googleapis.com/v1beta" : _options.BaseUrl.TrimEnd('/');
                var model = string.IsNullOrWhiteSpace(_options.Model) ? "gemini-3.8-flash" : _options.Model;
                var payload = JsonSerializer.Serialize(new
                {
                    contents = new[] { new { role = "user", parts = new object[] {
                        new { text = prompt }, new { inline_data = new { mime_type = mimeType, data = Convert.ToBase64String(audio) } }
                    } } },
                    generationConfig = new { temperature = 0.1, maxOutputTokens = 8192, responseMimeType = "application/json" }
                });
                var response = await SendRequestAsync(baseUrl, model, payload, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    if ((int)response.StatusCode == StatusCodes.Status404NotFound
                        && errorBody.Contains("no longer available", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(model, "gemini-3.8-flash", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning(
                            "Configured Gemini audio model {Model} is unavailable; retrying with gemini-3.8-flash.",
                            model);
                        response.Dispose();
                        model = "gemini-3.8-flash";
                        response = await SendRequestAsync(baseUrl, model, payload, cancellationToken);
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                        _logger.LogError("Gemini subtitle generation failed for model {Model}: {Body}", model, errorBody);
                        response.Dispose();
                        return null;
                    }
                }
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                response.Dispose();
                using var document = JsonDocument.Parse(responseBody);
                return document.RootElement.GetProperty("candidates")[0].GetProperty("content")
                    .GetProperty("parts")[0].GetProperty("text").GetString();
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Gemini audio transcription timed out after {Timeout} seconds.", _options.TimeoutSeconds);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error calling Gemini audio transcription.");
                return null;
            }
        }

        public async Task<string?> GenerateWithImagesAsync(string prompt, IReadOnlyList<byte[]> jpegImages,
            CancellationToken cancellationToken = default)
        {
            if (!IsConfigured || jpegImages.Count == 0) return null;
            var baseUrl = string.IsNullOrWhiteSpace(_options.BaseUrl)
                ? "https://generativelanguage.googleapis.com/v1beta" : _options.BaseUrl.TrimEnd('/');
            var model = string.IsNullOrWhiteSpace(_options.Model) ? "gemini-3.8-flash" : _options.Model;
            var parts = new List<object> { new { text = prompt } };
            parts.AddRange(jpegImages.Select(image => (object)new
            {
                inline_data = new { mime_type = "image/jpeg", data = Convert.ToBase64String(image) }
            }));
            var payload = JsonSerializer.Serialize(new
            {
                contents = new[] { new { role = "user", parts } },
                generationConfig = new { temperature = 0.2, maxOutputTokens = 2048, responseMimeType = "application/json" }
            });
            var response = await SendRequestAsync(baseUrl, model, payload, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                if ((int)response.StatusCode == StatusCodes.Status404NotFound
                    && errorBody.Contains("no longer available", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(model, "gemini-3.8-flash", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning(
                        "Configured Gemini highlight model {Model} is unavailable; retrying with gemini-3.8-flash.",
                        model);
                    response.Dispose();
                    model = "gemini-3.8-flash";
                    response = await SendRequestAsync(baseUrl, model, payload, cancellationToken);
                }

                if (!response.IsSuccessStatusCode)
                {
                    errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError(
                        "Gemini highlight analysis failed for model {Model}: {Body}", model, errorBody);
                    response.Dispose();
                    return null;
                }
            }
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            response.Dispose();
            using var document = JsonDocument.Parse(responseBody);
            return document.RootElement.GetProperty("candidates")[0].GetProperty("content")
                .GetProperty("parts")[0].GetProperty("text").GetString();
        }

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

                var model = string.IsNullOrWhiteSpace(_options.Model) ? "gemini-3.8-flash" : _options.Model;

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

                var response = await SendRequestAsync(baseUrl, model, jsonPayload, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var statusCode = (int)response.StatusCode;
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    if (statusCode == StatusCodes.Status404NotFound
                        && errorBody.Contains("no longer available", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(model, "gemini-3.8-flash", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning(
                            "Configured Gemini model {Model} is unavailable; retrying with gemini-3.8-flash.",
                            model);
                        response.Dispose();
                        model = "gemini-3.8-flash";
                        response = await SendRequestAsync(baseUrl, model, jsonPayload, cancellationToken);
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        statusCode = (int)response.StatusCode;
                        errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                        _logger.LogError(
                            "Gemini API call failed for model {Model} with StatusCode {StatusCode}: {ErrorBody}",
                            model,
                            statusCode,
                            errorBody);
                        response.Dispose();
                        return null;
                    }
                }

                var responseString = await response.Content.ReadAsStringAsync(cancellationToken);
                response.Dispose();
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

        private async Task<HttpResponseMessage> SendRequestAsync(
            string baseUrl,
            string model,
            string jsonPayload,
            CancellationToken cancellationToken)
        {
            var endpoint = $"{baseUrl}/models/{model}:generateContent";
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
            };
            request.Headers.TryAddWithoutValidation("x-goog-api-key", _apiKey);

            return await _httpClient.SendAsync(request, cancellationToken);
        }
    }
}
