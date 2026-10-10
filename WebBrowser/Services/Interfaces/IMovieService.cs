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
        Task<ApiResponse<List<MovieAutocompleteItemDto>>> Autocomplete(string query, int limit = 8);
        Task<CResponseMessage> delete_Season(decimal id, long? userId = null);
        Task<ApiResponse<MovieTableWrapper>> get_all(long? userId = null);
        Task<CResponseMessage> uppdate_Movie(UpdateMovieDto updateDto);
        Task<CResponseMessage> GetCatalogMovies(MovieCatalogFilterDto filter);
    }
}