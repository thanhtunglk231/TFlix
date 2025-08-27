using CoreLib.Dtos.Movies;
using CoreLib.Models;
using WebBrowser.Models;
using WebBrowser.Models.Movie;
using WebBrowser.Models.Season;

namespace WebBrowser.Services.Interfaces
{
    public interface IMovieService
    {
        Task<CResponseMessage> add_Movie(AddMovieDto addSeriesDto);
        Task<CResponseMessage> delete_Season(decimal id);
        Task<ApiResponse<MovieTableWrapper>> get_all();
        Task<CResponseMessage> uppdate_Movie(AddMovieDto updateDto);
    }
}