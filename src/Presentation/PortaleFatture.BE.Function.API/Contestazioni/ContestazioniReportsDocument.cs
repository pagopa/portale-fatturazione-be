using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Contestazioni.Payload;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.Notifiche.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Services;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;

namespace PortaleFatture.BE.Function.API.Contestazioni;


public class ContestazioniReportsDocument(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<ContestazioniReportsDocument>();

    [Function("ContestazioniReportsDocument")]
    public async Task<string?> RunAsync(
        [ActivityTrigger] ContestazioniReportsDocumentInternalRequest req,
        FunctionContext context)
    {
        var mediator = context.InstanceServices.GetRequiredService<IMediator>();
        var storageService = context.InstanceServices.GetRequiredService<IContestazioniStorageService>();
        var instanceId = context.InvocationId;

        _logger.LogInformation("HTTP trigger function processed a request.");

        var authInfo = new AuthenticationInfo()
        {
            IdEnte = req.Session!.IdEnte,
            Profilo = req.Session.Profilo,
        };

        var idReport = req.IdReport;
        var step = req.Step;

        var report = await mediator.Send(new ContestazioniReportStepQuery(authInfo)
        {
            IdReport = idReport,
        });


        if (report is null || report!.Steps.IsNullNotAny())
            throw new DomainException($"Non ci sono report registrati. {req.Session!.IdEnte!}");

        var result = string.Empty;
        if (step.HasValue)
        {
            var sstep = report!.Steps!.Where(x => x.Step == step).FirstOrDefault() ??
                throw new DomainException($"Non ci sono step registrati. {req.Session!.IdEnte!}");
            result = storageService.GetSASToken(sstep.Link!, sstep.NomeDocumento!);
            await LogResponse(mediator, context, req, result);
        }
        else
        { 
            result = storageService.GetSASToken(report.ReportContestazione!.LinkDocumento!, report.ReportContestazione!.NomeDocumento!);
            await LogResponse(mediator, context, req, result);
        }

        return result;
    }

    private async Task LogResponse(IMediator mediator, FunctionContext context, ContestazioniReportsDocumentInternalRequest request, string? response)
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