using CoreLib.Dtos.Episode;
using CoreLib.Dtos.Season;
using CoreLib.Models;
using Newtonsoft.Json;
using WebBrowser.Models;
using WebBrowser.Models.Season;
using WebBrowser.Services.HttpSevice.Interfaces;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Services.Implements
{
    public class SesonService : ISesonService
    {
        private readonly IHttpService _httpService;
        private readonly IHttpContextAccessor _http;

        public SesonService(IHttpService httpService, IHttpContextAccessor http)
        {
            _httpService = httpService;
            _http = http;
        }

        public async Task<CResponseMessage> add_season(AddSeasonDto addSeriesDto)
        {
            const string url = "/api/Season/add";
            var resp = await _httpService.PostAsync<CResponseMessage>(url, addSeriesDto);
            return resp!;
        }

        public async Task<CResponseMessage> uppdate_Season(UpdateSeasonDto updateDto)
        {
            const string url = "/api/Season/update";
            var resp = await _httpService.PostAsync<CResponseMessage>(url, updateDto);
            return resp!;
        }

        public async Task<CResponseMessage> delete_Season(decimal id)
        {
            const string url = "/api/Season/delete";
            var resp = await _httpService.PostAsync<CResponseMessage>(url, new { id });
            return resp!;
        }

        public async Task<ApiResponse<SeasonTableWrapper>> get_all()
        {
            const string url = "/api/Season/getall";
            var resp = await _httpService.GetAsync<ApiResponse<SeasonTableWrapper>>(url);
            resp.success = resp.success || resp.code == "200";
            return resp;
        }

        // === Gan Movie vao Season ===
        public async Task<CResponseMessage> AssignMovie(AssignMovieToSeasonDto dto)
        {
            const string url = "/api/Episode/assign-movie";
            Console.WriteLine("[SesonService] -> AssignMovie ENTER url=" + url);
            var resp = await _httpService.PostAsync<CResponseMessage>(url, dto);
            Console.WriteLine("[SesonService] <- AssignMovie EXIT: " + JsonConvert.SerializeObject(resp));
            return resp!;
        }

        public async Task<CResponseMessage> UpdateMovieInSeason(UpdateMovieInSeasonDto dto)
        {
            const string url = "/api/Episode/update-movie";
            Console.WriteLine("[SesonService] -> UpdateMovieInSeason ENTER url=" + url);
            var resp = await _httpService.PostAsync<CResponseMessage>(url, dto);
            Console.WriteLine("[SesonService] <- UpdateMovieInSeason EXIT: " + JsonConvert.SerializeObject(resp));
            return resp!;
        }

        public async Task<CResponseMessage> RemoveMovie(long episodeId)
        {
            const string url = "/api/Episode/remove-movie";
            Console.WriteLine($"[SesonService] -> RemoveMovie ENTER episodeId={episodeId}");
            var resp = await _httpService.PostAsync<CResponseMessage>(url, episodeId);
            Console.WriteLine("[SesonService] <- RemoveMovie EXIT: " + JsonConvert.SerializeObject(resp));
            return resp!;
        }

        public async Task<CResponseMessage> GetMoviesBySeason(long seasonId)
        {
            var url = $"/api/Episode/movies-by-season/{seasonId}";
            Console.WriteLine("[SesonService] -> GetMoviesBySeason ENTER url=" + url);
            var resp = await _httpService.GetAsync<CResponseMessage>(url);
            Console.WriteLine("[SesonService] <- GetMoviesBySeason EXIT: " + JsonConvert.SerializeObject(resp));
            return resp!;
        }
    }
}
