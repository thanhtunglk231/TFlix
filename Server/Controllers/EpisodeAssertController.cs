using CoreLib.Dtos.EpisodeAsset;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using static CoreLib.Dtos.EpisodeAsset.AddEpisodeAsset;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EpisodeAssetController : ControllerBase
    {
        private readonly ISupabaseService _supabase;
        private readonly ICloudflareR2Service _cloudflareR2;
        private readonly ICEpisodeAssets _episodeAssetService;
        private readonly ILogger<EpisodeAssetController> _logger;
        private readonly string _cloudflarePublicBaseUrl;

        public EpisodeAssetController(
            ISupabaseService supabase,
            ICloudflareR2Service cloudflareR2,
            ICEpisodeAssets episodeAssetService,
            IConfiguration configuration,
            ILogger<EpisodeAssetController> logger)
        {
            _supabase = supabase;
            _cloudflareR2 = cloudflareR2;
            _episodeAssetService = episodeAssetService;
            _logger = logger;
            _cloudflarePublicBaseUrl = (configuration["CloudflareR2:PublicBaseUrl"] ?? string.Empty).TrimEnd('/');
        }


        [HttpGet("Getbyid")]
        public async Task<IActionResult> Getbyid([FromQuery] decimal id)
        {
            var reslut = await _episodeAssetService.sp_get_by_id(id);
            return Ok(reslut);   
        }

        [HttpPost("add")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAndCreate([FromForm] EpisodeAssetAddForm form)
        {
            if (form.File == null || form.File.Length == 0)
                return BadRequest(new { code = "400", message = "File rỗng." });
            if (form.EpisodeId <= 0 || string.IsNullOrWhiteSpace(form.AssetType))
                return BadRequest(new { code = "400", message = "Thiếu EpisodeId/AssetType." });

            var publicUrl = await UploadFileAsync(form.File, form.EpisodeId);
            if (string.IsNullOrWhiteSpace(publicUrl))
                return StatusCode(503, new { code = "503", message = "Không thể upload tài nguyên tập phim lên hệ thống lưu trữ." });

            // Lưu DB (URL)
            var dto = new AddEpisodeAsset
            {
                EpisodeId = form.EpisodeId,
                AssetType = form.AssetType,
                Url = publicUrl,
                SortOrder = form.SortOrder ?? 0
            };

            var resp = await _episodeAssetService.Add(dto);
            if (!resp.Success)
            {
                _ = DeleteFileAsync(publicUrl); // rollback file
                return StatusCode(500, new { code = resp.code, message = resp.message });
            }

            return Ok(new { resp.code, resp.message, resp.Success, publicUrl, resp.Data });
        }

        // ===== 2) GET ALL =====
        [HttpGet("getall")]
        public async Task<IActionResult> GetAll()
        {
            var resp = await _episodeAssetService.sp_get_all();
            if (!resp.Success) return StatusCode(500, new { resp.code, resp.message });
            return Ok(new { resp.code, resp.message, resp.Data });
        }

        // ===== 3) REPLACE FILE: upload file mới -> cập nhật URL trong DB theo SP sp_episode_asset_update =====
        [HttpPost("{assetId:decimal}/replace-file")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ReplaceFile(decimal assetId, [FromForm] EpisodeAssetReplaceForm form)
        {
            if (assetId <= 0)
                return BadRequest(new { code = "400", message = "assetId không hợp lệ." });
            if (form.File == null || form.File.Length == 0)
                return BadRequest(new { code = "400", message = "File rỗng." });
            if (form.EpisodeId <= 0 || string.IsNullOrWhiteSpace(form.AssetType))
                return BadRequest(new { code = "400", message = "Thiếu EpisodeId/AssetType." });

            // Upload file mới
            var newUrl = await UploadFileAsync(form.File, form.EpisodeId);
            if (string.IsNullOrWhiteSpace(newUrl))
                return StatusCode(503, new { code = "503", message = "Không thể upload tài nguyên tập phim lên hệ thống lưu trữ." });

            // Update DB theo SP sp_episode_asset_update
            var dto = new UpdateEpisodeAsset
            {
                AssetId = assetId,
                EpisodeId = form.EpisodeId,
                AssetType = form.AssetType,
                Url = newUrl,
                SortOrder = form.SortOrder ?? 0
            };

            var resp = await _episodeAssetService.Update_episode(dto);
            if (!resp.Success)
            {
                _ = DeleteFileAsync(newUrl); // rollback file
                return StatusCode(500, new { code = resp.code, message = resp.message });
            }

            // Xoá file cũ nếu có gửi kèm
            if (!string.IsNullOrWhiteSpace(form.OldUrl))
                _ = DeleteFileAsync(form.OldUrl);

            return Ok(new { resp.code, resp.message, resp.Success, newUrl });
        }

        // ===== 4) DELETE: xoá record theo SP sp_episode_asset_delete (tuỳ chọn xoá file) =====
        [HttpDelete("{assetId:decimal}")]
        public async Task<IActionResult> Delete(decimal assetId, [FromQuery] string? url = null)
        {
            if (assetId <= 0)
                return BadRequest(new { code = "400", message = "assetId không hợp lệ." });

            var resp = await _episodeAssetService.Delete_episode(assetId);
            if (!resp.Success)
                return StatusCode(500, new { code = resp.code, message = resp.message });

            if (!string.IsNullOrWhiteSpace(url))
                _ = DeleteFileAsync(url); // best-effort

            return Ok(new { resp.code, resp.message, resp.Success });
        }

        private async Task<string?> UploadFileAsync(IFormFile file, int episodeId)
        {
            var supabaseUrl = await _supabase.UploadFileAsync(file);
            if (!string.IsNullOrWhiteSpace(supabaseUrl))
                return supabaseUrl;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var objectPath = $"episodes/{episodeId}/assets/{Guid.NewGuid():N}{extension}";
            _logger.LogWarning(
                "Supabase upload failed for episode {EpisodeId}; falling back to Cloudflare R2 at {ObjectPath}",
                episodeId,
                objectPath);

            return await _cloudflareR2.UploadFileAsync(file, objectPath);
        }

        private Task<bool> DeleteFileAsync(string pathOrUrl)
        {
            if (!string.IsNullOrWhiteSpace(_cloudflarePublicBaseUrl)
                && pathOrUrl.StartsWith(_cloudflarePublicBaseUrl, StringComparison.OrdinalIgnoreCase))
            {
                return _cloudflareR2.DeleteFileAsync(pathOrUrl);
            }

            return _supabase.DeleteFileAsync(pathOrUrl);
        }
    }

    // ======= FORM MODELS (chỉ DÙNG 1 THAM SỐ [FromForm] MỖI ACTION) =======
    public class EpisodeAssetAddForm
    {
        public IFormFile File { get; set; } = default!;
        public int EpisodeId { get; set; }
        public string AssetType { get; set; } = default!;  // STILL|THUMB|TRAILER
        public int? SortOrder { get; set; } = 0;
    }

    public class EpisodeAssetReplaceForm
    {
        public IFormFile File { get; set; } = default!;
        public int EpisodeId { get; set; }
        public string AssetType { get; set; } = default!;
        public int? SortOrder { get; set; } = 0;

        // optional: để xoá file cũ trên Supabase
        public string? OldUrl { get; set; }
    }
}
