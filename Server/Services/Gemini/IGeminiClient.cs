using System.Threading;
using System.Threading.Tasks;
using CoreLib.Dtos.Chat;
using CoreLib.Dtos.GeminiImage;

namespace Server.Services.Gemini
{
    public interface IGeminiClient
    {
        bool IsConfigured { get; }
        Task<string?> GenerateContentAsync(
            string systemInstruction,
            IEnumerable<ChatMessageDto> history,
            string currentPrompt,
            CancellationToken cancellationToken = default);
        Task<string?> GenerateWithImagesAsync(string prompt, IReadOnlyList<byte[]> jpegImages,
            CancellationToken cancellationToken = default);
        Task<string?> GenerateWithAudioAsync(string prompt, byte[] audio, string mimeType,
            CancellationToken cancellationToken = default);
        Task<GeminiGenerateImageResponseDto> GenerateImageAsync(
            string prompt,
            string? aspectRatio = "16:9",
            string? model = null,
            string? apiKey = null,
            CancellationToken cancellationToken = default);
        Task<string?> EnhancePromptAsync(
            string description,
            string? style = null,
            string? title = null,
            string? apiKey = null,
            CancellationToken cancellationToken = default);
    }
}
