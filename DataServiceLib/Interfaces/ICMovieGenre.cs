using CoreLib.Dtos.MovieGenre;
using CoreLib.Models;

namespace DataServiceLib.Interfaces
{
    public interface ICMovieGenre
    {
        Task<CResponseMessage> sp_add(AddMovieGenreDto addMovieAssetDto);
        Task<CResponseMessage> sp_delete(DeleteMovieGenreDto deleteMovieGenreDto);
        Task<CResponseMessage> sp_get_by_genre(decimal genreId);
        Task<CResponseMessage> sp_get_by_movie(decimal movieId);
        Task<CResponseMessage> sp_get_by_pk(decimal movieId, decimal genreId);
        Task<CResponseMessage> sp_update(UpdateMovieGenreDto updateMovieGenreDto);
    }
}