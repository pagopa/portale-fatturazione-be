
namespace PortaleFatture.BE.Infrastructure.Gateway.Email
{
    public interface IPspEmailSender
    {
        (string, bool) SendEmail(string to, string toName, string subject, string body, string applicationName);
        Dictionary<string, bool> SentFolder(Dictionary<string, string> input, string applicationName);
    }
}