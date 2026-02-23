using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Api.Modules.SEND.DatiRel.Extensions;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.RegolareEsecuzione.Extensions;
using PortaleFatture.BE.Function.API.RegolareEsecuzione.Payload;

namespace PortaleFatture.BE.Function.API.RegolareEsecuzione;

public class RelTipologiaFattura(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<RelTipologiaFattura>();

    [Function("RelTipologiaFattura")]
    public async Task<IEnumerable<string>> RunAsync(
        [ActivityTrigger] RELTipologiaFatturaInternalRequest req,
        FunctionContext context)
    {
        var mediator = context.InstanceServices.GetRequiredService<IMediator>();
        //var instanceId = context.InvocationId;

        _logger.LogInformation("HTTP trigger function processed a request.");

        var authInfo = new AuthenticationInfo()
        {
            IdEnte = req.Session!.IdEnte,
            Profilo = req.Session.Profilo,
        };

        var tipologie = await mediator.Send(req!.Map(authInfo));
        if (tipologie.IsNullNotAny())
            throw new DomainException($"Non ci sono REL Tipoloiga Fatture registrate. {req.Session!.IdEnte!}");
        
        await LogResponse(mediator, context, req, tipologie!);

        return tipologie!;
    }

    private async Task LogResponse(IMediator mediator, FunctionContext context, RELTipologiaFatturaInternalRequest request, IEnumerable<string> response)
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