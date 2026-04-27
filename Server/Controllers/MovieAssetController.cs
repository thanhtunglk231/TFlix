using CoreLib.Dtos.MovieAsset;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Server.CloudFlareServices;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MovieAssetController : ControllerBase
    {
        private readonly ICMovieAsset _cMovieAsset;
        private readonly ISupabaseService _supabase;
        private readonly IR2Service _r2Service;
        public MovieAssetController(ICMovieAsset cMovieAsset, ISupabaseService supabase,IR2Service r2Service)
        {
            _cMovieAsset = cMovieAsset;
            _supabase = supabase;
            _r2Service = r2Service;
        }

        // GET: /api/MovieAsset/getbyid/123
        [HttpGet("getbyid/{id:decimal}")]
        public async Task<IActionResult> GetById([FromRoute] decimal id)
        {
            var resp = await _cMovieAsset.sp_get_by_id(id);
            if (!resp.Success) return StatusCode(500, new { resp.code, resp.message });
            return Ok(new { resp.code, resp.message, resp.Data });
        }

        // GET: /api/MovieAsset/getall
        [HttpGet("getall")]
        public async Task<IActionResult> GetAll()
        {
            var resp = await _cMovieAsset.sp_get_all();
            if (!resp.Success) return StatusCode(500, new { resp.code, resp.message });
            return Ok(new { resp.code, resp.message, resp.Data });
        }

        [HttpPost("add")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAndCreate([FromForm] MovieAssetAddForm form)
        {
            string? publicUrl = null;

            try
            {
                if (form.File == null || form.File.Length == 0)
                    return BadRequest(new { code = "400", message = "File rỗng." });

                if (form.MovieId <= 0 || string.IsNullOrWhiteSpace(form.AssetType))
                    return BadRequest(new { code = "400", message = "Thiếu MovieId/AssetType." });

                var ext = Path.GetExtension(form.File.FileName).ToLowerInvariant();
                var safeFileName = $"{Guid.NewGuid():N}{ext}";

                var assetType = form.AssetType.Trim().ToLowerInvariant();
                var objectPath = $"movie-assets/{form.MovieId}/{assetType}/{safeFileName}";

                // Upload lên R2 / Firebase
                publicUrl = await _r2Service.UploadFileAsync(form.File, objectPath);

                if (string.IsNullOrWhiteSpace(publicUrl))
                    return StatusCode(500, new { code = "500", message = "Upload thất bại." });

                // Gọi SP lưu DB
                var dto = new AddMovieAssetDto
                {
                    MovieId = form.MovieId,
                    AssetType = form.AssetType,
                    Url = publicUrl,
                    SortOrder = form.SortOrder ?? 0
                };

                var resp = await _cMovieAsset.Add(dto);

                if (!resp.Success)
                {
                    await _r2Service.DeleteFileAsync(publicUrl); // rollback file

                    return StatusCode(500, new
                    {
                        code = resp.code,
                        message = resp.message
                    });
                }

                return Ok(new
                {
                    resp.code,
                    resp.message,
                    resp.Success,
                    publicUrl,
                    resp.Data
                });
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrWhiteSpace(publicUrl))
                    await _r2Service.DeleteFileAsync(publicUrl);

                return StatusCode(500, new
                {
                    code = "500",
                    message = ex.Message
                });
            }
        }

        // POST: /api/MovieAsset/{assetId}/replace-file
        [HttpPost("{assetId:decimal}/replace-file")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ReplaceFile(
            [FromRoute] decimal assetId,
            [FromForm] MovieAssetReplaceForm form)
        {
            string? newUrl = null;

            try
            {
                if (assetId <= 0)
                    return BadRequest(new { code = "400", message = "assetId không hợp lệ." });

                if (form.File == null || form.File.Length == 0)
                    return BadRequest(new { code = "400", message = "File rỗng." });

                if (form.MovieId <= 0 || string.IsNullOrWhiteSpace(form.AssetType))
                    return BadRequest(new { code = "400", message = "Thiếu MovieId/AssetType." });

                var ext = Path.GetExtension(form.File.FileName).ToLowerInvariant();
                var safeFileName = $"{Guid.NewGuid():N}{ext}";
                var assetType = form.AssetType.Trim().ToLowerInvariant();

                var objectPath = $"movie-assets/{form.MovieId}/{assetType}/{safeFileName}";

                newUrl = await _r2Service.UploadFileAsync(form.File, objectPath);

                if (string.IsNullOrWhiteSpace(newUrl))
                    return StatusCode(500, new { code = "500", message = "Upload thất bại." });

                var dto = new UpdateMovieAssetDto
                {
                    AssetId = assetId,
                    MovieId = form.MovieId,
                    AssetType = form.AssetType,
                    Url = newUrl,
                    SortOrder = form.SortOrder ?? 0
                };

                var resp = await _cMovieAsset.Update_episode(dto);

                if (!resp.Success)
                {
                    await _r2Service.DeleteFileAsync(newUrl);

                    return StatusCode(500, new
                    {
                        code = resp.code,
                        message = resp.message
                    });
                }

                if (!string.IsNullOrWhiteSpace(form.OldUrl))
                    await _r2Service.DeleteFileAsync(form.OldUrl);

                return Ok(new
                {
                    resp.code,
                    resp.message,
                    resp.Success,
                    newUrl
                });
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrWhiteSpace(newUrl))
                    await _r2Service.DeleteFileAsync(newUrl);

                return StatusCode(500, new
                {
                    code = "500",
                    message = ex.Message
                });
            }
        }


        // DELETE: /api/MovieAsset/{assetId}?url=xx
        [HttpDelete("{assetId:decimal}")]
        public async Task<IActionResult> Delete(
            [FromRoute] decimal assetId,
            [FromQuery] string? url = null)
        {
            if (assetId <= 0)
                return BadRequest(new { code = "400", message = "assetId không hợp lệ." });

            var resp = await _cMovieAsset.Delete_episode(assetId);

            if (!resp.Success)
                return StatusCode(500, new
                {
                    code = resp.code,
                    message = resp.message
                });

            if (!string.IsNullOrWhiteSpace(url))
                await _r2Service.DeleteFileAsync(url);

            return Ok(new
            {
                resp.code,
                resp.message,
                resp.Success
            });
        }
    }

    // ======= FORM MODELS (FromForm) =======
    public class MovieAssetAddForm
    {
        public IFormFile File { get; set; } = default!;
        public int MovieId { get; set; }
        public string AssetType { get; set; } = default!;  // STILL|THUMB|TRAILER
        public int? SortOrder { get; set; } = 0;
    }

    public class MovieAssetReplaceForm
    {
        public IFormFile File { get; set; } = default!;
        public int MovieId { get; set; }                     // ✅ dùng MovieId
        public string AssetType { get; set; } = default!;
        public int? SortOrder { get; set; } = 0;
        public string? OldUrl { get; set; }                  // optional
    }
}
