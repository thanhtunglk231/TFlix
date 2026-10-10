using CoreLib.Dtos.MovieAsset;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Server.Services;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MovieAssetController : ControllerBase
    {
        private readonly ICMovieAsset _cMovieAsset;
        private readonly ISupabaseService _supabase;
        private readonly ICloudflareR2Service _cloudflareR2;
        private readonly IRedisCacheService _cache;

        public MovieAssetController(
            ICMovieAsset cMovieAsset,
            ISupabaseService supabase,
            ICloudflareR2Service cloudflareR2,
            IRedisCacheService cache)
        {
            _cMovieAsset = cMovieAsset;
            _supabase = supabase;
            _cloudflareR2 = cloudflareR2;
            _cache = cache;
        }

        // GET: /api/MovieAsset/getbyid/123
        [HttpGet("getbyid/{id:decimal}")]
        public async Task<IActionResult> GetById([FromRoute] decimal id, [FromQuery] string ownerType = "MOVIE")
        {
            var cacheKey = $"tflix:movies:assets:{ownerType.ToLowerInvariant()}:{id}";
            var resp = await _cache.GetAsync<CResponseMessage>(cacheKey) ?? await _cMovieAsset.sp_get_by_id(id, ownerType);
            if (!resp.Success) return StatusCode(500, new { resp.code, resp.message });
            await _cache.SetAsync(cacheKey, resp, TimeSpan.FromMinutes(10));
            return Ok(new { resp.code, resp.message, resp.Data });
        }

        [HttpGet("owners")]
        public async Task<IActionResult> GetOwners()
        {
            const string cacheKey = "tflix:movies:assets:owners";
            var resp = await _cache.GetAsync<CResponseMessage>(cacheKey) ?? await _cMovieAsset.GetOwners();
            if (!resp.Success) return StatusCode(500, new { resp.code, resp.message });
            await _cache.SetAsync(cacheKey, resp, TimeSpan.FromMinutes(10));
            return Ok(new { resp.code, resp.message, resp.Data });
        }

        // GET: /api/MovieAsset/getall
        [HttpGet("getall")]
        public async Task<IActionResult> GetAll([FromQuery] int pageSize = 25, [FromQuery] string? cursorOwnerType = null,
            [FromQuery] long? cursorAssetId = null, [FromQuery] string? ownerType = null)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            var cacheKey = $"tflix:movies:assets:page:{ownerType ?? "ALL"}:{cursorOwnerType ?? "START"}:{cursorAssetId ?? 0}:{pageSize}";
            var resp = await _cache.GetAsync<CResponseMessage>(cacheKey) ?? await _cMovieAsset.sp_get_all(pageSize, cursorOwnerType, cursorAssetId, ownerType);
            if (!resp.Success) return StatusCode(500, new { resp.code, resp.message });
            await _cache.SetAsync(cacheKey, resp, TimeSpan.FromMinutes(10));
            return Ok(new { resp.code, resp.message, resp.Data });
        }

        // POST: /api/MovieAsset/add   (multipart/form-data)
        [HttpPost("add")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAndCreate([FromForm] MovieAssetAddForm form)
        {
            if (form.File == null || form.File.Length == 0)
                return BadRequest(new { code = "400", message = "File rỗng." });
            if (form.OwnerId <= 0 || !IsValidOwnerType(form.OwnerType) || string.IsNullOrWhiteSpace(form.AssetType))
                return BadRequest(new { code = "400", message = "Thiếu hoặc sai loại nội dung/AssetType." });

            var ownerType = NormalizeOwnerType(form.OwnerType);
            var publicUrl = await UploadFileAsync(form.File, form.OwnerId, ownerType);
            if (string.IsNullOrWhiteSpace(publicUrl))
                return StatusCode(503, new { code = "503", message = GetUploadFailureMessage(ownerType) });

            // Gọi SP lưu DB
            var dto = new AddMovieAssetDto
            {
                OwnerId = form.OwnerId,
                OwnerType = ownerType,
                AssetType = form.AssetType,
                Url = publicUrl,
                SortOrder = form.SortOrder ?? 0
            };

            var resp = await _cMovieAsset.Add(dto);
            if (!resp.Success)
            {
                _ = DeleteFileAsync(publicUrl, ownerType); // rollback file
                return StatusCode(500, new { code = resp.code, message = resp.message });
            }

            await _cache.RemoveByPrefixAsync("tflix:movies:");

            return Ok(new { resp.code, resp.message, resp.Success, publicUrl, resp.Data });
        }

        // POST: /api/MovieAsset/{assetId}/replace-file  (multipart/form-data)
        [HttpPost("{assetId:decimal}/replace-file")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ReplaceFile([FromRoute] decimal assetId, [FromForm] MovieAssetReplaceForm form)
        {
            if (assetId <= 0)
                return BadRequest(new { code = "400", message = "assetId không hợp lệ." });
            if (form.File == null || form.File.Length == 0)
                return BadRequest(new { code = "400", message = "File rỗng." });
            if (form.OwnerId <= 0 || !IsValidOwnerType(form.OwnerType) || string.IsNullOrWhiteSpace(form.AssetType))
                return BadRequest(new { code = "400", message = "Thiếu hoặc sai loại nội dung/AssetType." });

            var ownerType = NormalizeOwnerType(form.OwnerType);
            var newUrl = await UploadFileAsync(form.File, form.OwnerId, ownerType);
            if (string.IsNullOrWhiteSpace(newUrl))
                return StatusCode(503, new { code = "503", message = GetUploadFailureMessage(ownerType) });

            var dto = new UpdateMovieAssetDto
            {
                AssetId = assetId,
                OwnerId = form.OwnerId,
                OwnerType = ownerType,
                AssetType = form.AssetType,
                Url = newUrl,
                SortOrder = form.SortOrder ?? 0
            };

            var resp = await _cMovieAsset.Update_episode(dto); // (hàm tên cũ) => nếu được, rename thành Update
            if (!resp.Success)
            {
                _ = DeleteFileAsync(newUrl, ownerType); // rollback file
                return StatusCode(500, new { code = resp.code, message = resp.message });
            }

            if (!string.IsNullOrWhiteSpace(form.OldUrl))
                _ = DeleteFileAsync(form.OldUrl, ownerType); // best-effort

            await _cache.RemoveByPrefixAsync("tflix:movies:");

            return Ok(new { resp.code, resp.message, resp.Success, newUrl });
        }

        // DELETE: /api/MovieAsset/{assetId}?url=xx
        [HttpDelete("{assetId:decimal}")]
        public async Task<IActionResult> Delete([FromRoute] decimal assetId, [FromQuery] string ownerType = "MOVIE", [FromQuery] string? url = null)
        {
            if (assetId <= 0)
                return BadRequest(new { code = "400", message = "assetId không hợp lệ." });

            if (!IsValidOwnerType(ownerType))
                return BadRequest(new { code = "400", message = "Loại nội dung không hợp lệ." });

            var resp = await _cMovieAsset.Delete_episode(assetId, ownerType);
            if (!resp.Success)
                return StatusCode(500, new { code = resp.code, message = resp.message });

            if (!string.IsNullOrWhiteSpace(url))
                _ = DeleteFileAsync(url, NormalizeOwnerType(ownerType));

            await _cache.RemoveByPrefixAsync("tflix:movies:");

            return Ok(new { resp.code, resp.message, resp.Success });
        }

        private static bool IsValidOwnerType(string? ownerType)
            => ownerType is not null && new[] { "MOVIE", "SERIES", "EPISODE" }.Contains(ownerType.Trim().ToUpperInvariant());

        private static string NormalizeOwnerType(string ownerType) => ownerType.Trim().ToUpperInvariant();

        private async Task<string?> UploadFileAsync(IFormFile file, long ownerId, string ownerType)
        {
            if (ownerType != "MOVIE")
                return await _supabase.UploadFileAsync(file);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var objectPath = $"movies/{ownerId}/assets/{Guid.NewGuid():N}{extension}";
            return await _cloudflareR2.UploadFileAsync(file, objectPath);
        }

        private Task<bool> DeleteFileAsync(string pathOrUrl, string ownerType)
            => ownerType == "MOVIE"
                ? _cloudflareR2.DeleteFileAsync(pathOrUrl)
                : _supabase.DeleteFileAsync(pathOrUrl);

        private static string GetUploadFailureMessage(string ownerType)
            => ownerType == "MOVIE"
                ? "Upload tài nguyên phim lên Cloudflare R2 thất bại. Vui lòng kiểm tra cấu hình CloudflareR2."
                : "Upload tài nguyên lên Supabase thất bại.";
    }

    // ======= FORM MODELS (FromForm) =======
    public class MovieAssetAddForm
    {
        public IFormFile File { get; set; } = default!;
        public long OwnerId { get; set; }
        public string OwnerType { get; set; } = "MOVIE";
        public string AssetType { get; set; } = default!;  // STILL|THUMB|TRAILER
        public int? SortOrder { get; set; } = 0;
    }

    public class MovieAssetReplaceForm
    {
        public IFormFile File { get; set; } = default!;
        public long OwnerId { get; set; }
        public string OwnerType { get; set; } = "MOVIE";
        public string AssetType { get; set; } = default!;
        public int? SortOrder { get; set; } = 0;
        public string? OldUrl { get; set; }                  // optional
    }
}
