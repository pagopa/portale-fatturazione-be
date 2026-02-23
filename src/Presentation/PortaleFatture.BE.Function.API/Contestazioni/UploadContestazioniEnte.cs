using System.Diagnostics.Contracts;
using Azure.Storage.Sas;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.Messaggi;
using PortaleFatture.BE.Core.Entities.SEND.SelfCare.Dto;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Contestazioni.Payload;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Services;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti;
using PortaleFatture.BE.Infrastructure.Common.SEND.SelfCare.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Gateway.Storage.pagoPA;

namespace PortaleFatture.BE.Function.API.Contestazioni;

public class UploadContestazioniEnte(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<UploadContestazioniEnte>();

    [Function("UploadContestazioniEnte")]
    public async Task<string?> RunAsync(
        [ActivityTrigger] UploadContestazioniEnteApiInternalRequest req,
        FunctionContext context)
    {
        var mediator = context.InstanceServices.GetRequiredService<IMediator>();
        var factory = context.InstanceServices.GetRequiredService<IFattureDbContextFactory>();
        var storage = context.InstanceServices.GetRequiredService<IDocumentStorageSASService>();
        var sasTokenService = context.InstanceServices.GetRequiredService<IContestazioniStorageService>();
        var instanceId = context.InvocationId;

        _logger.LogInformation("HTTP trigger function processed a request.");

        // recupera contract id 
        EnteContrattoDto? contratto;
        using var uow = await factory.Create();
        {
            contratto = await uow.Query(new EnteCodiceSDIQueryGetByIdPersistence(req.Session!.IdEnte)); 
            if(contratto == null)
                throw new DomainException($"Non è stato possibile recuperare il contratto per l'ente {req.Session.IdEnte}.");
        }

        var authInfo = new AuthenticationInfo()
        {
            IdEnte = req.Session!.IdEnte,
            Profilo = req.Session.Profilo,
            Id = req.Session!.IdEnte,
            Prodotto = req.Session.Prodotto,
            Auth = AuthType.SELFCARE
        }; 

        var filename = $"{contratto!.RagioneSociale!.Replace(" ", "_")}_{req.Anno}_{req.Mese}*.csv";
        var documentKey = new DocumentiContestazioniSASSStorageKey(
                storage.BlobContainerContestazioniName,
                authInfo.IdEnte,
                contratto?.IdContratto,
                instanceId,
                filename);

        //inserisci report con prenotazione
        var insert = await mediator.Send(new ContestazioneReportCreateCommand(authInfo)
        {
            Stato = TipologiaStatoMessaggio.PrenotazioneFile,
            Storage = storage.StorageContestazioniName,
            NomeDocumento = null,
            LinkDocumento = $"{documentKey.DocumentReportLinkJson()}",
            UniqueId = instanceId,
            Anno = Convert.ToInt32(req.Anno),
            Mese = Convert.ToInt32(req.Mese),
            ContentLanguage = LanguageMapping.IT,
            ContentType = MimeMapping.JSON,
            InternalOrganizationId = authInfo.IdEnte,
            ContractId = contratto?.IdContratto,
            FileCaricato = filename, 
            IsUploadedByEnte = true // Indica che il file è stato caricato dall'ente 
        });

        if (!insert.Value)
            throw new DomainException($"Non è stato possibile prenotare la contestazione per l'ente {req.Session.IdEnte}.");

        var result = sasTokenService.GetSASToken(documentKey.Serialize(), BlobSasPermissions.Write);
        await LogResponse(mediator, context, req, result); 
        return result;
    }

    private async Task LogResponse(IMediator mediator, FunctionContext context, UploadContestazioniEnteApiInternalRequest request, string? response)
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