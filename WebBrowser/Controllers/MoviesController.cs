using CoreLib.Dtos.Movies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Controllers
{
    public class MoviesController : Controller
    {
        private readonly IMovieService _movieService;
        private readonly IGenresService _genresService;

        public MoviesController(IMovieService movieService, IGenresService genresService)
        {
            _movieService = movieService;
            _genresService = genresService;
        }

        private bool IsUserAuthenticated()
        {
            var token = HttpContext.Session.GetString("JWToken");
            return !string.IsNullOrEmpty(token) || (User != null && User.Identity != null && User.Identity.IsAuthenticated);
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? q, int? genreId, string? countryCode, int? year, string sortBy = "newest", int page = 1)
        {
            // Kiểm tra Đăng nhập
            if (!IsUserAuthenticated())
            {
                string returnUrl = Url.Action("Index", "Movies", new { q, genreId, countryCode, year, sortBy, page }) ?? "/Movies";
                return RedirectToAction("Index", "Auth", new { returnUrl });
            }
            var filter = new MovieCatalogFilterDto
            {
                Search = q,
                GenreId = genreId,
                CountryCode = countryCode,
                Year = year,
                SortBy = string.IsNullOrEmpty(sortBy) ? "newest" : sortBy,
                Page = page < 1 ? 1 : page,
                PageSize = 12
            };

            // Fetch genres for filter dropdown
            try
            {
                var genresResp = await _genresService.get_all();
                if (genresResp != null && genresResp.Data != null && genresResp.Data.Table != null)
                {
                    ViewBag.Genres = genresResp.Data.Table;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[MoviesController] Lỗi lấy danh sách thể loại: " + ex.Message);
            }

            // Fetch movie catalog data
            MovieCatalogResultDto catalogResult = new MovieCatalogResultDto();
            try
            {
                var resp = await _movieService.GetCatalogMovies(filter);
                if (resp != null && resp.Data != null)
                {
                    string jsonStr = JsonConvert.SerializeObject(resp.Data);
                    var parsedResult = JsonConvert.DeserializeObject<MovieCatalogResultDto>(jsonStr);
                    if (parsedResult != null)
                    {
                        catalogResult = parsedResult;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[MoviesController] Lỗi lấy danh sách phim: " + ex.Message);
            }

            ViewBag.Filter = filter;
            return View(catalogResult);
        }

        [HttpGet]
        public IActionResult Search(string q)
        {
            return RedirectToAction("Index", new { q = q });
        }
    }
}
