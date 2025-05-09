using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.ModuloCommessa.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries;

namespace PortaleFatture.BE.Function.API.ModuloCommessa;

public class ModuloCommessaGetByAnnoMeseDettaglioFunction(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<ModuloCommessaGetByAnnoMeseDettaglioFunction>();

    [Function("ModuloCommessaGetByAnnoMeseDettaglio")]
    public async Task<ModuloCommessaDocumentoDto> RunAsync(
        [ActivityTrigger] ModuloCommessaGetByAnnoMeseDettaglioInternalRequest req, 
        FunctionContext context)
    { 
        var mediator = context.InstanceServices.GetRequiredService<IMediator>();

        var authInfo = new AuthenticationInfo()
        {
            IdEnte = req.Session!.FkIdEnte,
            Prodotto = "prod-pn"            
        };

        var modulo = await mediator.Send(new DatiModuloCommessaDocumentoQueryGet(authInfo)
        {
            AnnoValidita = req.Anno,
            MeseValidita = req.Mese
        });

        var sse = req.Session;
        sse.Payload = modulo.Serialize();
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

        return modulo!; 
    }
}