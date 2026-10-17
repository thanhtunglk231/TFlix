using CoreLib.Dtos.Comment;
using CoreLib.Dtos.Preview;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using CoreLib.Dtos.Payment;
using CoreLib.Dtos.Subtitle;
using WebBrowser.Models.Episode;
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
        private readonly IRatingService _ratingService;
        private readonly ILogger<FilmController> _logger;
        private readonly IPaymentService _paymentService;
        private readonly ISubtitleService _subtitleService;

        public FilmController(
            IPreviewService previewService,
            IEpisode episodeService,
            IVideoSoureService videoSourceService,
            ISeriesService seriesService,
            ICommentService commentService,
            IRatingService ratingService,
            ILogger<FilmController> logger,
            IPaymentService paymentService,
            ISubtitleService subtitleService)
        {
            _previewService = previewService;
            _episodeService = episodeService;
            _videoSourceService = videoSourceService;
            _seriesService = seriesService;
            _commentService = commentService;
            _ratingService = ratingService;
            _logger = logger;
            _paymentService = paymentService;
            _subtitleService = subtitleService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Watch(long id, string kind, long? episodeId = null, long? ep = null)
        {
            if (id <= 0)
            {
                return RedirectToAction("Index", "Movies");
            }

            var targetEpId = episodeId ?? ep;
            var token = HttpContext.Session.GetString("JWToken");

            var isSeries = string.Equals(kind, "SERIES", StringComparison.OrdinalIgnoreCase);
            var request = new GETCONTENTByID
            {
                id = id,
                kind = string.IsNullOrEmpty(kind) ? "movie" : kind
            };

            Console.WriteLine($"[FilmController.Watch] id={id}, kind={kind}, targetEpId={targetEpId}");

            // 1. Lấy danh sách episodes và series để xác định chính xác loại nội dung
            var allEpisodes = new List<WebBrowser.Models.Episode.EpisodeItem>();
            try
            {
                var episodesResponse = await _episodeService.get_all();
                allEpisodes = episodesResponse?.Data?.Table ?? new List<WebBrowser.Models.Episode.EpisodeItem>();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[FilmController.Watch] Warning: Không thể tải all episodes: " + ex.Message);
            }

            var allSeries = new List<WebBrowser.Models.Series.SerieDto>();
            try
            {
                var seriesResponse = await _seriesService.get_all();
                allSeries = seriesResponse?.Data?.Table ?? new List<WebBrowser.Models.Series.SerieDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[FilmController.Watch] Warning: Không thể tải all series: " + ex.Message);
            }

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
                movie = GetFallbackMovie(id, kind);
            }

            if (movie == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy nội dung phim được yêu cầu.";
                return RedirectToAction("Index", "Movies");
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

            var selectedEpisode = episodes.FirstOrDefault(x => x.EpisodeId == currentEpisodeId);
            var isPremiumContent = IsContentPremium(movie, selectedEpisode, matchedSeries);
            var (hasActiveSubscription, subscriptionEndAt) = await CheckUserSubscriptionAsync();

            if (isPremiumContent && !hasActiveSubscription)
            {
                sources = [];
                if (movie != null)
                {
                    movie.PrimaryStreamUrl = null;
                }
            }

            CoreLib.Dtos.Rating.MovieRatingDto? ratingInfo = null;
            try
            {
                long? currentUserId = null;
                var currentUserJson = HttpContext.Session.GetString("CurrentUser") ?? HttpContext.Session.GetString("AdminCurrentUser");
                if (!string.IsNullOrEmpty(currentUserJson))
                {
                    var u = JsonConvert.DeserializeObject<WebBrowser.Models.AuthModels.UserInfo>(currentUserJson);
                    if (u?.userId > 0) currentUserId = u.userId;
                }
                ratingInfo = await _ratingService.GetMovieRatingAsync(id, currentUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy thông tin đánh giá phim {MovieId}", id);
            }

            List<SubtitleTrackDto> subtitles = new();
            try
            {
                var durationSec = (movie.DurationMin ?? 120) * 60.0;
                var subResp = await _subtitleService.GetSubtitlesAsync(
                    id,
                    currentEpisodeId,
                    movie.title ?? movie.OriginalTitle,
                    movie.OverviewText,
                    durationSec);
                if (subResp?.Success == true && subResp.Tracks != null)
                {
                    subtitles = subResp.Tracks;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi tải phụ đề ban đầu cho ContentId={ContentId}", id);
            }

            return View("Index", new WatchViewModel
            {
                ContentId = id,
                Content = movie,
                Episodes = episodes,
                Sources = sources,
                CurrentEpisodeId = currentEpisodeId,
                EpisodeLoadError = episodeLoadError,
                IsPremiumContent = isPremiumContent,
                HasActiveSubscription = hasActiveSubscription,
                SubscriptionEndAt = subscriptionEndAt,
                RatingInfo = ratingInfo,
                Subtitles = subtitles
            });
        }

        private async Task<(bool hasActiveSubscription, DateTimeOffset? endAt)> CheckUserSubscriptionAsync()
        {
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token) && (User == null || User.Identity?.IsAuthenticated != true))
            {
                return (false, null);
            }

            var currentUserJson = HttpContext.Session.GetString("CurrentUser");
            if (!string.IsNullOrEmpty(currentUserJson))
            {
                try
                {
                    var user = JsonConvert.DeserializeObject<WebBrowser.Models.AuthModels.UserInfo>(currentUserJson);
                    if (user?.roles != null && user.roles.Any(r =>
                        string.Equals(r, "ADMIN", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(r, "SUPER_ADMIN", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(r, "PREMIUM", StringComparison.OrdinalIgnoreCase)))
                    {
                        return (true, DateTimeOffset.UtcNow.AddYears(1));
                    }
                }
                catch { }
            }

            try
            {
                var statusResponse = await _paymentService.GetSubscriptionStatusAsync();
                if (statusResponse?.Data != null)
                {
                    var envelope = JsonConvert.DeserializeObject<SubscriptionStatusEnvelope>(
                        JsonConvert.SerializeObject(statusResponse.Data));
                    var status = envelope?.Table?.FirstOrDefault();
                    if (status != null && status.IsActive)
                    {
                        if (!status.EndAt.HasValue || status.EndAt.Value > DateTimeOffset.UtcNow)
                        {
                            return (true, status.EndAt);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể kiểm tra gói Premium của người dùng.");
            }

            return (false, null);
        }

        private static bool IsContentPremium(PreviewItem? movie, EpisodeItem? episode = null, WebBrowser.Models.Series.SerieDto? series = null)
        {
            if (movie != null)
            {
                if (string.Equals(movie.IsPremium, "Y", StringComparison.OrdinalIgnoreCase) ||
                    movie.IsPremium == "1" ||
                    string.Equals(movie.IsPremium, "True", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            if (series != null)
            {
                if (string.Equals(series.IsPremium, "Y", StringComparison.OrdinalIgnoreCase) ||
                    series.IsPremium == "1" ||
                    string.Equals(series.IsPremium, "True", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            if (episode != null && episode.IsPremium)
            {
                return true;
            }

            return false;
        }

        private sealed class SubscriptionStatusEnvelope
        {
            public List<SubscriptionStatusDto> Table { get; set; } = [];
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

            var currentUserJson = HttpContext.Session.GetString("CurrentUser") ?? HttpContext.Session.GetString("AdminCurrentUser");
            if (!string.IsNullOrEmpty(currentUserJson))
            {
                try
                {
                    var user = JsonConvert.DeserializeObject<WebBrowser.Models.AuthModels.UserInfo>(currentUserJson);
                    if (user != null && user.userId > 0)
                    {
                        dto.UserId = user.userId;
                        dto.UserName = string.IsNullOrWhiteSpace(user.fullName) ? dto.UserName : user.fullName;
                        dto.UserAvatar = string.IsNullOrWhiteSpace(user.avatarUrl) ? dto.UserAvatar : user.avatarUrl;
                    }
                }
                catch { }
            }

            var token = HttpContext.Session.GetString("JWToken") ?? HttpContext.Session.GetString("AdminJWToken");
            if (dto.UserId <= 0)
            {
                if (!string.IsNullOrWhiteSpace(token))
                {
                    dto.UserId = 1;
                    if (string.IsNullOrWhiteSpace(dto.UserName)) dto.UserName = "Người dùng TFlix";
                }
                else
                {
                    return Unauthorized(new { success = false, message = "Bạn cần đăng nhập để gửi bình luận." });
                }
            }

            var result = await _commentService.AddCommentAsync(dto);
            if (result == null || result.CommentId <= 0)
            {
                result = new CommentDto
                {
                    CommentId = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    MovieId = dto.MovieId,
                    EpisodeId = dto.EpisodeId,
                    UserId = dto.UserId,
                    UserName = string.IsNullOrWhiteSpace(dto.UserName) ? "Người dùng TFlix" : dto.UserName,
                    UserAvatar = !string.IsNullOrWhiteSpace(dto.UserAvatar) ? dto.UserAvatar : $"https://i.pravatar.cc/40?u={dto.UserId}",
                    Content = dto.Content,
                    CreatedAt = DateTime.Now,
                    FormattedTime = "Vừa xong"
                };
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
                if (episode == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy thông tin tập phim." });
                }

                WebBrowser.Models.Series.SerieDto? matchedSeries = null;
                if (episode.SeriesId > 0)
                {
                    try
                    {
                        var seriesResponse = await _seriesService.get_all();
                        matchedSeries = seriesResponse?.Data?.Table?.FirstOrDefault(x => x.SeriesId == episode.SeriesId);
                    }
                    catch { }
                }

                var isPremiumEp = episode.IsPremium || (matchedSeries != null && string.Equals(matchedSeries.IsPremium, "Y", StringComparison.OrdinalIgnoreCase));
                var (hasActiveSub, _) = await CheckUserSubscriptionAsync();

                if (isPremiumEp && !hasActiveSub)
                {
                    var token = HttpContext.Session.GetString("JWToken");
                    var isLoggedIn = !string.IsNullOrEmpty(token);
                    return Json(new
                    {
                        success = false,
                        isPremium = true,
                        isLoggedIn = isLoggedIn,
                        hasActiveSubscription = false,
                        message = isLoggedIn
                            ? "Nội dung này dành cho tài khoản Premium. Vui lòng thanh toán nâng cấp tài khoản để xem tiếp."
                            : "Nội dung này dành cho tài khoản Premium. Vui lòng đăng nhập và thanh toán nâng cấp tài khoản để xem.",
                        data = new
                        {
                            episodeId = episodeId,
                            episodeNo = episode.EpisodeNo,
                            episodeTitle = episode.EpisodeTitle,
                            isPremium = true,
                            hasSource = false,
                            sources = new object[0],
                            primaryUrl = ""
                        }
                    });
                }

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
                    isPremium = isPremiumEp,
                    hasActiveSubscription = hasActiveSub,
                    data = new
                    {
                        episodeId = episodeId,
                        episodeNo = episode.EpisodeNo,
                        episodeTitle = episode.EpisodeTitle,
                        durationMin = episode.DurationMin ?? 45,
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

        [HttpGet]
        public async Task<IActionResult> GetRating([FromQuery] long movieId)
        {
            if (movieId <= 0) return BadRequest(new { success = false, message = "ID phim không hợp lệ." });
            long? currentUserId = null;
            var currentUserJson = HttpContext.Session.GetString("CurrentUser") ?? HttpContext.Session.GetString("AdminCurrentUser");
            if (!string.IsNullOrEmpty(currentUserJson))
            {
                try
                {
                    var u = JsonConvert.DeserializeObject<WebBrowser.Models.AuthModels.UserInfo>(currentUserJson);
                    if (u?.userId > 0) currentUserId = u.userId;
                }
                catch { }
            }

            var result = await _ratingService.GetMovieRatingAsync(movieId, currentUserId);
            return Json(new { success = true, data = result });
        }

        [HttpPost]
        public async Task<IActionResult> RateMovie([FromBody] SetRatingClientRequest model)
        {
            if (model == null || model.MovieId <= 0 || model.Rating < 1 || model.Rating > 5)
            {
                return BadRequest(new { success = false, message = "Số sao đánh giá phải từ 1 đến 5 sao." });
            }

            long currentUserId = 0;
            var currentUserJson = HttpContext.Session.GetString("CurrentUser") ?? HttpContext.Session.GetString("AdminCurrentUser");
            if (!string.IsNullOrEmpty(currentUserJson))
            {
                try
                {
                    var u = JsonConvert.DeserializeObject<WebBrowser.Models.AuthModels.UserInfo>(currentUserJson);
                    if (u?.userId > 0) currentUserId = u.userId;
                }
                catch { }
            }

            if (currentUserId <= 0)
            {
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập để đánh giá phim." });
            }

            var result = await _ratingService.SetMovieRatingAsync(currentUserId, model.MovieId, model.Rating);
            return Json(new { success = true, message = "Đã lưu đánh giá thành công!", data = result });
        }

        private static PreviewItem? GetFallbackMovie(long id, string kind)
        {
            var isSeries = string.Equals(kind, "SERIES", StringComparison.OrdinalIgnoreCase);
            if (isSeries)
            {
                return new PreviewItem
                {
                    ContentId = id,
                    MovieId = id,
                    kind = "SERIES",
                    title = "Gia Tộc Rồng (Phần 2)",
                    OriginalTitle = "House of the Dragon Season 2",
                    ReleaseOrAirDate = new DateTime(2024, 6, 16),
                    CountryCode = "US",
                    LanguageCode = "en",
                    genres = "Kỳ ảo, Hành động, Phiêu lưu",
                    PrimaryPosterUrl = "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?w=500&auto=format&fit=crop",
                    IsPremium = "Y",
                    status = "PUBLISHED"
                };
            }

            var titles = new Dictionary<long, (string title, string orig, string isPrem, string poster)>
            {
                [1] = ("Lật Mặt 7: Một Điều Ước", "Wish 7", "Y", "https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=500&auto=format&fit=crop"),
                [2] = ("Mai", "Mai Movie", "N", "https://images.unsplash.com/photo-1485846234645-a62644f84728?w=500&auto=format&fit=crop"),
                [3] = ("Avatar: Dòng Chảy Của Nước", "Avatar: The Way of Water", "Y", "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?w=500&auto=format&fit=crop"),
                [4] = ("Oppenheimer", "Oppenheimer", "Y", "https://images.unsplash.com/photo-1440404653325-ab127d49abc1?w=500&auto=format&fit=crop"),
                [5] = ("Quật Mộ Trùng Phùng", "Exhuma", "Y", "https://images.unsplash.com/photo-1509198397868-475647b2a1e5?w=500&auto=format&fit=crop")
            };

            if (!titles.TryGetValue(id, out var info))
            {
                info = ("Lật Mặt 7: Một Điều Ước", "Wish 7", "Y", "https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=500&auto=format&fit=crop");
            }

            return new PreviewItem
            {
                ContentId = id,
                MovieId = id,
                kind = "MOVIE",
                title = info.title,
                OriginalTitle = info.orig,
                ReleaseOrAirDate = new DateTime(2024, 4, 26),
                CountryCode = "VN",
                LanguageCode = "vi",
                genres = "Hành động, Gia đình",
                PrimaryPosterUrl = info.poster,
                IsPremium = info.isPrem,
                status = "PUBLISHED"
            };
        }

        [HttpGet]
        public async Task<IActionResult> GetSubtitles(long id, long? episodeId = null, string? title = null, string? overview = null, double? duration = null)
        {
            var res = await _subtitleService.GetSubtitlesAsync(id, episodeId, title, overview, duration);
            return Json(res ?? new GenerateSubtitleResponseDto { Success = false, Message = "Không thể lấy danh sách phụ đề." });
        }

        [HttpPost]
        public async Task<IActionResult> GenerateSubtitles([FromBody] GenerateSubtitleRequestDto request)
        {
            if (request == null)
            {
                return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ." });
            }
            var res = await _subtitleService.GenerateSubtitlesAsync(request);
            return Json(res ?? new GenerateSubtitleResponseDto { Success = false, Message = "Không thể tạo phụ đề bằng Gemini." });
        }

        [HttpGet]
        public async Task<IActionResult> SubtitleVtt(long id, string lang, long? episodeId = null)
        {
            var vtt = await _subtitleService.GetWebVttAsync(id, episodeId, lang);
            return Content(vtt ?? "WEBVTT\n\n", "text/vtt; charset=utf-8");
        }
    }

    public class SetRatingClientRequest
    {
        public long MovieId { get; set; }
        public int Rating { get; set; }
    }
}
