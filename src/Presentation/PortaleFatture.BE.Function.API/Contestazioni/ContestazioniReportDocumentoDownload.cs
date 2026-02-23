using System.Text;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Api.Infrastructure.Documenti;
using PortaleFatture.BE.Api.Modules.SEND.Notifiche.Extensions;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Contestazioni.Payload;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Services;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;

namespace PortaleFatture.BE.Function.API.Contestazioni;

public class ContestazioniReportDocumentoDownload(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<ContestazioniReportDocumentoDownload>();

    [Function("ContestazioniReportDocumentoDownload")]
    public async Task<ContestazioniReportDocumentoDownloadResponse> RunAsync(
        [ActivityTrigger] ContestazioniReportDocumentoDownloadInternalRequest req,
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

        var report = await mediator.Send(new ContestazioniReportStepQuery(authInfo)
        {
            IdReport = (int)req.IdReport,
        });

        if (report?.Steps == null || !report.Steps.Any())
            throw new DomainException($"Non ci sono step associati all'id report: {req.IdReport} id ente: {req.Session.IdEnte}");

        if (string.IsNullOrEmpty(req.TipoReport))
            req.TipoReport = "json";

        var values = report.Steps.Map(storageService);

        var filename = $"{req.IdReport}_{req.Session.Id}";

        string? mime;
        byte[] bytes;
        if (req.TipoReport.Equals("json", StringComparison.CurrentCultureIgnoreCase))
        {
            mime = "application/json";
            var json = values.Serialize();
            bytes = Encoding.UTF8.GetBytes(json); 
            filename += ".json";
        }
        else
        {
            if (values?.Steps == null || !values.Steps.Any())
                throw new DomainException($"Non ci sono step associati all'id report: {req.IdReport} id ente: {req.Session.IdEnte}");

            IEnumerable<ReportContestazioneStepsWithLinkDto> r = values.Steps;
            var stream = await r.ToStream<ReportContestazioneStepsWithLinkDto, ReportContestazioneStepsDtoMap>();
            mime = "text/csv";
            stream.Position = 0;
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            bytes = ms.ToArray();
            filename += ".csv";
        }

        var sasToken = string.Empty;
        var file = $"temp/{req.Session!.IdEnte}/{instanceId}";
        using (var stream = new MemoryStream(bytes))
        {
            var result = await storageService!.UploadStreamAsync(stream, req.Session!.IdEnte!, instanceId!, filename, mime, "temp");
            sasToken = storageService.GetSASToken(file, filename);
        }

        await LogResponse(mediator, context, req, report);

        return new ContestazioniReportDocumentoDownloadResponse()
        {
            LinkDocumento = sasToken
        };
    }

    private async Task LogResponse(IMediator mediator, FunctionContext context, ContestazioniReportDocumentoDownloadInternalRequest request, ReportContestazioneByIdDto response)
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