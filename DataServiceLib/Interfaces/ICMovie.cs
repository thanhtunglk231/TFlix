using CoreLib.Dtos.Movies;
using CoreLib.Models;

namespace DataServiceLib.Interfaces
{
    public interface ICMovie
    {
        Task<CResponseMessage> Add_movie(AddMovieDto addMovieDto);
        Task<CResponseMessage> Delete_movie(decimal movieId);
        Task<CResponseMessage> get_all();
        Task<CResponseMessage> Update_movie(UpdateMovieDto updateMovieDto);
    }
}