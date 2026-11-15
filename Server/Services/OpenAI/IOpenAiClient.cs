using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CoreLib.Dtos.Chat;
using CoreLib.Dtos.GeminiImage;

namespace Server.Services.OpenAI
{
    public interface IOpenAiClient
    {
        bool IsConfigured { get; }

        Task<string?> GenerateContentAsync(string systemInstruction, IEnumerable<ChatMessageDto> history,
            string currentPrompt, CancellationToken cancellationToken = default);
        Task<string?> GenerateWithImagesAsync(string prompt, IReadOnlyList<byte[]> jpegImages,
            CancellationToken cancellationToken = default);
        Task<string?> TranscribeAudioAsync(byte[] audio, string mimeType, string prompt,
            CancellationToken cancellationToken = default);
        Task<GeminiGenerateImageResponseDto> GenerateImageAsync(string prompt, string? aspectRatio = "16:9",
            CancellationToken cancellationToken = default);
    }
}
