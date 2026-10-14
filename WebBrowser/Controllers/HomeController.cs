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
                if (result?.success != true || result.Data?.Table == null)
                {
                    return Json(new
                    {
                        code = result?.code ?? "500",
                        success = false,
                        message = result?.message ?? "Không thể tải phim bộ mới nhất.",
                        data = Array.Empty<object>()
                    });
                }

                var items = result.Data.Table
                    .Where(x => x.Status == "PUBLISHED" || x.Status == "ONGOING" || x.Status == "ENDED")
                    .OrderByDescending(x => x.LatestAirDate ?? x.UpdatedAt ?? x.LastAirDate ?? x.FirstAirDate ?? x.CreatedAt)
                    .Take(8)
                    .ToList();

                return Json(new
                {
                    code = "200",
                    success = true,
                    message = "Lấy phim bộ mới nhất thành công.",
                    data = items
                });
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    code = "500",
                    success = false,
                    message = "Đã xảy ra lỗi khi tải phim bộ mới nhất.",
                    data = Array.Empty<object>()
                });
            }
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
