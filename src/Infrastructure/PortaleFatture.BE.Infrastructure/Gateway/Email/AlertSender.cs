using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using MimeKit;
using Newtonsoft.Json.Linq;

namespace PortaleFatture.BE.Infrastructure.Gateway.Email;

public class AlertSender(
    string? accessToken,
    string? refreshToken,
    string? clientId,
    string? clientSecret,
    string? from,
    string? fromName) : IAlertSender
{
    private readonly string? _accessToken = accessToken;
    private readonly string? _refreshToken = refreshToken;
    private readonly string? _clientId = clientId;
    private readonly string? _clientSecret = clientSecret;
    private readonly string? _from = from;
    private readonly string? _fromName = fromName;
    private static readonly HttpClient _client = new HttpClient();
    public (string, bool) SendEmail(
        string to,
        string toName,
        string subject,
        string body,
        string applicationName)
    {
        try
        {
            var myaccessToken = string.Empty;


            if (IsAccessTokenExpired(_accessToken!))
                myaccessToken = RefreshAccessToken(_refreshToken!, _clientId!, _clientSecret!);
            else
                myaccessToken = _accessToken;

            var credential = GoogleCredential.FromAccessToken(myaccessToken);

            var service = new GmailService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = applicationName
            });


            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(_fromName, _from));
            mimeMessage.To.Add(new MailboxAddress(toName, to));
            mimeMessage.Subject = subject;
            mimeMessage.Body = new TextPart("html")
            {
                Text = body
            };

            var rawMessage = ConvertMimeMessageToRaw(mimeMessage);

            var message = new Message
            {
                Raw = rawMessage
            };

            var result = service.Users.Messages.Send(message, "me").Execute();
            return (result.Id, true);
        }
        catch (Exception ex)
        {
            return (ex.Message, false);
        }
    }


    private bool IsAccessTokenExpired(string accessToken)
    {

        var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v1/tokeninfo?access_token=" + accessToken);
        var response = _client.Send(requestMessage);
        return !response.IsSuccessStatusCode;
    }

    private string RefreshAccessToken(string refreshToken, string clientId, string clientSecret)
    {
        using var client = new HttpClient();
        var tokenUrl = "https://oauth2.googleapis.com/token";
        var requestBody = new MultipartFormDataContent
            {
                { new StringContent(clientId), "client_id" },
                { new StringContent(clientSecret), "client_secret" },
                { new StringContent(refreshToken), "refresh_token" },
                { new StringContent("refresh_token"), "grant_type" }
            };

        var response = client.PostAsync(tokenUrl, requestBody).Result;
        var responseString = response.Content.ReadAsStringAsync().Result;

        var tokenResponse = JObject.Parse(responseString);
        if (tokenResponse["access_token"] != null)
            return tokenResponse["access_token"]!.ToString();
        else
            throw new Exception("Failed to refresh the access token.");
    }

    private string ConvertMimeMessageToRaw(MimeMessage mimeMessage)
    {
        using var memoryStream = new MemoryStream();
        mimeMessage.WriteTo(memoryStream);
        var rawMessage = Convert.ToBase64String(memoryStream.ToArray())
            .Replace('+', '-')
            .Replace('/', '_')
            .Replace("=", "");
        return rawMessage;
    }
}