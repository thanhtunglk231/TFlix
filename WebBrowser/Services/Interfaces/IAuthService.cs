using CoreLib.Dtos.AuthDtos;
using CoreLib.Models;

namespace WebBrowser.Services.Interfaces
{
    public interface IAuthService
    {
        Task<CResponseMessage> LoginAsync(LoginDto loginDto);
        void Logout();
    }
}