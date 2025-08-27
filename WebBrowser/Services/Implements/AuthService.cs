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

        public AuthService(IHttpService httpService, IHttpContextAccessor http) // <-- inject interface
        {
            _httpService = httpService;
            _http = http;
        }

        public async Task<CResponseMessage> LoginAsync(LoginDto loginDto)
        {
            var resp = await _httpService.PostAsync<CResponseMessage>("/api/Auth/login", loginDto);
            Console.WriteLine("LoginAsync");
            // Thành công nếu Success == true hoặc code == "200"
            if (resp is { Data: not null } && (resp.Success || resp.code == "200"))
            {
                // Lấy token + user
                var dataJson = JsonConvert.SerializeObject(resp.Data);
                var data = JsonConvert.DeserializeObject<LoginResponseData>(dataJson);

                var token = data?.token;
                Console.WriteLine("LoginAsync Token: "+token);
                if (!string.IsNullOrWhiteSpace(token))
                {
                    _http.HttpContext?.Session.SetString("JWToken", token);
                    if (data?.user != null)
                        _http.HttpContext?.Session.SetString("CurrentUser", JsonConvert.SerializeObject(data.user));
                }
            }

            return resp!;
        }

        public void Logout()
        {
            _http.HttpContext?.Session.Remove("JWToken");
            _http.HttpContext?.Session.Remove("CurrentUser");
        }
    }
}
