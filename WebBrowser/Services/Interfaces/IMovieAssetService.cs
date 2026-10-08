using CoreLib.Dtos.MovieAsset;
using CoreLib.Models;
using WebBrowser.Models;
using WebBrowser.Models.MovieAsset;

namespace WebBrowser.Services.Interfaces
{
    public interface IMovieAssetService
    {
        Task<CResponseMessage> add_MovieAsset(IFormFile file, AddMovieAssetDto dto);
        Task<CResponseMessage> delete(decimal id, string ownerType, string? fileUrl = null);
        Task<ApiResponse<MovieAssetTableWrapper>> getByid(decimal id, string ownerType);
        Task<ApiResponse<MovieAssetTableWrapper>> get_all(int pageSize = 25, string? cursorOwnerType = null, long? cursorAssetId = null, string? ownerType = null);
        Task<ApiResponse<MovieAssetTableWrapper>> get_owners();
        Task<CResponseMessage> replaceFile(decimal assetId, IFormFile file, UpdateMovieAssetDto updateDto, string? oldUrl = null);
    }
}
