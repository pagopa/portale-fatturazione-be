using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.Alert.Dto;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.Alert.Services;
using PortaleFatture.BE.Infrastructure.Gateway.Email;
using PortaleFatture_BE_SendEmailFunction.Models.Alert;
using PortaleFatture_BE_SendEmailFunction.Models.pagoPA;

namespace PortaleFatture_BE_SendEmailFunction;

public class SendAlert(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<SendAlert>();

    [Function("SendAlert")]
    public async Task<RispostaAlert?> RunAsync([ActivityTrigger] AlertDataRequest req)
    {
        var response = new RispostaAlert();
        try
        {

            if (String.IsNullOrEmpty(ConfigurazioneAlert.ConnectionString) ||
                String.IsNullOrEmpty(ConfigurazioneAlert.AccessToken))
            {
                ConfigurazioneAlert.ConnectionString = GetEnvironmentVariable("CONNECTION_STRING");
                ConfigurazioneAlert.AccessToken = GetEnvironmentVariable("ACCESSTOKEN");
                ConfigurazioneAlert.RefreshToken = GetEnvironmentVariable("REFRESHTOKEN");
                ConfigurazioneAlert.ClientId = GetEnvironmentVariable("CLIENTID");
                ConfigurazioneAlert.ClientSecret = GetEnvironmentVariable("CLIENTSECRET");
                ConfigurazioneAlert.From = GetEnvironmentVariable("FROM");
                ConfigurazioneAlert.FromName = GetEnvironmentVariable("FROMNAME");
                ConfigurazioneAlert.To = GetEnvironmentVariable("TO");
                ConfigurazioneAlert.ToName = GetEnvironmentVariable("TONAME");
            }
            ;


            // params
            int idAlert = req.IdAlert;
            string? oggetto = req.Oggetto;
            string? messaggio = req.Messaggio;


            _logger.LogInformation($"SendAlert triggered. IdAlert: {idAlert}");


            var sender = new AlertSender(accessToken: ConfigurazioneAlert.AccessToken!,
                refreshToken: ConfigurazioneAlert.RefreshToken!,
                clientId: ConfigurazioneAlert.ClientId!,
                clientSecret: ConfigurazioneAlert.ClientSecret!,
                from: ConfigurazioneAlert.From,
                fromName: ConfigurazioneAlert.FromName!);


            var emailService = new AlertService(ConfigurazioneAlert.ConnectionString!);

            var (alert,recipients) = emailService.GetAlert(idAlert);

            // Replace with received value from request
            if (!oggetto.IsNullNotAny())
            {
                alert.Oggetto = oggetto;
            }

            if (!messaggio.IsNullNotAny())
            {
                alert.Messaggio = messaggio;
            }

            // Log alert
            _logger.LogInformation(alert.Serialize());

            foreach (var recipient in recipients)
            {
                if (recipient != null)
                {
                    var (msg, ver) = sender.SendEmail(recipient, "", alert.Oggetto!, alert.Messaggio!, Guid.NewGuid().ToString());

                    if (!ver)
                        _logger.LogInformation(msg);

                    emailService.InsertTracciatoEmail(new AlertTracking()
                    {
                        EventDate = DateTime.Now,
                        Recipient = recipient,
                        FkIdAlert = idAlert,
                        Sent = true
                    });
                }
            }

            response.RecipientCount = recipients.Count();

        }
        catch (Exception ex)
        {
            response.Error = ex.Message;
            _logger.LogInformation(ex.Message);
            return response;
        }

        _logger.LogInformation(response.Serialize());
        return response;
    }

    private static string? GetEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
    }
}