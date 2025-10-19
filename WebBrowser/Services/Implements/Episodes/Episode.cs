using CoreLib.Dtos.Episode;
using CoreLib.Dtos.Season;
using CoreLib.Models;
using Newtonsoft.Json;
using WebBrowser.Models;
using WebBrowser.Models.Episode;
using WebBrowser.Models.Season;
using WebBrowser.Services.HttpSevice.Interfaces;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Services.Implements.Episodes
{
    public class Episode : IEpisode
    {
        private readonly IHttpService _httpService;
        private readonly IHttpContextAccessor _http;

        public Episode(IHttpService httpService, IHttpContextAccessor http)
        {
            _httpService = httpService;
            _http = http;
        }

        public async Task<CResponseMessage> add_Episode(AddEpisodeDto addSeriesDto)
        {
            const string url = "/api/Episode/add";
            Console.WriteLine("[EpisodeService] -> add_Episode ENTER url=" + url);

            Console.WriteLine("[EpisodeService] add_Episode payload: "
                + JsonConvert.SerializeObject(addSeriesDto));

            var resp = await _httpService.PostAsync<CResponseMessage>(url, addSeriesDto);

            Console.WriteLine("[EpisodeService] <- add_Episode EXIT: "
                + JsonConvert.SerializeObject(resp));
            return resp!;
        }

        public async Task<CResponseMessage> uppdate_Episode(UpdateEpisodeDto updateDto)
        {
            const string url = "/api/Episode/update";
            Console.WriteLine("[EpisodeService] -> update_series ENTER url=" + url);





            var resp = await _httpService.PostAsync<CResponseMessage>(url, updateDto);

            Console.WriteLine("[EpisodeService] <- update_Episode EXIT: " + JsonConvert.SerializeObject(resp));
            return resp!;
        }

        public async Task<CResponseMessage> delete_Episode(decimal id)
        {
            const string url = "/api/Episode/delete";
            Console.WriteLine($"[EpisodeService] -> delete_Episode ENTER url={url}, id={id}");

            var resp = await _httpService.PostAsync<CResponseMessage>(url, new { id });

            Console.WriteLine("[EpisodeService] <- delete_Episode EXIT: " + JsonConvert.SerializeObject(resp));
            return resp!;
        }

        public async Task<ApiResponse<EpisodeTableWrapper>> get_all()
        {
            const string url = "/api/Episode/getall";
            Console.WriteLine("[SeriesService] -> get_all ENTER url=" + url);

            var resp = await _httpService.GetAsync<ApiResponse<EpisodeTableWrapper>>(url);

            Console.WriteLine("[EpisodeService] <- get_all EXIT: " + JsonConvert.SerializeObject(resp));
            resp.success = resp.success || resp.code == "200";
            return resp;
        }

    }
}
