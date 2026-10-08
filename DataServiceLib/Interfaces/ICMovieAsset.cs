using CoreLib.Dtos.EpisodeAsset;
using CoreLib.Dtos.MovieAsset;
using CoreLib.Models;

namespace DataServiceLib.Interfaces
{
    public interface ICMovieAsset
    {
        Task<CResponseMessage> Add(AddMovieAssetDto dto);
        Task<CResponseMessage> Delete_episode(decimal assetId, string ownerType = "MOVIE");
        Task<CResponseMessage> sp_get_all(int pageSize = 25, string? cursorOwnerType = null, long? cursorAssetId = null, string? ownerType = null);
        Task<CResponseMessage> sp_get_by_id(decimal id, string ownerType = "MOVIE");
        Task<CResponseMessage> GetOwners();
        Task<CResponseMessage> Update_episode(UpdateMovieAssetDto dto);
    }
}
