using CoreLib.Dtos.AuthDtos;
using CoreLib.Models;

namespace WebBrowser.Services.Interfaces
{
    public interface IAuthService
    {
        Task<CResponseMessage> LoginAsync(LoginDto loginDto, bool persistUserSession = true);
        Task<CResponseMessage> RegisterAsync(RegisterDto registerDto);
        void Logout();
    }
}
