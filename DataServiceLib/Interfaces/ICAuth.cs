using CoreLib.Dtos.AuthDtos;
using CoreLib.Models;

namespace DataServiceLib.Interfaces
{
    public interface ICAuth
    {
        Task<CResponseMessage> LoginAsync(LoginDto loginDto);
        Task<CResponseMessage> Register(RegisterDto loginDto);
        Task<CResponseMessage> IssueOtpAsync(string email, string purpose, string otpHash);
        Task<CResponseMessage> VerifyOtpAsync(string email, string purpose, string otpHash);
    }
}
