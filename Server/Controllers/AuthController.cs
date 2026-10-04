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

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {

        private readonly ICBaseProvider _BaseProvider;
        private readonly IConfiguration _jwtSection;
        private readonly ICAuth _auth;

        public AuthController(ICBaseProvider baseProvider, IConfiguration configuration, ICAuth cAuth)
        {
            _BaseProvider = baseProvider;
            _jwtSection = configuration.GetSection("JwtSettings");
            _auth = cAuth;
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

            var response = await _auth.Register(registerDto);
            if (response.code != "200")
                return BadRequest(new { code = response.code, success = false, message = response.message });

            return Ok(new { code = "200", success = true, message = response.message });
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
