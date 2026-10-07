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
            ILogger<SmtpEmailSender> logger)
        {
            _primarySettings = settings.Value;
            _fallbackSettings = configuration
                .GetSection("FallbackSmtpSettings")
                .Get<SmtpSettings>() ?? new SmtpSettings();
            _logger = logger;
        }

        public async Task SendAsync(string recipient, string subject, string htmlBody)
        {
            Exception? primaryException = null;
            try
            {
                await SendUsingAsync(_primarySettings, recipient, subject, htmlBody);
                _logger.LogInformation(
                    "OTP email sent to {MaskedEmail} using primary SMTP",
                    MaskEmail(recipient));
                return;
            }
            catch (Exception ex)
            {
                primaryException = ex;
                _logger.LogWarning(
                    ex,
                    "Primary SMTP failed for {MaskedEmail}; trying fallback SMTP",
                    MaskEmail(recipient));
            }

            try
            {
                await SendUsingAsync(_fallbackSettings, recipient, subject, htmlBody);
                _logger.LogInformation(
                    "OTP email sent to {MaskedEmail} using fallback SMTP",
                    MaskEmail(recipient));
            }
            catch (Exception fallbackException)
            {
                _logger.LogError(
                    fallbackException,
                    "Fallback SMTP also failed for {MaskedEmail}",
                    MaskEmail(recipient));
                throw new AggregateException(
                    "Không thể gửi email bằng SMTP chính và SMTP dự phòng.",
                    primaryException!,
                    fallbackException);
            }
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

        private static void ValidateSettings(SmtpSettings settings)
        {
            if (string.IsNullOrWhiteSpace(settings.Host) ||
                string.IsNullOrWhiteSpace(settings.Username) ||
                string.IsNullOrWhiteSpace(settings.Password))
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
