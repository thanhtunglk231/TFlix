using CoreLib.Dtos.Genres;
using CoreLib.Models;
using WebBrowser.Models;
using WebBrowser.Models.Genres;
using WebBrowser.Models.Movie;

namespace WebBrowser.Services.Interfaces
{
    public interface IGenresService
    {
        Task<CResponseMessage> add_Genre(AddGenreDto dto);
        Task<CResponseMessage> update_Genre(UpdateGenreDto dto);
        Task<CResponseMessage> delete_Genre(decimal id);
        Task<ApiResponse<GenreTableWrapper>> get_all();
    }
}