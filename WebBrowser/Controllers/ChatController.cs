using System;
using System.Threading.Tasks;
using CoreLib.Dtos.Chat;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebBrowser.Models.AuthModels;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Controllers
{
    public class ChatController : Controller
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { success = false, message = "Nội dung tin nhắn không được để trống." });
            }

            var guestId = EnsureGuestId();

            // Nếu user đã đăng nhập, gắn UserId vào request
            var user = GetCurrentUser();
            if (user != null && user.userId > 0)
            {
                request.UserId = user.userId.ToString();
            }

            var result = await _chatService.SendMessageAsync(request, guestId);
            if (result == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "Dịch vụ TFlix AI hiện đang bận hoặc gặp sự cố kết nối. Vui lòng thử lại sau giây lát."
                });
            }

            return Json(new { success = true, data = result });
        }

        [HttpGet]
        public async Task<IActionResult> GetHistory([FromQuery] string conversationId)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                return BadRequest(new { success = false, message = "Conversation ID không hợp lệ." });
            }

            var guestId = EnsureGuestId();
            var result = await _chatService.GetHistoryAsync(conversationId, guestId);

            return Json(new { success = true, data = result });
        }

        [HttpPost]
        public async Task<IActionResult> ClearHistory([FromQuery] string conversationId)
        {
            if (string.IsNullOrWhiteSpace(conversationId))
            {
                return BadRequest(new { success = false, message = "Conversation ID không hợp lệ." });
            }

            var guestId = EnsureGuestId();
            var result = await _chatService.ClearHistoryAsync(conversationId, guestId);

            return Json(new { success = result });
        }

        private UserInfo? GetCurrentUser()
        {
            var userJson = HttpContext.Session.GetString("CurrentUser");
            if (!string.IsNullOrEmpty(userJson))
            {
                try
                {
                    return JsonConvert.DeserializeObject<UserInfo>(userJson);
                }
                catch { }
            }
            return null;
        }

        private string EnsureGuestId()
        {
            if (Request.Cookies.TryGetValue("tflix_guest_id", out var guestId) && !string.IsNullOrWhiteSpace(guestId))
            {
                return guestId;
            }

            var newGuestId = Guid.NewGuid().ToString("N");
            Response.Cookies.Append("tflix_guest_id", newGuestId, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                HttpOnly = true,
                SameSite = SameSiteMode.Lax
            });

            return newGuestId;
        }
    }
}
