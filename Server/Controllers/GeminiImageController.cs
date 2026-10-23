using System;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CoreLib.Dtos.GeminiImage;
using CoreLib.Dtos.MovieAsset;
using CoreLib.Models;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Server.Services;
using Server.Services.Gemini;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "ADMIN,SUPER_ADMIN")]
    public class GeminiImageController : ControllerBase
    {
        private readonly IGeminiClient _geminiClient;
        private readonly ICMovieAsset _cMovieAsset;
        private readonly ICloudflareR2Service _cloudflareR2;
        private readonly ISupabaseService _supabase;
        private readonly IRedisCacheService _cache;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly ILogger<GeminiImageController> _logger;

        public GeminiImageController(
            IGeminiClient geminiClient,
            ICMovieAsset cMovieAsset,
            ICloudflareR2Service cloudflareR2,
            ISupabaseService supabase,
            IRedisCacheService cache,
            IWebHostEnvironment env,
            IConfiguration config,
            ILogger<GeminiImageController> logger)
        {
            _geminiClient = geminiClient;
            _cMovieAsset = cMovieAsset;
            _cloudflareR2 = cloudflareR2;
            _supabase = supabase;
            _cache = cache;
            _env = env;
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Tạo hình ảnh bằng Gemini Imagen 3 API
        /// </summary>
        [HttpPost("generate")]
        public async Task<IActionResult> Generate([FromBody] GeminiGenerateImageRequestDto dto, CancellationToken cancellationToken)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Prompt))
            {
                return BadRequest(new GeminiGenerateImageResponseDto
                {
                    Success = false,
                    ErrorMessage = "Vui lòng nhập prompt mô tả hình ảnh cần tạo."
                });
            }

            try
            {
                var result = await _geminiClient.GenerateImageAsync(
                    dto.Prompt,
                    dto.AspectRatio,
                    dto.Model,
                    dto.ApiKey,
                    cancellationToken);

                return Ok(result);
            }
            catch (OperationCanceledException)
            {
                return Ok(new GeminiGenerateImageResponseDto
                {
                    Success = false,
                    ErrorMessage = "Thời gian tạo ảnh quá lâu (Timeout) hoặc yêu cầu đã bị hủy."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi API tạo ảnh bằng Gemini.");
                return Ok(new GeminiGenerateImageResponseDto
                {
                    Success = false,
                    ErrorMessage = "Lỗi máy chủ khi tạo ảnh: " + ex.Message
                });
            }
        }

        /// <summary>
        /// Tối ưu và mở rộng prompt thành cinematic visual prompt bằng Gemini
        /// </summary>
        [HttpPost("enhance-prompt")]
        public async Task<IActionResult> EnhancePrompt([FromBody] GeminiEnhancePromptRequestDto dto, CancellationToken cancellationToken)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Description))
            {
                return Ok(new GeminiEnhancePromptResponseDto
                {
                    Success = false,
                    ErrorMessage = "Vui lòng nhập mô tả sơ lược hoặc tiêu đề phim."
                });
            }

            try
            {
                var prompt = await _geminiClient.EnhancePromptAsync(
                    dto.Description,
                    dto.Style,
                    dto.Title,
                    dto.ApiKey,
                    cancellationToken);

                if (string.IsNullOrWhiteSpace(prompt))
                {
                    return Ok(new GeminiEnhancePromptResponseDto
                    {
                        Success = false,
                        ErrorMessage = "Không thể tối ưu prompt với thông tin đã cung cấp. Vui lòng thử lại."
                    });
                }

                return Ok(new GeminiEnhancePromptResponseDto
                {
                    Success = true,
                    EnhancedPrompt = prompt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tối ưu prompt bằng Gemini.");
                return Ok(new GeminiEnhancePromptResponseDto
                {
                    Success = false,
                    ErrorMessage = "Lỗi khi tối ưu prompt: " + ex.Message
                });
            }
        }

        /// <summary>
        /// Lưu ảnh đã tạo làm Media Asset (Poster / Backdrop / Thumbnail) cho phim hoặc tập phim
        /// </summary>
        [HttpPost("save-asset")]
        public async Task<IActionResult> SaveAsset([FromBody] GeminiSaveAssetRequestDto dto)
        {
            if (dto == null || dto.OwnerId <= 0 || string.IsNullOrWhiteSpace(dto.Base64Data))
            {
                return BadRequest(new { success = false, message = "Thông tin ảnh hoặc mã phim không hợp lệ." });
            }

            var ownerType = (dto.OwnerType ?? "MOVIE").Trim().ToUpperInvariant();
            if (ownerType != "MOVIE" && ownerType != "SERIES" && ownerType != "EPISODE")
            {
                ownerType = "MOVIE";
            }

            var assetType = (dto.AssetType ?? "POSTER").Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(assetType))
            {
                assetType = "POSTER";
            }

            try
            {
                var publicUrl = await UploadAndSaveAssetAsync(
                    dto.OwnerId,
                    ownerType,
                    assetType,
                    dto.Base64Data,
                    dto.MimeType,
                    dto.SortOrder);

                await _cache.RemoveByPrefixAsync("tflix:movies:");
                await _cache.RemoveByPrefixAsync("tflix:home:");

                return Ok(new
                {
                    success = true,
                    message = $"Đã lưu hình ảnh AI thành công vào Media Assets ({assetType}) của {ownerType} #{dto.OwnerId}!",
                    url = publicUrl,
                    assetType = assetType,
                    ownerType = ownerType,
                    ownerId = dto.OwnerId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu Media Asset từ Gemini.");
                return StatusCode(500, new { success = false, message = "Lỗi khi lưu ảnh: " + ex.Message });
            }
        }

        /// <summary>
        /// Tự động sinh hình ảnh (Poster 3:4 & Theme/Backdrop 16:9) và tự động gắn vào movie_assets của phim
        /// </summary>
        [HttpPost("auto-generate-and-bind")]
        public async Task<IActionResult> AutoGenerateAndBind([FromBody] GeminiAutoBindRequestDto dto, CancellationToken cancellationToken)
        {
            if (dto == null || dto.MovieId <= 0)
            {
                return BadRequest(new GeminiAutoBindResponseDto
                {
                    Success = false,
                    Message = "Mã phim (MovieId) không hợp lệ."
                });
            }

            var response = new GeminiAutoBindResponseDto
            {
                MovieId = dto.MovieId,
                Success = false
            };

            try
            {
                // 1. Lấy thông tin phim từ CSDL nếu chưa được truyền đủ
                string movieTitle = dto.MovieTitle?.Trim() ?? string.Empty;
                string overview = dto.Overview?.Trim() ?? string.Empty;
                string genres = dto.Genres?.Trim() ?? string.Empty;
                string releaseYear = string.Empty;

                try
                {
                    var connStr = _config.GetConnectionString("SqlServer");
                    if (!string.IsNullOrWhiteSpace(connStr))
                    {
                        await using var conn = new SqlConnection(connStr);
                        await conn.OpenAsync(cancellationToken);
                        await using var cmd = conn.CreateCommand();
                        cmd.CommandText = @"
                            SELECT TOP 1 m.movie_id, m.title, m.original_title, m.overview, 
                                   YEAR(m.release_date) AS release_year,
                                   (
                                       SELECT STRING_AGG(g.genre_name, N', ') 
                                       FROM dbo.movie_genres mg 
                                       JOIN dbo.genres g ON mg.genre_id = g.genre_id 
                                       WHERE mg.movie_id = m.movie_id
                                   ) AS genres
                            FROM dbo.movies m
                            WHERE m.movie_id = @movie_id;";
                        cmd.Parameters.Add(new SqlParameter("@movie_id", SqlDbType.BigInt) { Value = dto.MovieId });

                        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                        if (await reader.ReadAsync(cancellationToken))
                        {
                            if (string.IsNullOrWhiteSpace(movieTitle))
                                movieTitle = reader["title"]?.ToString() ?? string.Empty;
                            if (string.IsNullOrWhiteSpace(overview))
                                overview = reader["overview"]?.ToString() ?? string.Empty;
                            if (string.IsNullOrWhiteSpace(genres))
                                genres = reader["genres"]?.ToString() ?? string.Empty;
                            releaseYear = reader["release_year"]?.ToString() ?? string.Empty;
                        }
                    }
                }
                catch (Exception dbEx)
                {
                    _logger.LogWarning(dbEx, "Không thể đọc trực tiếp phim #{MovieId} từ CSDL, sử dụng thông tin từ DTO.", dto.MovieId);
                }

                if (string.IsNullOrWhiteSpace(movieTitle))
                {
                    movieTitle = $"Movie #{dto.MovieId}";
                }
                response.MovieTitle = movieTitle;

                var styleContext = (dto.Style ?? "cinematic").ToLowerInvariant() switch
                {
                    "poster" => "theatrical movie poster style, graphic design elements, artistic title framing",
                    "anime" => "modern Japanese anime cinematic key art, vibrant colors, Makoto Shinkai aesthetic",
                    "realistic" => "ultra-realistic movie still, 35mm film photography, natural cinematic lighting",
                    "dark" => "dark atmospheric mystery thriller, moody shadows, low-key lighting, gritty cinematic tension",
                    _ => "cinematic theatrical movie key visual, dramatic volumetric lighting, 8k resolution, IMAX masterpiece composition"
                };

                // 2. Tạo & Gắn POSTER (Dọc 3:4)
                if (dto.GeneratePoster)
                {
                    _logger.LogInformation("Đang tự tạo Poster cho phim #{MovieId} ({Title})", dto.MovieId, movieTitle);
                    var posterPrompt = $"Official theatrical movie poster for \"{movieTitle}\" {(string.IsNullOrEmpty(releaseYear) ? "" : $"({releaseYear})")}. Genre: {(string.IsNullOrEmpty(genres) ? "Cinema" : genres)}. Visual theme: {(string.IsNullOrEmpty(overview) ? movieTitle : overview.Length > 200 ? overview.Substring(0, 200) : overview)}. {styleContext}, portrait vertical framing, hyper-detailed, award-winning movie poster.";

                    var posterResult = await _geminiClient.GenerateImageAsync(
                        posterPrompt,
                        "3:4",
                        "imagen-3.0-generate-002",
                        dto.ApiKey,
                        cancellationToken);

                    if (posterResult.Success && !string.IsNullOrWhiteSpace(posterResult.Base64Data))
                    {
                        response.PosterUrl = await UploadAndSaveAssetAsync(
                            dto.MovieId,
                            "MOVIE",
                            "POSTER",
                            posterResult.Base64Data,
                            posterResult.MimeType,
                            dto.SortOrder);
                    }
                    else
                    {
                        var err = $"Poster: {posterResult.ErrorMessage ?? "Không tạo được ảnh poster."}";
                        response.Errors.Add(err);
                        _logger.LogWarning("Tạo poster cho phim #{MovieId} thất bại: {Error}", dto.MovieId, err);
                    }
                }

                // 3. Tạo & Gắn THEME / BACKDROP (Ngang 16:9)
                if (dto.GenerateBackdrop)
                {
                    _logger.LogInformation("Đang tự tạo Theme/Backdrop cho phim #{MovieId} ({Title})", dto.MovieId, movieTitle);
                    var backdropPrompt = $"Cinematic wide theme backdrop and key landscape banner for movie \"{movieTitle}\" {(string.IsNullOrEmpty(releaseYear) ? "" : $"({releaseYear})")}. Genre: {(string.IsNullOrEmpty(genres) ? "Cinema" : genres)}. Story setting: {(string.IsNullOrEmpty(overview) ? movieTitle : overview.Length > 200 ? overview.Substring(0, 200) : overview)}. {styleContext}, panoramic 16:9 widescreen composition, epic atmospheric depth, film still aesthetic, IMAX scale.";

                    var backdropResult = await _geminiClient.GenerateImageAsync(
                        backdropPrompt,
                        "16:9",
                        "imagen-3.0-generate-002",
                        dto.ApiKey,
                        cancellationToken);

                    if (backdropResult.Success && !string.IsNullOrWhiteSpace(backdropResult.Base64Data))
                    {
                        response.BackdropUrl = await UploadAndSaveAssetAsync(
                            dto.MovieId,
                            "MOVIE",
                            "BACKDROP",
                            backdropResult.Base64Data,
                            backdropResult.MimeType,
                            dto.SortOrder);
                    }
                    else
                    {
                        var err = $"Backdrop: {backdropResult.ErrorMessage ?? "Không tạo được ảnh theme/backdrop."}";
                        response.Errors.Add(err);
                        _logger.LogWarning("Tạo backdrop cho phim #{MovieId} thất bại: {Error}", dto.MovieId, err);
                    }
                }

                // 4. Tạo & Gắn THUMBNAIL (Ngang 16:9) nếu yêu cầu
                if (dto.GenerateThumb)
                {
                    _logger.LogInformation("Đang tự tạo Thumbnail cho phim #{MovieId} ({Title})", dto.MovieId, movieTitle);
                    var thumbPrompt = $"High impact video scene preview thumbnail for movie \"{movieTitle}\". Dramatic cinematic scene, 16:9 widescreen, crisp film frame, high clarity.";

                    var thumbResult = await _geminiClient.GenerateImageAsync(
                        thumbPrompt,
                        "16:9",
                        "imagen-3.0-generate-002",
                        dto.ApiKey,
                        cancellationToken);

                    if (thumbResult.Success && !string.IsNullOrWhiteSpace(thumbResult.Base64Data))
                    {
                        response.ThumbUrl = await UploadAndSaveAssetAsync(
                            dto.MovieId,
                            "MOVIE",
                            "THUMB",
                            thumbResult.Base64Data,
                            thumbResult.MimeType,
                            dto.SortOrder);
                    }
                }

                // Xóa cache phim và trang chủ
                await _cache.RemoveByPrefixAsync("tflix:movies:");
                await _cache.RemoveByPrefixAsync("tflix:home:");

                bool hasAtLeastOne = !string.IsNullOrWhiteSpace(response.PosterUrl) || !string.IsNullOrWhiteSpace(response.BackdropUrl) || !string.IsNullOrWhiteSpace(response.ThumbUrl);
                response.Success = hasAtLeastOne;
                response.Message = hasAtLeastOne
                    ? $"Đã tự tạo và gắn hình ảnh AI thành công cho phim \"{movieTitle}\"!"
                    : $"Không thể tạo ảnh cho phim: {string.Join(" | ", response.Errors)}";

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tự động tạo và gắn ảnh AI cho phim #{MovieId}.", dto.MovieId);
                response.Success = false;
                response.Message = "Lỗi xử lý tạo và gắn hình ảnh: " + ex.Message;
                return StatusCode(500, response);
            }
        }

        private async Task<string> UploadAndSaveAssetAsync(
            long ownerId,
            string ownerType,
            string assetType,
            string base64Data,
            string? mimeType,
            int sortOrder)
        {
            var cleanBase64 = base64Data;
            var commaIndex = cleanBase64.IndexOf(',');
            if (commaIndex >= 0 && cleanBase64.Substring(0, commaIndex).Contains("base64"))
            {
                cleanBase64 = cleanBase64.Substring(commaIndex + 1);
            }

            var imageBytes = Convert.FromBase64String(cleanBase64.Trim());
            var extension = ".jpg";
            var cleanMimeType = string.IsNullOrWhiteSpace(mimeType) ? "image/jpeg" : mimeType;

            var ownerFolder = ownerType.ToLowerInvariant() switch
            {
                "movie" => "movies",
                "series" => "series",
                _ => "episodes"
            };
            var assetFolder = assetType.ToLowerInvariant();
            var objectPath = $"{ownerFolder}/{ownerId}/assets/{assetFolder}/gemini_{assetFolder}_{Guid.NewGuid():N}{extension}";

            // 1. Upload Cloudflare R2
            var publicUrl = await _cloudflareR2.UploadBytesAsync(imageBytes, objectPath, cleanMimeType);

            // 2. Fallback nếu R2 chưa có hoặc lỗi: lưu local static wwwroot
            if (string.IsNullOrWhiteSpace(publicUrl))
            {
                var localFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"), "uploads", "gemini-assets", ownerFolder, ownerId.ToString());
                Directory.CreateDirectory(localFolder);
                var fileName = $"gemini_{assetFolder}_{Guid.NewGuid():N}{extension}";
                var localFilePath = Path.Combine(localFolder, fileName);
                await System.IO.File.WriteAllBytesAsync(localFilePath, imageBytes);
                publicUrl = $"/uploads/gemini-assets/{ownerFolder}/{ownerId}/{fileName}";
            }

            // 3. Ghi vào CSDL qua CMovieAsset
            var addDto = new AddMovieAssetDto
            {
                OwnerId = ownerId,
                OwnerType = ownerType,
                AssetType = assetType,
                Url = publicUrl,
                SortOrder = sortOrder
            };

            var dbResp = await _cMovieAsset.Add(addDto);
            if (!dbResp.Success)
            {
                _logger.LogWarning("CMovieAsset.Add trả về lỗi khi lưu {AssetType} cho {OwnerType} #{OwnerId}: {Message}", assetType, ownerType, ownerId, dbResp.message);
            }

            return publicUrl;
        }
    }
}
