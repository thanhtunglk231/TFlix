// DataServiceLib/Implements/CEpisode.cs
using CoreLib.Dtos.Episode;
using CoreLib.Models;

namespace DataServiceLib.Interfaces
{
    public interface ICEpisode
    {
        Task<CResponseMessage> Add_episode(AddEpisodeDto addEpisodeDto);
        Task<CResponseMessage> Delete_episode(long episodeId);
        Task<CResponseMessage> sp_get_all_episode();
        Task<CResponseMessage> GetBySeriesAsync(long seriesId, bool publishedOnly);
        Task<CResponseMessage> Update_episode(UpdateEpisodeDto dto);
    }
}
