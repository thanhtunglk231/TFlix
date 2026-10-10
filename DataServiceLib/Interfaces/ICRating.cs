using CoreLib.Dtos.Rating;
using CoreLib.Models;

namespace DataServiceLib.Interfaces;

public interface ICRating
{
    Task<CResponseMessage> GetMovieRatingAsync(long movieId, long? userId = null);
    Task<CResponseMessage> SetMovieRatingAsync(SetMovieRatingRequest request);
}
