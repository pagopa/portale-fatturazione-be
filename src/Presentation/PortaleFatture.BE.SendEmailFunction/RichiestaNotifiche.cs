using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Api.Infrastructure.Documenti;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure;
using PortaleFatture.BE.Infrastructure.Common.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Services;
using PortaleFatture_BE_SendEmailFunction.Models;

namespace PortaleFatture_BE_SendEmailFunction;

public class RichiestaNotifiche(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<RichiestaNotifiche>();

    [Function("RichiestaNotifiche")]
    public async Task RunAsync([ActivityTrigger] NotificheRicercaRequest req,
        FunctionContext context)
    {
        var logger = context.GetLogger("RichiestaNotifiche");

        var instanceId = req.InstanceId;

        logger.LogError("RichiestaNotifiche with data1: {Data}", instanceId);

        await Task.Delay(TimeSpan.FromSeconds(10)); // 10 secondi per far scrivere il report successivo 

        // config
        ConfigurazioneSENDRichiestaNotifiche.AccountName = GetEnvironmentVariable("StorageNotificheAccountName");
        ConfigurazioneSENDRichiestaNotifiche.AccountKey = GetEnvironmentVariable("StorageNotificheAccountKey");
        ConfigurazioneSENDRichiestaNotifiche.BlobContainerName = GetEnvironmentVariable("StorageNotificheBlobContainerName");
        ConfigurazioneSENDRichiestaNotifiche.ConnectionString = GetEnvironmentVariable("CONNECTION_STRING");

 
        if (string.IsNullOrEmpty(ConfigurazioneSENDRichiestaNotifiche.ConnectionString))
            logger.LogError("ConfigurazioneSENDRichiestaNotifiche.ConnectionString is null or empty");

        if (string.IsNullOrEmpty(ConfigurazioneSENDRichiestaNotifiche.AccountName))
            logger.LogError("ConfigurazioneSENDRichiestaNotifiche.AccountName is null or empty");

        if (string.IsNullOrEmpty(ConfigurazioneSENDRichiestaNotifiche.AccountKey))
            logger.LogError("ConfigurazioneSENDRichiestaNotifiche.AccountKey is null or empty");

        if (string.IsNullOrEmpty(ConfigurazioneSENDRichiestaNotifiche.BlobContainerName))
            logger.LogError("ConfigurazioneSENDRichiestaNotifiche.BlobContainerName is null or empty");
 

        var services = new ServiceCollection();
        services.AddMediatR(x => x.RegisterServicesFromAssembly(typeof(RootInfrastructure).Assembly));
        services.AddSingleton<ISelfCareDbContextFactory>(new DbContextFactory(ConfigurazioneSENDRichiestaNotifiche.ConnectionString!, "pfd"));
        services.AddSingleton<IFattureDbContextFactory>(new DbContextFactory(ConfigurazioneSENDRichiestaNotifiche.ConnectionString!, "pfw"));
        services.AddLocalization(options => options.ResourcesPath = "Resources");
        services.AddControllersWithViews()
                .AddDataAnnotationsLocalization()
                .AddViewLocalization();
        var options = (new PortaleFattureOptions()
        {
            StorageNotifiche = new StorageNotifiche()
            {
                AccountName = ConfigurazioneSENDRichiestaNotifiche.AccountName,
                AccountKey = ConfigurazioneSENDRichiestaNotifiche.AccountKey,
                BlobContainerName = ConfigurazioneSENDRichiestaNotifiche.BlobContainerName
            }
        });

        services.AddSingleton<IBlobStorageNotifiche>(provider =>
        {
            return new BlobStorageNotifiche(options);
        });

        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var blobStorageNotifiche = serviceProvider.GetService<IBlobStorageNotifiche>();

        _logger.LogInformation("HTTP trigger function processed a request.");
        var authInfo = new AuthenticationInfo()
        {
            IdEnte = req.IdEnte
        };

        try
        {
            logger.LogError("RichiestaNotifiche with data2: {Data}", req.Serialize());
            // recupera id report 
            var report = await mediator.Send(new ReportNotificheByIdQueryCommand(authInfo)
            {
                IdEnte = req.IdEnte,
                IdReport = req.IdReport
            });

            var nomeDocumento = $"Notifiche_{req.RagioneSociale}_{req.Anno}_{req.Mese}_{report!.DataInserimento:MM-dd-yyyy-HH-mm-ss}.csv";
            var notifiche = await mediator.Send(Map(req, authInfo, null, null));
            if (notifiche == null || notifiche.Count == 0)
            {
                var aggiornamento = await mediator.Send(new ReportNotificheUpdateCommand(authInfo)
                {
                    NomeDocumento = null,
                    Stato = 2,
                    StatoAtteso = 0,
                    UniqueId = report.UniqueId,
                    Count = 0
                });

                if (!string.IsNullOrEmpty(aggiornamento))
                {
                    _logger.LogInformation($"Blob uploaded con instance id: {instanceId}");
                    return; // Exits the function early
                }
                else
                {
                    throw new Exception($"Errore aggiornamento report notifiche IdEnte {req.IdEnte!}");
                }
            }

            var stream = await notifiche!.Notifiche!.ToStream<SimpleNotificaDto, SimpleNotificaEnteDtoMap>();
            if (stream.Length == 0)
                throw new Exception($"Errore aggiornamento stream notifiche IdEnte {req.IdEnte!}");

            stream.Position = 0;
            var result = await blobStorageNotifiche!.UploadStreamAsync(stream, req.IdEnte!, req.Anno, req.Mese, instanceId!, nomeDocumento);
            if (result != null)
            {
                var aggiornamento = await mediator.Send(new ReportNotificheUpdateCommand(authInfo)
                {
                    NomeDocumento = nomeDocumento,
                    Stato = 1,
                    StatoAtteso = 0,
                    UniqueId = report.UniqueId,
                    Count = notifiche.Count
                });

                if (!string.IsNullOrEmpty(aggiornamento))
                {
                    _logger.LogInformation($"Blob uploaded con instance id: {instanceId}");
                    return; // Exits the function early
                }
                else
                    throw new Exception($"Errore aggiornamento report notifiche IdEnte {req.IdEnte!}");
            }
            else
                throw new Exception($"Errore scrittura report notifiche IdEnte {req.IdEnte!}");

        }
        catch (Exception ex)
        {
            logger.LogError("RichiestaNotificheEx errore: {Data}", ex.Message);
            _ = await mediator.Send(new ReportNotificheUpdateCommand(authInfo)
            {
                NomeDocumento = null,
                Stato = 3,
                StatoAtteso = 0,
                UniqueId = instanceId,
                Count = 0
            });
            throw;
        }
    }

    private static string? GetEnvironmentVariable(string name)
    {
        return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
    }

    public static NotificaQueryGetByIdEnte Map(NotificheRicercaRequest req, AuthenticationInfo authInfo, int? page, int? pageSize)
    {
        return new NotificaQueryGetByIdEnte(authInfo)
        {
            AnnoValidita = req.Anno,
            Cap = req.Cap,
            MeseValidita = req.Mese,
            Page = page,
            Prodotto = req.Prodotto,
            Profilo = req.Profilo,
            Size = pageSize,
            TipoNotifica = req.TipoNotifica,
            StatoContestazione = req.StatoContestazione,
            Iun = req.Iun,
            RecipientId = req.RecipientId
        };
    }
}