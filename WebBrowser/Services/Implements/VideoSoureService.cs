using CoreLib.Dtos.Episode;
using CoreLib.Dtos.VideSoure;
using CoreLib.Models;
using Microsoft.AspNetCore.Mvc;
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
        private readonly ILogger<VideoSoureService> _logger;

        public VideoSoureService(IHttpService httpService, IHttpContextAccessor http, ILogger<VideoSoureService> logger)
        {
            _httpService = httpService;
            _http = http;
            _logger = logger;
        }
        public async Task<CResponseMessage> add_VideoSoure(IFormFile? file, AddVideoSourceInputDto dto)
        {
            const string url = "/api/VideoSources/add";
            _logger.LogInformation("Adding video source through {ApiPath}", url);

            using var form = new MultipartFormDataContent();

            // ===== File debug =====
            if (file != null && file.Length > 0)
            {
                _logger.LogInformation("Video file received with size {FileSize} bytes and content type {ContentType}", file.Length, file.ContentType);
                var sc = new StreamContent(file.OpenReadStream());
                sc.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                form.Add(sc, "File", file.FileName);   // TÊN "File" trùng AddVideoSourceForm.File
            }
            else
            {
                _logger.LogInformation("No video file attached; using the supplied stream URL");
            }

            // ===== Helper add kèm log =====
            void Add(string name, object? v)
            {
                if (v == null) return;
                _logger.LogDebug("Adding video source metadata field {FieldName}", name);
                form.Add(new StringContent(Convert.ToString(v)!), name);
            }

            // ===== Metadata =====
            // CHỌN ĐÚNG 1 ĐÍCH: hoặc MovieId hoặc EpisodeId
            Add("MovieId", dto.MovieId);
            Add("EpisodeId", dto.EpisodeId);

            Add("Provider", dto.Provider);
            Add("ServerName", dto.ServerName);
            Add("StreamUrl", dto.StreamUrl);
            Add("Quality", dto.Quality);
            Add("Format", dto.Format);
            Add("DrmType", dto.DrmType);
            Add("DrmLicenseUrl", dto.DrmLicenseUrl);
            Add("IsPrimary", dto.IsPrimary); // bool -> "True"/"False"
            Add("Status", (dto.Status ?? "ACTIVE").Trim().ToUpperInvariant());

            _logger.LogInformation("Sending video source create request");

            var resp = await _httpService.PostMultipartAsync<CResponseMessage>(url, form);

            _logger.LogInformation("Video source create request completed with code {ResponseCode}", resp?.code);

            return resp!;
        }

        public async Task<CResponseMessage> add_HlsVideoSource(
            IFormFile playlist,
            IReadOnlyList<IFormFile> segments,
            AddVideoSourceInputDto dto,
            decimal? sourceId = null,
            string? oldStreamUrl = null)
        {
            const string url = "/api/VideoSources/add-hls";
            _logger.LogInformation("Adding HLS video source with {SegmentCount} segments", segments.Count);
            using var form = new MultipartFormDataContent();

            var playlistContent = new StreamContent(playlist.OpenReadStream());
            playlistContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/vnd.apple.mpegurl");
            form.Add(playlistContent, "Playlist", playlist.FileName);
            if (sourceId.HasValue)
                form.Add(new StringContent(sourceId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)), "SourceId");
            if (!string.IsNullOrWhiteSpace(oldStreamUrl))
                form.Add(new StringContent(oldStreamUrl), "OldStreamUrl");

            foreach (var segment in segments)
            {
                var segmentContent = new StreamContent(segment.OpenReadStream());
                segmentContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                form.Add(segmentContent, "Segments", segment.FileName);
            }

            void Add(string name, object? value)
            {
                if (value != null)
                    form.Add(new StringContent(Convert.ToString(value)!), name);
            }

            Add("MovieId", dto.MovieId);
            Add("EpisodeId", dto.EpisodeId);
            Add("Provider", dto.Provider);
            Add("ServerName", dto.ServerName);
            Add("Quality", dto.Quality);
            Add("DrmType", dto.DrmType);
            Add("DrmLicenseUrl", dto.DrmLicenseUrl);
            Add("IsPrimary", dto.IsPrimary);
            Add("Status", dto.Status);

            var response = await _httpService.PostMultipartAsync<CResponseMessage>(url, form);
            _logger.LogInformation("HLS video source request completed with code {ResponseCode}", response?.code);

            return response ?? new CResponseMessage
            {
                Success = false,
                code = "500",
                message = "Không nhận được phản hồi từ API."
            };
        }

        public async Task<CResponseMessage> UploadMp4ChunkAsync(Guid uploadId, int chunkIndex, int totalChunks, string fileFingerprint, IFormFile chunk)
        {
            var url = $"/api/VideoSources/mp4-uploads/{uploadId}/chunks/{chunkIndex}";
            using var form = new MultipartFormDataContent();
            form.Add(new StreamContent(chunk.OpenReadStream()), "Chunk", chunk.FileName);
            form.Add(new StringContent(totalChunks.ToString()), "totalChunks");
            form.Add(new StringContent(fileFingerprint), "fileFingerprint");

            _logger.LogInformation("Forwarding MP4 chunk {ChunkIndex}/{TotalChunks} for upload {UploadId}", chunkIndex + 1, totalChunks, uploadId);
            return await _httpService.PostMultipartAsync<CResponseMessage>(url, form)
                ?? new CResponseMessage { Success = false, code = "500", message = "Không nhận được phản hồi khi tải chunk." };
        }

            public Task<Mp4UploadStatusDto> GetMp4UploadStatusAsync(Guid uploadId, string fileFingerprint)
            {
                var url = $"/api/VideoSources/mp4-uploads/{uploadId}/status?fileFingerprint={Uri.EscapeDataString(fileFingerprint)}";
                return _httpService.GetAsync<Mp4UploadStatusDto>(url);
            }

        public Task<CResponseMessage> CompleteMp4UploadAsync(Guid uploadId, CompleteMp4VideoUploadDto request)
        {
            var url = $"/api/VideoSources/mp4-uploads/{uploadId}/complete";
            _logger.LogInformation("Finalizing chunked MP4 upload {UploadId} as HLS", uploadId);
            return _httpService.PostLongRunningAsync<CResponseMessage>(url, request);
        }

        public Task<CResponseMessage> CancelMp4UploadAsync(Guid uploadId)
        {
            var url = $"/api/VideoSources/mp4-uploads/{uploadId}/cancel";
            _logger.LogInformation("Cancelling temporary MP4 upload {UploadId}", uploadId);
            return _httpService.PostAsync<CResponseMessage>(url, new { });
        }




        public async Task<CResponseMessage> uppdate_VideoSoure(decimal sourceId, IFormFile file, UpdateVideoSourceInputDto meta)
        {
            var url = $"/api/VideoSources/{sourceId}/replace-file";
            _logger.LogInformation("Replacing video source file for source {SourceId}", sourceId);

            meta.SourceId = sourceId;

            using var form = new MultipartFormDataContent();

            // ===== File debug =====
            if (file != null && file.Length > 0)
            {
                _logger.LogInformation("Replacement file received with size {FileSize} bytes and content type {ContentType}", file.Length, file.ContentType);
                var sc = new StreamContent(file.OpenReadStream());
                sc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
                form.Add(sc, "file", file.FileName);
            }
            else
            {
                _logger.LogWarning("Video source replacement requested without a new file");
            }

            // ===== Helper add kèm log =====
            void Add(string name, object? v)
            {
                if (v == null) return;
                _logger.LogDebug("Adding video source metadata field {FieldName}", name);
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

            _logger.LogInformation("Sending video source replacement request");

            var resp = await _httpService.PutMultipartAsync<CResponseMessage>(url, form);

            _logger.LogInformation("Video source replacement completed with code {ResponseCode}", resp?.code);

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
            _logger.LogInformation("Loading video source list from {ApiPath}", url);

            var resp = await _httpService.GetAsync<ApiResponse<SourceTableWrapper>>(url);

            _logger.LogInformation("Video source list request completed with code {ResponseCode}", resp?.code);
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


        public async Task<CResponseMessage> Delete_video_source(decimal sourceId, string? streamUrl = null)
        {
            var url = "/api/VideoSources/delete";
            _logger.LogInformation("Calling delete video source API for id {SourceId}", sourceId);

            var resp = await _httpService.PostAsync<CResponseMessage>(url, new { id = sourceId, streamUrl });
            if (resp != null)
            {
                resp.Success = resp.Success || resp.code == "200";
            }
            return resp ?? new CResponseMessage { code = "500", message = "Không nhận được phản hồi từ server", Success = false };
        }
    }
}
