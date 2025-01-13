using System.Net;
using System.Reflection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.SEND.DatiRel.Dto;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Services;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti;
using PortaleFatture.BE.Infrastructure.Gateway.Email;
using PortaleFatture_BE_SendEmailFunction.Models;

namespace PortaleFatture_BE_SendEmailFunction;

public class SendEmail(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<SendEmail>();

    [Function("SendEmail")]
    public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        var risposta = new Risposta();
        try
        {
            // config
            ConfigurazioneSEND.ConnectionString = GetEnvironmentVariable("PortaleFattureOptions:ConnectionString");
            ConfigurazioneSEND.From = GetEnvironmentVariable("PortaleFattureOptions:FROM");
            ConfigurazioneSEND.Smtp = GetEnvironmentVariable("PortaleFattureOptions:SMTP");
            ConfigurazioneSEND.SmtpPort = Convert.ToInt32(GetEnvironmentVariable("PortaleFattureOptions:SMTP_PORT"));
            ConfigurazioneSEND.SmtpAuth = GetEnvironmentVariable("PortaleFattureOptions:SMTP_AUTH");
            ConfigurazioneSEND.SmtpPassword = GetEnvironmentVariable("PortaleFattureOptions:SMTP_PASSWORD");
            if (String.IsNullOrEmpty(ConfigurazioneSEND.ConnectionString) ||
                String.IsNullOrEmpty(ConfigurazioneSEND.From) ||
                String.IsNullOrEmpty(ConfigurazioneSEND.Smtp) ||
                String.IsNullOrEmpty(ConfigurazioneSEND.SmtpAuth) ||
                String.IsNullOrEmpty(ConfigurazioneSEND.SmtpPassword))
            {
                ConfigurazioneSEND.ConnectionString = GetEnvironmentVariable("CONNECTION_STRING");
                ConfigurazioneSEND.From = "send-fatturazione@pec.pagopa.it";
                ConfigurazioneSEND.Smtp = GetEnvironmentVariable("SMTP");
                ConfigurazioneSEND.SmtpPort = Convert.ToInt32(GetEnvironmentVariable("SMTP_PORT"));
                ConfigurazioneSEND.SmtpAuth = GetEnvironmentVariable("SMTP_AUTH");
                ConfigurazioneSEND.SmtpPassword = GetEnvironmentVariable("SMTP_PASSWORD"); 
            };



            var fileInfo = new FileInfo(Assembly.GetExecutingAssembly().Location);
            string path = fileInfo.Directory!.FullName;

            // params
            var anno = Convert.ToInt32(req.Query["anno"]);
            var mese = Convert.ToInt32(req.Query["mese"]);
            var tipologiafattura = req.Query["tipologiafattura"];
            var data = req.Query["data"];
            var ricalcola = Convert.ToInt32(req.Query["ricalcola"]);

            _logger.LogInformation("HTTP trigger function processed a request.");

            // response
            risposta = new Risposta()
            {
                Anno = anno,
                Mese = mese,
                TipologiaFattura = tipologiafattura,
                Data = data,
                Ricalcola = ricalcola
            };

            var builder = new DocumentBuilder(path);

            if(String.IsNullOrEmpty(data))
            {
                data = DateTime.UtcNow.ItalianTime().ToString("yyyy-MM-dd HH:mm:ss");
            }

            var subject = $"Notifica Regolare Esecuzione {tipologiafattura} Mese di {mese.GetMonth()}";
            var sender = new EmailSender(smtpSource: ConfigurazioneSEND.Smtp!,
                smtpPort: ConfigurazioneSEND.SmtpPort!,
                smtpUser: ConfigurazioneSEND.SmtpAuth!,
                smtpPassword: ConfigurazioneSEND.SmtpPassword!,
                from: ConfigurazioneSEND.From!);
            var emailService = new EmailRelService(ConfigurazioneSEND.ConnectionString!);
            var enti = emailService.GetSenderEmail(risposta.Anno, risposta.Mese, risposta.TipologiaFattura!);
            foreach (var ente in enti!)
                if (ente.Pec != null)
                {
                    var (msg, ver) = sender.SendEmail(ente.Pec, subject, builder.CreateEmailHtml(ente)!);
                    if (!ver) 
                        _logger.LogInformation(msg); 

                    emailService.InsertTracciatoEmail(new RelEmailTracking()
                    {
                        Data = data,
                        IdContratto = ente.IdContratto,
                        Invio = Convert.ToByte(ver == true ? 1 : 0),
                        Anno = ente.Anno,
                        Mese = ente.Mese,
                        Messaggio = msg,
                        Pec = ente.Pec,
                        IdEnte = ente.IdEnte,
                        RagioneSociale = ente.RagioneSociale,
                        TipologiaFattura = ente.TipologiaFattura
                    });
                }
        }
        catch (Exception ex)
        {
            risposta.DbConnection = false;
            risposta.Error = ex.Message;
            _logger.LogInformation(ex.Message);
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        response.WriteString(risposta.Serialize());
        return response;
    }

    private static string? GetEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
    }

}
