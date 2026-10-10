using CoreLib.Dtos.AuthDtos;
using CoreLib.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebBrowser.Models.AuthModels;
using WebBrowser.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies;
using CommonLib.Logging;
using System.Text;

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
            var clientId = _configuration["Authentication:Google:ClientId"];
            var clientSecret = _configuration["Authentication:Google:ClientSecret"];
            var hostUrl = $"{Request.Scheme}://{Request.Host}";
            var safeReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action("Index", "Home")!;

            var loginCheckLog = new StringBuilder();
            loginCheckLog.AppendLine($"Yêu cầu đăng nhập Google từ host: {hostUrl}");
            loginCheckLog.AppendLine($"ReturnUrl: {returnUrl} -> SafeReturnUrl: {safeReturnUrl}");
            loginCheckLog.AppendLine($"- ClientId: {(string.IsNullOrWhiteSpace(clientId) ? "❌ [TRỐNG]" : "✅ " + GoogleAuthDebugLogger.Mask(clientId))}");
            loginCheckLog.AppendLine($"- ClientSecret: {(string.IsNullOrWhiteSpace(clientSecret) ? "❌ [TRỐNG]" : "✅ " + GoogleAuthDebugLogger.MaskSecret(clientSecret))}");

            if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
            {
                var missingList = new List<string>();
                if (string.IsNullOrWhiteSpace(clientId)) missingList.Add("ClientId");
                if (string.IsNullOrWhiteSpace(clientSecret)) missingList.Add("ClientSecret");
                var missingDesc = string.Join(" và ", missingList);

                loginCheckLog.AppendLine($"❌ [TỪ CHỐI REQUEST]: Đang thiếu cấu hình ({missingDesc})!");
                loginCheckLog.AppendLine("-> HƯỚNG DẪN KHẮC PHỤC TRÊN SERVER:");
                loginCheckLog.AppendLine("   Mở file WebBrowser/appsettings.json trên server (hoặc appsettings.Production.json) và thêm:");
                loginCheckLog.AppendLine("   \"Authentication\": { \"Google\": { \"ClientId\": \"...\", \"ClientSecret\": \"...\", \"CallbackPath\": \"/signin-google\" } }");
                loginCheckLog.AppendLine("   Hoặc đặt biến môi trường: GOOGLE_CLIENT_ID và GOOGLE_CLIENT_SECRET.");
                GoogleAuthDebugLogger.Log("GOOGLE_LOGIN_CHECK_FAILED", loginCheckLog.ToString());

                _logger.LogWarning("Google sign-in is unavailable because WebBrowser Google credentials are not configured. Missing: {Missing}", missingDesc);
                return RedirectToAction(nameof(AuthenticationError), new
                {
                    message = $"Đăng nhập Google chưa được cấu hình (Server đang thiếu {missingDesc} trong file cấu hình WebBrowser). Chi tiết lỗi đã được ghi vào Logs/google_auth_debug.txt."
                });
            }

            loginCheckLog.AppendLine($"✅ Cấu hình hợp lệ. Chuyển hướng sang Google OAuth challenge với Callback: {Url.Action(nameof(GoogleCallback), new { returnUrl = safeReturnUrl })}");
            GoogleAuthDebugLogger.Log("GOOGLE_LOGIN_CHALLENGE", loginCheckLog.ToString());

            return Challenge(new AuthenticationProperties { RedirectUri = Url.Action(nameof(GoogleCallback), new { returnUrl = safeReturnUrl }) }, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleCallback(string? returnUrl = null, string? remoteError = null)
        {
            var safeReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl! : Url.Action("Index", "Home")!;
            var cbLog = new StringBuilder();
            cbLog.AppendLine($"Nhận Callback từ Google: {Request.Scheme}://{Request.Host}{Request.Path}{Request.QueryString}");

            if (!string.IsNullOrWhiteSpace(remoteError))
            {
                cbLog.AppendLine($"❌ Google trả về remoteError: {remoteError}");
                GoogleAuthDebugLogger.Log("GOOGLE_CALLBACK_REMOTE_ERROR", cbLog.ToString());
                _logger.LogWarning("Google returned an OAuth error during the sign-in callback: {RemoteError}", remoteError);
                await HttpContext.SignOutAsync("GoogleExternal");
                return RedirectToAction(nameof(AuthenticationError), new { message = $"Bạn đã hủy hoặc Google từ chối yêu cầu đăng nhập ({remoteError}). Chi tiết trong Logs/google_auth_debug.txt." });
            }

            var result = await HttpContext.AuthenticateAsync("GoogleExternal");
            if (!result.Succeeded || result.Properties == null)
            {
                cbLog.AppendLine("❌ AuthenticateAsync('GoogleExternal') thất bại!");
                cbLog.AppendLine($"Failure message: {result.Failure?.Message}");
                GoogleAuthDebugLogger.Log("GOOGLE_CALLBACK_AUTH_FAILED", cbLog.ToString(), result.Failure);
                _logger.LogWarning("Google sign-in callback did not produce an authenticated external ticket: {Message}", result.Failure?.Message);
                await HttpContext.SignOutAsync("GoogleExternal");
                return RedirectToAction(nameof(AuthenticationError), new { message = $"Không xác minh được phản hồi từ Google: {result.Failure?.Message}. Chi tiết trong Logs/google_auth_debug.txt." });
            }

            var idToken = result.Properties.GetTokenValue("id_token")
                ?? result.Principal?.FindFirst("id_token")?.Value;
            await HttpContext.SignOutAsync("GoogleExternal");

            if (string.IsNullOrWhiteSpace(idToken))
            {
                var claimsList = string.Join(", ", result.Principal?.Claims.Select(c => $"{c.Type}={c.Value}") ?? Enumerable.Empty<string>());
                cbLog.AppendLine("❌ Google không trả về id_token trong authentication ticket!");
                cbLog.AppendLine($"Available claims: {claimsList}");
                GoogleAuthDebugLogger.Log("GOOGLE_CALLBACK_NO_ID_TOKEN", cbLog.ToString());
                _logger.LogWarning("Google sign-in ticket did not include an ID token. Available claims: {Claims}", claimsList);
                return RedirectToAction(nameof(AuthenticationError), new { message = "Google không trả về thông tin định danh (id_token) hợp lệ. Chi tiết trong Logs/google_auth_debug.txt." });
            }

            cbLog.AppendLine($"✅ Nhận id_token thành công (Độ dài: {idToken.Length}). Đang gửi sang Server API /api/Auth/google...");
            var response = await _authService.GoogleLoginAsync(new GoogleLoginDto { IdToken = idToken });
            cbLog.AppendLine($"Server API phản hồi: Code = {response?.code}, Success = {response?.Success}, Message = {response?.message}");

            if (response == null || !(response.Success || response.code == "200"))
            {
                cbLog.AppendLine($"❌ Server API từ chối đăng nhập: {response?.message}");
                GoogleAuthDebugLogger.Log("GOOGLE_CALLBACK_API_REJECTED", cbLog.ToString());
                _logger.LogWarning("Google login API returned code {ResponseCode}: {Message}", response?.code, response?.message);
                return RedirectToAction(nameof(AuthenticationError), new { message = $"Đăng nhập Google thất bại từ phía Server API: {response?.message}. Chi tiết trong Logs/google_auth_debug.txt." });
            }

            cbLog.AppendLine("🎉 Đăng nhập Google thành công hoàn tất!");
            GoogleAuthDebugLogger.Log("GOOGLE_CALLBACK_SUCCESS", cbLog.ToString());
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
