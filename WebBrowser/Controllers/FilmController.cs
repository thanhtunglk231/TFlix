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

        public FilmController(
            IPreviewService previewService,
            IEpisode episodeService,
            IVideoSoureService videoSourceService,
            ISeriesService seriesService)
        {
            _previewService = previewService;
            _episodeService = episodeService;
            _videoSourceService = videoSourceService;
            _seriesService = seriesService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Watch(long id, string kind)
        {
            // Kiểm tra Đăng nhập (Authentication check)
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token) && (User == null || !User.Identity.IsAuthenticated))
            {
                // Chưa đăng nhập -> Chuyển hướng sang trang đăng nhập
                string returnUrl = Url.Action("Watch", "Film", new { id, kind }) ?? "/Movies";
                return RedirectToAction("Index", "Auth", new { returnUrl });
            }

            var request = new GETCONTENTByID
            {
                id = id,
                kind = string.IsNullOrEmpty(kind) ? "movie" : kind
            };

            Console.WriteLine($"[FilmController.Watch] id={id}, kind={kind}");

            PreviewItem? movie = null;
            if (string.Equals(kind, "SERIES", StringComparison.OrdinalIgnoreCase))
            {
                var seriesResponse = await _seriesService.get_all();
                var series = seriesResponse?.Data?.Table?.FirstOrDefault(x => x.SeriesId == id);
                if (series == null) return NotFound("Không tìm thấy series");

                movie = new PreviewItem
                {
                    ContentId = series.SeriesId,
                    kind = "SERIES",
                    title = series.Title,
                    OriginalTitle = series.OriginalTitle,
                    ReleaseOrAirDate = series.FirstAirDate,
                    CountryCode = series.CountryCode,
                    LanguageCode = series.LanguageCode,
                    status = series.Status,
                    IsPremium = series.IsPremium,
                    genres = series.Genres,
                    PrimaryPosterUrl = series.PosterUrl
                };
            }
            else
            {
                var resp = await _previewService.get_preview(request);
                Console.WriteLine("[Preview.Details] resp = " + JsonConvert.SerializeObject(resp));
                if (resp == null || resp.Data?.Table == null || resp.Data.Table.Count == 0)
                    return NotFound("Không tìm thấy nội dung");
                movie = resp.Data.Table[0];
            }

            Console.WriteLine(JsonConvert.SerializeObject(movie));

            var episodesResponse = await _episodeService.get_all();
            var sourcesResponse = await _videoSourceService.get_all();
            var episodes = episodesResponse?.Data?.Table?
                .Where(x => x.SeriesId == id && string.Equals(kind, "SERIES", StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.SeasonNo)
                .ThenBy(x => x.EpisodeNo)
                .ToList() ?? new List<WebBrowser.Models.Episode.EpisodeItem>();

            var episodeIds = episodes.Select(x => x.EpisodeId).ToHashSet();
            var sources = sourcesResponse?.Data?.Table?
                .Where(x => string.Equals(kind, "SERIES", StringComparison.OrdinalIgnoreCase)
                    ? x.EpisodeId.HasValue && episodeIds.Contains(x.EpisodeId.Value)
                    : x.MovieId == id)
                .Where(x => string.Equals(x.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
                .ToList() ?? new List<WebBrowser.Models.VideoSoure.SourceItem>();

            var currentEpisodeId = episodes
                .FirstOrDefault(x => sources.Any(source => source.EpisodeId == x.EpisodeId))?.EpisodeId
                ?? episodes.FirstOrDefault()?.EpisodeId;

            return View("Index", new WatchViewModel
            {
                Content = movie,
                Episodes = episodes,
                Sources = sources,
                CurrentEpisodeId = currentEpisodeId
            });
        }
    }
}
