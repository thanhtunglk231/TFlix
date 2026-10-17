using CoreLib.Models;
using WebBrowser.Models;
using WebBrowser.Models.VideoHighlight;
using WebBrowser.Services.HttpSevice.Interfaces;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Services.Implements;

public sealed class VideoHighlightService : IVideoHighlightService
{
    private readonly IHttpService _http;
    public VideoHighlightService(IHttpService http) => _http = http;
    public Task<ApiResponse<VideoHighlightTableWrapper>> GetAllAsync(int page = 1, int pageSize = 10, bool activeOnly = false)
        => _http.GetAsync<ApiResponse<VideoHighlightTableWrapper>>($"/api/VideoHighlights?page={page}&pageSize={pageSize}&activeOnly={activeOnly.ToString().ToLowerInvariant()}");
    public Task<CResponseMessage> GenerateAsync(long sourceId)
        => _http.PostLongRunningAsync<CResponseMessage>($"/api/VideoHighlights/generate/{sourceId}", new { });
    public Task<CResponseMessage> DeleteAsync(long highlightId, string? videoUrl, string? thumbnailUrl)
        => _http.DeleteResponseAsync($"/api/VideoHighlights/{highlightId}?videoUrl={Uri.EscapeDataString(videoUrl ?? "")}&thumbnailUrl={Uri.EscapeDataString(thumbnailUrl ?? "")}");
}
