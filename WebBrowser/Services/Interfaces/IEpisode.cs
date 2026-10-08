using CoreLib.Dtos.Episode;
using CoreLib.Models;
using WebBrowser.Models;
using WebBrowser.Models.Episode;
using WebBrowser.Models.Season;

namespace WebBrowser.Services.Interfaces
{
    public interface IEpisode
    {
        Task<CResponseMessage> add_Episode(AddEpisodeDto addSeriesDto);
        Task<CResponseMessage> delete_Episode(long id);
        Task<ApiResponse<EpisodeTableWrapper>> get_all();
        Task<ApiResponse<EpisodeTableWrapper>> GetBySeriesAsync(long seriesId, bool publishedOnly = true);
        Task<CResponseMessage> uppdate_Episode(UpdateEpisodeDto updateDto);
    }
}
