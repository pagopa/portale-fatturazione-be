using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Contestazioni.Payload;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.SelfCare.Queries.Persistence;

namespace PortaleFatture.BE.Function.API.Contestazioni;


public class ContestazioniRecapEnte(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<ContestazioniReportsDocument>();

    [Function("ContestazioniRecapEnte")]
    public async Task<IEnumerable<ContestazioneRecap>?> RunAsync(
        [ActivityTrigger] ContestazioniRecapEnteApiInternalRequest req,
        FunctionContext context)
    {
        var mediator = context.InstanceServices.GetRequiredService<IMediator>();
        var factory = context.InstanceServices.GetRequiredService<IFattureDbContextFactory>();
        //var instanceId = context.InvocationId;

        _logger.LogInformation("HTTP trigger function processed a request.");

        var authInfo = new AuthenticationInfo()
        {
            IdEnte = req.Session!.IdEnte,
            Profilo = req.Session.Profilo,
        };

        // recupera contract id 
        string? contractId;
        using var uow = await factory.Create();
        {
            var contratto = await uow.Query(new EnteCodiceSDIQueryGetByIdPersistence(authInfo.IdEnte));
            contractId = contratto?.IdContratto;
        }

        var recap = await mediator.Send(new ContestazioniRecapQuery(authInfo)
        {
            Anno = req.Anno.ToString(),
            Mese = req.Mese.ToString(),
            ContractId = contractId,
            IdEnte = authInfo.IdEnte
        });

        if (recap.IsNullNotAny())
            throw new DomainException($"Non ci sono recap da segnalare. {req.Session!.IdEnte!}"); 
 
        await LogResponse(mediator, context, req, recap); 

        return recap;
    }

    private async Task LogResponse(IMediator mediator, FunctionContext context, ContestazioniRecapEnteApiInternalRequest request, IEnumerable<ContestazioneRecap>? response)
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