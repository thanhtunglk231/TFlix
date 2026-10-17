using CoreLib.Dtos.EpisodeAsset;
using CoreLib.Dtos.MovieAsset;
using CoreLib.Models;

namespace DataServiceLib.Interfaces
{
    public interface ICMovieAsset
    {
        Task<CResponseMessage> Add(AddMovieAssetDto dto);
        Task<CResponseMessage> Delete_episode(decimal assetId, string ownerType = "MOVIE");
        Task<CResponseMessage> sp_get_all(int page = 1, int pageSize = 10, string? ownerType = null);
        Task<CResponseMessage> sp_get_by_id(decimal id, string ownerType = "MOVIE");
        Task<CResponseMessage> GetOwners();
        Task<CResponseMessage> GetByOwner(long ownerId, string ownerType);
        Task<CResponseMessage> Update_episode(UpdateMovieAssetDto dto);
    }
}
