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
using CoreLib.Dtos.GeminiImage;
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

        public async Task<string?> GenerateWithImagesAsync(string prompt, IReadOnlyList<byte[]> jpegImages,
            CancellationToken cancellationToken = default)
        {
            if (!IsConfigured || jpegImages.Count == 0) return null;
            var content = new List<object> { new { type = "input_text", text = prompt } };
            foreach (var image in jpegImages.Take(8))
                content.Add(new { type = "input_image", image_url = "data:image/jpeg;base64," + Convert.ToBase64String(image) });
            var payload = new { model = _options.VisionModel, input = new[] { new { role = "user", content } }, max_output_tokens = 700 };
            return await SendForTextAsync(payload, "OpenAI vision request", cancellationToken);
        }

        public async Task<string?> TranscribeAudioAsync(byte[] audio, string mimeType, string prompt,
            CancellationToken cancellationToken = default)
        {
            if (!IsConfigured || audio.Length == 0) return null;
            using var content = new MultipartFormDataContent();
            var audioContent = new ByteArrayContent(audio);
            audioContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType);
            content.Add(audioContent, "file", "audio" + GetAudioExtension(mimeType));
            content.Add(new StringContent(_options.TranscriptionModel), "model");
            content.Add(new StringContent("verbose_json"), "response_format");
            content.Add(new StringContent(prompt), "prompt");
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/audio/transcriptions") { Content = content };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            try
            {
                using var response = await _httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode) { _logger.LogWarning("OpenAI transcription request failed with status {StatusCode}.", (int)response.StatusCode); return null; }
                return await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException) { _logger.LogWarning(ex, "OpenAI transcription request failed."); return null; }
        }

        public async Task<GeminiGenerateImageResponseDto> GenerateImageAsync(string prompt, string? aspectRatio = "16:9",
            CancellationToken cancellationToken = default)
        {
            if (!IsConfigured) return new GeminiGenerateImageResponseDto { Success = false, ErrorMessage = "OpenAI API chưa được cấu hình." };
            var payload = new { model = _options.ImageModel, prompt, size = ToImageSize(aspectRatio), quality = _options.ImageQuality, response_format = "b64_json" };
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/images/generations") { Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json") };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            try
            {
                using var response = await _httpClient.SendAsync(request, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode) return new GeminiGenerateImageResponseDto { Success = false, ErrorMessage = "OpenAI không thể tạo ảnh." };
                using var document = JsonDocument.Parse(body);
                var item = document.RootElement.GetProperty("data")[0];
                return new GeminiGenerateImageResponseDto { Success = item.TryGetProperty("b64_json", out var b64), Base64Data = b64.GetString(), MimeType = "image/png", ModelUsed = _options.ImageModel };
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException) { _logger.LogWarning(ex, "OpenAI image request failed."); return new GeminiGenerateImageResponseDto { Success = false, ErrorMessage = "Không thể tạo ảnh bằng OpenAI." }; }
        }

        private async Task<string?> SendForTextAsync(object payload, string operation, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, ResponsesEndpoint) { Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json") };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            try { using var response = await _httpClient.SendAsync(request, cancellationToken); if (!response.IsSuccessStatusCode) { _logger.LogWarning("{Operation} failed with status {StatusCode}.", operation, (int)response.StatusCode); return null; } using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken)); return document.RootElement.TryGetProperty("output_text", out var text) ? text.GetString() : ExtractOutputText(document.RootElement); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException) { _logger.LogWarning(ex, "{Operation} failed.", operation); return null; }
        }

        private static string ToImageSize(string? aspectRatio) => aspectRatio switch { "3:4" or "2:3" or "9:16" => "1024x1536", "1:1" => "1024x1024", _ => "1536x1024" };
        private static string GetAudioExtension(string mimeType) => mimeType.Contains("wav", StringComparison.OrdinalIgnoreCase) ? ".wav" : mimeType.Contains("webm", StringComparison.OrdinalIgnoreCase) ? ".webm" : mimeType.Contains("mp4", StringComparison.OrdinalIgnoreCase) ? ".m4a" : ".mp3";

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
