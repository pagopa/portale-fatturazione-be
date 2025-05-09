using System.Net;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.ModuloCommessa.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries;

namespace PortaleFatture.BE.Function.API.ModuloCommessa;

public class ModuloCommessaGetAnniMesiDataFunction(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<ModuloCommessaGetAnniMesiDataFunction>();

    [Function("ModuloCommessaGetAnniMesiData")]
    public async Task<IEnumerable<ModuloCommessaAnnoMeseDto>?> RunAsync(
        [ActivityTrigger] ModuloCommessaGetAnniMesiRequest req, 
        FunctionContext context)
    { 
        var mediator = context.InstanceServices.GetRequiredService<IMediator>();

        var authentication = new AuthenticationInfo()
        {
            IdEnte = req.Session!.FkIdEnte,
            Prodotto = req.Prodotto,
        };

        var queryCommand = new DatiModuloCommessaGetAnniMesi(authentication) { };
        var values = await mediator.Send(queryCommand); 

        var sse = req.Session;
        sse.Payload = values.Serialize();
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
        return values; 
    }
}