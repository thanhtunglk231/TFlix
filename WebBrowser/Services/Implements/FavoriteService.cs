using CoreLib.Dtos.Favorite;
using CoreLib.Models;
using Newtonsoft.Json;
using WebBrowser.Services.HttpSevice.Interfaces;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Services.Implements;

public class FavoriteService : IFavoriteService
{
    private readonly IHttpService _httpService;
    public FavoriteService(IHttpService httpService) => _httpService = httpService;

    public async Task<List<FavoriteMovieDto>> GetMoviesAsync(long userId)
        => await GetDataAsync<List<FavoriteMovieDto>>($"/api/Favorite/movies?userId={userId}") ?? new();

    public async Task<List<long>> GetMovieIdsAsync(long userId)
        => await GetDataAsync<List<long>>($"/api/Favorite/movie-ids?userId={userId}") ?? new();

    public async Task<FavoriteToggleResult?> ToggleMovieAsync(long userId, long movieId)
    {
        var response = await _httpService.PostAsync<CResponseMessage>("/api/Favorite/toggle", new FavoriteMovieRequest { UserId = userId, MovieId = movieId });
        return ConvertData<FavoriteToggleResult>(response);
    }

    private async Task<T?> GetDataAsync<T>(string url)
    {
        var response = await _httpService.GetAsync<CResponseMessage>(url);
        return ConvertData<T>(response);
    }

    private static T? ConvertData<T>(CResponseMessage? response)
    {
        if (response?.Success != true || response.Data == null) return default;
        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(response.Data));
    }
}
