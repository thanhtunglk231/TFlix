using CoreLib.Models;

namespace Server.Services
{
    public interface IAuthOtpService
    {
        Task<CResponseMessage> RequestAsync(string email, string purpose);
        string Hash(string email, string purpose, string otp);
    }
}
