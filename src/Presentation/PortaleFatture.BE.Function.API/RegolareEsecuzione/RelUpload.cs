using System.Net;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.DatiRel;
using PortaleFatture.BE.Core.Entities.SEND.DatiRel.Dto;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Function.API.Extensions;
using PortaleFatture.BE.Function.API.Models;
using PortaleFatture.BE.Function.API.RegolareEsecuzione.Payload;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Commands;
using PortaleFatture.BE.Infrastructure.Gateway.Storage;

namespace PortaleFatture.BE.Function.API.RegolareEsecuzione;

public static class RelUpload
{
    [Function("RelUpload")]
    [OpenApiOperation(operationId: "relupload", tags: ["Regolare Esecuzione"], Summary = "Permette di caricare la REL firmata con 'multipart/form-data'.", Description = "Permette di caricare la REL firmata xon  'multipart/form-data'.")]
    [OpenApiRequestBody(
    contentType: "multipart/form-data",
    bodyType: typeof(RelUploadRequest),
    Required = true,
    Description = "File PDF firmato e id testata. 'multipart/form-data'")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(bool), Description = "Returns OK.")]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Unauthorized, Description = MessageHelper.Unauthorized)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.Forbidden, Description = MessageHelper.Forbidden)]
    [OpenApiResponseWithoutBody(statusCode: HttpStatusCode.BadRequest, Description = MessageHelper.BadRequestLogging)]

    public static async Task<HttpResponseData> RunHttpStart(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "v1/rel/upload")] 
    HttpRequestData req,
    FunctionContext context)
    {
        var mediator = context.InstanceServices.GetRequiredService<IMediator>();
        var storageService = context.InstanceServices.GetRequiredService<IRelStorageService>();
    
        //var logResponse = context.Response();
        var session = context.GetSession();

        var authInfo = new AuthenticationInfo()
        {
            IdEnte = session!.IdEnte,
            Prodotto = session!.Prodotto,
            IdTipoContratto = session.IdTipoContratto,
            Profilo = session.Profilo,
        };

        var fileUploaded = context.GetItem<FileUploadDto>("UploadFile"); 
 
        var idTestata = fileUploaded!.FormTextValue;

        var contentType = req.Headers.GetValues("Content-Type").FirstOrDefault();
        if (contentType == null || !contentType.Contains("multipart/form-data"))
            return req.CreateResponse(HttpStatusCode.BadRequest);

        var boundary = HeaderUtilities.RemoveQuotes(MediaTypeHeaderValue.Parse(contentType).Boundary).Value;
        if (string.IsNullOrEmpty(boundary))
            return req.CreateResponse(HttpStatusCode.BadRequest); 
   
        var extension = Path.GetExtension(fileUploaded.FileName);

        if (!string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Estensione non valida. Atteso un PDF.");

        if (fileUploaded.FileBytes!.Length == 0)
            throw new ValidationException("File mancante o vuoto.");

        var key = RelTestataKey.Deserialize(idTestata!);
        if (authInfo.IdEnte != key.IdEnte)
            throw new DomainException("Wrong Id Ente");

        if (key.TipologiaFattura!.ToLower().Contains("semestrale")
            || key.TipologiaFattura!.ToLower().Contains("annuale"))
            throw new NotFoundException("File mancante o vuoto.");
 
        using var memoryStream = new MemoryStream(fileUploaded.FileBytes!);
        {
            await storageService.AddDocument(key, memoryStream);
            var result = await mediator.Send(new RelUploadCreateCommand(authInfo)
            {
                Anno = key.Anno,
                IdContratto = key.IdContratto,
                IdEnte = key.IdEnte,
                IdUtente = "API",
                Mese = key.Mese,
                TipologiaFattura = key.TipologiaFattura,
                DataEvento = DateTime.UtcNow.ItalianTime(),
                Azione = RelAzioneDocumento.Upload,
                Hash = memoryStream.ToArray().GetHashSHA512()
            });

            var resp = req.CreateResponse(HttpStatusCode.OK);
            await resp.WriteAsJsonAsync(result);
            return resp;
        }
    }
} 