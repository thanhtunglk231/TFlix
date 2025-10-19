using CoreLib.Dtos.MovieGenre;
using CoreLib.Models;
using WebBrowser.Models;
using WebBrowser.Models.Episode;
using WebBrowser.Models.MovieGenre;

namespace WebBrowser.Services.Interfaces
{
    public interface IMovieGenreService
    {
        Task<CResponseMessage> add_MovieGenre(AddMovieGenreDto addSeriesDto);
        Task<CResponseMessage> delete_Episode(DeleteMovieGenreDto id);
        Task<ApiResponse<MovieGenreTableWrapper>> get_byid(decimal id);
        Task<CResponseMessage> uppdate_MovieGenre(UpdateMovieGenreDto updateDto);
    }
}