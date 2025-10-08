
namespace PortaleFatture.BE.Infrastructure.Gateway.Email
{
    public interface IAlertSender
    {
        (string, bool) SendEmail(string to, string toName, string subject, string body, string applicationName);
    }
}