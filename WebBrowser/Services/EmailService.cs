using Aspose.Words.Drawing;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using WebBrowser.Models;

namespace WebBrowser.Services
{
    public class EmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IOptions<EmailSettings> options,    
            ILogger<EmailService> logger
           )
        {
            _settings = options.Value;
            _logger = logger;
           
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(to))
                    throw new ArgumentException("Email người nhận không được để trống");

                if (string.IsNullOrWhiteSpace(_settings.Email))
                    throw new ArgumentException("Email gửi trong cấu hình không được để trống");

                if (string.IsNullOrWhiteSpace(_settings.Password))
                    throw new ArgumentException("Mật khẩu email không được để trống");

                var message = new MimeMessage();

                message.From.Add(new MailboxAddress("QLBH", _settings.Email.Trim()));

                var emails = to
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim())
                    .Where(x => MailboxAddress.TryParse(x, out _));

                foreach (var email in emails)
                {
                    message.To.Add(MailboxAddress.Parse(email));
                }

                if (!message.To.Any())
                    throw new ArgumentException("Không có email người nhận hợp lệ");

                message.Subject = subject ?? "";

                message.Body = new TextPart("html")
                {
                    Text = body ?? ""
                };

                using var smtp = new SmtpClient();

                await smtp.ConnectAsync(
                    _settings.Host,
                    _settings.Port,
                    SecureSocketOptions.StartTls);

                await smtp.AuthenticateAsync(
                    _settings.Email.Trim(),
                    _settings.Password.Trim());

                await smtp.SendAsync(message);

                await smtp.DisconnectAsync(true);

                _logger.LogInformation("Gửi email thành công tới {Email}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi gửi email tới {Email}", to);
                throw;
            }
        }
    }
}
