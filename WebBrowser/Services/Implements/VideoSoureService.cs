using CoreLib.Dtos.Episode;
using CoreLib.Dtos.VideSoure;
using CoreLib.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebBrowser.Models;
using WebBrowser.Models.Episode;
using WebBrowser.Models.VideoSoure;
using WebBrowser.Services.HttpSevice.Interfaces;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Services.Implements
{
    public class VideoSoureService : IVideoSoureService
    {
        private readonly IHttpService _httpService;
        private readonly IHttpContextAccessor _http;
        public VideoSoureService(IHttpService httpService, IHttpContextAccessor http)
        {
            _httpService = httpService;
            _http = http;
        }
        public async Task<CResponseMessage> add_VideoSoure(IFormFile file, AddVideoSourceInputDto dto)
        {
            const string url = "/api/VideoSources/add";
            Console.WriteLine("[VideoSoureService] -> add_VideoSoure ENTER url=" + url);

            using var form = new MultipartFormDataContent();

            // ===== File debug =====
            if (file != null && file.Length > 0)
            {
                Console.WriteLine($"[VideoSoureService] File upload: name={file.FileName}, size={file.Length}, contentType={file.ContentType}");
                var sc = new StreamContent(file.OpenReadStream());
                sc.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                form.Add(sc, "File", file.FileName);   // TÊN "File" trùng AddVideoSourceForm.File
            }
            else
            {
                Console.WriteLine("[VideoSoureService] Warning: không có file upload.");
            }

            // ===== Helper add kèm log =====
            void Add(string name, object? v)
            {
                if (v == null) return;
                Console.WriteLine($"[VideoSoureService] META {name} = {v}");
                form.Add(new StringContent(Convert.ToString(v)!), name);
            }

            // ===== Metadata =====
            // CHỌN ĐÚNG 1 ĐÍCH: hoặc MovieId hoặc EpisodeId
            Add("MovieId", dto.MovieId);
            Add("EpisodeId", dto.EpisodeId);

            Add("Provider", dto.Provider);
            Add("ServerName", dto.ServerName);
            Add("Quality", dto.Quality);
            Add("Format", dto.Format);
            Add("DrmType", dto.DrmType);
            Add("DrmLicenseUrl", dto.DrmLicenseUrl);
            Add("IsPrimary", dto.IsPrimary); // bool -> "True"/"False"
            Add("Status", (dto.Status ?? "ACTIVE").Trim().ToUpperInvariant());

            Console.WriteLine("[VideoSoureService] Sending POST multipart request...");

            var resp = await _httpService.PostMultipartAsync<CResponseMessage>(url, form);

            Console.WriteLine("[VideoSoureService] <- add_VideoSoure EXIT: " + JsonConvert.SerializeObject(resp));

            return resp!;
        }




        public async Task<CResponseMessage> uppdate_VideoSoure(decimal sourceId, IFormFile file, UpdateVideoSourceInputDto meta)
        {
            var url = $"/api/VideoSources/{sourceId}/replace-file";
            Console.WriteLine("[VideoSoureService] -> update_VideoSoure ENTER url=" + url);

            meta.SourceId = sourceId;

            using var form = new MultipartFormDataContent();

            // ===== File debug =====
            if (file != null && file.Length > 0)
            {
                Console.WriteLine($"[VideoSoureService] File upload: name={file.FileName}, size={file.Length}, contentType={file.ContentType}");
                var sc = new StreamContent(file.OpenReadStream());
                sc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                form.Add(sc, "file", file.FileName);
            }
            else
            {
                Console.WriteLine("[VideoSoureService] Warning: update không có file mới.");
            }

            // ===== Helper add kèm log =====
            void Add(string name, object? v)
            {
                if (v == null) return;
                Console.WriteLine($"[VideoSoureService] META {name} = {v}");
                form.Add(new StringContent(Convert.ToString(v)!), $"meta.{name}");
            }

            // ===== Add metadata =====
            Add(nameof(meta.SourceId), meta.SourceId);
            Add(nameof(meta.MovieId), meta.MovieId);
            Add(nameof(meta.EpisodeId), meta.EpisodeId);
            Add(nameof(meta.Provider), meta.Provider);
            Add(nameof(meta.ServerName), meta.ServerName);
            Add(nameof(meta.Quality), meta.Quality);
            Add(nameof(meta.Format), meta.Format);
            Add(nameof(meta.DrmType), meta.DrmType);
            Add(nameof(meta.DrmLicenseUrl), meta.DrmLicenseUrl);
            Add(nameof(meta.IsPrimary), meta.IsPrimary);
            Add(nameof(meta.Status), meta.Status);
            Add(nameof(meta.OldStreamUrl), meta.OldStreamUrl);

            Console.WriteLine("[VideoSoureService] Sending PUT request...");

            var resp = await _httpService.PutMultipartAsync<CResponseMessage>(url, form);

            Console.WriteLine("[VideoSoureService] <- update_VideoSoure EXIT: " + JsonConvert.SerializeObject(resp));

            return resp!;
        }




        //public async Task<CResponseMessage> delete_Episode(decimal id)
        //{
        //    const string url = "/api/Episode/delete";
        //    Console.WriteLine($"[EpisodeService] -> delete_Episode ENTER url={url}, id={id}");

        //    var resp = await _httpService.PostAsync<CResponseMessage>(url, new { id });

        //    Console.WriteLine("[EpisodeService] <- delete_Episode EXIT: " + JsonConvert.SerializeObject(resp));
        //    return resp!;
        //}

        public async Task<ApiResponse<SourceTableWrapper>> get_all()
        {
            const string url = "/api/VideoSources/getall";
            Console.WriteLine("[SeriesService] -> get_all ENTER url=" + url);

            var resp = await _httpService.GetAsync<ApiResponse<SourceTableWrapper>>(url);

            Console.WriteLine("[EpisodeService] <- get_all EXIT: " + JsonConvert.SerializeObject(resp));
            resp.success = resp.success || resp.code == "200";
            return resp;
        }

        private static string ToQueryString(object? obj)
        {
            if (obj == null) return string.Empty;
            var dict = new Dictionary<string, string?>();
            foreach (var p in obj.GetType().GetProperties())
            {
                var val = p.GetValue(obj);
                if (val == null) continue;
                dict[p.Name] = val.ToString();
            }
            return dict.Count == 0 ? string.Empty
                : "?" + string.Join("&", dict.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}"));
        }


    }
}
