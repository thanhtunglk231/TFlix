using CoreLib.Dtos.Season;
using CoreLib.Dtos.Series;
using CoreLib.Models;
using Newtonsoft.Json;
using WebBrowser.Models;
using WebBrowser.Models.Season;
using WebBrowser.Models.Series;
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
            Console.WriteLine("[SesonService] -> add_season ENTER url=" + url);

            Console.WriteLine("[SesonService] add_season payload: "
                + JsonConvert.SerializeObject(addSeriesDto));

            var resp = await _httpService.PostAsync<CResponseMessage>(url, addSeriesDto);

            Console.WriteLine("[SesonService] <- add_season EXIT: "
                + JsonConvert.SerializeObject(resp));
            return resp!;
        }

        public async Task<CResponseMessage> uppdate_Season(UpdateSeasonDto updateDto)
        {
            const string url = "/api/Season/update";
            Console.WriteLine("[SeasonService] -> update_series ENTER url=" + url);





            var resp = await _httpService.PostAsync<CResponseMessage>(url, updateDto);

            Console.WriteLine("[SeasonService] <- update_Season EXIT: " + JsonConvert.SerializeObject(resp));
            return resp!;
        }

        public async Task<CResponseMessage> delete_Season(decimal id)
        {
            const string url = "/api/Season/delete";
            Console.WriteLine($"[SeasonService] -> delete_Season ENTER url={url}, id={id}");

            var resp = await _httpService.PostAsync<CResponseMessage>(url, new { id });

            Console.WriteLine("[SeasonService] <- delete_Season EXIT: " + JsonConvert.SerializeObject(resp));
            return resp!;
        }

        public async Task<ApiResponse<SeasonTableWrapper>> get_all()
        {
            const string url = "/api/Season/getall";
            Console.WriteLine("[SeriesService] -> get_all ENTER url=" + url);

            var resp = await _httpService.GetAsync<ApiResponse<SeasonTableWrapper>>(url);

            Console.WriteLine("[SeasonService] <- get_all EXIT: " + JsonConvert.SerializeObject(resp));
            resp.success = resp.success || resp.code == "200";
            return resp;
        }
    }
}
