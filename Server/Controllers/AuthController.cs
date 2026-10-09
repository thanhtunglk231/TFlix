using CoreLib.Dtos.AuthDtos;
using DataServiceLib.Implements;
using DataServiceLib.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Server.Services;
using Google.Apis.Auth;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {

        private readonly ICBaseProvider _BaseProvider;
        private readonly IConfiguration _jwtSection;
        private readonly ICAuth _auth;
        private readonly IAuthOtpService _otpService;
        private readonly IConfiguration _configuration;

        public AuthController(ICBaseProvider baseProvider, IConfiguration configuration, ICAuth cAuth, IAuthOtpService otpService)
        {
            _BaseProvider = baseProvider;
            _jwtSection = configuration.GetSection("JwtSettings");
            _configuration = configuration;
            _auth = cAuth;
            _otpService = otpService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (loginDto == null || string.IsNullOrWhiteSpace(loginDto.Username) || string.IsNullOrWhiteSpace(loginDto.Password))
                return Ok(new { code = "400", message = "Invalid login request.", data = (object)null });

            var response = await _auth.LoginAsync(loginDto);

            // Nếu lỗi hệ thống thì cứ trả code trong body cho thống nhất
            if (response.code != "200")
                return Ok(new { code = response.code, success = false, message = response.message, data = (object)null });

            // Lấy user từ DataSet (bảng ở o_user ref cursor)
            var user = MapUserFromDataSet(response.Data as DataSet);
            var email = user?.Email ?? loginDto.Username;
            var token = GenerateJwtToken(email, user?.Roles ?? new List<string>());

            return Ok(new
            {
                code = response.code,
                success = true,
                message = response.message,
                data = new
                {
                    token,
                    user
                }
            });

        }

        private static UserDto? MapUserFromDataSet(DataSet ds)
        {
            if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0) return null;

            var r = ds.Tables[0].Rows[0];
            return new UserDto
            {
                UserId = Convert.ToInt64(r["USER_ID"]),
                Email = r["EMAIL"]?.ToString(),
                FullName = r["FULL_NAME"]?.ToString(),
                AvatarUrl = r.Table.Columns.Contains("AVATAR_URL") ? r["AVATAR_URL"]?.ToString() : null,
                Phone = r.Table.Columns.Contains("PHONE") ? r["PHONE"]?.ToString() : null,
                CountryCode = r.Table.Columns.Contains("COUNTRY_CODE") ? r["COUNTRY_CODE"]?.ToString() : null,
                LanguageCode = r.Table.Columns.Contains("LANGUAGE_CODE") ? r["LANGUAGE_CODE"]?.ToString() : null,
                IsEmailVerified = r.Table.Columns.Contains("IS_EMAIL_VERIFIED") && r["IS_EMAIL_VERIFIED"]?.ToString() == "Y",
                Status = r["STATUS"]?.ToString(),
                Roles = r.Table.Columns.Contains("ROLES")
                    ? (r["ROLES"]?.ToString() ?? string.Empty)
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .ToList()
                    : new List<string>()
            };
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            if (registerDto == null || string.IsNullOrWhiteSpace(registerDto.Email) ||
                string.IsNullOrWhiteSpace(registerDto.FullName) || string.IsNullOrWhiteSpace(registerDto.Password))
            {
                return BadRequest(new { code = "400", success = false, message = "Vui lòng nhập đầy đủ họ tên, email và mật khẩu." });
            }

            if (!System.Net.Mail.MailAddress.TryCreate(registerDto.Email.Trim(), out _))
                return BadRequest(new { code = "400", success = false, message = "Email không đúng định dạng." });

            if (registerDto.Password.Length < 8)
                return BadRequest(new { code = "400", success = false, message = "Mật khẩu phải có ít nhất 8 ký tự." });

            if (string.IsNullOrWhiteSpace(registerDto.Otp))
                return BadRequest(new { code = "400", success = false, message = "Vui lòng nhập mã OTP đăng ký." });

            var otpResponse = await _otpService.VerifyAsync(registerDto.Email, AuthOtpService.RegisterPurpose, registerDto.Otp);
            if (!otpResponse.Success)
                return BadRequest(new { code = otpResponse.code, success = false, message = otpResponse.message });

            var response = await _auth.Register(registerDto);
            if (response.code != "200")
                return BadRequest(new { code = response.code, success = false, message = response.message });

            return Ok(new { code = "200", success = true, message = response.message });
        }

        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto request)
        {
            if (string.IsNullOrWhiteSpace(request?.IdToken))
                return BadRequest(new { code="400",success=false,message="Google ID token không hợp lệ." });
            try
            {
                var audience = _configuration["Authentication:Google:ClientId"];
                if (string.IsNullOrWhiteSpace(audience))
                    return StatusCode(503, new { code="503",success=false,message="Google Login chưa được cấu hình." });
                var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken,
                    new GoogleJsonWebSignature.ValidationSettings { Audience = new[] { audience } });
                if (payload.EmailVerified != true || string.IsNullOrWhiteSpace(payload.Subject) || string.IsNullOrWhiteSpace(payload.Email))
                    return Unauthorized(new { code="401",success=false,message="Tài khoản Google chưa xác minh email." });
                var response = await _auth.GoogleLoginAsync(new GoogleIdentityDto
                {
                    Subject=payload.Subject, Email=payload.Email, EmailVerified=true,
                    FullName=payload.Name ?? payload.Email, AvatarUrl=payload.Picture
                });
                if (!response.Success) return StatusCode(response.code == "409" ? 409 : response.code == "403" ? 403 : 400, response);
                var user = MapUserFromDataSet(response.Data as DataSet);
                if (user == null) return StatusCode(500, new { code="500",success=false,message="Không đọc được tài khoản Google." });
                return Ok(new { code="200",success=true,message=response.message,data=new { token=GenerateJwtToken(user.Email,user.Roles),user } });
            }
            catch (InvalidJwtException)
            {
                return Unauthorized(new { code="401",success=false,message="Phiên xác thực Google không hợp lệ hoặc đã hết hạn." });
            }
        }

        [HttpPost("otp/register/request")]
        public async Task<IActionResult> RequestRegisterOtp([FromBody] OtpRequestDto request)
        {
            if (request == null || !System.Net.Mail.MailAddress.TryCreate(request.Email?.Trim(), out _))
                return BadRequest(new { code = "400", success = false, message = "Email không đúng định dạng." });
            var response = await _otpService.RequestAsync(request.Email, AuthOtpService.RegisterPurpose);
            return StatusCode(response.Success ? 200 : 400, response);
        }

        [HttpPost("otp/admin/request")]
        public async Task<IActionResult> RequestAdminOtp([FromBody] OtpRequestDto request)
        {
            if (request == null || !System.Net.Mail.MailAddress.TryCreate(request.Email?.Trim(), out _))
                return BadRequest(new { code = "400", success = false, message = "Email không đúng định dạng." });
            var response = await _otpService.RequestAsync(request.Email, AuthOtpService.AdminLoginPurpose);
            return StatusCode(response.Success ? 200 : response.code == "403" ? 403 : 400, response);
        }

        [HttpPost("otp/login/request")]
        public async Task<IActionResult> RequestLoginOtp([FromBody] OtpRequestDto request)
        {
            if (request == null || !System.Net.Mail.MailAddress.TryCreate(request.Email?.Trim(), out _))
                return BadRequest(new { code = "400", success = false, message = "Email không đúng định dạng." });
            var response = await _otpService.RequestAsync(request.Email, AuthOtpService.LoginPurpose);
            return StatusCode(response.Success ? 200 : 400, response);
        }

        [HttpPost("otp/login")]
        public async Task<IActionResult> OtpLogin([FromBody] OtpLoginDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Otp))
                return BadRequest(new { code = "400", success = false, message = "Vui lòng nhập email và mã OTP." });

            var response = await _otpService.VerifyAsync(request.Email, AuthOtpService.LoginPurpose, request.Otp);
            if (!response.Success)
                return StatusCode(response.code == "401" ? 401 : 400, response);

            var user = MapUserFromDataSet(response.Data as DataSet);
            if (user == null)
                return StatusCode(500, new { code = "500", success = false, message = "Không đọc được thông tin tài khoản." });

            return Ok(new
            {
                code = "200",
                success = true,
                message = "Đăng nhập OTP thành công.",
                data = new { token = GenerateJwtToken(user.Email ?? request.Email, user.Roles), user }
            });
        }

        [HttpPost("otp/admin-login")]
        public async Task<IActionResult> AdminOtpLogin([FromBody] OtpLoginDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Otp))
                return BadRequest(new { code = "400", success = false, message = "Vui lòng nhập email và mã OTP." });
            var response = await _otpService.VerifyAsync(request.Email, AuthOtpService.AdminLoginPurpose, request.Otp);
            if (!response.Success) return StatusCode(response.code == "401" ? 401 : 400, response);
            var user = MapUserFromDataSet(response.Data as DataSet);
            if (user == null || !user.Roles.Any(x => x.Equals("ADMIN", StringComparison.OrdinalIgnoreCase) || x.Equals("SUPER_ADMIN", StringComparison.OrdinalIgnoreCase)))
                return StatusCode(403, new { code = "403", success = false, message = "Tài khoản không có quyền Quản trị." });
            return Ok(new { code = "200", success = true, message = "Đăng nhập OTP thành công.", data = new { token = GenerateJwtToken(user.Email ?? request.Email, user.Roles), user } });
        }
        private string GenerateJwtToken(string email, IEnumerable<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Email, email ?? "")
            };

            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSection["SecretKey"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSection["Issuer"],
                audience: _jwtSection["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
