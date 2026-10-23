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
using CoreLib.Dtos.GeminiImage;
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

        public async Task<GeminiGenerateImageResponseDto> GenerateImageAsync(
            string prompt,
            string? aspectRatio = "16:9",
            string? model = null,
            string? apiKey = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                return new GeminiGenerateImageResponseDto
                {
                    Success = false,
                    ErrorMessage = "Prompt mô tả hình ảnh không được để trống."
                };
            }

            // Map aspect ratio for Imagen 3: "1:1", "3:4", "4:3", "9:16", "16:9"
            var mappedAspectRatio = (aspectRatio ?? "").Trim().ToLowerInvariant() switch
            {
                "2:3" => "3:4",
                "3:4" => "3:4",
                "1:1" => "1:1",
                "9:16" => "9:16",
                "4:3" => "4:3",
                _ => "16:9"
            };

            var effectiveKey = !string.IsNullOrWhiteSpace(apiKey) ? apiKey.Trim() : CurrentApiKey;
            string lastError = string.Empty;

            // 1. Thử gọi Google Gemini Imagen API nếu có key
            if (!string.IsNullOrWhiteSpace(effectiveKey))
            {
                var candidateModels = new List<string>();
                if (!string.IsNullOrWhiteSpace(model))
                {
                    candidateModels.Add(model.Trim());
                }
                candidateModels.Add("imagen-3.0-generate-002");
                candidateModels.Add("imagen-3.0-fast-generate-001");

                var distinctModels = candidateModels.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                var baseUrl = string.IsNullOrWhiteSpace(_options.BaseUrl)
                    ? "https://generativelanguage.googleapis.com/v1beta"
                    : _options.BaseUrl.TrimEnd('/');

                var payloadObj = new
                {
                    instances = new[]
                    {
                        new { prompt = prompt.Trim() }
                    },
                    parameters = new
                    {
                        sampleCount = 1,
                        aspectRatio = mappedAspectRatio,
                        outputMimeType = "image/jpeg"
                    }
                };
                var jsonPayload = JsonSerializer.Serialize(payloadObj);

                foreach (var candidateModel in distinctModels)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var endpoint = $"{baseUrl}/models/{candidateModel}:predict";

                    try
                    {
                        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                        {
                            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
                        };
                        request.Headers.TryAddWithoutValidation("x-goog-api-key", effectiveKey);

                        using var response = await _httpClient.SendAsync(request, cancellationToken);
                        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

                        if (response.IsSuccessStatusCode)
                        {
                            using var doc = JsonDocument.Parse(responseBody);
                            if (doc.RootElement.TryGetProperty("predictions", out var predictions) && predictions.GetArrayLength() > 0)
                            {
                                var firstPrediction = predictions[0];
                                string? base64 = null;
                                if (firstPrediction.TryGetProperty("bytesBase64Encoded", out var b64Elem))
                                {
                                    base64 = b64Elem.GetString();
                                }
                                var mime = firstPrediction.TryGetProperty("mimeType", out var mimeElem)
                                    ? mimeElem.GetString() ?? "image/jpeg"
                                    : "image/jpeg";

                                if (!string.IsNullOrWhiteSpace(base64))
                                {
                                    return new GeminiGenerateImageResponseDto
                                    {
                                        Success = true,
                                        Base64Data = base64,
                                        MimeType = mime,
                                        ImageUrl = $"data:{mime};base64,{base64}",
                                        ModelUsed = candidateModel
                                    };
                                }
                            }
                        }

                        lastError = responseBody;
                        _logger.LogWarning("Imagen model {Model} returned HTTP {Status}: {Body}", candidateModel, (int)response.StatusCode, responseBody);
                    }
                    catch (Exception ex)
                    {
                        lastError = ex.Message;
                        _logger.LogWarning(ex, "Exception calling Imagen model {Model}", candidateModel);
                    }
                }
            }

            // 2. Tự động chuyển sang Fallback tạo ảnh AI chất lượng cao (Pollinations FLUX)
            // Đảm bảo tạo ảnh tức thì không bị lỗi 404/429
            _logger.LogInformation("Chuyển sang cơ chế tạo ảnh AI điện ảnh độ phân giải cao (Pollinations FLUX fallback)...");
            var fallbackResult = await TryGenerateWithPollinationsAsync(prompt, mappedAspectRatio, cancellationToken);
            if (fallbackResult != null && fallbackResult.Success)
            {
                return fallbackResult;
            }

            var friendlyError = ExtractFriendlyGeminiError(lastError);

            return new GeminiGenerateImageResponseDto
            {
                Success = false,
                ErrorMessage = !string.IsNullOrWhiteSpace(friendlyError)
                    ? friendlyError
                    : "Không thể tạo ảnh từ dịch vụ AI. Vui lòng kiểm tra lại kết nối mạng hoặc thử lại sau."
            };
        }

        private async Task<GeminiGenerateImageResponseDto?> TryGenerateWithPollinationsAsync(
            string prompt,
            string mappedAspectRatio,
            CancellationToken cancellationToken)
        {
            try
            {
                var (width, height) = mappedAspectRatio switch
                {
                    "3:4" => (768, 1024),
                    "9:16" => (720, 1280),
                    "1:1" => (1024, 1024),
                    "4:3" => (1024, 768),
                    _ => (1280, 720) // 16:9
                };

                var randomSeed = Random.Shared.Next(100000, 999999);
                var encodedPrompt = Uri.EscapeDataString(prompt.Trim());
                var url = $"https://image.pollinations.ai/prompt/{encodedPrompt}?width={width}&height={height}&nologo=true&seed={randomSeed}&model=flux";

                _logger.LogInformation("Đang tải ảnh từ Pollinations AI FLUX ({Width}x{Height})...", width, height);

                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.TryAddWithoutValidation("User-Agent", "TFlix-OTT/1.0 (Windows NT 10.0; Win64; x64)");

                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                    if (bytes != null && bytes.Length > 2048)
                    {
                        var base64 = Convert.ToBase64String(bytes);
                        _logger.LogInformation("Tạo ảnh AI thành công ({Bytes} bytes).", bytes.Length);
                        return new GeminiGenerateImageResponseDto
                        {
                            Success = true,
                            Base64Data = base64,
                            MimeType = "image/jpeg",
                            ImageUrl = $"data:image/jpeg;base64,{base64}",
                            ModelUsed = "pollinations-ai-flux"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi gọi Pollinations AI fallback.");
            }
            return null;
        }

        public async Task<string?> EnhancePromptAsync(
            string description,
            string? style = null,
            string? title = null,
            string? apiKey = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(description))
                return null;

            var styleDesc = string.IsNullOrWhiteSpace(style) ? "Cinematic Movie Poster" : style;
            var titleInfo = string.IsNullOrWhiteSpace(title) ? "" : $"Movie: \"{title}\". ";

            var effectiveKey = !string.IsNullOrWhiteSpace(apiKey) ? apiKey.Trim() : CurrentApiKey;
            if (!string.IsNullOrWhiteSpace(effectiveKey))
            {
                var systemInstruction = "You are an expert cinematic visual concept artist and prompt engineer for high-end movie posters, key visuals, and streaming platform banners. " +
                    "Transform the user's description into an evocative, highly-detailed English prompt suitable for AI image generation. " +
                    "Specify cinematic lighting, camera angle, atmospheric depth, texture, and mood. Do NOT include text or titles in the image. " +
                    "Return ONLY the final prompt in English, with NO introduction, NO quotes, NO markdown formatting, under 120 words.";

                var userPrompt = $"{titleInfo}Style: {styleDesc}. Description: {description}";

                try
                {
                    var baseUrl = string.IsNullOrWhiteSpace(_options.BaseUrl)
                        ? "https://generativelanguage.googleapis.com/v1beta"
                        : _options.BaseUrl.TrimEnd('/');
                    var model = string.IsNullOrWhiteSpace(_options.Model) ? "gemini-3.7-flash" : _options.Model;

                    var requestBody = new
                    {
                        system_instruction = new { parts = new object[] { new { text = systemInstruction } } },
                        contents = new[]
                        {
                            new { role = "user", parts = new object[] { new { text = userPrompt } } }
                        },
                        generationConfig = new
                        {
                            temperature = 0.7,
                            maxOutputTokens = 300,
                            topP = 0.95
                        }
                    };

                    var json = JsonSerializer.Serialize(requestBody);
                    var models = GetCandidateModels(model);

                    foreach (var m in models)
                    {
                        var endpoint = $"{baseUrl}/models/{m}:generateContent";
                        using var req = new HttpRequestMessage(HttpMethod.Post, endpoint)
                        {
                            Content = new StringContent(json, Encoding.UTF8, "application/json")
                        };
                        req.Headers.TryAddWithoutValidation("x-goog-api-key", effectiveKey);

                        using var resp = await _httpClient.SendAsync(req, cancellationToken);
                        if (resp.IsSuccessStatusCode)
                        {
                            var body = await resp.Content.ReadAsStringAsync(cancellationToken);
                            using var doc = JsonDocument.Parse(body);
                            if (doc.RootElement.TryGetProperty("candidates", out var cands) && cands.GetArrayLength() > 0)
                            {
                                var firstCand = cands[0];
                                if (firstCand.TryGetProperty("content", out var contentElem) &&
                                    contentElem.TryGetProperty("parts", out var parts) &&
                                    parts.GetArrayLength() > 0)
                                {
                                    var text = parts[0].GetProperty("text").GetString();
                                    if (!string.IsNullOrWhiteSpace(text))
                                        return text.Trim().Trim('"', '\'');
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to enhance prompt using Gemini, using cinematic heuristic fallback.");
                }
            }

            // Heuristic cinematic prompt fallback nếu API hết quota
            return $"Cinematic masterpiece film still for {titleInfo}Art style: {styleDesc}. Atmospheric volumetric lighting, shallow depth of field, 8k resolution, IMAX composition, hyper-realistic cinematic texture: {description}.";
        }

        private static string ExtractFriendlyGeminiError(string rawError)
        {
            if (string.IsNullOrWhiteSpace(rawError)) return string.Empty;

            try
            {
                using var doc = JsonDocument.Parse(rawError);
                if (doc.RootElement.TryGetProperty("error", out var errElem))
                {
                    var msg = errElem.TryGetProperty("message", out var msgElem) ? msgElem.GetString() : null;
                    var status = errElem.TryGetProperty("status", out var statusElem) ? statusElem.GetString() : null;

                    if (status == "RESOURCE_EXHAUSTED" || (msg != null && msg.Contains("quota", StringComparison.OrdinalIgnoreCase)))
                    {
                        return "Gemini API Key đã hết hạn mức (Quota limit reached / RESOURCE_EXHAUSTED). Vui lòng thử lại sau hoặc nhập API Key khác.";
                    }
                    if (status == "PERMISSION_DENIED" || (msg != null && (msg.Contains("API_KEY_INVALID", StringComparison.OrdinalIgnoreCase) || msg.Contains("API key not valid", StringComparison.OrdinalIgnoreCase))))
                    {
                        return "Gemini API Key không hợp lệ hoặc không có quyền truy cập Imagen API. Vui lòng kiểm tra lại key.";
                    }
                    if (!string.IsNullOrWhiteSpace(msg))
                    {
                        return $"Lỗi từ Google Gemini: {msg}";
                    }
                }
            }
            catch
            {
                // Not JSON
            }

            if (rawError.Contains("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase) || rawError.Contains("quota", StringComparison.OrdinalIgnoreCase))
            {
                return "Gemini API Key đã hết hạn mức (Quota limit reached). Vui lòng đổi API Key khác.";
            }

            return string.Empty;
        }
    }
}
