using CoreLib.Dtos.AuthDtos;
using CoreLib.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebBrowser.Models.AuthModels;
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
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
