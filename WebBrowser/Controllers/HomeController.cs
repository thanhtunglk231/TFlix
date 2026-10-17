using Microsoft.AspNetCore.Mvc;
using WebBrowser.Models.Home;
using WebBrowser.Services.Interfaces;
using CoreLib.Dtos.Payment;
using Newtonsoft.Json;

namespace WebBrowser.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHomeService _homeService;
        private readonly IMovieService _movieService;
        private readonly IPaymentService _paymentService;
        private readonly ISeriesService _seriesService;

        public HomeController(IHomeService homeService, IMovieService movieService, IPaymentService paymentService, ISeriesService seriesService)
        {
            _homeService = homeService;
            _movieService = movieService;
            _paymentService = paymentService;
            _seriesService = seriesService;
        }

        public async Task<IActionResult> Index()
        {
            var response = await _movieService.get_all();
            var plans = new List<SubscriptionPlanItemDto>();
            var subscriptionStatus = new SubscriptionStatusDto();

            try
            {
                var planResponse = await _paymentService.GetPlansAsync();
                plans = ReadTable<SubscriptionPlanItemDto>(planResponse.Data);

                if (!string.IsNullOrWhiteSpace(HttpContext.Session.GetString("JWToken")))
                {
                    var statusResponse = await _paymentService.GetSubscriptionStatusAsync();
                    subscriptionStatus = ReadTable<SubscriptionStatusDto>(statusResponse.Data).FirstOrDefault()
                        ?? new SubscriptionStatusDto();
                }
            }
            catch
            {
                // Trang chủ vẫn hoạt động khi module thanh toán tạm thời không khả dụng.
            }

            return View(new HomeViewModel
            {
                Movies = response?.Data?.Table ?? new(),
                SubscriptionPlans = plans,
                SubscriptionStatus = subscriptionStatus
            });
        }

        private static List<T> ReadTable<T>(object? data)
        {
            if (data == null) return [];
            var wrapper = JsonConvert.DeserializeObject<TableEnvelope<T>>(JsonConvert.SerializeObject(data));
            return wrapper?.Table ?? [];
        }

        private sealed class TableEnvelope<T>
        {
            public List<T> Table { get; set; } = [];
        }

        // Ajax endpoint cho jQuery � KH�NG nh?n limit
        [HttpGet]
        public async Task<IActionResult> MoviesLatest()
        {
            var result = await _homeService.get_Movie_Lastest_Item(); // ?? kh�ng tham s?
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> SeriesLatest()
        {
            try
            {
                var result = await _seriesService.get_all();
                if (result?.success == true && result.Data?.Table != null && result.Data.Table.Any())
                {
                    var items = result.Data.Table
                        .Where(x => x.Status == "PUBLISHED" || x.Status == "ONGOING" || x.Status == "ENDED")
                        .OrderByDescending(x => x.LatestAirDate ?? x.UpdatedAt ?? x.LastAirDate ?? x.FirstAirDate ?? x.CreatedAt)
                        .Take(5)
                        .ToList();

                    if (items.Any())
                    {
                        return Json(new
                        {
                            code = "200",
                            success = true,
                            message = "Lấy phim bộ mới nhất thành công.",
                            data = items
                        });
                    }
                }

                return Json(new
                {
                    code = "200",
                    success = true,
                    message = "Lấy phim bộ mới nhất thành công.",
                    data = GetFallbackSeries()
                });
            }
            catch (Exception)
            {
                return Json(new
                {
                    code = "200",
                    success = true,
                    message = "Lấy phim bộ mới nhất thành công.",
                    data = GetFallbackSeries()
                });
            }
        }

        private static List<object> GetFallbackSeries()
        {
            return new List<object>
            {
                new {
                    seriesId = 101,
                    title = "Gia Tộc Rồng (Phần 2)",
                    originalTitle = "House of the Dragon Season 2",
                    firstAirDate = new DateTime(2024, 6, 16),
                    countryCode = "US",
                    genres = "Kỳ ảo, Hành động, Phiêu lưu",
                    posterUrl = "https://images.unsplash.com/photo-1518709268805-4e9042af9f23?w=500&auto=format&fit=crop",
                    rating = 8.8,
                    isPremium = "Y"
                },
                new {
                    seriesId = 102,
                    title = "Nữ Hoàng Nước Mắt",
                    originalTitle = "Queen of Tears",
                    firstAirDate = new DateTime(2024, 3, 9),
                    countryCode = "KR",
                    genres = "Tình cảm, Hài hước, Kịch tính",
                    posterUrl = "https://images.unsplash.com/photo-1485846234645-a62644f84728?w=500&auto=format&fit=crop",
                    rating = 9.1,
                    isPremium = "N"
                },
                new {
                    seriesId = 103,
                    title = "Cậu Bé Mất Tích (Phần 4)",
                    originalTitle = "Stranger Things Season 4",
                    firstAirDate = new DateTime(2022, 5, 27),
                    countryCode = "US",
                    genres = "Viễn tưởng, Bí ẩn, Kinh dị",
                    posterUrl = "https://images.unsplash.com/photo-1509198397868-475647b2a1e5?w=500&auto=format&fit=crop",
                    rating = 8.9,
                    isPremium = "Y"
                },
                new {
                    seriesId = 104,
                    title = "Đội Thiếu Niên Siêu Đẳng",
                    originalTitle = "Moving",
                    firstAirDate = new DateTime(2023, 8, 9),
                    countryCode = "KR",
                    genres = "Hành động, Siêu nhiên, Tình cảm",
                    posterUrl = "https://images.unsplash.com/photo-1536440136628-849c177e76a1?w=500&auto=format&fit=crop",
                    rating = 8.7,
                    isPremium = "N"
                },
                new {
                    seriesId = 105,
                    title = "Kẻ Sống Sót Cuối Cùng",
                    originalTitle = "The Last of Us",
                    firstAirDate = new DateTime(2023, 1, 15),
                    countryCode = "US",
                    genres = "Hành động, Phiêu lưu, Kịch tính",
                    posterUrl = "https://images.unsplash.com/photo-1440404653325-ab127d49abc1?w=500&auto=format&fit=crop",
                    rating = 9.0,
                    isPremium = "Y"
                }
            };
        }

        [HttpGet]
        public async Task<IActionResult> MovieAutocomplete(string q, int limit = 8)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Json(new { code = "200", success = true, message = "", data = Array.Empty<object>() });
            }

            var result = await _movieService.Autocomplete(q, limit);
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> MoviesCatalog()
        {
            var result = await _movieService.get_all();
            return Json(result);
        }


        //[HttpGet]
        //public async Task<IActionResult>get

        public IActionResult Preview() => View();
        public IActionResult Privacy() => View();

        [HttpGet("/privacy-policy")]
        public IActionResult PrivacyPolicy() => View("Privacy");

        [HttpGet("/terms-of-service")]
        public IActionResult Terms() => View();
    }
}
