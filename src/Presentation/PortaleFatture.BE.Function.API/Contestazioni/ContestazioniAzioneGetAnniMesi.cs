using System.Linq;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.Notifiche.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries;

namespace PortaleFatture.BE.Function.API.Contestazioni;


public class ContestazioniAzioneGetAnniMesi(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<ContestazioniAzioneGetAnniMesi>();

    [Function("ContestazioniAzioneGetAnniMesi")]
    public async Task<IEnumerable<NotificheAnniMesiResponse>?> RunAsync(
        [ActivityTrigger] NotificheAnniMesiInternalRequest req,
        FunctionContext context)
    {
        var mediator = context.InstanceServices.GetRequiredService<IMediator>();
        var instanceId = context.InvocationId;

        _logger.LogInformation("HTTP trigger function processed a request.");

        var authInfo = new AuthenticationInfo()
        {
            IdEnte = req.Session!.IdEnte,
            Profilo = req.Session.Profilo,
        };

        var annimesi = await mediator.Send(new ContestazioniEnteAnniMesiQuery(authInfo));
        if (annimesi.IsNullNotAny())
            throw new DomainException($"Non ci sono notifiche da contestare. {req.Session!.IdEnte!}");
        else
        {
            List<NotificheAnniMesiResponse> result = [];
            var anni = annimesi!.Select(x=>x.AnnoContestazione).Distinct().ToList();
            foreach (var anno in anni!)
            { 
                var response = annimesi!.Where(x=> x.AnnoContestazione == anno).Select(x => new NotificheAnniMesiResponse()
                {
                    Anno = Convert.ToInt32(x.AnnoContestazione),
                    Mese = Convert.ToInt32(x.MeseContestazione),
                    Descrizione = Convert.ToInt32(x.MeseContestazione).GetMonth()
                });
                result.AddRange(response);
            }

            await LogResponse(mediator, context, req, result);
            return result.OrderByDescending(x=>x.Anno).OrderByDescending(x=>x.Mese);
        }
    }

    private async Task LogResponse(IMediator mediator, FunctionContext context, NotificheAnniMesiInternalRequest request, List<NotificheAnniMesiResponse> response)
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