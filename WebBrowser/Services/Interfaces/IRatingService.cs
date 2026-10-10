using CoreLib.Dtos.Rating;

namespace WebBrowser.Services.Interfaces;

public interface IRatingService
{
    Task<MovieRatingDto?> GetMovieRatingAsync(long movieId, long? userId = null);
    Task<MovieRatingDto?> SetMovieRatingAsync(long userId, long movieId, int ratingVal);
}
