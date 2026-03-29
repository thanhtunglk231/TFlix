using CoreLib.Dtos;
using CoreLib.Dtos.VideSoure;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Server.Forms;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VideoSourcesController : ControllerBase
    {
        private readonly ISupabaseService _supabase;
        private readonly ICVideoSoure _videoSourceService;

        public VideoSourcesController(ISupabaseService supabase, ICVideoSoure videoSourceService)
        {
            _supabase = supabase;
            _videoSourceService = videoSourceService;
        }

        // =========================================================
        //  HLS: Playlist + Segments
        // =========================================================
        [HttpPost("add-hls")]
        [Consumes("multipart/form-data")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
        public async Task<IActionResult> UploadHlsAndCreate([FromForm] AddHlsVideoSourceForm form)
        {
            string? playlistUrl = null;
            var uploadedSegmentUrls = new List<string>();

            try
            {
                // 1) Validate
                if (form.Playlist == null || form.Playlist.Length == 0)
                    return BadRequest("Playlist HLS (.m3u8) không được rỗng.");

                if (form.Segments == null || form.Segments.Count == 0)
                    return BadRequest("Danh sách segments không được rỗng.");

                var hasMovie = form.MovieId.HasValue;
                var hasEpisode = form.EpisodeId.HasValue;
                if (hasMovie == hasEpisode)
                    return BadRequest("Phải chọn duy nhất một đích: movie_id hoặc episode_id.");

                var status = (form.Status ?? "ACTIVE").Trim().ToUpperInvariant();

                // 2) Tạo basePath dùng chung cho playlist + segments
                var ownerId = form.MovieId ?? form.EpisodeId ?? 0;
                var quality = string.IsNullOrWhiteSpace(form.Quality) ? "unknown" : form.Quality.Trim();
                var basePath = $"hls/{ownerId}/{quality}/{Guid.NewGuid():N}/"; // ví dụ: hls/42/720p/xxxxxxxxxx/

                // 3) Upload playlist .m3u8, GIỮ NGUYÊN TÊN FILE
                var playlistObjectPath = $"{basePath}{form.Playlist.FileName}";
                playlistUrl = await _supabase.UploadFileAsync(form.Playlist, playlistObjectPath);

                if (string.IsNullOrWhiteSpace(playlistUrl))
                    return StatusCode(500, "Upload playlist lên Supabase thất bại.");

                // 4) Tạo video_sources
                var addDto = new AddVideoSourceDto
                {
                    MovieId = form.MovieId,
                    EpisodeId = form.EpisodeId,
                    Provider = form.Provider ?? string.Empty,
                    ServerName = form.ServerName,
                    StreamUrl = playlistUrl,   // URL .m3u8
                    Quality = form.Quality,
                    Format = "HLS",
                    DrmType = form.DrmType,
                    DrmLicenseUrl = form.DrmLicenseUrl,
                    IsPrimary = form.IsPrimary,
                    Status = status
                };

                var sourceResp = await _videoSourceService.Add_video_source(addDto);
                if (!sourceResp.Success)
                {
                    await _supabase.DeleteFileAsync(playlistUrl);
                    return StatusCode(500, sourceResp.message);
                }

                if (sourceResp.Data == null)
                {
                    await _supabase.DeleteFileAsync(playlistUrl);
                    return StatusCode(500, "Không lấy được dữ liệu từ Add_video_source.");
                }

                dynamic data = sourceResp.Data;
                if (data.SourceId == null)
                {
                    await _supabase.DeleteFileAsync(playlistUrl);
                    return StatusCode(500, "Không lấy được SourceId từ DB.");
                }

                decimal sourceId = Convert.ToDecimal(data.SourceId);

                // 5) Upload từng segment & insert video_source_parts
                var segmentsOrdered = form.Segments
                    .OrderBy(f => f.FileName, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                int index = 0;
                foreach (var seg in segmentsOrdered)
                {
                    // objectPath = basePath + tên file nguyên bản
                    var segObjectPath = $"{basePath}{seg.FileName}";
                    var segUrl = await _supabase.UploadFileAsync(seg, segObjectPath);

                    if (string.IsNullOrWhiteSpace(segUrl))
                        throw new Exception($"Upload segment {seg.FileName} thất bại.");

                    uploadedSegmentUrls.Add(segUrl);

                    var partDto = new AddVideoSourcePartDto
                    {
                        SourceId = sourceId,
                        PartIndex = index++,
                        Url = segUrl,
                        ByteSize = seg.Length,
                        DurationSec = null,
                        Checksum = null
                    };

                    var partResp = await _videoSourceService.Add_video_source_part(partDto);
                    if (!partResp.Success)
                        throw new Exception(partResp.message ?? "Thêm video_source_part thất bại.");
                }

                return Ok(new
                {
                    sourceResp.code,
                    sourceResp.message,
                    sourceResp.Success,
                    playlistUrl,
                    SourceId = sourceId,
                    SegmentsCount = uploadedSegmentUrls.Count
                });
            }
            catch (Exception ex)
            {
                // rollback file trên Supabase
                if (!string.IsNullOrWhiteSpace(playlistUrl))
                    await _supabase.DeleteFileAsync(playlistUrl);

                foreach (var url in uploadedSegmentUrls)
                    await _supabase.DeleteFileAsync(url);

                // TODO: nếu có SP xoá video_source thì có thể gọi thêm ở đây

                return StatusCode(500, ex.Message);
            }
        }

        // =========================================================
        //  ADD video thường (1 file)
        // =========================================================
        [HttpPost("add")]
        [Consumes("multipart/form-data")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
        public async Task<IActionResult> UploadAndCreate([FromForm] AddVideoSourceForm form)
        {
            if (form.File == null || form.File.Length == 0)
                return BadRequest("File rỗng.");

            var hasMovie = form.MovieId.HasValue;
            var hasEpisode = form.EpisodeId.HasValue;
            if (hasMovie == hasEpisode)
                return BadRequest("Phải chọn duy nhất một đích: movie_id hoặc episode_id.");

            var status = (form.Status ?? "ACTIVE").Trim().ToUpperInvariant();

            // Đặt folder riêng cho video thường
            var ownerId = form.MovieId ?? form.EpisodeId ?? 0;
            var folder = $"videos/{ownerId}/{Guid.NewGuid():N}/";
            var objectPath = $"{folder}{form.File.FileName}";

            var publicUrl = await _supabase.UploadFileAsync(form.File, objectPath);
            if (string.IsNullOrWhiteSpace(publicUrl))
                return StatusCode(500, "Upload Supabase thất bại.");

            var dto = new AddVideoSourceDto
            {
                MovieId = form.MovieId,
                EpisodeId = form.EpisodeId,
                Provider = form.Provider ?? string.Empty,
                ServerName = form.ServerName,
                StreamUrl = publicUrl,
                Quality = form.Quality,
                Format = form.Format,
                DrmType = form.DrmType,
                DrmLicenseUrl = form.DrmLicenseUrl,
                IsPrimary = form.IsPrimary,
                Status = status
            };

            var resp = await _videoSourceService.Add_video_source(dto);

            if (!resp.Success)
            {
                await _supabase.DeleteFileAsync(publicUrl);
                return StatusCode(500, resp.message);
            }

            return Ok(new { resp.code, resp.message, resp.Success, publicUrl, resp.Data });
        }

        // =========================================================
        //  GET
        // =========================================================
        [HttpGet("getall")]
        public IActionResult GetAll()
        {
            var result = _videoSourceService.get_all();
            return Ok(result);
        }

        [HttpGet("getbyid")]
        public IActionResult Getid(int id)
        {
            var result = _videoSourceService.get_bu_id(id);
            return Ok(result);
        }

        // =========================================================
        //  REPLACE FILE
        // =========================================================
        [HttpPut("{sourceId:decimal}/replace-file")]
        [Consumes("multipart/form-data")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
        public async Task<IActionResult> ReplaceFile(
            decimal sourceId,
            [FromForm] ReplaceVideoSourceForm form)
        {
            if (sourceId <= 0) return BadRequest("sourceId không hợp lệ.");
            if (form.File == null || form.File.Length == 0) return BadRequest("File rỗng.");

            // folder mới cho file mới
            var ownerId = (decimal?)(form.MovieId) ?? (decimal?)(form.EpisodeId) ?? sourceId;
            var folder = $"videos/{ownerId}/{Guid.NewGuid():N}/";
            var objectPath = $"{folder}{form.File.FileName}";

            var newUrl = await _supabase.UploadFileAsync(form.File, objectPath);
            if (string.IsNullOrWhiteSpace(newUrl))
                return StatusCode(500, "Upload Supabase thất bại.");

            var dto = new UpdateVideoSourceDto
            {
                SourceId = sourceId,
                MovieId = form.MovieId,
                EpisodeId = form.EpisodeId,
                Provider = form.Provider,
                ServerName = form.ServerName,
                StreamUrl = newUrl,
                Quality = form.Quality,
                Format = form.Format,
                DrmType = form.DrmType,
                DrmLicenseUrl = form.DrmLicenseUrl,
                IsPrimary = form.IsPrimary,
                Status = form.Status
            };

            var resp = await _videoSourceService.Update_video_source(dto);
            if (!resp.Success)
            {
                await _supabase.DeleteFileAsync(newUrl);
                return StatusCode(500, resp.message);
            }

            // Xoá file cũ nếu có
            if (!string.IsNullOrWhiteSpace(form.OldStreamUrl))
                _ = _supabase.DeleteFileAsync(form.OldStreamUrl);

            return Ok(new { resp.code, resp.message, resp.Success, newUrl });
        }
    }

    // ================= FORM MODELS =================

    public class AddVideoSourceForm
    {
        public IFormFile File { get; set; } = default!;
        public decimal? MovieId { get; set; }
        public decimal? EpisodeId { get; set; }
        public string? Provider { get; set; }
        public string? ServerName { get; set; }
        public string? Quality { get; set; }
        public string? Format { get; set; }
        public string? DrmType { get; set; }
        public string? DrmLicenseUrl { get; set; }
        public bool IsPrimary { get; set; }
        public string? Status { get; set; }
    }

    public class ReplaceVideoSourceForm
    {
        public IFormFile File { get; set; } = default!;

        public decimal? MovieId { get; set; }
        public decimal? EpisodeId { get; set; }
        public string? Provider { get; set; }
        public string? ServerName { get; set; }
        public string? Quality { get; set; }
        public string? Format { get; set; }
        public string? DrmType { get; set; }
        public string? DrmLicenseUrl { get; set; }
        public bool IsPrimary { get; set; }
        public string? Status { get; set; }

        public string? OldStreamUrl { get; set; }
    }
}
