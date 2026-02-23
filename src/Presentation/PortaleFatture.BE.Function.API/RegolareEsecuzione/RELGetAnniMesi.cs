using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.RegolareEsecuzione.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries;

namespace PortaleFatture.BE.Function.API.RegolareEsecuzione; 

public class RELGetAnniMesi(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<RELGetAnniMesi>();

    [Function("RELGetAnniMesi")]
    public async Task<IEnumerable<RELAnniMesiResponse>?> RunAsync(
        [ActivityTrigger] RELAnniMesiInternalRequest req,
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

        var anni = await mediator.Send(new RelAnniQuery(authInfo));
        if (anni.IsNullNotAny())
            throw new DomainException($"Non ci sono REL registrate. {req.Session!.IdEnte!}");
        else
        {
            List<RELAnniMesiResponse> result = [];
            foreach (var anno in anni!)
            {
                var mesi = await mediator.Send(new RelMesiByIdEnteQuery(authInfo)
                {
                    Anno = anno
                });

                var response = mesi!.Select(x => new RELAnniMesiResponse()
                {
                    Anno = Convert.ToInt32(anno),
                    Mese = Convert.ToInt32(x),
                    Descrizione = Convert.ToInt32(x).GetMonth()
                });
                result.AddRange(response);
            }

            await LogResponse(mediator, context, req, result);
            return result;
        }
    }

    private async Task LogResponse(IMediator mediator, FunctionContext context, RELAnniMesiInternalRequest request, List<RELAnniMesiResponse> response)
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