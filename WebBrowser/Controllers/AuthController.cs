using CoreLib.Dtos.AuthDtos;
using CoreLib.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebBrowser.Models.AuthModels;
using WebBrowser.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace WebBrowser.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IAuthService authService,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _authService = authService;
            _configuration = configuration;
            _logger = logger;
        }
        public IActionResult Index()
        {
            return View();
        }


        public IActionResult test()
        {
            // Trả về view đăng nhập
            return Ok("You hit test ");
        }
        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (loginDto == null || string.IsNullOrWhiteSpace(loginDto.Username) || string.IsNullOrWhiteSpace(loginDto.Password))
            {
                return new JsonResult(new CResponseMessage
                {
                    Success = false,
                    code = "400",
                    message = "Thiếu username/password."
                })
                { StatusCode = 400 };
            }

            var response = await _authService.LoginAsync(loginDto);

            bool ok = response != null && (response.Success || response.code == "200");

            if (ok && response!.Data != null)
            {
                // Lấy token & user từ response.Data
                var dataJson = JsonConvert.SerializeObject(response.Data);
                var data = JsonConvert.DeserializeObject<LoginResponseData>(dataJson);

                if (!string.IsNullOrWhiteSpace(data?.token))
                {
                    HttpContext.Session.SetString("JWToken", data.token);
                   
                    if (data.user != null)
                        HttpContext.Session.SetString("CurrentUser", JsonConvert.SerializeObject(data.user));

                    // Trả về đúng đối tượng phản hồi dưới dạng JSON
                    return new JsonResult(new
                    {
                        code = response.code ?? "200",
                        success = true,
                        message = response.message ?? "Đăng nhập thành công.",
                        data = new
                        {
                            token = data.token,
                            user = data.user
                        }
                    })
                    { StatusCode = 200 };
                }

                // Thành công nhưng không có token
                return new JsonResult(new CResponseMessage
                {
                    Success = false,
                    code = "502",
                    message = "Phản hồi đăng nhập không chứa token."
                })
                { StatusCode = 502 };
            }

            // Không thành công -> vẫn trả JSON với mã phù hợp
            var status = response?.code == "401" ? 401 : 400;
            return new JsonResult(response ?? new CResponseMessage
            {
                Success = false,
                code = "500",
                message = "Đăng nhập thất bại."
            })
            { StatusCode = status };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestRegisterOtp([FromBody] OtpRequestDto request)
        {
            request.Purpose = "REGISTER";
            var response = await _authService.RequestOtpAsync(request);
            return new JsonResult(response) { StatusCode = response.Success || response.code == "200" ? 200 : 400 };
        }

        [HttpGet]
        public IActionResult GoogleLogin(string? returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientId"]) ||
                string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientSecret"]))
            {
                _logger.LogWarning("Google sign-in is unavailable because WebBrowser Google credentials are not configured.");
                return RedirectToAction(nameof(AuthenticationError), new
                {
                    message = "Đăng nhập Google chưa được cấu hình. Vui lòng liên hệ quản trị viên."
                });
            }

            var safeReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action("Index", "Home")!;
            return Challenge(new AuthenticationProperties { RedirectUri = Url.Action(nameof(GoogleCallback), new { returnUrl = safeReturnUrl }) }, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleCallback(string? returnUrl = null, string? remoteError = null)
        {
            var safeReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action("Index", "Home")!;
            if (!string.IsNullOrWhiteSpace(remoteError))
            {
                _logger.LogWarning("Google returned an OAuth error during the sign-in callback.");
                await HttpContext.SignOutAsync("GoogleExternal");
                return RedirectToAction(nameof(AuthenticationError), new { message = "Bạn đã hủy hoặc Google từ chối yêu cầu đăng nhập." });
            }
            var result = await HttpContext.AuthenticateAsync("GoogleExternal");
            if (!result.Succeeded || result.Properties == null)
            {
                _logger.LogWarning("Google sign-in callback did not produce an authenticated external ticket.");
                await HttpContext.SignOutAsync("GoogleExternal");
                return RedirectToAction(nameof(AuthenticationError), new { message = "Không xác minh được phản hồi từ Google." });
            }
            var idToken = result.Properties.GetTokenValue("id_token");
            await HttpContext.SignOutAsync("GoogleExternal");
            if (string.IsNullOrWhiteSpace(idToken))
            {
                _logger.LogWarning("Google sign-in ticket did not include an ID token.");
                return RedirectToAction(nameof(AuthenticationError), new { message = "Google không trả về thông tin định danh hợp lệ." });
            }
            var response = await _authService.GoogleLoginAsync(new GoogleLoginDto { IdToken = idToken });
            if (!(response.Success || response.code == "200"))
            {
                _logger.LogWarning("Google login API returned code {ResponseCode}.", response.code);
                return RedirectToAction(nameof(AuthenticationError), new { message = response.message });
            }
            _logger.LogInformation("Google sign-in completed successfully.");
            return LocalRedirect(safeReturnUrl);
        }

        [HttpGet]
        public IActionResult AuthenticationError(string? message = null)
        {
            ViewData["Message"] = string.IsNullOrWhiteSpace(message) ? "Không thể đăng nhập bằng Google. Vui lòng thử lại." : message;
            return View();
        }

        [HttpGet("/auth/access-denied")]
        public IActionResult AccessDenied()
        {
            Response.StatusCode = StatusCodes.Status403Forbidden;
            ViewData["Message"] = "Tài khoản của bạn không có quyền truy cập chức năng này.";
            return View("AuthenticationError");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestLoginOtp([FromBody] OtpRequestDto request)
        {
            request.Purpose = "LOGIN";
            var response = await _authService.RequestOtpAsync(request);
            return new JsonResult(response) { StatusCode = response.Success || response.code == "200" ? 200 : 400 };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWithOtp([FromBody] OtpLoginDto request)
        {
            var response = await _authService.LoginWithOtpAsync(request);
            var ok = response != null && (response.Success || response.code == "200");
            if (!ok || response?.Data == null)
                return new JsonResult(response) { StatusCode = response?.code == "401" ? 401 : 400 };

            var data = JsonConvert.DeserializeObject<LoginResponseData>(JsonConvert.SerializeObject(response.Data));
            if (string.IsNullOrWhiteSpace(data?.token))
                return new JsonResult(new CResponseMessage { Success = false, code = "502", message = "Phản hồi đăng nhập OTP không hợp lệ." }) { StatusCode = 502 };

            HttpContext.Session.SetString("JWToken", data.token);
            if (data.user != null)
                HttpContext.Session.SetString("CurrentUser", JsonConvert.SerializeObject(data.user));
            return Json(new { success = true, code = "200", message = "Đăng nhập OTP thành công." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            if (registerDto == null || string.IsNullOrWhiteSpace(registerDto.FullName) ||
                string.IsNullOrWhiteSpace(registerDto.Email) || string.IsNullOrWhiteSpace(registerDto.Password))
            {
                return new JsonResult(new CResponseMessage
                {
                    Success = false,
                    code = "400",
                    message = "Vui lòng nhập đầy đủ họ tên, email và mật khẩu."
                }) { StatusCode = 400 };
            }

            if (!System.Net.Mail.MailAddress.TryCreate(registerDto.Email.Trim(), out _))
            {
                return new JsonResult(new CResponseMessage
                {
                    Success = false,
                    code = "400",
                    message = "Email không đúng định dạng."
                }) { StatusCode = 400 };
            }

            if (registerDto.Password.Length < 8)
            {
                return new JsonResult(new CResponseMessage
                {
                    Success = false,
                    code = "400",
                    message = "Mật khẩu phải có ít nhất 8 ký tự."
                }) { StatusCode = 400 };
            }

            var response = await _authService.RegisterAsync(registerDto);
            var ok = response != null && (response.Success || response.code == "200");
            return new JsonResult(response)
            {
                StatusCode = ok ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest
            };
        }


        public IActionResult Logout()
        {
            _authService.Logout();                // đã xóa JWToken + CurrentUser trong service
            return RedirectToAction("Index", "Home");
        }

    }
}
