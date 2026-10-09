using System.Threading;
using System.Threading.Tasks;
using CoreLib.Dtos.Chat;

namespace Server.Services.Chat
{
    public interface IChatbotService
    {
        Task<ChatResponseDto> ProcessMessageAsync(ChatRequestDto request, string userKey, CancellationToken cancellationToken = default);
        Task<ChatHistoryDto> GetHistoryAsync(string conversationId, string userKey, CancellationToken cancellationToken = default);
        Task<bool> ClearHistoryAsync(string conversationId, string userKey, CancellationToken cancellationToken = default);
    }
}
