using WebBrowser.Models;
using WebBrowser.Models.Movie;

namespace WebBrowser.Services.Interfaces
{
    public interface IPreviewService
    {
        Task<ApiResponse<MovieTableWrapper>> get_movie(int id);
    }
}