using Newtonsoft.Json;
using WebBrowser.Models;
using WebBrowser.Models.Movie;
using WebBrowser.Models.VideoSoure;
using WebBrowser.Services.HttpSevice.Interfaces;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Services.Implements
{
    public class PreviewService : IPreviewService
    {
        private readonly IHttpService _httpService;
        private readonly IHttpContextAccessor _http;
        public PreviewService(IHttpService httpService, IHttpContextAccessor http)
        {
            _httpService = httpService;
            _http = http;
        }

        public async Task<ApiResponse<MovieTableWrapper>> get_movie(int id)
        {
             string url = $"/api/Preview/movie?movieId={id}";
            Console.WriteLine("[SeriesService] -> get_all ENTER url=" + url);

            var resp = await _httpService.GetAsync<ApiResponse<MovieTableWrapper>>(url);

            Console.WriteLine("[EpisodeService] <- get_all EXIT: " + JsonConvert.SerializeObject(resp));
            resp.success = resp.success || resp.code == "200";
            return resp;
        }
    }
}
