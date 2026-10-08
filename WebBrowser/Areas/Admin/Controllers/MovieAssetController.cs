using CoreLib.Dtos.MovieAsset;
using Microsoft.AspNetCore.Mvc;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Areas.Admin.Controllers
{
    [Route("Admin/[controller]")]
    public class MovieAssetController : AdminBaseController
    {
        private readonly IMovieAssetService _assetService;
        private readonly ILogger<MovieAssetController> _logger;

        public MovieAssetController(IMovieAssetService assetService, ILogger<MovieAssetController> logger)
        {
            _assetService = assetService;
            _logger = logger;
        }

        [HttpGet("")]
        public IActionResult Index() => View();

        [HttpGet("getall")]
        public async Task<IActionResult> GetAll([FromQuery] int pageSize = 25, [FromQuery] string? cursorOwnerType = null,
            [FromQuery] long? cursorAssetId = null, [FromQuery] string? ownerType = null)
            => Ok(await _assetService.get_all(pageSize, cursorOwnerType, cursorAssetId, ownerType));

        [HttpGet("owners")]
        public async Task<IActionResult> GetOwners() => Ok(await _assetService.get_owners());

        [HttpGet("{id:decimal}")]
        public async Task<IActionResult> GetById(decimal id, [FromQuery] string ownerType = "MOVIE")
            => id <= 0 ? BadRequest(new { code = "400", message = "AssetId không hợp lệ." }) : Ok(await _assetService.getByid(id, ownerType));

        [HttpPost("add")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> Add([FromForm] IFormFile? file, [FromForm] long? ownerId,
            [FromForm] string? ownerType, [FromForm] string? assetType, [FromForm] int? sortOrder)
        {
            if (file is null || file.Length == 0)
                return BadRequest(new { code = "400", message = "Vui lòng chọn file." });
            if (ownerId is null or <= 0 || !IsValidOwnerType(ownerType) || string.IsNullOrWhiteSpace(assetType))
                return BadRequest(new { code = "400", message = "Thông tin nội dung hoặc loại asset không hợp lệ." });

            _logger.LogInformation("Creating {OwnerType} asset for owner {OwnerId}", ownerType, ownerId);
            return Ok(await _assetService.add_MovieAsset(file, new AddMovieAssetDto
            {
                OwnerId = ownerId.Value,
                OwnerType = ownerType!.ToUpperInvariant(),
                AssetType = assetType,
                SortOrder = sortOrder ?? 0
            }));
        }

        [HttpPost("{id:decimal}/replace-file")]
        [DisableRequestSizeLimit]
        public async Task<IActionResult> ReplaceFile(decimal id, [FromForm] IFormFile? file, [FromForm] long? ownerId,
            [FromForm] string? ownerType, [FromForm] string? assetType, [FromForm] int? sortOrder, [FromForm] string? oldUrl)
        {
            if (id <= 0 || file is null || file.Length == 0)
                return BadRequest(new { code = "400", message = "AssetId hoặc file không hợp lệ." });
            if (ownerId is null or <= 0 || !IsValidOwnerType(ownerType) || string.IsNullOrWhiteSpace(assetType))
                return BadRequest(new { code = "400", message = "Thông tin nội dung hoặc loại asset không hợp lệ." });

            return Ok(await _assetService.replaceFile(id, file, new UpdateMovieAssetDto
            {
                AssetId = id,
                OwnerId = ownerId.Value,
                OwnerType = ownerType!.ToUpperInvariant(),
                AssetType = assetType,
                SortOrder = sortOrder ?? 0
            }, oldUrl));
        }

        [HttpDelete("{id:decimal}")]
        public async Task<IActionResult> Delete(decimal id, [FromQuery] string ownerType = "MOVIE", [FromQuery] string? url = null)
        {
            if (id <= 0 || !IsValidOwnerType(ownerType))
                return BadRequest(new { code = "400", message = "AssetId hoặc loại nội dung không hợp lệ." });
            return Ok(await _assetService.delete(id, ownerType.ToUpperInvariant(), url));
        }

        private static bool IsValidOwnerType(string? value)
            => value is not null && new[] { "MOVIE", "SERIES", "EPISODE" }.Contains(value.Trim().ToUpperInvariant());
    }
}
