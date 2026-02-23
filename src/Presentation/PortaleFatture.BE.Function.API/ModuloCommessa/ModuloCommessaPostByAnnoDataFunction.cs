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

public class ModuloCommessaPostByAnnoDataFunction(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<ModuloCommessaPostByAnnoDataFunction>();

    [Function("ModuloCommessaPostByAnnoData")]
    public async Task<IEnumerable<ModuloCommessaPrevisionaleTotaleDto>> RunAsync(
        [ActivityTrigger] ModuloCommessaGetByAnnoInternalRequest req, 
        FunctionContext context)
    { 
        var mediator = context.InstanceServices.GetRequiredService<IMediator>();
        var anno = req.Anno;

        var authInfo = new AuthenticationInfo()
        {
            IdEnte = req.Session!.IdEnte,
            Prodotto = req.Session!.Prodotto,
        };

        var modulo = await mediator.Send(new DatiModuloCommessaPrevisionaleQueryGetByAnno(authInfo)
        {
            AnnoValidita = anno,
        });

        if (modulo.IsNullNotAny())
            throw new NotFoundException(MessageHelper.NotFound);

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