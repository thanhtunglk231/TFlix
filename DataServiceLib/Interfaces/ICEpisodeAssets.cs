using CoreLib.Dtos.EpisodeAsset;
using CoreLib.Models;

namespace DataServiceLib.Interfaces
{
    public interface ICEpisodeAssets
    {
        Task<CResponseMessage> Add(AddEpisodeAsset addEpisodeDto);
        Task<CResponseMessage> Delete_episode(decimal episodeId);
        Task<CResponseMessage> sp_get_all();
        Task<CResponseMessage> Update_episode(UpdateEpisodeAsset dto);
        Task<CResponseMessage> sp_get_by_id(decimal id);
    }
}