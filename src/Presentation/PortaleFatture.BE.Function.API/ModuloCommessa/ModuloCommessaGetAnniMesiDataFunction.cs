using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.ModuloCommessa.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries;

namespace PortaleFatture.BE.Function.API.ModuloCommessa;

public class ModuloCommessaGetAnniMesiDataFunction(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<ModuloCommessaGetAnniMesiDataFunction>();

    [Function("ModuloCommessaGetAnniMesiData")]
    public async Task<IEnumerable<string>?> RunAsync(
        [ActivityTrigger] ModuloCommessaGetAnniMesiRequest req, 
        FunctionContext context)
    { 
        var mediator = context.InstanceServices.GetRequiredService<IMediator>();

        var authentication = new AuthenticationInfo()
        {
            IdEnte = req.Session!.IdEnte,
            Prodotto = req.Session!.Prodotto,
        };

        var anni = await mediator.Send(new DatiModuloCommessaGetAnni(authentication)); 

        var sse = req.Session;
        sse.Payload = anni.Serialize();
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
        return anni; 
    }
}