using System.Text;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Api.Modules.SEND.DatiRel.Extensions;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.DatiRel;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.RegolareEsecuzione.Extensions;
using PortaleFatture.BE.Function.API.RegolareEsecuzione.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Services;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti;

namespace PortaleFatture.BE.Function.API.RegolareEsecuzione;

public class RelDownload(ILoggerFactory loggerFactory)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<RelDownload>();

    [Function("RelDownload")]
    public async Task<RELDownloadResponse> RunAsync(
        [ActivityTrigger] RELDownloadInternalRequest req,
        FunctionContext context)
    {
        var mediator = context.InstanceServices.GetRequiredService<IMediator>();
        var documentBuilder = context.InstanceServices.GetRequiredService<IDocumentBuilder>();
        var blobStorageRelDownload = context.InstanceServices.GetRequiredService<IBlobStorageRelDownload>(); 
        var instanceId = context.InvocationId;

        _logger.LogInformation("HTTP trigger function processed a request.");

        var authInfo = new AuthenticationInfo()
        {
            IdEnte = req.Session!.IdEnte,
            Profilo = req.Session.Profilo,
        };

        var key = RelTestataKey.Deserialize(req.IdTestata!);
        if (authInfo.IdEnte != key.IdEnte)
            throw new DomainException("Wrong Id Ente");

        var request = req.Map();

        var rel = await mediator.Send(request.Map(authInfo));
        if (rel == null 
            || rel.TipologiaFattura!.ToLower().Contains("var")
            || rel.TipologiaFattura!.ToLower().Contains("semestrale")
            || rel.TipologiaFattura!.ToLower().Contains("annuale"))
        {
            _logger.LogInformation($"{LoggerHelper.PREFIX}{MessageHelper.NotFound}{LoggerHelper.PIPE}{req.Session!.IdEnte.Serialize()}");
            throw new DomainException(MessageHelper.NotFound);
        }

        string? mime;
        byte[]? bytes;
        string? sasToken;

        string? filename;
        if (req.TypeDocument != null && req.TypeDocument == TipoDocumentoREL.PDF)
        {
            bytes = documentBuilder.CreateModuloRelPdf(rel.Map()!);
            filename = $"{Guid.NewGuid()}.pdf";
            mime = "application/pdf";
        }
        else
        {
            var content = documentBuilder.CreateModuloRelHtml(rel.Map()!);
            filename = $"{Guid.NewGuid()}.html";
            bytes = Encoding.UTF8.GetBytes(content!);
            mime = "text/html";
        }

        using (var stream = new MemoryStream(bytes))
        {
            var result = await blobStorageRelDownload!.UploadStreamAsync(stream, rel.IdEnte!, Convert.ToInt32(rel.Anno), Convert.ToInt32(rel.Mese), instanceId!, filename, mime);
            sasToken = blobStorageRelDownload.GetSasToken(rel.IdEnte!, Convert.ToInt32(rel.Anno), Convert.ToInt32(rel.Mese), instanceId!, filename);
        } 
        await LogResponse(mediator, context, req, $"Ok document {sasToken}");

        return new RELDownloadResponse()
        {
            LinkDocumento = sasToken
        };
    }

    private async Task LogResponse(IMediator mediator, FunctionContext context, RELDownloadInternalRequest request, string response)
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