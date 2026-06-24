using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using RentalHub.Services.Interfaces;

namespace RentalHub.Services.Implementations;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendAsync(string toEmail, string toName, string subject, string body)
    {
        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(
            _config["Email:SenderName"],
            _config["Email:SenderEmail"]
        ));

        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;

        message.Body = new TextPart("html") { Text = body };

        using var client = new SmtpClient();

        await client.ConnectAsync(
            _config["Email:Host"],
            int.Parse(_config["Email:Port"]!),
            SecureSocketOptions.StartTls
        );

        await client.AuthenticateAsync(
            _config["Email:Username"],
            _config["Email:Password"]
        );

        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
