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
        private readonly List<string> _apiKeys;
        private int _currentKeyIndex = 0;

        public GeminiClient(
            IHttpClientFactory httpClientFactory,
            IOptions<GeminiOptions> options,
            ILogger<GeminiClient> logger)
        {
            _httpClient = httpClientFactory.CreateClient("GeminiClient");
            _options = options.Value;
            _logger = logger;

            // Đọc API Key từ Options -> Fallback sang Environment Variable (hỗ trợ nhiều key cách nhau bằng dấu , hoặc ;)
            var rawKeys = _options.ApiKey;
            if (string.IsNullOrWhiteSpace(rawKeys))
            {
                rawKeys = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                          ?? Environment.GetEnvironmentVariable("Gemini__ApiKey")
                          ?? string.Empty;
            }

            _apiKeys = rawKeys
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(k => k.Trim())
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Distinct()
                .ToList();

            if (_options.TimeoutSeconds > 0)
            {
                _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
            }
        }

        public bool IsConfigured => _apiKeys.Count > 0;

        private string CurrentApiKey => _apiKeys.Count > 0 ? _apiKeys[_currentKeyIndex % _apiKeys.Count] : string.Empty;

        private void RotateApiKey()
        {
            if (_apiKeys.Count > 1)
            {
                _currentKeyIndex = (_currentKeyIndex + 1) % _apiKeys.Count;
                _logger.LogInformation("Rotated to next Gemini API key (index {Index}/{Total}).", _currentKeyIndex + 1, _apiKeys.Count);
            }
        }

        public async Task<string?> GenerateWithAudioAsync(string prompt, byte[] audio, string mimeType,
            CancellationToken cancellationToken = default)
        {
            if (!IsConfigured || audio.Length == 0) return null;
            try
            {
                var baseUrl = string.IsNullOrWhiteSpace(_options.BaseUrl)
                    ? "https://generativelanguage.googleapis.com/v1beta" : _options.BaseUrl.TrimEnd('/');
                var model = string.IsNullOrWhiteSpace(_options.Model) ? "gemini-3.7-flash" : _options.Model;
                var payload = JsonSerializer.Serialize(new
                {
                    contents = new[] { new { role = "user", parts = new object[] {
                        new { text = prompt }, new { inline_data = new { mime_type = mimeType, data = Convert.ToBase64String(audio) } }
                    } } },
                    generationConfig = new { temperature = 0.1, maxOutputTokens = 8192, responseMimeType = "application/json" }
                });
                return await ExecuteWithFallbackAsync(baseUrl, model, payload, "AudioTranscription", cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Gemini audio transcription was cancelled or timed out after {Timeout} seconds.", _options.TimeoutSeconds);
                throw;
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
            try
            {
                var baseUrl = string.IsNullOrWhiteSpace(_options.BaseUrl)
                    ? "https://generativelanguage.googleapis.com/v1beta" : _options.BaseUrl.TrimEnd('/');
                var model = string.IsNullOrWhiteSpace(_options.Model) ? "gemini-3.7-flash" : _options.Model;
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
                return await ExecuteWithFallbackAsync(baseUrl, model, payload, "HighlightAnalysis", cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Gemini highlight analysis was cancelled or timed out after {Timeout} seconds.", _options.TimeoutSeconds);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error calling Gemini highlight analysis.");
                return null;
            }
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

                var model = string.IsNullOrWhiteSpace(_options.Model) ? "gemini-3.7-flash" : _options.Model;

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

                return await ExecuteWithFallbackAsync(baseUrl, model, jsonPayload, "ChatbotGeneration", cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Gemini API call was cancelled or timed out after {Timeout} seconds.", _options.TimeoutSeconds);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected exception during Gemini API call.");
                return null;
            }
        }

        private IEnumerable<string> GetCandidateModels(string? preferredModel)
        {
            var candidates = new List<string>();
            if (!string.IsNullOrWhiteSpace(preferredModel))
            {
                candidates.Add(preferredModel.Trim());
            }

            // Các model Gemini đang hoạt động tốt nhất và có quota sẵn sàng
            candidates.Add("gemini-3.7-flash");
            candidates.Add("gemini-flash-latest");
            candidates.Add("gemini-3.8-flash");
            candidates.Add("gemini-3.5-flash-lite");
            candidates.Add("gemini-3.5-flash");

            return candidates.Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private async Task<string?> ExecuteWithFallbackAsync(
            string baseUrl,
            string? preferredModel,
            string jsonPayload,
            string operationName,
            CancellationToken cancellationToken)
        {
            var models = GetCandidateModels(preferredModel);
            string lastError = string.Empty;

            // Timeout linh hoạt cho từng tác vụ: Audio transcription cần nhiều thời gian hơn
            var perModelTimeoutSeconds = operationName switch
            {
                "AudioTranscription" => Math.Clamp(_options.TimeoutSeconds, 45, 180),
                "HighlightAnalysis" => Math.Clamp(_options.TimeoutSeconds, 30, 90),
                _ => Math.Clamp(_options.TimeoutSeconds, 30, 60)
            };

            foreach (var model in models)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var perModelCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                perModelCts.CancelAfter(TimeSpan.FromSeconds(perModelTimeoutSeconds));

                try
                {
                    using var response = await SendRequestAsync(baseUrl, model, jsonPayload, perModelCts.Token);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync(perModelCts.Token);
                        using var document = JsonDocument.Parse(responseBody);
                        if (document.RootElement.TryGetProperty("candidates", out var candidates) &&
                            candidates.GetArrayLength() > 0)
                        {
                            var firstCandidate = candidates[0];
                            if (firstCandidate.TryGetProperty("content", out var contentElem) &&
                                contentElem.TryGetProperty("parts", out var parts) &&
                                parts.GetArrayLength() > 0)
                            {
                                return parts[0].GetProperty("text").GetString();
                            }
                        }
                        _logger.LogWarning("Gemini model {Model} response for {Op} did not contain text candidates.", model, operationName);
                        continue;
                    }

                    var statusCode = (int)response.StatusCode;
                    lastError = await response.Content.ReadAsStringAsync(perModelCts.Token);

                    _logger.LogWarning(
                        "Gemini {Op} failed for model {Model} with status {Status}: {Body}. Trying next candidate model if available.",
                        operationName, model, statusCode, lastError);

                    if (statusCode == 429)
                    {
                        RotateApiKey();
                        continue;
                    }

                    if (statusCode == 400 && !lastError.Contains("model", StringComparison.OrdinalIgnoreCase) && !lastError.Contains("JSON mode", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Gemini model {Model} timed out after {Timeout}s for {Op}. Trying next candidate model...", model, perModelTimeoutSeconds, operationName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Exception calling Gemini {Op} on model {Model}. Trying next candidate.", operationName, model);
                }
            }

            _logger.LogError("All Gemini candidate models failed for {Op}. Last error: {LastError}", operationName, lastError);
            return null;
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
            request.Headers.TryAddWithoutValidation("x-goog-api-key", CurrentApiKey);

            return await _httpClient.SendAsync(request, cancellationToken);
        }
    }
}
