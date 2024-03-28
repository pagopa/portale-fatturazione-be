using MailKit.Security;
using MimeKit.Text;
using MimeKit;
using MailKit.Net.Smtp;

namespace PortaleFatture.BE.Infrastructure.Gateway.Email;

public class EmailSender(string smtpSource, int smtpPort, string smtpUser, string smtpPassword, string from) : IEmailSender
{
    private readonly string _smtpSource = smtpSource;
    private readonly int _smtpPort = smtpPort;
    private readonly string _smtpUser = smtpUser;
    private readonly string _smtpPassword = smtpPassword;
    private readonly string _from = from;

    public (string, bool) SendEmail(
        string to,
        string subject,
        string message)
    {
        try
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_from));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = message };

            using var smtp = new SmtpClient();
            smtp.Timeout = 10000;
            smtp.Connect(_smtpSource, _smtpPort, SecureSocketOptions.SslOnConnect);
            smtp.Authenticate(_smtpUser, _smtpPassword);
            var result = smtp.Send(email);
            smtp.Disconnect(true);
            return (result, true);
        }
        catch (Exception ex)
        {
            return (ex.Message, false);
        }
    }
}