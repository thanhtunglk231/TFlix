using CoreLib.Dtos.Movies;
using CoreLib.Models;

namespace DataServiceLib.Interfaces
{
    public interface ICMovie
    {
        Task<CResponseMessage> Add_movie(AddMovieDto addMovieDto);
        Task<CResponseMessage> Autocomplete(MovieAutocompleteQueryDto request);
        Task<CResponseMessage> Delete_movie(decimal movieId, long? userId = null);
        Task<CResponseMessage> get_all(long? userId = null);
        Task<CResponseMessage> Update_movie(UpdateMovieDto updateMovieDto);
        Task<CResponseMessage> GetCatalogMovies(MovieCatalogFilterDto filter);
        Task<CResponseMessage> SeedSampleMovies();
    }
}