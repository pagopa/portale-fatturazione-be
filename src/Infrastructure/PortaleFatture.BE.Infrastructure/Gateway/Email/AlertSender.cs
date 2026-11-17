using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Newtonsoft.Json.Linq;

namespace PortaleFatture.BE.Infrastructure.Gateway.Email;

public class AlertSender(
    string? accessToken,
    string? refreshToken,
    string? clientId,
    string? clientSecret,
    string? from,
    string? fromName,
    GmailEndpoints? endpoints = null) : IAlertSender
{
    private readonly string? _accessToken = accessToken;
    private readonly string? _refreshToken = refreshToken;
    private readonly string? _clientId = clientId;
    private readonly string? _clientSecret = clientSecret;
    private readonly string? _from = from;
    private readonly string? _fromName = fromName;
    private readonly GmailEndpoints _endpoints = endpoints ?? GmailEndpoints.Google;
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
                BaseUri = _endpoints.ApiBaseUri,
                ApplicationName = applicationName
            });


            var mimeMessage = GmailMessageBuilder.Componi(_fromName, _from, toName, to, subject, body);

            var rawMessage = GmailMessageBuilder.ToRaw(mimeMessage);

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

        var requestMessage = new HttpRequestMessage(HttpMethod.Get, _endpoints.TokenInfoUrl + "?access_token=" + accessToken);
        var response = _client.Send(requestMessage);
        return !response.IsSuccessStatusCode;
    }

    private string RefreshAccessToken(string refreshToken, string clientId, string clientSecret)
    {
        using var client = new HttpClient();
        var tokenUrl = _endpoints.TokenUrl;
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
}