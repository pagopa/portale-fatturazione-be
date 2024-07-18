namespace PortaleFatture.BE.Infrastructure.Gateway.Email
{
    public interface IEmailSender
    {
        (string, bool) SendEmail(string to, string subject, string message);
    }
}