using CoreLib.Dtos.AuthDtos;
using CoreLib.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebBrowser.Models.AuthModels;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Index(string? returnUrl)
        {
            // Nếu đã đăng nhập AdminJWToken rồi thì redirect vào /Admin
            var adminToken = HttpContext.Session.GetString("AdminJWToken");
            if (!string.IsNullOrEmpty(adminToken))
            {
                return Redirect(GetSafeReturnUrl(returnUrl));
            }

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RequestOtp([FromBody] OtpRequestDto request)
        {
            request.Purpose = "ADMIN_LOGIN";
            var response = await _authService.RequestOtpAsync(request);
            return new JsonResult(response) { StatusCode = response.Success || response.code == "200" ? 200 : 400 };
        }

        [HttpPost]
        public async Task<IActionResult> LoginWithOtp([FromBody] OtpLoginDto request, [FromQuery] string? returnUrl)
        {
            var response = await _authService.LoginWithOtpAsync(request, adminLogin: true);
            var ok = response != null && (response.Success || response.code == "200");
            if (!ok || response?.Data == null)
                return new JsonResult(response) { StatusCode = response?.code == "401" ? 401 : 400 };

            var data = JsonConvert.DeserializeObject<LoginResponseData>(JsonConvert.SerializeObject(response.Data));
            var isAdmin = data?.user?.roles?.Any(role =>
                string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role, "SUPER_ADMIN", StringComparison.OrdinalIgnoreCase)) == true;
            if (!isAdmin || string.IsNullOrWhiteSpace(data?.token))
                return new JsonResult(new CResponseMessage { Success = false, code = "403", message = "Tài khoản không có quyền Quản trị." }) { StatusCode = 403 };

            HttpContext.Session.SetString("AdminJWToken", data.token);
            HttpContext.Session.SetString("AdminCurrentUser", JsonConvert.SerializeObject(data.user));
            return Json(new { success = true, code = "200", message = "Đăng nhập OTP thành công.", redirectUrl = GetSafeReturnUrl(returnUrl) });
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto, [FromQuery] string? returnUrl)
        {
            if (loginDto == null || string.IsNullOrWhiteSpace(loginDto.Username) || string.IsNullOrWhiteSpace(loginDto.Password))
            {
                return new JsonResult(new CResponseMessage
                {
                    Success = false,
                    code = "400",
                    message = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu Quản trị."
                })
                { StatusCode = 400 };
            }

            var response = await _authService.LoginAsync(loginDto, persistUserSession: false);
            bool ok = response != null && (response.Success || response.code == "200");

            if (ok && response!.Data != null)
            {
                var dataJson = JsonConvert.SerializeObject(response.Data);
                var data = JsonConvert.DeserializeObject<LoginResponseData>(dataJson);

                var isAdmin = data?.user?.roles?.Any(role =>
                string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role, "SUPER_ADMIN", StringComparison.OrdinalIgnoreCase)) == true;

                if (!isAdmin)
                {
                    HttpContext.Session.Remove("AdminJWToken");
                    HttpContext.Session.Remove("AdminCurrentUser");
                    return new JsonResult(new CResponseMessage
                    {
                        Success = false,
                        code = "403",
                        message = "Tài khoản này không có quyền truy cập trang Quản trị."
                    })
                    { StatusCode = 403 };
                }

                if (!string.IsNullOrWhiteSpace(data?.token))
                {
                    // LƯU TOKEN VÀ USER VÀO SỐ PHIÊN ADMIN RIÊNG BIỆT (AdminJWToken)
                    HttpContext.Session.SetString("AdminJWToken", data.token);
                    if (data.user != null)
                    {
                        HttpContext.Session.SetString("AdminCurrentUser", JsonConvert.SerializeObject(data.user));
                    }

                    string redirect = GetSafeReturnUrl(returnUrl);
                    return new JsonResult(new
                    {
                        success = true,
                        code = "200",
                        message = "Đăng nhập trang Quản trị thành công!",
                        redirectUrl = redirect
                    })
                    { StatusCode = 200 };
                }

                return new JsonResult(new CResponseMessage
                {
                    Success = false,
                    code = "502",
                    message = "Phản hồi đăng nhập Quản trị không hợp lệ."
                })
                { StatusCode = 502 };
            }

            var status = response?.code switch
            {
                "401" => StatusCodes.Status401Unauthorized,
                "403" => StatusCodes.Status403Forbidden,
                "500" or "502" or "503" => StatusCodes.Status503ServiceUnavailable,
                _ => StatusCodes.Status400BadRequest
            };
            return new JsonResult(response ?? new CResponseMessage
            {
                Success = false,
                code = "500",
                message = "Đăng nhập trang Quản trị thất bại. Kiểm tra lại thông tin tài khoản."
            })
            { StatusCode = status };
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("AdminJWToken");
            HttpContext.Session.Remove("AdminCurrentUser");
            return RedirectToAction("Index", "Auth", new { area = "Admin" });
        }

        private string GetSafeReturnUrl(string? returnUrl)
        {
            return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? returnUrl
                : "/Admin";
        }
    }
}
