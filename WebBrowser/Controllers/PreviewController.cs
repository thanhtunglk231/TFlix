using CoreLib.Dtos.Comment;
using CoreLib.Dtos.Preview;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebBrowser.Models.Episode;
using WebBrowser.Models.Genres;
using WebBrowser.Models.Movie;
using WebBrowser.Models.Preview;
using WebBrowser.Helpers;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Controllers
{
    public class PreviewController : Controller
    {
        private readonly IPreviewService _previewService;
        private readonly IMovieService _movieService;
        private readonly IGenresService _genresService;
        private readonly IEpisode _episodeService;
        private readonly ICommentService _commentService;
        private readonly IRatingService _ratingService;
        private readonly ISeriesService _seriesService;

        public PreviewController(
            IPreviewService previewService,
            IMovieService movieService,
            IGenresService genresService,
            IEpisode episodeService,
            ICommentService commentService,
            IRatingService ratingService,
            ISeriesService seriesService)
        {
            _previewService = previewService;
            _movieService = movieService;
            _genresService = genresService;
            _episodeService = episodeService;
            _commentService = commentService;
            _ratingService = ratingService;
            _seriesService = seriesService;
        }

        private bool IsUserAuthenticated()
        {
            var token = HttpContext.Session.GetString("JWToken") ?? HttpContext.Session.GetString("AdminJWToken");
            return !string.IsNullOrEmpty(token) || (User != null && User.Identity != null && User.Identity.IsAuthenticated);
        }

        public IActionResult Index()
        {
            if (!IsUserAuthenticated())
            {
                return RedirectToAction("Index", "Auth", new { returnUrl = "/Movies" });
            }
            return View();
        }

        public async Task<IActionResult> Details(int id, string kind, string? slug = null)
        {
            var request = new GETCONTENTByID
            {
                id = id,
                kind = string.IsNullOrEmpty(kind) ? "movie" : kind
            };

            Console.WriteLine($"[Preview.Details] id={id}, kind={kind}");

            var resp = await _previewService.get_preview(request);
            Console.WriteLine("[Preview.Details] resp = " + JsonConvert.SerializeObject(resp));

            if (resp == null || !resp.success || resp.Data?.Table == null || resp.Data.Table.Count == 0)
            {
                return NotFound("Không tìm thấy nội dung phim");
            }

            var movie = resp.Data.Table[0];
            var expectedSlug = SeoUrlHelper.ToSlug(movie.title ?? movie.OriginalTitle);
            var isSeries = string.Equals(kind, "SERIES", StringComparison.OrdinalIgnoreCase);
            if (!string.Equals(slug, expectedSlug, StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToRoutePermanent(
                    isSeries ? "series-details" : "movie-details",
                    new { slug = expectedSlug, id });
            }

            // 1. Fetch catalog movies for sidebar (Upcoming & Trending) and single movies
            List<MovieItem> upcomingMovies = new();
            List<MovieItem> trendingMovies = new();
            List<MovieItem> singleMovies = new();
            try
            {
                var moviesResp = await _movieService.get_all();
                if (moviesResp?.Data?.Table != null)
                {
                    var allMovies = moviesResp.Data.Table;
                    upcomingMovies = allMovies.Where(x => x.MovieId != id).Take(4).ToList();
                    trendingMovies = allMovies.Where(x => x.MovieId != id).Take(5).ToList();
                    singleMovies = allMovies.Where(x => x.MovieId != id).Take(6).ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Preview.Details] Error loading sidebar movies: " + ex.Message);
            }

            // 1b. Fetch series movies
            List<WebBrowser.Models.Series.SerieDto> seriesMovies = new();
            try
            {
                var seriesResp = await _seriesService.get_all();
                if (seriesResp?.Data?.Table != null)
                {
                    seriesMovies = seriesResp.Data.Table.Take(6).ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Preview.Details] Error loading series movies: " + ex.Message);
            }

            // 2. Fetch genres for sidebar Hot Tags
            List<GenreItem> hotTags = new();
            try
            {
                var genresResp = await _genresService.get_all();
                hotTags = genresResp?.Data?.Table ?? new List<GenreItem>();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Preview.Details] Error loading genres: " + ex.Message);
            }

            // Episodes belong to series; movie and series IDs can overlap.
            List<EpisodeItem> episodes = new();
            if (string.Equals(kind, "SERIES", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var episodesResp = await _episodeService.GetBySeriesAsync(id);
                    episodes = episodesResp?.Data?.Table?
                        .OrderBy(x => x.SeasonNo)
                        .ThenBy(x => x.EpisodeNo)
                        .ThenBy(x => x.EpisodeId)
                        .ToList() ?? new List<EpisodeItem>();

                    if (episodes.Any())
                    {
                        movie.kind = "SERIES";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[Preview.Details] Error loading episodes: " + ex.Message);
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
                Console.WriteLine("[Preview.Details] Error loading rating: " + ex.Message);
            }

            var vm = new PreviewDetailsViewModel
            {
                Movie = movie,
                UpcomingMovies = upcomingMovies,
                TrendingMovies = trendingMovies,
                HotTags = hotTags,
                Episodes = episodes,
                RatingInfo = ratingInfo,
                SeriesMovies = seriesMovies,
                SingleMovies = singleMovies
            };

            return View("Index", vm);
        }

        public async Task<IActionResult> getpreview([FromQuery] GETCONTENTByID movie)
        {
            var result = await _previewService.get_preview(movie);
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetComments([FromQuery] long? movieId, [FromQuery] long? episodeId)
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
        public async Task<IActionResult> GetRating([FromQuery] long movieId)
        {
            if (movieId <= 0) return BadRequest(new { success = false, message = "ID phim không hợp lệ." });
            long? currentUserId = null;
            var currentUserJson = HttpContext.Session.GetString("CurrentUser");
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
    }
}
