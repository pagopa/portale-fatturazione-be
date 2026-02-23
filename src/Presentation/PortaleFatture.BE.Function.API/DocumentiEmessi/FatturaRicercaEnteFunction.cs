using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.DocumentiEmessi.Payload;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

namespace PortaleFatture.BE.Function.API.DocumentiEmessi;

public class FatturaRicercaEnteFunction(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<FatturaRicercaEnteFunction>();

    [Function("FatturaRicercaEnte")]
    public async Task<FattureListaDto> RunAsync(
        [ActivityTrigger] FatturaRicercaEnteInternalRequest req,
        FunctionContext context)
    {
        var mediator = context.InstanceServices.GetRequiredService<IMediator>();

        var authInfo = new AuthenticationInfo()
        {
            IdEnte = req.Session!.IdEnte,
            Prodotto = req.Session!.Prodotto,
        };

        var request = new FattureQueryRicercaByEnte(authInfo)
        {
            Anno = req.Anno,
            Mese = req.Mese,
            TipologiaFattura = req.TipologiaFattura
        };

        var fatture = await mediator.Send(request);
        //if (fatture == null || !fatture!.Any())
        //    return NotFound();
      

        var sse = req.Session;
        sse.Payload = fatture.Serialize();
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

        return fatture!;
    }
}