using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using CoreLib.Dtos.Chat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Server.Services.Chat;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatbotService _chatbotService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IChatbotService chatbotService, ILogger<ChatController> logger)
        {
            _chatbotService = chatbotService;
            _logger = logger;
        }

        [HttpPost("message")]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequestDto request, CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { success = false, message = "Nội dung tin nhắn không được để trống." });
            }

            var userKey = ResolveUserKey(request.UserId);

            try
            {
                var response = await _chatbotService.ProcessMessageAsync(request, userKey, cancellationToken);
                return Ok(new
                {
                    success = true,
                    data = response
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Rate limit triggered
                return StatusCode(StatusCodes.Status429TooManyRequests, new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi xử lý tin nhắn chat của {UserKey}", userKey);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi trong quá trình xử lý tin nhắn tư vấn."
                });
            }
        }

        [HttpGet("history/{conversationId}")]
        public async Task<IActionResult> GetHistory(string conversationId, [FromQuery] string? guestId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                return BadRequest(new { success = false, message = "Mã hội thoại không hợp lệ." });
            }

            var userKey = ResolveUserKey(guestId);
            var history = await _chatbotService.GetHistoryAsync(conversationId, userKey, cancellationToken);

            return Ok(new
            {
                success = true,
                data = history
            });
        }

        [HttpDelete("history/{conversationId}")]
        public async Task<IActionResult> ClearHistory(string conversationId, [FromQuery] string? guestId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                return BadRequest(new { success = false, message = "Mã hội thoại không hợp lệ." });
            }

            var userKey = ResolveUserKey(guestId);
            var success = await _chatbotService.ClearHistoryAsync(conversationId, userKey, cancellationToken);

            return Ok(new
            {
                success = success,
                message = success ? "Đã xóa lịch sử hội thoại." : "Không tìm thấy lịch sử để xóa."
            });
        }

        private string ResolveUserKey(string? clientProvidedId)
        {
            // Kiểm tra Claims xem đã đăng nhập chưa
            var userIdClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User?.FindFirst("userId")?.Value
                              ?? User?.FindFirst("sub")?.Value;

            if (!string.IsNullOrWhiteSpace(userIdClaim))
            {
                return $"user_{userIdClaim}";
            }

            // Nếu là khách, dùng header X-Guest-Id hoặc clientProvidedId hoặc remote IP
            var headerGuestId = Request.Headers["X-Guest-Id"].ToString();
            if (!string.IsNullOrWhiteSpace(headerGuestId))
            {
                return $"guest_{headerGuestId.Trim()}";
            }

            if (!string.IsNullOrWhiteSpace(clientProvidedId))
            {
                return $"guest_{clientProvidedId.Trim()}";
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "anon";
            return $"guest_{ip}";
        }
    }
}
