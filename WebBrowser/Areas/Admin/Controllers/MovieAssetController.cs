using CoreLib.Dtos.EpisodeAsset;
using CoreLib.Dtos.MovieAsset;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Areas.Admin.Controllers
{
    public class MovieAssetController : Controller
    {
        private readonly IMovieAssetService _episodeAsset;
        private readonly ILogger<EpisodeAssetController> _logger;

        public MovieAssetController(IMovieAssetService episodeAsset, ILogger<EpisodeAssetController> logger)
        {
            _episodeAsset = episodeAsset;
            _logger = logger;
        }

   
        public IActionResult Index() => View();

        [HttpGet("getall")]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("[Admin/MovieAssetController] -> GetAll ENTER");
            var result = await _episodeAsset.get_all();
            _logger.LogInformation("[Admin/MovieAssetController] <- GetAll EXIT: {json}", JsonConvert.SerializeObject(result));
            return Ok(result);
        }

        // ========== GET BY ID ==========
        // GET Admin/EpisodeAsset/{id}
        [HttpGet("{id:decimal}")]
        public async Task<IActionResult> GetById([FromRoute] decimal id)
        {
            if (id <= 0) return BadRequest(new { code = "400", message = "Invalid id." });

            _logger.LogInformation("[Admin/MovieAssetController] -> GetById ENTER, id={id}", id);
            var result = await _episodeAsset.getByid(id);
            _logger.LogInformation("[Admin/MovieAssetController] <- GetById EXIT: {json}", JsonConvert.SerializeObject(result));
            return Ok(result);
        }

        // ========== ADD (UPLOAD FILE + META) ==========
        // POST Admin/EpisodeAsset/add
        // multipart/form-data: file, EpisodeId, AssetType, SortOrder
        [HttpPost("add")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> Add(
            [FromForm(Name = "file")] IFormFile? file,
            [FromForm] decimal? EpisodeId,
            [FromForm] string? AssetType,
            [FromForm] int? SortOrder)
        {
            _logger.LogInformation("[Admin/MovieAssetController] -> Add ENTER");

            if (file == null || file.Length == 0)
                return BadRequest(new { code = "400", message = "File rỗng." });
            if (EpisodeId is null or <= 0 || string.IsNullOrWhiteSpace(AssetType))
                return BadRequest(new { code = "400", message = "Thiếu EpisodeId/AssetType." });

            var dto = new AddMovieAssetDto
            {
                MovieId = EpisodeId.Value,
                AssetType = AssetType,
                SortOrder = SortOrder ?? 0
                // Url do API backend sinh sau khi upload
            };

            var result = await _episodeAsset.add_MovieAsset(file, dto);
            _logger.LogInformation("[Admin/MovieAssetController] <- Add EXIT: {json}", JsonConvert.SerializeObject(result));
            return Ok(result);
        }

        // ========== UPDATE (REPLACE FILE + UPDATE DB BY ID) ==========
        // POST Admin/EpisodeAsset/{id}/replace-file
        // multipart/form-data: file (có thể kèm), EpisodeId, AssetType, SortOrder, OldUrl (optional)
        [HttpPost("{id:decimal}/replace-file")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> ReplaceFile(
            [FromRoute] decimal id,
            [FromForm(Name = "file")] IFormFile? file,
            [FromForm] decimal? EpisodeId,
            [FromForm] string? AssetType,
            [FromForm] int? SortOrder,
            [FromForm] string? OldUrl)
        {
            _logger.LogInformation("[Admin/EpisodeAsset] -> ReplaceFile ENTER, id={id}", id);

            if (id <= 0) return BadRequest(new { code = "400", message = "Invalid AssetId." });
            if (EpisodeId is null or <= 0 || string.IsNullOrWhiteSpace(AssetType))
                return BadRequest(new { code = "400", message = "Thiếu EpisodeId/AssetType." });

            var dto = new UpdateMovieAssetDto
            {
                AssetId = id,
                MovieId = EpisodeId.Value,
                AssetType = AssetType,
                SortOrder = SortOrder ?? 0,
                // dùng tạm Url để chuyển OldUrl sang service (service sẽ map thành field OldUrl trong form)
                Url = OldUrl ?? string.Empty
            };

            // Lưu ý: API backend hiện là POST /api/EpisodeAsset/{id}/replace-file (not PUT).
            // Service _episodeAsset.uppdate(...) sẽ gọi đúng route này.
            var result = await _episodeAsset.replaceFile(id, file!, dto);
            _logger.LogInformation("[Admin/EpisodeAsset] <- ReplaceFile EXIT: {json}", JsonConvert.SerializeObject(result));
            return Ok(result);
        }

        // ========== DELETE BY ID ==========
        // DELETE Admin/EpisodeAsset/{id}?url=...
        [HttpDelete("{id:decimal}")]
        public async Task<IActionResult> Delete([FromRoute] decimal id, [FromQuery] string? url = null)
        {
            _logger.LogInformation("[Admin/EpisodeAsset] -> Delete ENTER, id={id}", id);
            if (id <= 0) return BadRequest(new { code = "400", message = "Invalid id." });

            var result = await _episodeAsset.delete(id, url);
            _logger.LogInformation("[Admin/EpisodeAsset] <- Delete EXIT: {json}", JsonConvert.SerializeObject(result));
            return Ok(result);
        }

    }
}
