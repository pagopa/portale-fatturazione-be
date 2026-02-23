using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Api.Infrastructure.Documenti;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.Notifiche.Extensions;
using PortaleFatture.BE.Function.API.Notifiche.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Services;

namespace PortaleFatture.BE.Function.API.Notifiche;

public class NotificheGetByQuery(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<NotificheGetByQuery>();

    [Function("NotificheGetByQuery")]
    public async Task<NotificheResponse?> RunAsync(
        [ActivityTrigger] NotificheRicercaInternalRequest req,
        FunctionContext context)
    {
        var mediator = context.InstanceServices.GetRequiredService<IMediator>(); 
        var blobStorageNotifiche = context.InstanceServices.GetService<IBlobStorageNotifiche>();
        var options = context.InstanceServices.GetService<IPortaleFattureOptions>();
        var instanceId = context.InvocationId; 

        _logger.LogInformation("HTTP trigger function processed a request.");

        var authInfo = new AuthenticationInfo()
        {
            IdEnte = req.Session!.IdEnte,
            Profilo = req.Session.Profilo,
        }; 

        try
        { 
            req.IdEnte = req.Session!.IdEnte;
            req.RagioneSociale = req.Session!.RagioneSociale;
            req.IdContratto = req.Session!.IdContratto;

            var istanza = new ReportNotificheByIdHashQueryCommand(authInfo)
            {
                Json = req.Serialize(),
            };

            var report = await mediator.Send(istanza);
            if (report != null)
                throw new DomainException($"Attendi l'esecuzione della richiesta precedente. {req.IdEnte!}");
  
            var command = new ReportNotificheCreateCommand(authInfo)
            {
                UniqueId = instanceId,
                Json = req.Serialize(),
                Anno = req.Anno!.Value,
                Mese = req.Mese!.Value,
                ContractId = req.IdContratto,
                Storage = options!.StorageNotifiche!.AccountName,
                NomeDocumento = null,
                Link = null
            };

            var idReport = await mediator.Send(command);
            if (!idReport.HasValue)
                throw new DomainException($"Si è verificato un errore. {req.IdEnte!}");

            req.IdReport = idReport;

            var preport = await mediator.Send(new ReportNotificheByIdQueryCommand(authInfo)
            {
                IdEnte = req.Session!.IdEnte,
                IdReport = req.IdReport
            });

            var nomeDocumento = $"Notifiche_{req.Session.RagioneSociale}_{req.Anno}_{req.Mese}_{preport!.DataInserimento:MM-dd-yyyy-HH-mm-ss}.csv";
            var notifiche = await mediator.Send(req.Mapv2().Map(authInfo, null, null));
            if (notifiche == null || notifiche.Count == 0)
            {
                var aggiornamento = await mediator.Send(new ReportNotificheUpdateCommand(authInfo)
                {
                    NomeDocumento = null,
                    Stato = 2,
                    StatoAtteso = 0,
                    UniqueId = instanceId,
                    Count = 0
                });

                if (!string.IsNullOrEmpty(aggiornamento))
                {
                    var response = new NotificheResponse()
                    {
                        Count = 0,
                        Stato = 2,
                        LinkDocumento = null,
                        UniqueId = instanceId,
                    };

                    _logger.LogInformation($"Blob uploaded con instance id: {instanceId}");

                    await LogResponse(mediator, context, req, response);

                    return response;
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
                    UniqueId = instanceId,
                    Count = notifiche.Count
                });

                if (!string.IsNullOrEmpty(aggiornamento))
                {
                    _logger.LogInformation($"Blob uploaded con instance id: {instanceId}");
                    var sasToken = blobStorageNotifiche.GetSasToken(req.Session!.IdEnte!, req.Anno, req.Mese, instanceId, nomeDocumento);

                    var response = new NotificheResponse()
                    {
                        Count = notifiche.Count,
                        Stato = 1,
                        LinkDocumento = sasToken,
                        UniqueId = instanceId,
                    };

                    await LogResponse(mediator, context, req, response);

                    return sasToken == null
                        ? throw new Exception($"Errore generazione SAS Token per IdEnte {req.Session.IdEnte!}")
                        : response;
                }
                else
                    throw new Exception($"Errore aggiornamento report notifiche IdEnte {req.IdEnte!}");
            }
            else
                throw new Exception($"Errore scrittura report notifiche IdEnte {req.IdEnte!}");

        }
        catch
        {
            var response = new NotificheResponse()
            {
                LinkDocumento = null,
                Stato = 3, 
                UniqueId = instanceId,
                Count = 0
            };

            await LogResponse(mediator, context, req, response); 

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

    private async Task LogResponse(IMediator mediator, FunctionContext context, NotificheRicercaInternalRequest request, NotificheResponse response)
    {
        var sse = request.Session;
        sse!.Payload = response.Serialize();
        var logResponse = context.Response(sse);
        try
        {
            await mediator.Send(logResponse);
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"{LoggerHelper.PREFIX}{MessageHelper.BadRequestLogging}{LoggerHelper.PIPE}{ex.Serialize()}");
            throw new DomainException(MessageHelper.BadRequestLogging, ex);
        }
    }
}