using CoreLib.Dtos.Movies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebBrowser.Models.Genres;
using WebBrowser.Models.Movie;
using WebBrowser.Models.Series;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Controllers
{
    public class MoviesController : Controller
    {
        private readonly IMovieService _movieService;
        private readonly IGenresService _genresService;
        private readonly ISeriesService _seriesService;

        public MoviesController(IMovieService movieService, IGenresService genresService, ISeriesService seriesService)
        {
            _movieService = movieService;
            _genresService = genresService;
            _seriesService = seriesService;
        }

        private bool IsUserAuthenticated()
        {
            var token = HttpContext.Session.GetString("JWToken");
            return !string.IsNullOrEmpty(token) || (User != null && User.Identity != null && User.Identity.IsAuthenticated);
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? q, int? genreId, string? countryCode, int? year, string sortBy = "newest", string type = "all", int page = 1)
        {
            // Kiểm tra Đăng nhập
            if (!IsUserAuthenticated())
            {
                string returnUrl = Url.Action("Index", "Movies", new { q, genreId, countryCode, year, sortBy, type, page }) ?? "/Movies";
                return RedirectToAction("Index", "Auth", new { returnUrl });
            }

            var filter = new MovieCatalogFilterDto
            {
                Search = q?.Trim(),
                GenreId = genreId,
                CountryCode = countryCode?.Trim(),
                Year = year,
                SortBy = string.IsNullOrEmpty(sortBy) ? "newest" : sortBy.ToLowerInvariant(),
                Page = page < 1 ? 1 : page,
                PageSize = 12
            };

            // 1. Lấy danh sách thể loại cho bộ lọc
            List<GenreItem> genres = new();
            try
            {
                var genresResp = await _genresService.get_all();
                if (genresResp != null && genresResp.Data != null && genresResp.Data.Table != null)
                {
                    genres = genresResp.Data.Table;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[MoviesController] Lỗi lấy danh sách thể loại: " + ex.Message);
            }
            ViewBag.Genres = genres;

            string selectedGenreName = "";
            if (filter.GenreId.HasValue && filter.GenreId.Value > 0)
            {
                var matchedG = genres.FirstOrDefault(g => g.GenreId == filter.GenreId.Value);
                if (matchedG != null) selectedGenreName = matchedG.GenreName?.Trim() ?? "";
            }

            List<MovieCatalogItemDto> allItems = new();

            // 2. Lấy danh sách Phim lẻ (Single Movies) nếu type != "series"
            if (!string.Equals(type, "series", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var movieResp = await _movieService.get_all();
                    var rawMovies = movieResp?.Data?.Table ?? new List<MovieItem>();

                    foreach (var m in rawMovies)
                    {
                        // Filter
                        if (!string.IsNullOrWhiteSpace(filter.Search))
                        {
                            bool matchSearch = (!string.IsNullOrEmpty(m.Title) && m.Title.Contains(filter.Search, StringComparison.OrdinalIgnoreCase))
                                            || (!string.IsNullOrEmpty(m.OriginalTitle) && m.OriginalTitle.Contains(filter.Search, StringComparison.OrdinalIgnoreCase))
                                            || (!string.IsNullOrEmpty(m.Genres) && m.Genres.Contains(filter.Search, StringComparison.OrdinalIgnoreCase));
                            if (!matchSearch) continue;
                        }

                        if (!string.IsNullOrEmpty(selectedGenreName))
                        {
                            if (string.IsNullOrEmpty(m.Genres) || !m.Genres.Contains(selectedGenreName, StringComparison.OrdinalIgnoreCase))
                                continue;
                        }

                        if (!string.IsNullOrWhiteSpace(filter.CountryCode))
                        {
                            if (!string.Equals(m.CountryCode, filter.CountryCode, StringComparison.OrdinalIgnoreCase))
                                continue;
                        }

                        if (filter.Year.HasValue && filter.Year.Value > 0)
                        {
                            if (!m.ReleaseDate.HasValue || m.ReleaseDate.Value.Year != filter.Year.Value)
                                continue;
                        }

                        allItems.Add(new MovieCatalogItemDto
                        {
                            MovieId = m.MovieId,
                            Title = m.Title ?? "",
                            OriginalTitle = m.OriginalTitle,
                            Overview = "",
                            ReleaseDate = m.ReleaseDate,
                            DurationMin = m.DurationMin,
                            CountryCode = m.CountryCode,
                            LanguageCode = m.LanguageCode,
                            Status = m.Status,
                            IsPremiumYN = m.IsPremium ? "Y" : "N",
                            CreatedAt = m.CreatedAt?.DateTime ?? DateTime.Now,
                            PosterUrl = m.PosterUrl,
                            Genres = m.Genres,
                            ContentType = "MOVIE",
                            EpisodeCount = 0
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[MoviesController] Lỗi lấy danh sách phim lẻ: " + ex.Message);
                }
            }

            // 3. Lấy danh sách Phim bộ (Series) nếu type != "movie"
            if (!string.Equals(type, "movie", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var seriesResp = await _seriesService.get_all();
                    var rawSeries = seriesResp?.Data?.Table ?? new List<SerieDto>();

                    foreach (var s in rawSeries)
                    {
                        // Filter
                        if (!string.IsNullOrWhiteSpace(filter.Search))
                        {
                            bool matchSearch = (!string.IsNullOrEmpty(s.Title) && s.Title.Contains(filter.Search, StringComparison.OrdinalIgnoreCase))
                                            || (!string.IsNullOrEmpty(s.OriginalTitle) && s.OriginalTitle.Contains(filter.Search, StringComparison.OrdinalIgnoreCase))
                                            || (!string.IsNullOrEmpty(s.Genres) && s.Genres.Contains(filter.Search, StringComparison.OrdinalIgnoreCase));
                            if (!matchSearch) continue;
                        }

                        if (!string.IsNullOrEmpty(selectedGenreName))
                        {
                            if (string.IsNullOrEmpty(s.Genres) || !s.Genres.Contains(selectedGenreName, StringComparison.OrdinalIgnoreCase))
                                continue;
                        }

                        if (!string.IsNullOrWhiteSpace(filter.CountryCode))
                        {
                            if (!string.Equals(s.CountryCode, filter.CountryCode, StringComparison.OrdinalIgnoreCase))
                                continue;
                        }

                        if (filter.Year.HasValue && filter.Year.Value > 0)
                        {
                            bool matchYear = (s.FirstAirDate.HasValue && s.FirstAirDate.Value.Year == filter.Year.Value)
                                          || (s.LatestAirDate.HasValue && s.LatestAirDate.Value.Year == filter.Year.Value);
                            if (!matchYear) continue;
                        }

                        allItems.Add(new MovieCatalogItemDto
                        {
                            MovieId = s.SeriesId,
                            Title = s.Title ?? "",
                            OriginalTitle = s.OriginalTitle,
                            Overview = "",
                            ReleaseDate = s.FirstAirDate ?? s.CreatedAt,
                            DurationMin = null,
                            CountryCode = s.CountryCode,
                            LanguageCode = s.LanguageCode,
                            Status = s.Status,
                            IsPremiumYN = s.IsPremium ?? "N",
                            CreatedAt = s.CreatedAt,
                            PosterUrl = s.PosterUrl,
                            Genres = s.Genres,
                            ContentType = "SERIES",
                            EpisodeCount = s.EpisodeCount
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[MoviesController] Lỗi lấy danh sách phim bộ: " + ex.Message);
                }
            }

            // 4. Sắp xếp kết quả
            IEnumerable<MovieCatalogItemDto> query = allItems;
            query = filter.SortBy switch
            {
                "oldest" => query.OrderBy(x => x.ReleaseDate ?? x.CreatedAt),
                "title" => query.OrderBy(x => x.Title),
                _ => query.OrderByDescending(x => x.ReleaseDate ?? x.CreatedAt)
            };

            // 5. Phân trang
            int totalCount = query.Count();
            var pagedItems = query.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToList();

            var catalogResult = new MovieCatalogResultDto
            {
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize,
                Items = pagedItems
            };

            ViewBag.Filter = filter;
            ViewBag.Type = type;
            return View(catalogResult);
        }

        [HttpGet]
        public IActionResult Search(string q)
        {
            return RedirectToAction("Index", new { q = q });
        }
    }
}
