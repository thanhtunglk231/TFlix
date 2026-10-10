using CoreLib.Dtos.Rating;
using CoreLib.Models;
using Newtonsoft.Json;
using WebBrowser.Services.HttpSevice.Interfaces;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Services.Implements;

public class RatingService : IRatingService
{
    private readonly IHttpService _httpService;

    public RatingService(IHttpService httpService)
    {
        _httpService = httpService;
    }

    public async Task<MovieRatingDto?> GetMovieRatingAsync(long movieId, long? userId = null)
    {
        var url = userId.HasValue && userId.Value > 0
            ? $"/api/Rating/movie?movieId={movieId}&userId={userId.Value}"
            : $"/api/Rating/movie?movieId={movieId}";
        var response = await _httpService.GetAsync<CResponseMessage>(url);
        return ConvertData<MovieRatingDto>(response);
    }

    public async Task<MovieRatingDto?> SetMovieRatingAsync(long userId, long movieId, int ratingVal)
    {
        var request = new SetMovieRatingRequest
        {
            UserId = userId,
            MovieId = movieId,
            RatingVal = ratingVal
        };
        var response = await _httpService.PostAsync<CResponseMessage>("/api/Rating/rate", request);
        return ConvertData<MovieRatingDto>(response);
    }

    private static T? ConvertData<T>(CResponseMessage? response)
    {
        if (response?.Success != true || response.Data == null) return default;
        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(response.Data));
    }
}
