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

        public async Task SendComment(string groupKey, CreateCommentDto request)
        {
            if (string.IsNullOrWhiteSpace(request?.Content)) return;

            // Persist comment via CommentService
            CommentDto? created = await _commentService.AddCommentAsync(request);

            if (created == null)
            {
                // Fallback comment object for realtime broadcast if service failed
                created = new CommentDto
                {
                    CommentId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    MovieId = request.MovieId,
                    EpisodeId = request.EpisodeId,
                    UserId = request.UserId,
                    UserName = string.IsNullOrWhiteSpace(request.UserName) ? "Người dùng TFlix" : request.UserName,
                    UserAvatar = !string.IsNullOrWhiteSpace(request.UserAvatar)
                        ? request.UserAvatar
                        : $"https://i.pravatar.cc/40?u={request.UserId}",
                    Content = request.Content,
                    CreatedAt = DateTime.Now,
                    FormattedTime = "Vừa xong"
                };
            }

            if (string.IsNullOrWhiteSpace(groupKey))
            {
                groupKey = request.EpisodeId.HasValue && request.EpisodeId.Value > 0
                    ? $"ep_{request.EpisodeId.Value}"
                    : $"movie_{request.MovieId ?? 0}";
            }

            // Broadcast to all clients in group
            await Clients.Group(groupKey).SendAsync("ReceiveComment", created);
        }
    }
}
