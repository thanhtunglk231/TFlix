using CoreLib.Dtos.Support;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebBrowser.Hubs
{
    public class SupportChatHub : Hub
    {
        private readonly ILogger<SupportChatHub> _logger;
        private readonly IHubContext<SupportChatHub> _hubContext;

        public SupportChatHub(ILogger<SupportChatHub> logger, IHubContext<SupportChatHub> hubContext)
        {
            _logger = logger;
            _hubContext = hubContext;
        }

        public async Task JoinSupportSession(string sessionId)
        {
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
                _logger.LogInformation("Connection {ConnectionId} joined support session {SessionId}", Context.ConnectionId, sessionId);
            }
        }

        public async Task LeaveSupportSession(string sessionId)
        {
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
            }
        }

        public async Task<SupportMessageDto> SendUserMessage(SendSupportMessageRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                throw new HubException("Nội dung tin nhắn không được để trống.");
            }

            if (string.IsNullOrWhiteSpace(request.SessionId))
            {
                request.SessionId = Context.ConnectionId;
            }

            var userMsg = new SupportMessageDto
            {
                SessionId = request.SessionId,
                UserId = request.UserId,
                SenderName = string.IsNullOrWhiteSpace(request.SenderName) ? "Bạn" : request.SenderName,
                SenderRole = "user",
                AvatarUrl = request.AvatarUrl,
                Message = request.Message.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            // Broadcast lại cho chính session đó (nếu mở nhiều tab)
            await Clients.Group(request.SessionId).SendAsync("ReceiveSupportMessage", userMsg);

            var sessionId = request.SessionId;
            var userText = request.Message;
            var movieTitle = request.MovieTitle;

            // Xử lý phản hồi tự động từ Trợ lý Tư vấn viên TFlix qua HubContext
            _ = Task.Run(async () =>
            {
                try
                {
                    // Thông báo "Tư vấn viên đang soạn tin..."
                    await Task.Delay(800);
                    await _hubContext.Clients.Group(sessionId).SendAsync("SupportTyping", true);

                    await Task.Delay(1200);

                    string botReply = GenerateSupportResponse(userText, movieTitle);

                    var botMsg = new SupportMessageDto
                    {
                        SessionId = sessionId,
                        SenderName = "TFlix Chăm Sóc Khách Hàng",
                        SenderRole = "agent",
                        AvatarUrl = "/images/support-agent.png",
                        Message = botReply,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _hubContext.Clients.Group(sessionId).SendAsync("SupportTyping", false);
                    await _hubContext.Clients.Group(sessionId).SendAsync("ReceiveSupportMessage", botMsg);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi phản hồi tự động trong SupportChatHub");
                }
            });

            return userMsg;
        }

        private static string GenerateSupportResponse(string userText, string? movieTitle)
        {
            var text = userText.ToLowerInvariant();

            if (text.Contains("giật") || text.Contains("lag") || text.Contains("chậm") || text.Contains("đơ") || text.Contains("xoay"))
            {
                return "Dạ để khắc phục tình trạng xem phim bị gián đoạn/xoay vòng, bạn có thể thử: \n1. Nhấn vào mục 'Máy chủ phát' dưới khung video và đổi sang Server dự phòng.\n2. Chọn hạ độ phân giải xuống '720p HD' thay vì 4K nếu đường truyền mạng đang không ổn định.\n3. Nhấn F5 tải lại hoặc xóa bộ nhớ đệm trình duyệt nhé!";
            }

            if (text.Contains("chất lượng") || text.Contains("4k") || text.Contains("1080") || text.Contains("độ phân giải") || text.Contains("mờ"))
            {
                return "TFlix hỗ trợ công nghệ Adaptive HLS Stream tự động tối ưu độ nét theo tốc độ mạng. Để xem ở mức cao nhất (1080p Full HD hoặc 4K), bạn bấm vào biểu tượng bánh răng / menu 'Độ phân giải' ngay trên thanh điều khiển trình phát để chọn thủ công nhé!";
            }

            if (text.Contains("lỗi") || text.Contains("không xem được") || text.Contains("đen màn") || text.Contains("404") || text.Contains("cors"))
            {
                return $"Đội ngũ kỹ thuật TFlix đã ghi nhận báo cáo cho nội dung {(string.IsNullOrWhiteSpace(movieTitle) ? "bộ phim này" : $"'{movieTitle}'")}. Hệ thống Stream Proxy đang tự động chuyển hướng đường truyền. Bạn hãy thử chọn tập khác hoặc máy chủ khác trong danh sách nhé!";
            }

            if (text.Contains("vip") || text.Contains("nâng cấp") || text.Contains("gói") || text.Contains("thanh toán") || text.Contains("giá"))
            {
                return "Tài khoản VIP TFlix mang đến trải nghiệm đỉnh cao: Không quảng cáo, chuẩn hình ảnh 4K HDR Dolby Vision, âm thanh vòm sống động và mở khóa toàn bộ kho phim chiếu rạp mới nhất. Bạn có thể xem bảng giá và nâng cấp tại mục Tài Khoản -> Nâng cấp gói VIP!";
            }

            if (text.Contains("gợi ý") || text.Contains("phim hay") || text.Contains("xem gì") || text.Contains("đề xuất"))
            {
                return "Hôm nay TFlix gợi ý bạn các siêu phẩm đang lọt top xem nhiều nhất: \n✨ Godzilla x Kong: Đế Chế Mới (Hành động 4K)\n✨ Lần Đầu Tôi Kể (Series tình cảm lãng mạn)\n✨ Oppenheimer (Điện ảnh đoạt giải Oscar)\nBạn có thể nhấn vào mục 'Kho phim' trên menu để khám phá thêm nhé!";
            }

            if (text.Contains("tập") || text.Contains("chuyển tập") || text.Contains("tập tiếp") || text.Contains("next"))
            {
                return "Bạn có thể bấm trực tiếp vào danh sách tập ở cột bên phải. Hệ thống TFlix áp dụng công nghệ chuyển tập tức thì không cần tải lại trang, giúp bạn xem liền mạch và khung tư vấn này sẽ luôn được giữ nguyên!";
            }

            if (text.Contains("xin chào") || text.Contains("hello") || text.Contains("hi") || text.Contains("chào") || text.Contains("alo"))
            {
                return "Dạ TFlix xin chào bạn! Tôi là tư vấn viên trực tuyến TFlix. Tôi có thể hỗ trợ gì cho trải nghiệm xem phim của bạn hôm nay ạ?";
            }

            return "Cảm ơn bạn đã nhắn tin cho TFlix Support! Đội ngũ tư vấn viên trực tuyến đã tiếp nhận thông tin của bạn. Nếu bạn gặp vấn đề về nguồn phát hoặc cần hỗ trợ tài khoản, bạn có thể chọn nhanh các câu hỏi gợi ý bên dưới hoặc gửi yêu cầu cụ thể để được hỗ trợ ngay lập tức.";
        }
    }
}
