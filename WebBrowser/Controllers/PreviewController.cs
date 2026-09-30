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

        public PreviewController(
            IPreviewService previewService,
            IMovieService movieService,
            IGenresService genresService,
            IEpisode episodeService)
        {
            _previewService = previewService;
            _movieService = movieService;
            _genresService = genresService;
            _episodeService = episodeService;
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

            // 3. Fetch episodes if kind == "SERIES" or if episodes exist
            List<EpisodeItem> episodes = new();
            try
            {
                var episodesResp = await _episodeService.get_all();
                var allEp = episodesResp?.Data?.Table ?? new List<EpisodeItem>();
                episodes = allEp.Where(x => x.SeriesId == id).OrderBy(x => x.SeasonNo).ThenBy(x => x.EpisodeNo).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Preview.Details] Error loading episodes: " + ex.Message);
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
    }
}
