using CoreLib.Dtos.Comment;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Hubs
{
    public class CommentHub : Hub
    {
        private readonly ICommentService _commentService;

        public CommentHub(ICommentService commentService)
        {
            _commentService = commentService;
        }

        public async Task JoinContentGroup(string groupKey)
        {
            if (!string.IsNullOrWhiteSpace(groupKey))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, groupKey);
            }
        }

        public async Task LeaveContentGroup(string groupKey)
        {
            if (!string.IsNullOrWhiteSpace(groupKey))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupKey);
            }
        }

        public async Task<CommentDto> SendComment(string groupKey, CreateCommentDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Content))
                throw new HubException("Nội dung bình luận không được để trống.");

            // Persist comment via CommentService
            CommentDto? created = await _commentService.AddCommentAsync(request);

            if (created == null)
            {
                throw new HubException("Không thể lưu bình luận vào database.");
            }

            if (string.IsNullOrWhiteSpace(groupKey))
            {
                groupKey = request.EpisodeId.HasValue && request.EpisodeId.Value > 0
                    ? $"ep_{request.EpisodeId.Value}"
                    : $"movie_{request.MovieId ?? 0}";
            }

            // Người gửi nhận dữ liệu qua kết quả invoke; chỉ broadcast cho các client khác.
            await Clients.OthersInGroup(groupKey).SendAsync("ReceiveComment", created);
            return created;
        }
    }
}
