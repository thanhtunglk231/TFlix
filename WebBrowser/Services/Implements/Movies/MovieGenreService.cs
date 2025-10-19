using CoreLib.Dtos.Episode;
using CoreLib.Dtos.MovieGenre;
using CoreLib.Models;
using Newtonsoft.Json;
using WebBrowser.Models;
using WebBrowser.Models.Episode;
using WebBrowser.Models.MovieGenre;
using WebBrowser.Services.HttpSevice.Interfaces;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Services.Implements.Movies
{
    public class MovieGenreService : IMovieGenreService
    {
        private readonly IHttpService _httpService;
        private readonly IHttpContextAccessor _http;

        public MovieGenreService(IHttpService httpService, IHttpContextAccessor http)
        {
            _httpService = httpService;
            _http = http;
        }

        public async Task<CResponseMessage> add_MovieGenre(AddMovieGenreDto addSeriesDto)
        {
            const string url = "/api/MovieGenre/add";
            Console.WriteLine("[MovieGenreService] -> add_MovieGenre ENTER url=" + url);

            Console.WriteLine("[MovieGenreeService] add_MovieGenre payload: "
                + JsonConvert.SerializeObject(addSeriesDto));

            var resp = await _httpService.PostAsync<CResponseMessage>(url, addSeriesDto);

            Console.WriteLine("[MovieGenreeService] <- add_Episode EXIT: "
                + JsonConvert.SerializeObject(resp));
            return resp!;
        }

        public async Task<CResponseMessage> uppdate_MovieGenre(UpdateMovieGenreDto updateDto)
        {
            const string url = "/api/MovieGenre/update";
            Console.WriteLine("[MovieGenreService] -> update_MovieGenre ENTER url=" + url);





            var resp = await _httpService.PostAsync<CResponseMessage>(url, updateDto);

            Console.WriteLine("[MovieGenreService] <- update_MovieGenre EXIT: " + JsonConvert.SerializeObject(resp));
            return resp!;
        }

        public async Task<CResponseMessage> delete_Episode(DeleteMovieGenreDto id)
        {
            const string url = "/api/MovieGenre/delete";
            Console.WriteLine($"[MovieGenreService] -> delete_MovieGenre ENTER url={url}, id={id}");

            var resp = await _httpService.PostAsync<CResponseMessage>(url, id);

            Console.WriteLine("[MovieGenreService] <- delete_Episode EXIT: " + JsonConvert.SerializeObject(resp));
            return resp!;
        }

        public async Task<ApiResponse<MovieGenreTableWrapper>> get_byid(decimal id)
        {
            string url = $"/api/MovieGenre/getbyid?id={id}";
            Console.WriteLine("[MovieGenreService] -> get_all ENTER url=" + url);

            var resp = await _httpService.GetAsync<ApiResponse<MovieGenreTableWrapper>>(url);

            Console.WriteLine("[MovieGenreService] <- get_all EXIT: " + JsonConvert.SerializeObject(resp));
            resp.success = resp.success || resp.code == "200";
            return resp;
        }

    }
}
