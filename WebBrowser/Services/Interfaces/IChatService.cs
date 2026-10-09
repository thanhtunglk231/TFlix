using System.Threading.Tasks;
using CoreLib.Dtos.Chat;

namespace WebBrowser.Services.Interfaces
{
    public interface IChatService
    {
        Task<ChatResponseDto?> SendMessageAsync(ChatRequestDto request, string? guestId = null);
        Task<ChatHistoryDto?> GetHistoryAsync(string conversationId, string? guestId = null);
        Task<bool> ClearHistoryAsync(string conversationId, string? guestId = null);
    }
}
