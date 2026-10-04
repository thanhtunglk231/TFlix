using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebBrowser.Models.Genres;
using WebBrowser.Models.Series;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Controllers
{
    public class SeriesController : Controller
    {
        private readonly ISeriesService _seriesService;
        private readonly IGenresService _genresService;

        public SeriesController(ISeriesService seriesService, IGenresService genresService)
        {
            _seriesService = seriesService;
            _genresService = genresService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? q, int? genreId, string? countryCode, int? year, string sortBy = "newest", int page = 1)
        {
            var filter = new SeriesCatalogFilter
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
                Console.WriteLine("[SeriesController] Lỗi lấy danh sách thể loại: " + ex.Message);
            }
            ViewBag.Genres = genres;

            // 2. Lấy toàn bộ danh sách phim bộ từ Service
            List<SerieDto> allSeries = new();
            try
            {
                var resp = await _seriesService.get_all();
                if (resp != null && resp.Data != null && resp.Data.Table != null)
                {
                    allSeries = resp.Data.Table;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[SeriesController] Lỗi lấy danh sách phim bộ: " + ex.Message);
            }

            // 3. Áp dụng bộ lọc tìm kiếm
            IEnumerable<SerieDto> query = allSeries;

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var term = filter.Search;
                query = query.Where(s =>
                    (!string.IsNullOrEmpty(s.Title) && s.Title.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(s.OriginalTitle) && s.OriginalTitle.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(s.Genres) && s.Genres.Contains(term, StringComparison.OrdinalIgnoreCase))
                );
            }

            if (filter.GenreId.HasValue && filter.GenreId.Value > 0)
            {
                var matchedGenre = genres.FirstOrDefault(g => g.GenreId == filter.GenreId.Value);
                if (matchedGenre != null && !string.IsNullOrWhiteSpace(matchedGenre.GenreName))
                {
                    var genreName = matchedGenre.GenreName.Trim();
                    query = query.Where(s => !string.IsNullOrEmpty(s.Genres) && s.Genres.Contains(genreName, StringComparison.OrdinalIgnoreCase));
                }
            }

            if (!string.IsNullOrWhiteSpace(filter.CountryCode))
            {
                query = query.Where(s => !string.IsNullOrEmpty(s.CountryCode) && string.Equals(s.CountryCode, filter.CountryCode, StringComparison.OrdinalIgnoreCase));
            }

            if (filter.Year.HasValue && filter.Year.Value > 0)
            {
                query = query.Where(s =>
                    (s.FirstAirDate.HasValue && s.FirstAirDate.Value.Year == filter.Year.Value) ||
                    (s.LatestAirDate.HasValue && s.LatestAirDate.Value.Year == filter.Year.Value)
                );
            }

            // 4. Sắp xếp kết quả
            query = filter.SortBy switch
            {
                "oldest" => query.OrderBy(s => s.FirstAirDate ?? s.CreatedAt),
                "rating" => query.OrderByDescending(s => s.AvgRating ?? 0).ThenByDescending(s => s.CreatedAt),
                "episodes" => query.OrderByDescending(s => s.EpisodeCount).ThenByDescending(s => s.CreatedAt),
                "title" => query.OrderBy(s => s.Title),
                _ => query.OrderByDescending(s => s.FirstAirDate ?? s.CreatedAt)
            };

            // 5. Phân trang
            int totalCount = query.Count();
            var pagedItems = query.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToList();

            var viewModel = new SeriesCatalogViewModel
            {
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize,
                Items = pagedItems,
                Filter = filter
            };

            ViewBag.Filter = filter;
            return View(viewModel);
        }
    }
}
