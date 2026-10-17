using CoreLib.Models;
using WebBrowser.Models;
using WebBrowser.Models.VideoHighlight;

namespace WebBrowser.Services.Interfaces;

public interface IVideoHighlightService
{
    Task<ApiResponse<VideoHighlightTableWrapper>> GetAllAsync(int page = 1, int pageSize = 10, bool activeOnly = false);
    Task<CResponseMessage> GenerateAsync(long sourceId);
    Task<CResponseMessage> DeleteAsync(long highlightId, string? videoUrl, string? thumbnailUrl);
}
