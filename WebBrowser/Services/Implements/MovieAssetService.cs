using CoreLib.Dtos.MovieAsset;
using CoreLib.Models;
using Newtonsoft.Json;
using WebBrowser.Models;
using WebBrowser.Models.MovieAsset;
using WebBrowser.Services.HttpSevice.Interfaces;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Services.Implements
{
    public class MovieAssetService : IMovieAssetService
    {
        private readonly IHttpService _httpService;
        private readonly IHttpContextAccessor _http;

        public MovieAssetService(IHttpService httpService, IHttpContextAccessor http)
        {
            _httpService = httpService;
            _http = http;
        }

        // ========== 1) ADD ==========
        public async Task<CResponseMessage> add_MovieAsset(IFormFile file, AddMovieAssetDto dto)
        {
            const string url = "/api/MovieAsset/add"; // POST (multipart/form-data)
            using var form = new MultipartFormDataContent();

            if (file != null && file.Length > 0)
            {
                var sc = new StreamContent(file.OpenReadStream());
                sc.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
                form.Add(sc, "File", file.FileName); // field name: File
            }

            // ✅ KHỚP với MovieAssetAddForm
            if (dto.MovieId > 0) form.Add(new StringContent(dto.MovieId.ToString()), "MovieId");
            if (!string.IsNullOrWhiteSpace(dto.AssetType)) form.Add(new StringContent(dto.AssetType), "AssetType");
            form.Add(new StringContent(dto.SortOrder.ToString()), "SortOrder");

            var resp = await _httpService.PostMultipartAsync<CResponseMessage>(url, form);
            return resp!;
        }

        // ========== 2) REPLACE FILE ==========
        public async Task<CResponseMessage> replaceFile(decimal assetId, IFormFile file, UpdateMovieAssetDto updateDto, string? oldUrl = null)
        {
            var url = $"/api/MovieAsset/{assetId}/replace-file"; // POST (multipart/form-data)

            using var form = new MultipartFormDataContent();

            if (file != null && file.Length > 0)
            {
                var sc = new StreamContent(file.OpenReadStream());
                sc.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
                form.Add(sc, "File", file.FileName);
            }

            // ✅ KHỚP với MovieAssetReplaceForm
            if (updateDto.MovieId > 0) form.Add(new StringContent(updateDto.MovieId.ToString()), "MovieId");
            if (!string.IsNullOrWhiteSpace(updateDto.AssetType)) form.Add(new StringContent(updateDto.AssetType), "AssetType");
            form.Add(new StringContent(updateDto.SortOrder.ToString()), "SortOrder");

            if (!string.IsNullOrWhiteSpace(oldUrl))
                form.Add(new StringContent(oldUrl), "OldUrl");

            var resp = await _httpService.PostMultipartAsync<CResponseMessage>(url, form);
            return resp!;
        }

        // ========== 3) DELETE ==========
        public async Task<CResponseMessage> delete(decimal id, string? fileUrl = null)
        {
            var url = $"/api/MovieAsset/{id}";
            if (!string.IsNullOrWhiteSpace(fileUrl))
                url += $"?url={Uri.EscapeDataString(fileUrl)}";

            var resp = await _httpService.DeleteResponseAsync(url);
            return resp!;
        }

        // ========== 4) GET ALL ==========
        public async Task<ApiResponse<MovieAssetTableWrapper>> get_all()
        {
            const string url = "/api/MovieAsset/getall";
            var resp = await _httpService.GetAsync<ApiResponse<MovieAssetTableWrapper>>(url);
            resp.success = resp.success || resp.code == "200";
            return resp;
        }

        // ========== 5) GET BY ID ==========
        public async Task<ApiResponse<MovieAssetTableWrapper>> getByid(decimal id)
        {
            string url = $"/api/MovieAsset/getbyid/{id}";
            var resp = await _httpService.GetAsync<ApiResponse<MovieAssetTableWrapper>>(url);
            Console.WriteLine("MovieAsset Getbyid " + resp);
            resp.success = resp.success || resp.code == "200";
            return resp;
        }
    }
}
