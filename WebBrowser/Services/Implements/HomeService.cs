using CoreLib.Models;
using Newtonsoft.Json;
using WebBrowser.Models;
using WebBrowser.Models.Episode;
using WebBrowser.Models.Home;
using WebBrowser.Services.HttpSevice.Interfaces;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Services.Implements
{
    public class HomeService : IHomeService
    {
        private readonly IHttpService _httpService;
        private readonly IHttpContextAccessor _http;

        public HomeService(IHttpService httpService, IHttpContextAccessor http)
        {
            _httpService = httpService;
            _http = http;
        }

        public async Task<ApiResponse<ContentSearchTableWrapper>> search_contents(string keyword)
        {
            string url = $"/api/Home/search?keyword={Uri.EscapeDataString(keyword ?? "")}";

            Console.WriteLine("[HomeService] -> search_contents ENTER url=" + url);

            var resp = await _httpService.GetAsync<ApiResponse<ContentSearchTableWrapper>>(url);

            Console.WriteLine("[HomeService] <- search_contents EXIT: " + JsonConvert.SerializeObject(resp));

            if (resp == null)
            {
                return new ApiResponse<ContentSearchTableWrapper>
                {
                    code = "500",
                    success = false,
                    message = "Null response from API",
                    Data = new ContentSearchTableWrapper()
                };
            }

            resp.success = resp.success || resp.code == "200";
            resp.Data ??= new ContentSearchTableWrapper();

            return resp;
        }
        public async Task<ApiResponse<EpisodeLatestTableWrapper>> get_episode_latest()
        {
            const string url = "/api/Home/EpisodeLatestItem";

            Console.WriteLine("[HomeService] -> get_episode_latest ENTER url=" + url);

            var resp = await _httpService.GetAsync<ApiResponse<EpisodeLatestTableWrapper>>(url);

            Console.WriteLine("[HomeService] <- get_episode_latest EXIT: " + JsonConvert.SerializeObject(resp));

            if (resp == null)
            {
                return new ApiResponse<EpisodeLatestTableWrapper>
                {
                    code = "500",
                    success = false,
                    message = "Null response from API",
                    Data = new EpisodeLatestTableWrapper()
                };
            }

            resp.success = resp.success || resp.code == "200";
            resp.Data ??= new EpisodeLatestTableWrapper();

            return resp;
        }

        public async Task<ApiResponse<MovieLastestTableWrappepr>> get_Movie_Lastest_Item()
        {
            const string url = "/api/Home/MovieLastestItem";
            Console.WriteLine("[HomeService] -> get_Movie_Lastest_Item ENTER url=" + url);

            var resp = await _httpService.GetAsync<ApiResponse<MovieLastestTableWrappepr>>(url);

            Console.WriteLine("[HomeService] <- get_Movie_Lastest_Item EXIT: " + JsonConvert.SerializeObject(resp));
            // resp.Data.Table sẽ là list phim
            return resp;
        }

    }
}
