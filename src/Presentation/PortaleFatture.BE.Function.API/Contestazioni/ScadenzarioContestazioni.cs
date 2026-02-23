using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Contestazioni.Payload;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Scadenziari.Queries;

namespace PortaleFatture.BE.Function.API.Contestazioni;

public class ScadenzarioContestazioni(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<ScadenzarioContestazioni>();

    [Function("ScadenzarioContestazioni")]
    public async Task<IEnumerable<CalendarioContestazioniExtendedResponse>?> RunAsync(
        [ActivityTrigger] ScadenziarioContestazioniInternalRequest req,
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

        var scadenziario = await mediator.Send(new CalendarioContestazioneQueryGetAll(authInfo));
        if (scadenziario.IsNullNotAny())
            throw new DomainException($"Non ci sono notifiche registrate. {req.Session!.IdEnte!}");

        await LogResponse(mediator, context, req, scadenziario.Mapperv2());
        var calendari = scadenziario.Mapperv2();

        return calendari.ToExtendedResponses(); 
    }

    private async Task LogResponse(IMediator mediator, FunctionContext context, ScadenziarioContestazioniInternalRequest request, IEnumerable<CalendarioContestazioniStatoResponse> response)
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