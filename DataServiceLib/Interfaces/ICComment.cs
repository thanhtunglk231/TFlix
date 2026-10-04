using CoreLib.Dtos.Comment;
using CoreLib.Models;
using System.Threading.Tasks;

namespace DataServiceLib.Interfaces
{
    public interface ICComment
    {
        Task<CResponseMessage> GetCommentsByContentAsync(long? movieId, long? episodeId);
        Task<CResponseMessage> AddCommentAsync(CreateCommentDto request);
    }
}
