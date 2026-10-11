using Microsoft.Extensions.Options;
using Server.Models;
using System.Net;
using System.Net.Mail;

namespace Server.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly SmtpSettings _primarySettings;
        private readonly SmtpSettings _fallbackSettings;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(
            IOptions<SmtpSettings> settings,
            IConfiguration configuration,
            IHostEnvironment env,
            ILogger<SmtpEmailSender> logger)
        {
            _primarySettings = settings.Value;
            var fallback = configuration
                .GetSection("FallbackSmtpSettings")
                .Get<SmtpSettings>() ?? new SmtpSettings();

            // Nếu Fallback chưa có, thử lấy từ WebBrowser/appsettings.json (EmailSettings)
            if (string.IsNullOrWhiteSpace(fallback.Username) || string.IsNullOrWhiteSpace(fallback.Password))
            {
                try
                {
                    var webAppsettingsPath = Path.Combine(env.ContentRootPath, "..", "WebBrowser", "appsettings.json");
                    if (File.Exists(webAppsettingsPath))
                    {
                        var webConfig = new ConfigurationBuilder().AddJsonFile(webAppsettingsPath, optional: true).Build();
                        var webEmail = webConfig["EmailSettings:Email"];
                        var webPass = webConfig["EmailSettings:Password"];
                        if (!string.IsNullOrWhiteSpace(webEmail) && !string.IsNullOrWhiteSpace(webPass))
                        {
                            fallback.Host = webConfig["EmailSettings:Host"] ?? "smtp.gmail.com";
                            fallback.Port = int.TryParse(webConfig["EmailSettings:Port"], out var p) ? p : 587;
                            fallback.Username = webEmail;
                            fallback.Password = webPass;
                            fallback.EnableSsl = true;
                            fallback.FromName = "TFlix";
                        }
                    }
                }
                catch { }
            }

            _fallbackSettings = fallback;
            _logger = logger;
        }

        public async Task SendAsync(string recipient, string subject, string htmlBody)
        {
            Exception? primaryException = null;
            var primaryValid = IsValidSettings(_primarySettings);
            if (primaryValid)
            {
                try
                {
                    await SendUsingAsync(_primarySettings, recipient, subject, htmlBody);
                    _logger.LogInformation(
                        "OTP email sent to {MaskedEmail} using primary SMTP ({Username})",
                        MaskEmail(recipient),
                        _primarySettings.Username);
                    return;
                }
                catch (Exception ex)
                {
                    primaryException = ex;
                    _logger.LogWarning(
                        ex,
                        "Primary SMTP ({Username}) failed for {MaskedEmail}: {Message}; trying fallback SMTP",
                        _primarySettings.Username,
                        MaskEmail(recipient),
                        ex.Message);
                }
            }

            var fallbackValid = IsValidSettings(_fallbackSettings);
            if (fallbackValid)
            {
                try
                {
                    await SendUsingAsync(_fallbackSettings, recipient, subject, htmlBody);
                    _logger.LogInformation(
                        "OTP email sent to {MaskedEmail} using fallback SMTP ({Username})",
                        MaskEmail(recipient),
                        _fallbackSettings.Username);
                    return;
                }
                catch (Exception fallbackException)
                {
                    _logger.LogError(
                        fallbackException,
                        "Fallback SMTP ({Username}) also failed for {MaskedEmail}: {Message}",
                        _fallbackSettings.Username,
                        MaskEmail(recipient),
                        fallbackException.Message);

                    if (primaryException != null)
                    {
                        throw new AggregateException(
                            "Không thể gửi email bằng cả SMTP chính và SMTP dự phòng.",
                            primaryException,
                            fallbackException);
                    }
                    throw;
                }
            }

            if (primaryException != null)
            {
                throw primaryException;
            }

            throw new InvalidOperationException("Chưa cấu hình tài khoản SMTP hợp lệ.");
        }

        private static async Task SendUsingAsync(
            SmtpSettings settings,
            string recipient,
            string subject,
            string htmlBody)
        {
            ValidateSettings(settings);

            using var message = new MailMessage
            {
                From = new MailAddress(settings.Username, settings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(recipient);

            using var client = new SmtpClient(settings.Host, settings.Port)
            {
                EnableSsl = settings.EnableSsl,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(settings.Username, settings.Password),
                Timeout = 15000
            };

            await client.SendMailAsync(message);
        }

        private static bool IsValidSettings(SmtpSettings? settings)
        {
            return settings != null &&
                   !string.IsNullOrWhiteSpace(settings.Host) &&
                   !string.IsNullOrWhiteSpace(settings.Username) &&
                   !string.IsNullOrWhiteSpace(settings.Password);
        }

        private static void ValidateSettings(SmtpSettings settings)
        {
            if (!IsValidSettings(settings))
            {
                throw new InvalidOperationException("Cấu hình SMTP chưa đầy đủ.");
            }
        }

        private static string MaskEmail(string email)
        {
            var atIndex = email.IndexOf('@');
            if (atIndex <= 1)
                return "***";

            return $"{email[0]}***{email[(atIndex - 1)..]}";
        }
    }
}
