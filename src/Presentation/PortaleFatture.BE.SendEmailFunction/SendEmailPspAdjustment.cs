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

public class SendEmailPspAdjustment(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<SendEmailPspAdjustment>();

    [Function(nameof(SendEmailPspAdjustment))]
    public async Task<RispostapagoPA> RunAsync([ActivityTrigger] EmailPspAdjustmentDataRequest req)
    {
        var risposta = new RispostapagoPA();
        var processati = 0;
        var inviati = 0;
        var loggati = 0;
        var logAnteprime = 0;

        try
        {
            // /|\ etection produzione per esclusione (ambiente sconosciuto ⇒ invio reale) — 
            // passare ad allow-list esplicita; blocco duplicato in SendEmail.cs e SendEmailPspAdjustment.cs, 
            // estrarre helper condiviso
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
            var anno = 2026;
            //int? reinvio = Convert.ToInt32(req.Reinvio);
            var trimestre = "2026_1";
            var tipologia = EmailPspTipologia.FinancialAdjust;
            var data = DateTime.UtcNow.ItalianTime().ToString("yyyy-MM-dd HH:mm:ss");
            var preview = req.Preview ?? true;

            _logger.LogInformation("HTTP trigger function processed a request.");

            // response
            risposta = new RispostapagoPA()
            {
                Environment = currentEnvironment,
                Anno = anno,
                Trimestre = trimestre,
                Tipologia = tipologia,
                Data = data
            };

            var builder = new DocumentPspBuilder(path);

            if (string.IsNullOrEmpty(data))
                data = DateTime.UtcNow.ItalianTime().ToString("yyyy-MM-dd HH:mm:ss");

            var subject = $"piattaforma pagoPA - Report aggiornati Q1 2026 e conguagli in fatturazione Q2 2026";

            IEnumerable<PspEmail>? psps = [];
            var emailService = new EmailPspService(ConfigurazionepagoPA.ConnectionString!);

            var count = emailService.CountInvioAdjustment(risposta.Trimestre);
            if (!count)
                psps = emailService.GetSenderEmailAdjustment(risposta.Trimestre);
            else
            {
                var message = $"The email adjustment has been already sent for quarter {trimestre}.";
                risposta.Error = message;
                _logger.LogInformation($"The email adjustment has been already sent");
            }

            //psps = emailService.GetSenderEmailAdjustment(risposta.Trimestre);

           var apiKeyFilePath = builder.ApiKeyFilePath();
            _logger.LogInformation(psps.Serialize());


            foreach (var psp in psps!)
                if (psp.Email != null)
                {
                    var body = builder.CreateEmailAdjsutmentHtml(psp);
                    if (!preview)
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
                                Messaggio = $"{msg}",
                                Oggetto = subject,
                                Corpo = body,
                                Link = psp.DetailReport ?? psp.AgentReport ?? psp.DiscountReport,
                                Email = psp.Email,
                                Trimestre = psp.Trimestre,
                                RagioneSociale = psp.RagioneSociale,
                                Tipologia = psp.Tipologia
                            });
                            inviati++;
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
                                Messaggio = $"Modalità di test: email NON inviata a {psp.Email} con oggetto {subject} e inserita nella tracking",
                                Oggetto = subject,
                                Corpo = body,
                                Link = psp.DetailReport ?? psp.AgentReport ?? psp.DiscountReport,
                                Email = psp.Email,
                                Trimestre = psp.Trimestre,
                                RagioneSociale = psp.RagioneSociale,
                                Tipologia = psp.Tipologia
                            });
                        }
                        loggati++;
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
                        logAnteprime++;
                    }
                    processati++;
                }

            risposta.NumeroInvio = psps.Count();
            risposta.Processati = processati;
            risposta.Inviati = inviati;
            risposta.Loggati = loggati;
            risposta.LogAnteprime = logAnteprime;
        }
        catch (Exception ex)
        {
            risposta.DbConnection = false;
            risposta.Error = ex.Message;
            _logger.LogInformation(ex.Message);
        }

        //var output = risposta.Serialize();
        _logger.LogInformation(risposta.Serialize());
        return risposta;
    }

    private static string? GetEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
    }
}