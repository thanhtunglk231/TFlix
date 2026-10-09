using CoreLib.Dtos.Comment;
using CoreLib.Dtos.Preview;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebBrowser.Models.Episode;
using WebBrowser.Models.Genres;
using WebBrowser.Models.Movie;
using WebBrowser.Models.Preview;
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

        public PreviewController(
            IPreviewService previewService,
            IMovieService movieService,
            IGenresService genresService,
            IEpisode episodeService,
            ICommentService commentService)
        {
            _previewService = previewService;
            _movieService = movieService;
            _genresService = genresService;
            _episodeService = episodeService;
            _commentService = commentService;
        }

        private bool IsUserAuthenticated()
        {
            var token = HttpContext.Session.GetString("JWToken");
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

        public async Task<IActionResult> Details(int id, string kind)
        {
            if (!IsUserAuthenticated())
            {
                string returnUrl = Url.Action("Details", "Preview", new { id, kind }) ?? "/Movies";
                return RedirectToAction("Index", "Auth", new { returnUrl });
            }

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

            // 1. Fetch catalog movies for sidebar (Upcoming & Trending)
            List<MovieItem> upcomingMovies = new();
            List<MovieItem> trendingMovies = new();
            try
            {
                var moviesResp = await _movieService.get_all();
                if (moviesResp?.Data?.Table != null)
                {
                    var allMovies = moviesResp.Data.Table;
                    upcomingMovies = allMovies.Where(x => x.MovieId != id).Take(4).ToList();
                    trendingMovies = allMovies.Where(x => x.MovieId != id).Take(5).ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Preview.Details] Error loading sidebar movies: " + ex.Message);
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

            var vm = new PreviewDetailsViewModel
            {
                Movie = movie,
                UpcomingMovies = upcomingMovies,
                TrendingMovies = trendingMovies,
                HotTags = hotTags,
                Episodes = episodes
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
                return Unauthorized(new { success = false, message = "Bạn cần đăng nhập để gửi bình luận." });
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
    }
}
