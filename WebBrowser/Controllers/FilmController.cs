using CoreLib.Dtos.Comment;
using CoreLib.Dtos.Preview;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebBrowser.Models.Film;
using WebBrowser.Models.Preview;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Controllers
{
    public class FilmController : Controller
    {
        private readonly IPreviewService _previewService;
        private readonly IEpisode _episodeService;
        private readonly IVideoSoureService _videoSourceService;
        private readonly ISeriesService _seriesService;
        private readonly ICommentService _commentService;
        private readonly ILogger<FilmController> _logger;

        public FilmController(
            IPreviewService previewService,
            IEpisode episodeService,
            IVideoSoureService videoSourceService,
            ISeriesService seriesService,
            ICommentService commentService,
            ILogger<FilmController> logger)
        {
            _previewService = previewService;
            _episodeService = episodeService;
            _videoSourceService = videoSourceService;
            _seriesService = seriesService;
            _commentService = commentService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Watch(long id, string kind, long? episodeId = null, long? ep = null)
        {
            var targetEpId = episodeId ?? ep;
            // Kiểm tra Đăng nhập (Authentication check)
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token) && (User == null || User.Identity?.IsAuthenticated != true))
            {
                // Chưa đăng nhập -> Chuyển hướng sang trang đăng nhập
                string returnUrl = Url.Action("Watch", "Film", new { id, kind, episodeId = targetEpId }) ?? "/Movies";
                return RedirectToAction("Index", "Auth", new { returnUrl });
            }

            var isSeries = string.Equals(kind, "SERIES", StringComparison.OrdinalIgnoreCase);
            var request = new GETCONTENTByID
            {
                id = id,
                kind = string.IsNullOrEmpty(kind) ? "movie" : kind
            };

            Console.WriteLine($"[FilmController.Watch] id={id}, kind={kind}, targetEpId={targetEpId}");

            // 1. Lấy danh sách episodes và series để xác định chính xác loại nội dung
            var episodesResponse = await _episodeService.get_all();
            var allEpisodes = episodesResponse?.Data?.Table ?? new List<WebBrowser.Models.Episode.EpisodeItem>();

            var seriesResponse = await _seriesService.get_all();
            var allSeries = seriesResponse?.Data?.Table ?? new List<WebBrowser.Models.Series.SerieDto>();

            var matchedSeries = allSeries.FirstOrDefault(x => x.SeriesId == id);
            var seriesEpisodes = allEpisodes.Where(x => x.SeriesId == id).OrderBy(x => x.SeasonNo).ThenBy(x => x.EpisodeNo).ToList();

            if (matchedSeries != null || seriesEpisodes.Any())
            {
                isSeries = true;
                kind = "SERIES";
            }

            PreviewItem? movie = null;
            var episodes = new List<WebBrowser.Models.Episode.EpisodeItem>();
            string? episodeLoadError = null;

            if (isSeries)
            {
                if (matchedSeries != null)
                {
                    movie = new PreviewItem
                    {
                        ContentId = matchedSeries.SeriesId,
                        kind = "SERIES",
                        title = matchedSeries.Title,
                        OriginalTitle = matchedSeries.OriginalTitle,
                        ReleaseOrAirDate = matchedSeries.FirstAirDate,
                        CountryCode = matchedSeries.CountryCode,
                        LanguageCode = matchedSeries.LanguageCode,
                        status = matchedSeries.Status,
                        IsPremium = matchedSeries.IsPremium,
                        genres = matchedSeries.Genres,
                        PrimaryPosterUrl = matchedSeries.PosterUrl
                    };
                }
                else
                {
                    var resp = await _previewService.get_preview(new GETCONTENTByID { id = id, kind = "SERIES" });
                    if (resp?.Data?.Table != null && resp.Data.Table.Count > 0)
                    {
                        movie = resp.Data.Table[0];
                    }
                }

                try
                {
                    var epResp = await _episodeService.GetBySeriesAsync(id);
                    if (epResp?.success == true && epResp.Data?.Table != null)
                    {
                        episodes = epResp.Data.Table
                            .OrderBy(x => x.SeasonNo)
                            .ThenBy(x => x.EpisodeNo)
                            .ThenBy(x => x.EpisodeId)
                            .ToList();
                    }
                    else if (seriesEpisodes.Any())
                    {
                        episodes = seriesEpisodes;
                    }
                    else
                    {
                        episodeLoadError = epResp?.message ?? "Không thể tải danh sách tập phim.";
                    }
                }
                catch (Exception ex)
                {
                    if (seriesEpisodes.Any())
                    {
                        episodes = seriesEpisodes;
                    }
                    else
                    {
                        episodeLoadError = "Tạm thời không thể tải danh sách tập phim. Vui lòng tải lại trang.";
                    }
                    _logger.LogError(ex, "Không thể tải tập phim cho SeriesId={SeriesId}", id);
                }
            }
            else
            {
                var resp = await _previewService.get_preview(request);
                Console.WriteLine("[FilmController.Watch] resp = " + JsonConvert.SerializeObject(resp));
                if (resp != null && resp.Data?.Table != null && resp.Data.Table.Count > 0)
                {
                    movie = resp.Data.Table[0];
                }
                else if (matchedSeries != null)
                {
                    isSeries = true;
                    kind = "SERIES";
                    movie = new PreviewItem
                    {
                        ContentId = matchedSeries.SeriesId,
                        kind = "SERIES",
                        title = matchedSeries.Title,
                        OriginalTitle = matchedSeries.OriginalTitle,
                        ReleaseOrAirDate = matchedSeries.FirstAirDate,
                        CountryCode = matchedSeries.CountryCode,
                        LanguageCode = matchedSeries.LanguageCode,
                        status = matchedSeries.Status,
                        IsPremium = matchedSeries.IsPremium,
                        genres = matchedSeries.Genres,
                        PrimaryPosterUrl = matchedSeries.PosterUrl
                    };
                    episodes = seriesEpisodes;
                }

                if (seriesEpisodes.Any())
                {
                    episodes = seriesEpisodes;
                }
            }

            if (movie == null)
            {
                return NotFound("Không tìm thấy nội dung");
            }

            Console.WriteLine(JsonConvert.SerializeObject(movie));

            WebBrowser.Models.ApiResponse<WebBrowser.Models.VideoSoure.SourceTableWrapper>? sourcesResponse = null;
            try
            {
                sourcesResponse = await _videoSourceService.get_all();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Không thể tải nguồn video cho ContentId={ContentId}, Kind={Kind}. Trang xem vẫn hiển thị thông tin và danh sách tập.",
                    id,
                    kind);
            }

            var episodeIds = episodes.Select(x => x.EpisodeId).ToHashSet();
            var sources = sourcesResponse?.Data?.Table?
                .Where(x => isSeries
                    ? (x.EpisodeId.HasValue && episodeIds.Contains(x.EpisodeId.Value))
                    : (x.MovieId == id || (x.EpisodeId.HasValue && episodeIds.Contains(x.EpisodeId.Value))))
                .Where(x => string.Equals(x.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                .ToList() ?? new List<WebBrowser.Models.VideoSoure.SourceItem>();

            _logger.LogInformation(
                "Film source debug: ContentId={ContentId}, Kind={Kind}, ApiSourceCount={ApiSourceCount}, MatchedSourceCount={MatchedSourceCount}, EpisodeCount={EpisodeCount}",
                id,
                kind,
                sourcesResponse?.Data?.Table?.Count ?? 0,
                sources.Count,
                episodes.Count);

            foreach (var source in sources)
            {
                _logger.LogInformation(
                    "Film source debug: SourceId={SourceId}, MovieId={MovieId}, EpisodeId={EpisodeId}, Format={Format}, Status={Status}, StreamUrl={StreamUrl}",
                    source.SourceId,
                    source.MovieId,
                    source.EpisodeId,
                    source.Format,
                    source.Status,
                    source.StreamUrl);
            }

            // Chọn tập phim: Ưu tiên tập được chỉ định qua tham số episodeId/ep, nếu không thì chọn tập đầu tiên có video source hoạt động
            var currentEpisodeId = (targetEpId.HasValue && episodes.Any(x => x.EpisodeId == (int)targetEpId.Value))
                ? (int)targetEpId.Value
                : episodes.FirstOrDefault(x => sources.Any(source => source.EpisodeId == x.EpisodeId))?.EpisodeId
                  ?? episodes.FirstOrDefault()?.EpisodeId;

            return View("Index", new WatchViewModel
            {
                ContentId = id,
                Content = movie,
                Episodes = episodes,
                Sources = sources,
                CurrentEpisodeId = currentEpisodeId,
                EpisodeLoadError = episodeLoadError
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetComments(long? movieId, long? episodeId)
        {
            var list = await _commentService.GetCommentsByContentAsync(movieId, episodeId);
            return Json(new { success = true, data = list });
        }

        [HttpPost]
        public async Task<IActionResult> PostComment([FromBody] CreateCommentDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Content))
            {
                return BadRequest(new { success = false, message = "Nội dung bình luận không được để trống" });
            }

            if (!dto.MovieId.HasValue && !dto.EpisodeId.HasValue)
            {
                return BadRequest(new { success = false, message = "Bình luận phải thuộc một phim hoặc tập phim." });
            }

            var currentUserJson = HttpContext.Session.GetString("CurrentUser");
            if (!string.IsNullOrEmpty(currentUserJson))
            {
                try
                {
                    var user = JsonConvert.DeserializeObject<WebBrowser.Models.AuthModels.UserInfo>(currentUserJson);
                    if (user != null)
                    {
                        dto.UserId = user.userId;
                        dto.UserName = string.IsNullOrWhiteSpace(user.fullName) ? dto.UserName : user.fullName;
                        dto.UserAvatar = string.IsNullOrWhiteSpace(user.avatarUrl) ? dto.UserAvatar : user.avatarUrl;
                    }
                }
                catch { }
            }

            if (dto.UserId <= 0)
            {
                return Unauthorized(new { success = false, message = "Không xác định được người dùng đăng nhập." });
            }

            var result = await _commentService.AddCommentAsync(dto);
            if (result == null || result.CommentId <= 0)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    success = false,
                    message = "Không thể lưu bình luận vào database."
                });
            }

            return Json(new { success = true, data = result });
        }

        [HttpGet]
        public async Task<IActionResult> GetEpisodePlayInfo([FromQuery] long episodeId)
        {
            try
            {
                var episodesResponse = await _episodeService.get_all();
                var allEpisodes = episodesResponse?.Data?.Table ?? new List<WebBrowser.Models.Episode.EpisodeItem>();
                var episode = allEpisodes.FirstOrDefault(x => x.EpisodeId == episodeId);

                var sourcesResponse = await _videoSourceService.get_all();
                var allSources = sourcesResponse?.Data?.Table ?? new List<WebBrowser.Models.VideoSoure.SourceItem>();
                var episodeSources = allSources
                    .Where(x => x.EpisodeId == episodeId && string.Equals(x.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => string.Equals(x.Format, "HLS", StringComparison.OrdinalIgnoreCase))
                    .ThenByDescending(x => x.IsPrimary)
                    .Select(x => new
                    {
                        sourceId = x.SourceId,
                        serverName = x.ServerName ?? x.Provider ?? "Cloudflare CDN",
                        quality = x.Quality ?? "Auto",
                        format = x.Format ?? "HLS",
                        streamUrl = x.StreamUrl,
                        isPrimary = x.IsPrimary
                    })
                    .ToList();

                return Json(new
                {
                    success = true,
                    data = new
                    {
                        episodeId = episodeId,
                        episodeNo = episode?.EpisodeNo ?? 1,
                        episodeTitle = episode?.EpisodeTitle ?? $"Tập {episodeId}",
                        durationMin = episode?.DurationMin ?? 45,
                        hasSource = episodeSources.Any(),
                        sources = episodeSources,
                        primaryUrl = episodeSources.FirstOrDefault()?.streamUrl ?? ""
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy nguồn phát tập phim EpisodeId={EpisodeId}", episodeId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = "Không thể tải nguồn phát tập phim." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> HlsProxy([FromQuery] string url)
        {
            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return BadRequest("URL không hợp lệ.");
            }

            try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode);
                }

                var cleanPath = uri.AbsolutePath.ToLowerInvariant();
                if (cleanPath.EndsWith(".m3u8"))
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var baseUrl = url.Substring(0, url.LastIndexOf('/') + 1);
                    var lines = content.Split('\n');
                    var sb = new System.Text.StringBuilder();

                    foreach (var rawLine in lines)
                    {
                        var line = rawLine.TrimEnd('\r');
                        var trimmed = line.Trim();
                        if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("#"))
                        {
                            var fullSegmentUrl = trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                                ? trimmed
                                : baseUrl + trimmed;
                            var proxiedUrl = Url.Action("HlsProxy", "Film", new { url = fullSegmentUrl });
                            sb.AppendLine(proxiedUrl ?? fullSegmentUrl);
                        }
                        else
                        {
                            sb.AppendLine(line);
                        }
                    }

                    Response.Headers["Access-Control-Allow-Origin"] = "*";
                    return Content(sb.ToString(), "application/vnd.apple.mpegurl", System.Text.Encoding.UTF8);
                }

                Response.Headers["Access-Control-Allow-Origin"] = "*";
                var stream = await response.Content.ReadAsStreamAsync();
                var contentType = response.Content.Headers.ContentType?.ToString() ?? "video/mp2t";
                return File(stream, contentType, enableRangeProcessing: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi proxy HLS stream: {Url}", url);
                return StatusCode(StatusCodes.Status502BadGateway, "Lỗi kết nối tới máy chủ lưu trữ video.");
            }
        }
    }
}
