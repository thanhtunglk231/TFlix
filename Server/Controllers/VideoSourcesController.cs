using CoreLib.Dtos.VideSoure;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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

        // ----------------- ADD (upload + lưu URL) -----------------
        [HttpPost("add")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAndCreate([FromForm] AddVideoSourceForm form)
        {
            // 1) Validate cơ bản
            if (form.File == null || form.File.Length == 0)
                return BadRequest("File rỗng.");

            // Chọn đúng 1 đích
            var hasMovie = form.MovieId.HasValue;
            var hasEpisode = form.EpisodeId.HasValue;
            if (hasMovie == hasEpisode) // cả 2 null hoặc cả 2 có
                return BadRequest("Phải chọn duy nhất một đích: movie_id hoặc episode_id.");

            // Chuẩn hoá status
            var status = (form.Status ?? "ACTIVE").Trim().ToUpperInvariant();

            // 2) Upload lên Supabase
            var publicUrl = await _supabase.UploadFileAsync(form.File);
            if (string.IsNullOrWhiteSpace(publicUrl))
                return StatusCode(500, "Upload Supabase thất bại.");

            // 3) Map DTO (dùng decimal? nếu service/DB là NUMBER)
            var dto = new AddVideoSourceDto
            {
                MovieId = form.MovieId,      // đổi form sang decimal? nếu cần
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

            // 4) Gọi SP
            var resp = await _videoSourceService.Add_video_source(dto);

            // 5) Rollback storage nếu DB fail
            if (!resp.Success)
            {
                // Lưu ý: nếu DeleteFileAsync cần "key" thay vì URL, hãy truyền key.
                await _supabase.DeleteFileAsync(publicUrl);
                return StatusCode(500, resp.message);
            }

            return Ok(new { resp.code, resp.message, resp.Success, publicUrl, resp.Data });
        }


        // ----------------- GET ALL -----------------
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




        [HttpPut("{sourceId:decimal}/replace-file")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ReplaceFile(
            decimal sourceId,
            [FromForm] ReplaceVideoSourceForm form)
        {
            if (sourceId <= 0) return BadRequest("sourceId không hợp lệ.");
            if (form.File == null || form.File.Length == 0) return BadRequest("File rỗng.");

            // A) Upload file mới
            var newUrl = await _supabase.UploadFileAsync(form.File);
            if (string.IsNullOrWhiteSpace(newUrl))
                return StatusCode(500, "Upload Supabase thất bại.");

            // B) Map DTO
            var dto = new UpdateVideoSourceDto
            {
                SourceId = sourceId,
                MovieId = form.MovieId,
                EpisodeId = form.EpisodeId,
                Provider = form.Provider,
                ServerName = form.ServerName,
                StreamUrl = newUrl, // gán URL mới
                Quality = form.Quality,
                Format = form.Format,
                DrmType = form.DrmType,
                DrmLicenseUrl = form.DrmLicenseUrl,
                IsPrimary = form.IsPrimary,
                Status = form.Status
            };

            // C) Update DB
            var resp = await _videoSourceService.Update_video_source(dto);
            if (!resp.Success)
            {
                await _supabase.DeleteFileAsync(newUrl); // rollback storage
                return StatusCode(500, resp.message);
            }

            // D) Xoá file cũ nếu client có gửi
            if (!string.IsNullOrWhiteSpace(form.OldStreamUrl))
                _ = _supabase.DeleteFileAsync(form.OldStreamUrl);

            return Ok(new { resp.code, resp.message, resp.Success, newUrl });
        }
    }

    // ================= FORM MODELS =================

    public class AddVideoSourceForm
    {
        public IFormFile File { get; set; } = default!;
        public decimal? MovieId { get; set; }      // đồng bộ NUMBER
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

        public int? MovieId { get; set; }
        public int? EpisodeId { get; set; }
        public string? Provider { get; set; }
        public string? ServerName { get; set; }
        public string? Quality { get; set; }
        public string? Format { get; set; }
        public string? DrmType { get; set; }
        public string? DrmLicenseUrl { get; set; }
        public bool IsPrimary { get; set; }
        public string? Status { get; set; }

        // để server xoá file cũ sau khi thay
        public string? OldStreamUrl { get; set; }
    }
}
