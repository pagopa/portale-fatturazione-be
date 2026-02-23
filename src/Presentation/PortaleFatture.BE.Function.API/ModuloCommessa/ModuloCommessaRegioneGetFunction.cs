using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.ModuloCommessa.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries;

namespace PortaleFatture.BE.Function.API.ModuloCommessa;

public class ModuloCommessaRegioneGetFunction(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<ModuloCommessaRegioneGetFunction>();

    [Function("ModuloCommessaRegioneGet")]
    public async Task<IEnumerable<ModuloCommessaRegioneDto>> RunAsync(
        [ActivityTrigger] ModuloCommessaRegioneInternalRequest ireq,
        FunctionContext context)
    {
        var mediator = context.InstanceServices.GetRequiredService<IMediator>();
        var localizer = context.InstanceServices.GetRequiredService<IStringLocalizer<Localization>>();
        
        int? idTipoContratto = ireq.Session!.IdTipoContratto;

        var authInfo = new AuthenticationInfo()
        {
            IdEnte = ireq.Session!.IdEnte,
            Prodotto = ireq.Session!.Prodotto,
            IdTipoContratto = idTipoContratto,
        };
 
        var regioni = await mediator.Send(new DatiRegioniModuloCommessaQueryGet(authInfo)
        {

        });

        if (regioni.IsNullNotAny()) 
            throw new Exception(localizer[MessageHelper.NotFound]); 

        var sse = ireq.Session;
        sse.Payload = regioni.Serialize();
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

        return regioni!;
    }
}