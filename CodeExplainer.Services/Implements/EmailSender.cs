using System.Net.Mail;
using CodeExplainer.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace CodeExplainer.Services.Implements;

public class EmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    public EmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        try
        {
            string fromMail = Environment.GetEnvironmentVariable("EMAIL")!;
            if (string.IsNullOrWhiteSpace(fromMail))
            {
                fromMail = _configuration["EmailSettings:Email"]!;
                if (string.IsNullOrWhiteSpace(fromMail))
                {
                    throw new Exception("Email sender not configured");
                }
            }

            string fromPassword = Environment.GetEnvironmentVariable("EMAIL_PASSWORD")!;
            if (string.IsNullOrWhiteSpace(fromPassword))
            {
                fromPassword = _configuration["EmailSettings:Password"]!;
                if (string.IsNullOrWhiteSpace(fromPassword))
                {
                    throw new Exception("Email password not configured");
                }
            }

            string host = Environment.GetEnvironmentVariable("EMAIL_HOST")!;
            if (string.IsNullOrWhiteSpace(host))
            {
                host = _configuration["EmailSettings:Host"]!;
                if (string.IsNullOrWhiteSpace(host))
                {
                    throw new Exception("Email SMTP host not configured");
                }
            }

            string port = Environment.GetEnvironmentVariable("EMAIL_PORT")!;
            if (string.IsNullOrWhiteSpace(port))
            {
                port = _configuration["EmailSettings:Port"]!;
                if (string.IsNullOrWhiteSpace(port))
                {
                    throw new Exception("Email SMTP port not configured");
                }
            }
            
            using var smtpClient = new SmtpClient();
            smtpClient.Host = host; // SMTP server
            smtpClient.Port = int.Parse(port); // TLS port
            smtpClient.EnableSsl = true; // Enable TLS (SSL)
            smtpClient.Credentials = new System.Net.NetworkCredential(fromMail, fromPassword);

            // Setting From , To and CC
            using var mail = new MailMessage();
            mail.From = new MailAddress(fromMail);
            mail.Subject = subject;
            mail.Body = GenerateEmailTemplate(email, subject, htmlMessage);
            mail.IsBodyHtml = true;

            mail.To.Add(new MailAddress(email));

            await smtpClient.SendMailAsync(mail);
        }
        catch (Exception ex)
        {
            // TODO: inject ILogger<EmailSender> and log instead
            Console.WriteLine($"[EmailSender] Failed: {ex.Message}");
            throw;
        }
    }

    private string GenerateEmailTemplate(string email, string subject, string htmlMsg)
    {
        return "Add your email template here" +
               $"<h3>{subject}</h3>" +
               $"<p>To: {email}</p>" +
               $"<div>{htmlMsg}</div>";
    }
}