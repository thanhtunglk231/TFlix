using CoreLib.Dtos.Genres;
using CoreLib.Models;
using Newtonsoft.Json;
using WebBrowser.Models;
using WebBrowser.Models.Genres;
using WebBrowser.Models.Movie;
using WebBrowser.Services.HttpSevice.Interfaces;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Services.Implements
{
    public class GenresService : IGenresService
    {
        private const string BASE = "/api/Genres"; // đổi thành "/api/Genre" nếu controller của bạn dùng số ít
        private readonly IHttpService _httpService;
        private readonly IHttpContextAccessor _http;

        public GenresService(IHttpService httpService, IHttpContextAccessor http)
        {
            _httpService = httpService;
            _http = http;
        }

        // ========== ADD ==========
        public async Task<CResponseMessage> add_Genre(AddGenreDto dto)
        {
            var url = $"{BASE}/add";
            Console.WriteLine("[GenresService] -> add_Genre ENTER url=" + url);
            Console.WriteLine("[GenresService] add_Genre payload: " + JsonConvert.SerializeObject(dto));

            var resp = await _httpService.PostAsync<CResponseMessage>(url, dto);

            Console.WriteLine("[GenresService] <- add_Genre EXIT: " + JsonConvert.SerializeObject(resp));
            return resp!;
        }

        // ========== UPDATE ==========
        public async Task<CResponseMessage> update_Genre(UpdateGenreDto dto)
        {
            var url = $"{BASE}/update";
            Console.WriteLine("[GenresService] -> update_Genre ENTER url=" + url);
            Console.WriteLine("[GenresService] update_Genre payload: " + JsonConvert.SerializeObject(dto));

            var resp = await _httpService.PostAsync<CResponseMessage>(url, dto);

            Console.WriteLine("[GenresService] <- update_Genre EXIT: " + JsonConvert.SerializeObject(resp));
            return resp!;
        }

        // ========== DELETE ==========
        public async Task<CResponseMessage> delete_Genre(decimal id)
        {
            var url = $"{BASE}/delete";
            Console.WriteLine($"[GenresService] -> delete_Genre ENTER url={url}, id={id}");

            // Backend nhận trực tiếp decimal trong request body.
            var resp = await _httpService.PostAsync<CResponseMessage>(url, id);

            Console.WriteLine("[GenresService] <- delete_Genre EXIT: " + JsonConvert.SerializeObject(resp));
            return resp!;
        }

        // ========== GET ALL ==========
        // Nếu API trả về data.table như Movie, bạn có thể dùng GenreTableWrapper (khai báo ở dưới) cho chuẩn type-safe.
        public async Task<ApiResponse<GenreTableWrapper>> get_all(int page = 1, int pageSize = 20, string? search = null)
        {
            var url = $"{BASE}/getall?page={Math.Max(1, page)}&pageSize={Math.Clamp(pageSize, 1, 100)}";
            if (!string.IsNullOrWhiteSpace(search))
                url += $"&search={Uri.EscapeDataString(search.Trim())}";
            Console.WriteLine("[GenresService] -> get_all ENTER url=" + url);

            var resp = await _httpService.GetAsync<ApiResponse<GenreTableWrapper>>(url);
            resp.success = resp.success || resp.code == "200";
            Console.WriteLine("[GenresService] <- get_all EXIT: " + JsonConvert.SerializeObject(resp));
            return resp;
        }

    }
}
