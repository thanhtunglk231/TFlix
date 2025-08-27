using CoreLib.Dtos.Series;
using CoreLib.Models;
using Newtonsoft.Json;
using WebBrowser.Models;
using WebBrowser.Models.Series;
using WebBrowser.Services.HttpSevice.Interfaces;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Services.Implements
{
    public class SeriesService : ISeriesService
    {
        private readonly IHttpService _httpService;
        private readonly IHttpContextAccessor _http;

        public SeriesService(IHttpService httpService, IHttpContextAccessor http)
        {
            _httpService = httpService;
            _http = http;
        }

        public async Task<CResponseMessage> add_series(AddSeriesDto addSeriesDto)
        {
            const string url = "/api/Series/add";
            Console.WriteLine("[SeriesService] -> add_series ENTER url=" + url);

            Console.WriteLine("[SeriesService] add_series payload: "
                + JsonConvert.SerializeObject(addSeriesDto));

            var resp = await _httpService.PostAsync<CResponseMessage>(url, addSeriesDto);

            Console.WriteLine("[SeriesService] <- add_series EXIT: "
                + JsonConvert.SerializeObject(resp));
            return resp!;
        }


        public async Task<CResponseMessage> uppdate_series(UpdateSeriesDto updateDto)
        {
            const string url = "/api/Series/update";
            Console.WriteLine("[SeriesService] -> update_series ENTER url=" + url);

        

            var resp = await _httpService.PostAsync<CResponseMessage>(url, updateDto);

            Console.WriteLine("[SeriesService] <- update_series EXIT: " + JsonConvert.SerializeObject(resp));
            return resp!;
        }

        public async Task<CResponseMessage> delete_series(decimal id)
        {
            const string url = "/api/Series/delete";
            Console.WriteLine($"[SeriesService] -> delete_series ENTER url={url}, id={id}");

            var resp = await _httpService.PostAsync<CResponseMessage>(url, new { id });

            Console.WriteLine("[SeriesService] <- delete_series EXIT: " + JsonConvert.SerializeObject(resp));
            return resp!;
        }

        public async Task<ApiResponse<SeriesResponseData>> get_all()
        {
            const string url = "/api/Series/getall";
            Console.WriteLine("[SeriesService] -> get_all ENTER url=" + url);

            var resp = await _httpService.GetAsync<ApiResponse<SeriesResponseData>>(url);

            Console.WriteLine("[SeriesService] <- get_all EXIT: " + JsonConvert.SerializeObject(resp));
            resp.success = resp.success || resp.code == "200";
            return resp;
        }
    }
}
