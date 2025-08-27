// DataServiceLib/Implements/CEpisode.cs
using CoreLib.Dtos.Episode;
using CoreLib.Models;

namespace DataServiceLib.Interfaces
{
    public interface ICEpisode
    {
        Task<CResponseMessage> Add_episode(AddEpisodeDto addEpisodeDto);
        Task<CResponseMessage> Delete_episode(decimal episodeId);
        Task<CResponseMessage> sp_get_all_episode();
        Task<CResponseMessage> Update_episode(UpdateEpisodeDto dto);
        Task<CResponseMessage> sp_get_by_id(decimal id);
    }
}