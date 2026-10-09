using System.Threading;
using System.Threading.Tasks;
using CoreLib.Dtos.Chat;

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
    }
}
