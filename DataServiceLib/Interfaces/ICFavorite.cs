using CoreLib.Dtos.Favorite;
using CoreLib.Models;

namespace DataServiceLib.Interfaces;

public interface ICFavorite
{
    Task<CResponseMessage> GetMoviesAsync(long userId);
    Task<CResponseMessage> GetMovieIdsAsync(long userId);
    Task<CResponseMessage> ToggleMovieAsync(FavoriteMovieRequest request);
}
