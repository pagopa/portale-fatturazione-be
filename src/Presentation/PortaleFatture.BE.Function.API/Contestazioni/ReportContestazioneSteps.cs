using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Contestazioni.Payload;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries;

namespace PortaleFatture.BE.Function.API.Contestazioni;


public class ReportContestazioneSteps(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<ReportContestazioneSteps>();

    [Function("ReportContestazioneSteps")]
    public async Task<IEnumerable<ReportContestazioneStepsDto>?> RunAsync(
        [ActivityTrigger] ReportContestazioneStepsInternalRequest req,
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

        var report = await mediator.Send(new ContestazioniReportStepQuery(authInfo)
        {
            IdReport = req.IdReport
        });

        if (report == null || report.Steps.IsNullNotAny())
            throw new DomainException($"Non ci sono step associati all'id report: {req.IdReport} id ente: {req.Session.IdEnte}"); 
       

        await LogResponse(mediator, context, req, report.Steps);

        return report.Steps;
    }

    private async Task LogResponse(IMediator mediator, FunctionContext context, ReportContestazioneStepsInternalRequest request, IEnumerable<ReportContestazioneStepsDto>? response)
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