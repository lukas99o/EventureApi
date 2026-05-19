using MailKit.Net.Smtp;
using MimeKit;

namespace Vänskap_Api.Service
{
    public class EmailService : IService.IEmailService
    {
        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse("friendship@lukas99o.com"));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;

            var builder = new BodyBuilder();
            builder.HtmlBody = htmlBody;

            var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "logo.png");
            var image = builder.LinkedResources.Add(imagePath);
            image.ContentId = "LogoImage";
            image.ContentType.MediaType = "image";
            image.ContentType.MediaSubtype = "png";
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync("smtp-relay.brevo.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(Environment.GetEnvironmentVariable("BrevoEmail"), Environment.GetEnvironmentVariable("BrevoPassword"));
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}


