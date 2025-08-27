using CoreLib.Dtos.EpisodeAsset;
using CoreLib.Dtos.MovieAsset;
using CoreLib.Models;

namespace DataServiceLib.Interfaces
{
    public interface ICMovieAsset
    {
        Task<CResponseMessage> Add(AddMovieAssetDto dto);
        Task<CResponseMessage> Delete_episode(decimal assetId);
        Task<CResponseMessage> sp_get_all();
        Task<CResponseMessage> sp_get_by_id(decimal id);
        Task<CResponseMessage> Update_episode(UpdateMovieAssetDto dto);
    }
}