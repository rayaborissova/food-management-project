using System.Net;
using System.Net.Mail;

namespace FoodManagementProject.Services
{
    // lets the user reset workflow request an email through a common interface
    public interface IEmailSender
    {
        Task SendAsync(string to, string subject, string body);
    }

    // for local development with the test accounts
    // logs the email so the reset link can be tested before an SMTP server is configured
    public class LogEmailSender : IEmailSender
    {
        private readonly ILogger<LogEmailSender> _logger;

        public LogEmailSender(ILogger<LogEmailSender> logger)
        {
            _logger = logger;
        }

        public Task SendAsync(string to, string subject, string body)
        {
            // the email body can contain a password reset token
            _logger.LogWarning("EMAIL NOT SENT. To: {To} | Subject: {Subject}\n{Body}", to, subject, body);
            // logging finishes synchrounously; returns a completed task to match the interface
            return Task.CompletedTask;
        }
    }

    // sends email through an SMTP server using the application's email settings
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;

        public SmtpEmailSender(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            // default to port 587 and enable STARTTLS when the settings are ommited
            using var client = new SmtpClient(_config["Email:Host"], int.Parse(_config["Email:Port"] ?? "587"))
            {
                EnableSsl = bool.Parse(_config["Email:EnableSsl"] ?? "true"),
                Credentials = new NetworkCredential(_config["Email:Username"], _config["Email:Password"])
            };

            // use the configured sender address, or fall back to the SMTP username
            var from = _config["Email:From"] ?? _config["Email:Username"]!;
            using var message = new MailMessage(from, to, subject, body);

            await client.SendMailAsync(message);
        }
    }
}
