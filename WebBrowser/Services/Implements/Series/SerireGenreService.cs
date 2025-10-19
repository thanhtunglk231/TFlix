using CoreLib.Dtos.MovieGenre;
using CoreLib.Dtos.SeriesGenre;
using CoreLib.Models;
using Newtonsoft.Json;
using WebBrowser.Models;
using WebBrowser.Models.MovieGenre;
using WebBrowser.Models.SerieGenre;
using WebBrowser.Services.HttpSevice.Interfaces;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Services.Implements.Series
{
    public class SerireGenreService : ISerireGenreService
    {
        private readonly IHttpService _httpService;
        private readonly IHttpContextAccessor _http;

        public SerireGenreService(IHttpService httpService, IHttpContextAccessor http)
        {
            _httpService = httpService;
            _http = http;
        }

        public async Task<CResponseMessage> add_MovieGenre(AddSeriesGenreDto addSeriesDto)
        {
            const string url = "/api/SeriesGenres/add";
            Console.WriteLine("[MovieGenreService] -> add_MovieGenre ENTER url=" + url);

            Console.WriteLine("[SeriesGenresService] add_MovieGenre payload: "
                + JsonConvert.SerializeObject(addSeriesDto));

            var resp = await _httpService.PostAsync<CResponseMessage>(url, addSeriesDto);

            Console.WriteLine("[SeriesGenresService] <- add_Episode EXIT: "
                + JsonConvert.SerializeObject(resp));
            return resp!;
        }

        public async Task<CResponseMessage> uppdate_MovieGenre(UpdateSeriesGenreDto updateDto)
        {
            const string url = "/api/SeriesGenres/update";
            Console.WriteLine("[SeriesGenresService] -> update_SeriesGenres ENTER url=" + url);





            var resp = await _httpService.PostAsync<CResponseMessage>(url, updateDto);

            Console.WriteLine("[SeriesGenresService] <- SeriesGenres EXIT: " + JsonConvert.SerializeObject(resp));
            return resp!;
        }

        public async Task<CResponseMessage> delete_Episode(DeleteSeriesGenreDto id)
        {
            const string url = "/api/SeriesGenres/delete";
            Console.WriteLine($"[SeriesGenresService] -> delete_MovieGenre ENTER url={url}, id={id}");

            var resp = await _httpService.PostAsync<CResponseMessage>(url, id);

            Console.WriteLine("[SeriesGenresService] <- delete_SeriesGenres EXIT: " + JsonConvert.SerializeObject(resp));
            return resp!;
        }

        public async Task<ApiResponse<SeriesGenreTableWrapper>> get_byid(decimal id)
        {
            string url = $"/api/SeriesGenres/getbyid?id={id}";
            Console.WriteLine("[SeriesGenresService] -> get_all ENTER url=" + url);

            var resp = await _httpService.GetAsync<ApiResponse<SeriesGenreTableWrapper>>(url);

            Console.WriteLine("[SeriesGenresService] <- get_all EXIT: " + JsonConvert.SerializeObject(resp));
            resp.success = resp.success || resp.code == "200";
            return resp;
        }
    }
}
