using CoreLib.Models;
using DataServiceLib.Interfaces;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Server.Services
{
    public class AuthOtpService : IAuthOtpService
    {
        public const string RegisterPurpose = "REGISTER";
        public const string LoginPurpose = "LOGIN";
        public const string AdminLoginPurpose = "ADMIN_LOGIN";

        private readonly ICAuth _auth;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthOtpService> _logger;

        public AuthOtpService(
            ICAuth auth,
            IEmailSender emailSender,
            IConfiguration configuration,
            ILogger<AuthOtpService> logger)
        {
            _auth = auth;
            _emailSender = emailSender;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<CResponseMessage> RequestAsync(string email, string purpose)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            var response = await _auth.IssueOtpAsync(normalizedEmail, purpose, Hash(normalizedEmail, purpose, otp));

            if (!response.Success)
                return response;

            try
            {
                var encodedOtp = WebUtility.HtmlEncode(otp);
                var action = purpose == RegisterPurpose ? "đăng ký tài khoản" : "đăng nhập";
                var body = $"""
                    <div style="font-family:Arial,sans-serif;max-width:520px;margin:auto;padding:24px;border:1px solid #e5e7eb;border-radius:12px">
                        <h2 style="color:#19b3a5">TFlix</h2>
                        <p>Mã OTP để {action} của bạn là:</p>
                        <div style="font-size:32px;font-weight:700;letter-spacing:8px;margin:20px 0;color:#111827">{encodedOtp}</div>
                        <p>Mã có hiệu lực trong 5 phút. Không chia sẻ mã này với bất kỳ ai.</p>
                    </div>
                    """;

                await _emailSender.SendAsync(normalizedEmail, $"Mã OTP TFlix - {action}", body);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to send OTP email to {Email}", normalizedEmail);
                return new CResponseMessage
                {
                    Success = false,
                    code = "502",
                    message = "Không thể gửi email OTP. Vui lòng kiểm tra cấu hình SMTP và thử lại."
                };
            }
        }

        public string Hash(string email, string purpose, string otp)
        {
            var secret = _configuration["JwtSettings:SecretKey"]
                ?? throw new InvalidOperationException("JwtSettings:SecretKey chưa được cấu hình.");
            var payload = $"{email.Trim().ToLowerInvariant()}|{purpose}|{otp.Trim()}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        }

        public Task<CResponseMessage> VerifyAsync(string email, string purpose, string otp)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            return _auth.VerifyOtpAsync(normalizedEmail, purpose, Hash(normalizedEmail, purpose, otp));
        }
    }
}
