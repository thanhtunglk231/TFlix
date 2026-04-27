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
                return Ok(new { code = response.code, message = response.message, data = (object)null });

            // Lấy user từ DataSet (bảng ở o_user ref cursor)
            var user = MapUserFromDataSet(response.Data as DataSet);
            var email = user?.Email ?? loginDto.Username;
            var token = GenerateJwtToken(email);

            return Ok(new
            {
                code = response.code,
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
                Status = r["STATUS"]?.ToString()
            };
        }


        [HttpPost("register")]
        public async Task<IActionResult> register([FromBody] RegisterDto loginDto)
        {
            if (loginDto == null || string.IsNullOrEmpty(loginDto.Email) || string.IsNullOrEmpty(loginDto.Password))
                return BadRequest("Invalid login request.");

            var response = await _auth.Register(loginDto);
            if (response.code != "200")
                return Unauthorized(response.message);

           

            return Ok(response);
        }
        private string GenerateJwtToken( string email)
        {
            

            var claims = new[]
            {
               
                new Claim(ClaimTypes.Email, email ?? "")
            };

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
