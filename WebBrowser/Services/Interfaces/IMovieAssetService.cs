using CoreLib.Dtos.MovieAsset;
using CoreLib.Models;
using WebBrowser.Models;
using WebBrowser.Models.MovieAsset;

namespace WebBrowser.Services.Interfaces
{
    public interface IMovieAssetService
    {
        Task<CResponseMessage> add_MovieAsset(IFormFile file, AddMovieAssetDto dto);
        Task<CResponseMessage> delete(decimal id, string? fileUrl = null);
        Task<ApiResponse<MovieAssetTableWrapper>> getByid(decimal id);
        Task<ApiResponse<MovieAssetTableWrapper>> get_all();
        Task<CResponseMessage> replaceFile(decimal assetId, IFormFile file, UpdateMovieAssetDto updateDto, string? oldUrl = null);
    }
}