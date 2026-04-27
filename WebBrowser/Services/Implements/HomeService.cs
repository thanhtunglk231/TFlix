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
