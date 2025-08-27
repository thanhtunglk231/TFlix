using CoreLib.Dtos.Movies;
using CoreLib.Dtos.Season;
using CoreLib.Models;
using Newtonsoft.Json;
using WebBrowser.Models;
using WebBrowser.Models.Movie;
using WebBrowser.Models.Season;
using WebBrowser.Services.HttpSevice.Interfaces;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Services.Implements
{
    public class MovieService : IMovieService
    {
        private readonly IHttpService _httpService;
        private readonly IHttpContextAccessor _http;

        public MovieService(IHttpService httpService, IHttpContextAccessor http)
        {
            _httpService = httpService;
            _http = http;   
        }

        public async Task<CResponseMessage> add_Movie(AddMovieDto addSeriesDto)
        {
            const string url = "/api/Movie/add";
            Console.WriteLine("[MovieService] -> add_Movie ENTER url=" + url);

            Console.WriteLine("[MovieService] add_Movie payload: "
                + JsonConvert.SerializeObject(addSeriesDto));

            var resp = await _httpService.PostAsync<CResponseMessage>(url, addSeriesDto);

            Console.WriteLine("[MovieService] <- add_Movie EXIT: "
                + JsonConvert.SerializeObject(resp));
            return resp!;
        }

        public async Task<CResponseMessage> uppdate_Movie(AddMovieDto updateDto)
        {
            const string url = "/api/Movie/update";
            Console.WriteLine("[MovieService] -> update_Movie ENTER url=" + url);





            var resp = await _httpService.PostAsync<CResponseMessage>(url, updateDto);

            Console.WriteLine("[MovieService] <- update_SMovie EXIT: " + JsonConvert.SerializeObject(resp));
            return resp!;
        }

        public async Task<CResponseMessage> delete_Season(decimal id)
        {
            const string url = "/api/Movie/delete";
            Console.WriteLine($"[MovieService] -> delete_Season ENTER url={url}, id={id}");

            var resp = await _httpService.PostAsync<CResponseMessage>(url, new { id });

            Console.WriteLine("[MovieService] <- delete_Season EXIT: " + JsonConvert.SerializeObject(resp));
            return resp!;
        }

        public async Task<ApiResponse<MovieTableWrapper>> get_all()
        {
            const string url = "/api/Movie/getall";
            Console.WriteLine("[MoviesService] -> get_all ENTER url=" + url);

            var resp = await _httpService.GetAsync<ApiResponse<MovieTableWrapper>>(url);

            Console.WriteLine("[MovieService] <- get_all EXIT: " + JsonConvert.SerializeObject(resp));
            resp.success = resp.success || resp.code == "200";
            return resp;
        }
    }
}
