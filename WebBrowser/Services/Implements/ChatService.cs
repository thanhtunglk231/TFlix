using System;
using System.Threading.Tasks;
using CoreLib.Dtos.Chat;
using Microsoft.Extensions.Logging;
using WebBrowser.Models;
using WebBrowser.Services.HttpSevice.Interfaces;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Services.Implements
{
    public class ChatService : IChatService
    {
        private readonly IHttpService _httpService;
        private readonly ILogger<ChatService> _logger;

        public ChatService(IHttpService httpService, ILogger<ChatService> logger)
        {
            _httpService = httpService;
            _logger = logger;
        }

        public async Task<ChatResponseDto?> SendMessageAsync(ChatRequestDto request, string? guestId = null)
        {
            try
            {
                var response = await _httpService.PostAsync<ApiResponse<ChatResponseDto>>("/api/chat/message", request);
                if (response != null && response.success && response.Data != null)
                {
                    return response.Data;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi API chat/message từ WebBrowser");
            }
            return null;
        }

        public async Task<ChatHistoryDto?> GetHistoryAsync(string conversationId, string? guestId = null)
        {
            try
            {
                var url = $"/api/chat/history/{conversationId}" + (!string.IsNullOrWhiteSpace(guestId) ? $"?guestId={guestId}" : "");
                var response = await _httpService.GetAsync<ApiResponse<ChatHistoryDto>>(url);
                if (response != null && response.success && response.Data != null)
                {
                    return response.Data;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy lịch sử chat cho conversation {ConversationId}", conversationId);
            }
            return null;
        }

        public async Task<bool> ClearHistoryAsync(string conversationId, string? guestId = null)
        {
            try
            {
                var url = $"/api/chat/history/{conversationId}" + (!string.IsNullOrWhiteSpace(guestId) ? $"?guestId={guestId}" : "");
                var response = await _httpService.DeleteResponseAsync(url);
                return response?.Success ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa lịch sử chat cho conversation {ConversationId}", conversationId);
                return false;
            }
        }
    }
}
