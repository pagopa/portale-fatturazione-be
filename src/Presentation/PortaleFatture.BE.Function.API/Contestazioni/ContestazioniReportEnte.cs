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


public class ContestazioniReportEnte(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<ContestazioniReportEnte>();

    [Function("ContestazioniReportEnte")]
    public async Task<ReportContestazioniList?> RunAsync(
        [ActivityTrigger] ContestazioniReportEntePagingInternalRequest req,
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

        var reports = await mediator.Send(new ContestazioniReportQuery(authInfo)
        {
            Anno = req.Anno,
            Mese = req.Mese,
            IdEnti = [authInfo.IdEnte!],
            IdTipologiaReports = req.IdTipologiaReports,
            Page = req.Page,
            Size = req.PageSize
        });

        if (reports == null || reports.Count == 0)
            throw new DomainException($"Non ci sono report registrati. {req.Session!.IdEnte!}");

        await LogResponse(mediator, context, req, reports); 

        return reports;
    }

    private async Task LogResponse(IMediator mediator, FunctionContext context, ContestazioniReportEntePagingInternalRequest request, ReportContestazioniList? response)
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