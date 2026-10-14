using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using WebBrowser.Helpers;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Controllers
{
    public sealed class SeoController : Controller
    {
        private static readonly XNamespace SitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";
        private readonly IMovieService _movieService;
        private readonly ISeriesService _seriesService;
        private readonly ILogger<SeoController> _logger;

        public SeoController(IMovieService movieService, ISeriesService seriesService, ILogger<SeoController> logger)
        {
            _movieService = movieService;
            _seriesService = seriesService;
            _logger = logger;
        }

        [HttpGet("/sitemap.xml")]
        [ResponseCache(Duration = 1800, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> Sitemap()
        {
            var urls = new List<XElement>
            {
                CreateUrl(AbsoluteUrl("/"), DateTimeOffset.UtcNow, "daily", "1.0"),
                CreateUrl(AbsoluteUrl("/phim"), null, "daily", "0.9"),
                CreateUrl(AbsoluteUrl("/phim-bo"), null, "daily", "0.9")
            };

            try
            {
                var movies = (await _movieService.get_all())?.Data?.Table ?? [];
                urls.AddRange(movies
                    .Where(movie => movie.MovieId > 0 && !string.IsNullOrWhiteSpace(movie.Title))
                    .Select(movie => CreateUrl(
                        AbsoluteUrl(Url.RouteUrl("movie-details", new { slug = SeoUrlHelper.ToSlug(movie.Title), id = movie.MovieId })!),
                        movie.UpdatedAt ?? movie.CreatedAt,
                        "weekly",
                        "0.8")));

                var series = (await _seriesService.get_all())?.Data?.Table ?? [];
                urls.AddRange(series
                    .Where(item => item.SeriesId > 0 && !string.IsNullOrWhiteSpace(item.Title))
                    .Select(item => CreateUrl(
                        AbsoluteUrl(Url.RouteUrl("series-details", new { slug = SeoUrlHelper.ToSlug(item.Title), id = item.SeriesId })!),
                        item.UpdatedAt ?? item.CreatedAt,
                        "weekly",
                        "0.8")));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Could not load all content while generating sitemap.xml");
            }

            var document = new XDocument(new XElement(SitemapNamespace + "urlset", urls));
            return Content(document.ToString(SaveOptions.DisableFormatting), "application/xml", Encoding.UTF8);
        }

        [HttpGet("/robots.txt")]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
        public IActionResult Robots()
        {
            return Content(
                $"User-agent: *\nAllow: /\nDisallow: /Admin/\nDisallow: /api/\nSitemap: {AbsoluteUrl("/sitemap.xml")}\n",
                "text/plain",
                Encoding.UTF8);
        }

        private string AbsoluteUrl(string relativeUrl) =>
            $"{Request.Scheme}://{Request.Host}{Request.PathBase}{relativeUrl}";

        private static XElement CreateUrl(string location, DateTimeOffset? lastModified, string changeFrequency, string priority)
        {
            var element = new XElement(SitemapNamespace + "url",
                new XElement(SitemapNamespace + "loc", location),
                new XElement(SitemapNamespace + "changefreq", changeFrequency),
                new XElement(SitemapNamespace + "priority", priority));
            if (lastModified.HasValue)
                element.Add(new XElement(SitemapNamespace + "lastmod", lastModified.Value.UtcDateTime.ToString("yyyy-MM-dd")));
            return element;
        }
    }
}
