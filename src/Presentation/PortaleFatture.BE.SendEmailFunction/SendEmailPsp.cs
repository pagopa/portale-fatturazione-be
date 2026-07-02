using System.Reflection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.pagoPA.AnagraficaPSP;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Dto;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Services;
using PortaleFatture.BE.Infrastructure.Gateway.Email;
using PortaleFatture_BE_SendEmailFunction.Models.pagoPA;

namespace PortaleFatture_BE_SendEmailFunction;

public class SendEmailPsp(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<SendEmail>();

    [Function("SendEmailPsp")]
    public async Task RunAsync([ActivityTrigger] EmailPspDataRequest req)
    {
        var risposta = new RispostapagoPA();
        try
        {

            string[] environments = ["fat-d-api-func", "fat-u-api-func", "debug"];

            ConfigurazionepagoPA.Environment = GetEnvironmentVariable("PortaleFattureOptions:WEBSITE_SITE_NAME");

            if(String.IsNullOrEmpty(ConfigurazionepagoPA.Environment))
            {
                ConfigurazionepagoPA.Environment = GetEnvironmentVariable("WEBSITE_SITE_NAME");
            }

            var currentEnvironment = ConfigurazionepagoPA.Environment?.Trim();
            var production = !string.IsNullOrWhiteSpace(currentEnvironment)
                && !environments.Contains(currentEnvironment, StringComparer.OrdinalIgnoreCase);

            ConfigurazionepagoPA.ConnectionString = GetEnvironmentVariable("PortaleFattureOptions:ConnectionString");

            if (String.IsNullOrEmpty(ConfigurazionepagoPA.ConnectionString)){
                ConfigurazionepagoPA.ConnectionString = GetEnvironmentVariable("CONNECTION_STRING");
            }

            if(production){
                // config
                ConfigurazionepagoPA.ConnectionString = GetEnvironmentVariable("PortaleFattureOptions:ConnectionString");
                ConfigurazionepagoPA.AccessToken = GetEnvironmentVariable("PortaleFattureOptions:AccessToken");
                ConfigurazionepagoPA.RefreshToken = GetEnvironmentVariable("PortaleFattureOptions:RefreshToken");
                ConfigurazionepagoPA.ClientId = GetEnvironmentVariable("PortaleFattureOptions:ClientId");
                ConfigurazionepagoPA.ClientSecret = GetEnvironmentVariable("PortaleFattureOptions:ClientSecret");
                ConfigurazionepagoPA.From = GetEnvironmentVariable("PortaleFattureOptions:From");
                ConfigurazionepagoPA.FromName = GetEnvironmentVariable("PortaleFattureOptions:FromName");
                ConfigurazionepagoPA.To = GetEnvironmentVariable("PortaleFattureOptions:To");
                ConfigurazionepagoPA.ToName = GetEnvironmentVariable("PortaleFattureOptions:ToName");

                if (String.IsNullOrEmpty(ConfigurazionepagoPA.ConnectionString) ||
                    String.IsNullOrEmpty(ConfigurazionepagoPA.AccessToken))
                {
                    ConfigurazionepagoPA.ConnectionString = GetEnvironmentVariable("CONNECTION_STRING");
                    ConfigurazionepagoPA.AccessToken = GetEnvironmentVariable("ACCESSTOKEN");
                    ConfigurazionepagoPA.RefreshToken = GetEnvironmentVariable("REFRESHTOKEN");
                    ConfigurazionepagoPA.ClientId = GetEnvironmentVariable("CLIENTID");
                    ConfigurazionepagoPA.ClientSecret = GetEnvironmentVariable("CLIENTSECRET");
                    ConfigurazionepagoPA.From = GetEnvironmentVariable("FROM");
                    ConfigurazionepagoPA.FromName = GetEnvironmentVariable("FROMNAME");
                    ConfigurazionepagoPA.To = GetEnvironmentVariable("TO");
                    ConfigurazionepagoPA.ToName = GetEnvironmentVariable("TONAME");
                }
            }

            ;


            var fileInfo = new FileInfo(Assembly.GetExecutingAssembly().Location);
            var path = fileInfo.Directory!.FullName;

            // params
            var anno = Convert.ToInt32(req.Anno);
            int? reinvio = Convert.ToInt32(req.Reinvio);
            var trimestre = req.Trimestre;
            var tipologia = EmailPspTipologia.Financial;
            var data = req.Date;
            var preview = req.Preview;

            _logger.LogInformation("HTTP trigger function processed a request.");

            // response
            risposta = new RispostapagoPA()
            {
                Anno = anno,
                Trimestre = trimestre,
                Tipologia = tipologia,
                Data = data
            };

            var builder = new DocumentPspBuilder(path);

            if (string.IsNullOrEmpty(data))
                data = DateTime.UtcNow.ItalianTime().ToString("yyyy-MM-dd HH:mm:ss");

            var subject = $"Detailed invoice reports {trimestre}";

            IEnumerable<PspEmail>? psps = [];
            var emailService = new EmailPspService(ConfigurazionepagoPA.ConnectionString!);
            if (reinvio is null || reinvio != 1)
            {
                // verifica comunque se c'è stato un invio
                var count = emailService.CountInvio(risposta.Trimestre);
                if (!count)
                    psps = emailService.GetSenderEmail(risposta.Trimestre);
                else
                {
                    var message = $"The email has been already sent for quarter {trimestre}.";
                    risposta.Error = message;
                    _logger.LogInformation($"The email has been already sent");
                }
            }
            else // reinvio == 1
            {
                var subPsps = emailService.GetSenderEmailReinvio(risposta.Trimestre);
                if (!subPsps.IsNullNotAny())
                {
                    var totalPsps = emailService.GetSenderEmail(risposta.Trimestre);
                    psps = totalPsps!.Where(x => subPsps.Contains(x.IdContratto));
                }
            }

            var apiKeyFilePath = builder.ApiKeyFilePath();
            _logger.LogInformation(psps.Serialize());

            Thread.Sleep(60000); // 1 minuto

            foreach (var psp in psps!)
                if (psp.Email != null)
                {
                    var body = builder.CreateEmailHtml(psp);
                    if (!preview.HasValue || !preview.Value)
                    {
                        if(production)
                        {
                            var sender = new PspEmailSender(accessToken: ConfigurazionepagoPA.AccessToken!,
                                refreshToken: ConfigurazionepagoPA.RefreshToken!,
                                clientId: ConfigurazionepagoPA.ClientId!,
                                clientSecret: ConfigurazionepagoPA.ClientSecret!,
                                from: ConfigurazionepagoPA.From,
                                fromName: ConfigurazionepagoPA.FromName!);

                            var (msg, ver) = sender.SendEmail(psp.Email, psp.RagioneSociale!, subject, body!, Guid.NewGuid().ToString());
                            //var (msg, ver) = sender.SendEmail(ConfigurazionepagoPA.To!, ConfigurazionepagoPA.ToName!, subject, body!, Guid.NewGuid().ToString());

                            if (!ver)
                                _logger.LogInformation(msg);

                            emailService.InsertTracciatoEmail(new PspEmailTracking()
                            {
                                Data = data,
                                IdContratto = psp.IdContratto,
                                Invio = Convert.ToByte(ver == true ? 1 : 0),
                                Anno = psp.Anno,
                                Messaggio = $"{msg}\n\nOGGETTO: {subject}\n\nCORPO:\n{body!}",
                                Oggetto = subject,
                                Corpo = body,
                                Link = psp.DetailReport ?? psp.AgentReport ?? psp.DiscountReport,
                                Email = psp.Email,
                                Trimestre = psp.Trimestre,
                                RagioneSociale = psp.RagioneSociale,
                                Tipologia = psp.Tipologia
                            });
                        }
                        else
                        {
                            _logger.LogInformation($"Modalità di test: email NON inviata a {psp.Email} con oggetto {subject} e inserita nella tracking");
                            emailService.InsertTracciatoEmail(new PspEmailTracking()
                            {
                                Data = data,
                                IdContratto = psp.IdContratto,
                                Invio = 0,
                                Anno = psp.Anno,
                                Messaggio = $"OGGETTO: {subject}\n\nCORPO:\n{body!}",
                                Oggetto = subject,
                                Corpo = body,
                                Link = psp.DetailReport ?? psp.AgentReport ?? psp.DiscountReport,
                                Email = psp.Email,
                                Trimestre = psp.Trimestre,
                                RagioneSociale = psp.RagioneSociale,
                                Tipologia = psp.Tipologia
                            });
                        }

                    }
                    else
                    {
                        _logger.LogInformation($"Modalità preview: email NON inviata a {psp.Email} con oggetto {subject} e inserita nella tracking preview");

                        emailService.InsertPreviewEmail(new PspEmailTracking()
                        {
                            Data = data,
                            IdContratto = psp.IdContratto,
                            Invio = 0,
                            Anno = psp.Anno,
                            Email = psp.Email,
                            Trimestre = psp.Trimestre,
                            RagioneSociale = psp.RagioneSociale,
                            Tipologia = psp.Tipologia,
                            Oggetto = subject,
                            Corpo = body,
                            Link = psp.DetailReport ?? psp.AgentReport ?? psp.DiscountReport,
                            TipoContratto = null
                        });
                    }
                }

            risposta.NumeroInvio = psps.Count();
        }
        catch (Exception ex)
        {
            risposta.DbConnection = false;
            risposta.Error = ex.Message;
            _logger.LogInformation(ex.Message);
        }

        _logger.LogInformation(risposta.Serialize());
    }

    private static string? GetEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
    }
}