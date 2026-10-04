using CoreLib.Dtos.AuthDtos;
using CoreLib.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using WebBrowser.Models.AuthModels; // chứa LoginResponseData (token, user)
using WebBrowser.Services.HttpSevice.Interfaces; // <-- dùng Interface
using WebBrowser.Services.Interfaces;

namespace WebBrowser.Services.Implements
{
    public class AuthService : IAuthService
    {
        private readonly IHttpService _httpService;           // <-- đổi sang interface
        private readonly IHttpContextAccessor _http;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IHttpService httpService, IHttpContextAccessor http, ILogger<AuthService> logger) // <-- inject interface
        {
            _httpService = httpService;
            _http = http;
            _logger = logger;
        }

        public async Task<CResponseMessage> LoginAsync(LoginDto loginDto, bool persistUserSession = true)
        {
            _logger.LogInformation("Calling authentication API");
            var resp = await _httpService.PostAsync<CResponseMessage>("/api/Auth/login", loginDto);
            _logger.LogInformation("Authentication API completed with code {ResponseCode}", resp?.code);
            // Thành công nếu Success == true hoặc code == "200"
            if (persistUserSession && resp is { Data: not null } && (resp.Success || resp.code == "200"))
            {
                // Lấy token + user
                var dataJson = JsonConvert.SerializeObject(resp.Data);
                var data = JsonConvert.DeserializeObject<LoginResponseData>(dataJson);

                var token = data?.token;
                if (!string.IsNullOrWhiteSpace(token))
                {
                    _http.HttpContext?.Session.SetString("JWToken", token);
                    if (data?.user != null)
                        _http.HttpContext?.Session.SetString("CurrentUser", JsonConvert.SerializeObject(data.user));
                }
            }

            return resp!;
        }

        public async Task<CResponseMessage> RegisterAsync(RegisterDto registerDto)
        {
            _logger.LogInformation("Calling registration API for {Email}", registerDto.Email);
            var response = await _httpService.PostAsync<CResponseMessage>("/api/Auth/register", registerDto);
            _logger.LogInformation("Registration API completed with code {ResponseCode}", response?.code);
            return response!;
        }

        public void Logout()
        {
            _http.HttpContext?.Session.Remove("JWToken");
            _http.HttpContext?.Session.Remove("CurrentUser");
        }
    }
}
