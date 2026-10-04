using CoreLib.Dtos.Favorite;

namespace WebBrowser.Services.Interfaces;

public interface IFavoriteService
{
    Task<List<FavoriteMovieDto>> GetMoviesAsync(long userId);
    Task<List<long>> GetMovieIdsAsync(long userId);
    Task<FavoriteToggleResult?> ToggleMovieAsync(long userId, long movieId);
}
