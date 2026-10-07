using CoreLib.Models;

namespace Server.Services
{
    public interface IAuthOtpService
    {
        Task<CResponseMessage> RequestAsync(string email, string purpose);
        Task<CResponseMessage> VerifyAsync(string email, string purpose, string otp);
        string Hash(string email, string purpose, string otp);
    }
}
