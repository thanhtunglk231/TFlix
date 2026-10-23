// DataServiceLib/Interfaces/ICEpisode.cs
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

        // === Gán Movie vào Season ===
        Task<CResponseMessage> AssignMovieToSeason(AssignMovieToSeasonDto dto);
        Task<CResponseMessage> UpdateMovieInSeason(UpdateMovieInSeasonDto dto);
        Task<CResponseMessage> RemoveMovieFromSeason(long episodeId);
        Task<CResponseMessage> GetMoviesBySeason(long seasonId);
    }
}
