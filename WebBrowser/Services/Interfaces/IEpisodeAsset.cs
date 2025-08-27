using CoreLib.Dtos.EpisodeAsset;
using CoreLib.Models;
using WebBrowser.Models;
using WebBrowser.Models.EpisodeAsset;

namespace WebBrowser.Services.Interfaces
{
    public interface IEpisodeAsset
    {
        Task<CResponseMessage> add_Episode(IFormFile file, AddEpisodeAsset dto);
        Task<CResponseMessage> delete(decimal id, string? fileUrl = null);
        Task<ApiResponse<EpisodeAssetTableWrapper>> get_all();
        Task<CResponseMessage> uppdate(decimal id, IFormFile file, UpdateEpisodeAsset updateDto);
        Task<ApiResponse<EpisodeAssetTableWrapper>> getByid(decimal id);
    }
}