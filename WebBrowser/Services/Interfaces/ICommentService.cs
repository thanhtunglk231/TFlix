using CoreLib.Dtos.Comment;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebBrowser.Services.Interfaces
{
    public interface ICommentService
    {
        Task<List<CommentDto>> GetCommentsByContentAsync(long? movieId, long? episodeId);
        Task<CommentDto?> AddCommentAsync(CreateCommentDto request);
    }
}
