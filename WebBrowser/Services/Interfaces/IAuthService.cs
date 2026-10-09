using CoreLib.Dtos.AuthDtos;
using CoreLib.Models;

namespace WebBrowser.Services.Interfaces
{
    public interface IAuthService
    {
        Task<CResponseMessage> LoginAsync(LoginDto loginDto, bool persistUserSession = true);
        Task<CResponseMessage> RegisterAsync(RegisterDto registerDto);
        Task<CResponseMessage> RequestOtpAsync(OtpRequestDto request);
        Task<CResponseMessage> LoginWithOtpAsync(OtpLoginDto request, bool adminLogin = false);
        Task<CResponseMessage> GoogleLoginAsync(GoogleLoginDto request);
        void Logout();
    }
}
