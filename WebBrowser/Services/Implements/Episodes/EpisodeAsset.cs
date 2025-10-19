using CoreLib.Dtos.EpisodeAsset;
using CoreLib.Models;
using Newtonsoft.Json;
using WebBrowser.Models;
using WebBrowser.Models.EpisodeAsset;
using WebBrowser.Services.HttpSevice.Interfaces;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Services.Implements.Episodes
{
    public class EpisodeAsset : IEpisodeAsset
    {
        private readonly IHttpService _httpService;
        private readonly IHttpContextAccessor _http;

        public EpisodeAsset(IHttpService httpService, IHttpContextAccessor http)
        {
            _httpService = httpService;
            _http = http;
        }

        // ========== 1) ADD ==========
        public async Task<CResponseMessage> add_Episode(IFormFile file, AddEpisodeAsset dto)
        {
            const string url = "/api/EpisodeAsset/add"; // POST (multipart/form-data)
            using var form = new MultipartFormDataContent();

            if (file != null && file.Length > 0)
            {
                var sc = new StreamContent(file.OpenReadStream());
                sc.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
                form.Add(sc, "File", file.FileName); // tên field phải là "File"
            }

            // Tên phải khớp EpisodeAssetAddForm
            if (dto.EpisodeId > 0) form.Add(new StringContent(dto.EpisodeId.ToString()), "EpisodeId");
            if (!string.IsNullOrWhiteSpace(dto.AssetType)) form.Add(new StringContent(dto.AssetType), "AssetType");
            form.Add(new StringContent(dto.SortOrder.ToString()), "SortOrder");

            var resp = await _httpService.PostMultipartAsync<CResponseMessage>(url, form);
            return resp!;
        }

        // ========== 2) REPLACE FILE / UPDATE (upload file mới + update DB) ==========
        public async Task<CResponseMessage> uppdate(decimal id, IFormFile file, UpdateEpisodeAsset updateDto)
        {
            var url = $"/api/EpisodeAsset/{id}/replace-file"; // POST (multipart/form-data)
            Console.WriteLine("[EpisodeAssetService] -> replace-file ENTER url=" + url);

            using var form = new MultipartFormDataContent();

            if (file != null && file.Length > 0)
            {
                var sc = new StreamContent(file.OpenReadStream());
                sc.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
                form.Add(sc, "File", file.FileName); // nên thống nhất "File"
            }
            else
            {
                Console.WriteLine("[EpisodeAssetService] Warning: update không có file mới.");
            }

            // Tên phải khớp EpisodeAssetReplaceForm
            if (updateDto.EpisodeId > 0) form.Add(new StringContent(updateDto.EpisodeId.ToString()), "EpisodeId");
            if (!string.IsNullOrWhiteSpace(updateDto.AssetType)) form.Add(new StringContent(updateDto.AssetType), "AssetType");
            form.Add(new StringContent(updateDto.SortOrder.ToString()), "SortOrder");


            // API là POST, không phải PUT
            var resp = await _httpService.PostMultipartAsync<CResponseMessage>(url, form);

            Console.WriteLine("[EpisodeAssetService] <- replace-file EXIT: " + JsonConvert.SerializeObject(resp));
            return resp!;
        }

        // ========== 3) DELETE ==========
        public async Task<CResponseMessage> delete(decimal id, string? fileUrl = null)
        {
            var url = $"/api/EpisodeAsset/{id}";
            if (!string.IsNullOrWhiteSpace(fileUrl))
            {
                // tuỳ chọn: gửi url để API xoá luôn file trên Supabase
                url += $"?url={Uri.EscapeDataString(fileUrl)}";
            }

            Console.WriteLine($"[EpisodeAssetService] -> delete ENTER url={url}, id={id}");
            var resp = await _httpService.DeleteResponseAsync(url);
            Console.WriteLine("[EpisodeAssetService] <- delete EXIT: " + JsonConvert.SerializeObject(resp));
            return resp!;
        }

        // ========== 4) GET ALL ==========
        public async Task<ApiResponse<EpisodeAssetTableWrapper>> get_all()
        {
            const string url = "/api/EpisodeAsset/getall";
            Console.WriteLine("[EpisodeAssetService] -> get_all ENTER url=" + url);

            var resp = await _httpService.GetAsync<ApiResponse<EpisodeAssetTableWrapper>>(url);

            Console.WriteLine("[EpisodeAssetService] <- get_all EXIT: " + JsonConvert.SerializeObject(resp));
            resp.success = resp.success || resp.code == "200";
            return resp;
        }

        public async Task<ApiResponse<EpisodeAssetTableWrapper>> getByid(decimal id)
        {
            var url = $"/api/EpisodeAsset/Getbyid?id={id}";
            Console.WriteLine("[EpisodeAssetService] -> get_by_id ENTER url=" + url);

            var resp = await _httpService.GetAsync<ApiResponse<EpisodeAssetTableWrapper>>(url);

            Console.WriteLine("[EpisodeAssetService] <- get_by_id EXIT: " + JsonConvert.SerializeObject(resp));
            resp.success = resp.success || resp.code == "200";
            return resp;
        }

    }
}
