using System.IO;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Api.Infrastructure.Documenti;
using PortaleFatture.BE.Api.Modules.SEND.DatiFatturazioni.Payload.Request;
using PortaleFatture.BE.Api.Modules.SEND.DatiRel.Extensions;
using PortaleFatture.BE.Api.Modules.SEND.DatiRel.Payload.Request;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;
using PortaleFatture.BE.Api.Modules.SEND.Notifiche.Extensions;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.DatiRel;
using PortaleFatture.BE.Core.Entities.SEND.DatiRel.Dto;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiRel.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;
using PortaleFatture.BE.Infrastructure.Gateway.Storage;
using static Microsoft.AspNetCore.Http.TypedResults;

namespace PortaleFatture.BE.Api.Modules.Notifiche;

public partial class RelModule
{
    #region pagoPA

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> GetRelNonFatturateExcelAsync(
    HttpContext context, 
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
        {
            var authInfo = context.GetAuthInfo();
            var rel = await handler.Send(new RelNonFatturateQuery(authInfo));
            if (rel == null || !rel!.Any())
                return NotFound();
            var mime = "application/vnd.ms-excel";
            var filename = $"{Guid.NewGuid()}.xlsx";

            var dataSet = rel.FillOneSheetv2();
            var content = dataSet.ToExcel();
            var result = new DisposableStreamResult(content, mime)
            {
                FileDownloadName = filename
            };
            return Results.Stream(result.FileStream, result.ContentType, result.FileDownloadName);
        }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<string>>, NotFound>> PostTipologiaFatturaPagoPAAsync(
    HttpContext context,
    [FromBody] RelTipologiaFatturaPagoPARequest? request,
    [FromServices] IMediator handler,
    [FromServices] IStringLocalizer<Localization> localizer)
        {
            var authInfo = context.GetAuthInfo();

            var tipologie = await handler.Send(request!.Map(authInfo));
            if (tipologie == null || tipologie.Count() == 0)
                return NotFound();
            return Ok(tipologie);
        }


    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> GetDownloadPagoPAAsync(
    HttpContext context,
    [FromRoute] string? id,
    [FromQuery] string? tipo,
    [FromServices] IDocumentBuilder documentBuilder,
    [FromServices] IMediator handler,
    [FromServices] IStringLocalizer<Localization> localizer)
    {
        var authInfo = context.GetAuthInfo();
        var request = new RelTestataByIdRequest()
        {
            IdTestata = id
        };
        var rel = await handler.Send(request.Map(authInfo));
        if (rel == null 
            || rel.TipologiaFattura!.ToLower().Contains("var")
            || rel.TipologiaFattura!.ToLower().Contains("semestrale")
            || rel.TipologiaFattura!.ToLower().Contains("annuale"))
            return NotFound();

        if (tipo != null && tipo == "pdf")
        {
            var bytes = documentBuilder.CreateModuloRelPdf(rel.Map()!);
            var filename = $"{Guid.NewGuid()}.pdf";
            var mime = "application/pdf";
            return Results.File(bytes!, mime, filename);
        }
        else
        {
            var content = documentBuilder.CreateModuloRelHtml(rel.Map()!);
            var mime = "text/html";
            return Results.Text(content!, mime);
        }
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<int?>, NotFound>> PostPagoPAFatturabileAsync(
    HttpContext context,
    [FromBody] RelFatturabileByIdEntiRequest? request,
    [FromServices] IMediator handler,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IRelStorageService storageService)
    {
        var authInfo = context.GetAuthInfo();
        var fatturabile = await handler.Send(request!.Map(authInfo));
        if (fatturabile == null)
            return NotFound();
        return Ok(fatturabile);
    }


    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<RelUpload>>, NotFound>> PostPagoPADownloadLogAsync(
    HttpContext context,
    [FromBody] RelUploadByIdRequest? request,
    [FromServices] IMediator handler,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IRelStorageService storageService)
    {
        var authInfo = context.GetAuthInfo();
        authInfo.IdEnte = request!.IdEnte;
        var upload = await handler.Send(request!.Map(authInfo));
        if (upload == null)
            return NotFound();
        return Ok(upload);
    }



    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> PostPagoPADownloadFirmaZipAsync(
    HttpContext context,
    [FromBody] RelTestataRicercaRequestPagoPA request,
    [FromServices] IMediator handler,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IRelStorageService storageService)
    {
        var authInfo = context.GetAuthInfo();
        if (request.Caricata == null || request.Caricata.Value == 0)
            return NotFound();

        var rels = await handler.Send(request.Map(authInfo, null, null));
        if (rels == null || rels.Count == 0)
            return NotFound();
        var (bytes, list) = await storageService.ReadZip(rels);
        await handler.Send(rels.RelTestate.Map(list, authInfo));

        var mime = "application/zip";
        var filename = $"{Guid.NewGuid()}.zip";
        return Results.File(bytes!, mime, filename);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> GetPagoPADownloadFirmaAsync(
    HttpContext context,
    [FromRoute] string? id,
    [FromQuery] bool? binary,
    [FromServices] IMediator handler,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IRelStorageService storageService)
    {
        var authInfo = context.GetAuthInfo();
        var key = RelTestataKey.Deserialize(id!);

        var request = new RelTestataByIdRequest()
        {
            IdTestata = id
        };
        var rel = await handler.Send(request.Map(authInfo));
        if (rel == null)
            return NotFound();

        var bytes = await storageService.ReadBytes(key);
        await handler.Send(new RelDownloadCommand(authInfo)
        {
            Anno = key.Anno,
            IdContratto = key.IdContratto,
            IdEnte = key.IdEnte,
            IdUtente = authInfo.Id,
            Mese = key.Mese,
            TipologiaFattura = key.TipologiaFattura,
            DataEvento = DateTime.UtcNow.ItalianTime(),
            Azione = RelAzioneDocumento.Download,
            Hash = bytes.GetHashSHA512()
        });
        var mime = "application/pdf";
        var filename = $"{Guid.NewGuid()}.pdf";

        if (binary == null)
            return Ok(new DocumentDto() { Documento = Convert.ToBase64String(bytes) });
        else
            return Results.File(bytes!, mime, filename);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<RelTestataDto>, NotFound>> GetPagoPARelTestataByRicercaAsync(
    HttpContext context,
    [FromBody] RelTestataRicercaRequestPagoPA request,
    [FromQuery] int page,
    [FromQuery] int pageSize,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var rel = await handler.Send(request.Map(authInfo, page, pageSize));
        if (rel == null || rel.Count == 0)
            return NotFound();
        return Ok(rel);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [Authorize()]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> GetPagoPARelRicercaDocumentAsync(
    HttpContext context,
    [FromBody] RelTestataRicercaRequestPagoPA request,
    [FromQuery] bool? binary,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var rels = await handler.Send(request.Map(authInfo, null, null));
        if (rels == null || rels.Count == 0)
            return NotFound();

        var mime = "application/vnd.ms-excel";
        var filename = $"{Guid.NewGuid()}.xlsx";

        var dataSet = rels.RelTestate!.FillOneSheetWithTotalsRel();
        var content = dataSet.ToExcel();
        if (binary == null)
            return Ok(new DocumentDto() { Documento = Convert.ToBase64String(content.ToArray()) });
        else
            return Results.File(content!, mime, filename);
    }


    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [Authorize()]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> GetPagoPAQuadraturaRicercaDocumentAsync(
    HttpContext context,
    [FromBody] RelTestataRicercaRequestPagoPA request,
    [FromQuery] bool? binary,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var rels = await handler.Send(request.Map2(authInfo, null, null));
        if (rels == null || rels.Count == 0)
            return NotFound();

        var mime = "application/vnd.ms-excel";
        var filename = $"{Guid.NewGuid()}.xlsx";

        var dataSet = rels.Quadratura!.FillOneSheetWithTotalsRel();
        var content = dataSet.ToExcel();
        if (binary == null)
            return Ok(new DocumentDto() { Documento = Convert.ToBase64String(content.ToArray()) });
        else
            return Results.File(content!, mime, filename);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [Authorize()]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> GetPagoPARelRigheDocumentAsync(
    HttpContext context,
    [FromRoute] string? id,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler,
    [FromQuery] bool? binary = null)
    {
        var authInfo = context.GetAuthInfo();
        var idEnte = id!.Split("_")[0];
        authInfo.IdEnte = idEnte;
        var request = new RelRigheByIdRequest()
        {
            IdTestata = id
        };

        var rels = await handler.Send(request.Map(authInfo));
        if (rels.IsNullNotAny())
            return NotFound();

        var stream = await rels!.ToStream<RigheRelDto, RigheRelDtoPagoPAMap>();
        var filename = $"{Guid.NewGuid()}.csv";
        var mimeCsv = "text/csv";
        stream.Position = 0;
        return Results.Stream(stream, mimeCsv, filename);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<RelTestataDettaglioDto>, NotFound>> GetPagoPARelTestataByIdAsync(
    HttpContext context,
    [FromRoute] string? id,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var idEnte = id!.Split("_")[0];
        authInfo.IdEnte = idEnte;
        var request = new RelTestataByIdRequest()
        {
            IdTestata = id
        };

        var rel = await handler.Send(request.Map(authInfo));
        if (rel == null)
            return NotFound();
        return Ok(rel);
    }
    #endregion

    #region selfcare

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<string>>, NotFound>> PostTipologiaFatturaAsync(
    HttpContext context,
    [FromBody] RelTipologiaFatturaRequest? request,
    [FromServices] IMediator handler,
    [FromServices] IStringLocalizer<Localization> localizer)
    {
        var authInfo = context.GetAuthInfo();

        var tipologie = await handler.Send(request!.Map(authInfo));
        if (tipologie == null || tipologie.Count() == 0)
            return NotFound();
        return Ok(tipologie);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<RelUpload>>, NotFound>> PostDownloadLogAsync(
    HttpContext context,
    [FromBody] RelUploadByIdRequest? request,
    [FromServices] IMediator handler,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IRelStorageService storageService)
    {
        var authInfo = context.GetAuthInfo();

        if (request == null
            || request.TipologiaFattura!.ToLower().Contains("var")
            || request.TipologiaFattura!.ToLower().Contains("semestrale")
            || request.TipologiaFattura!.ToLower().Contains("annuale"))
            return NotFound();

        var upload = await handler.Send(request!.Map(authInfo));
        if (upload == null || upload.Count() == 0)
            return NotFound();
        return Ok(upload);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> GetDownloadFirmaAsync(
    HttpContext context,
    [FromRoute] string? id,
    [FromQuery] bool? binary,
    [FromServices] IMediator handler,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IRelStorageService storageService)
    {
        var authInfo = context.GetAuthInfo();
        var key = RelTestataKey.Deserialize(id!);
        if (authInfo.IdEnte != key.IdEnte)
            throw new DomainException("Wrong Id Ente");

        var request = new RelTestataByIdRequest()
        {
            IdTestata = id
        };
        var rel = await handler.Send(request.Map(authInfo));
        if (rel == null
            || rel.Caricata != 1
            || rel.TipologiaFattura!.ToLower().Contains("var")
            || rel.TipologiaFattura!.ToLower().Contains("semestrale")
            || rel.TipologiaFattura!.ToLower().Contains("annuale"))
            return NotFound(); 

        var bytes = await storageService.ReadBytes(key);
        await handler.Send(new RelDownloadCommand(authInfo)
        {
            Anno = key.Anno,
            IdContratto = key.IdContratto,
            IdEnte = key.IdEnte,
            IdUtente = authInfo.Id,
            Mese = key.Mese,
            TipologiaFattura = key.TipologiaFattura,
            DataEvento = DateTime.UtcNow.ItalianTime(),
            Azione = RelAzioneDocumento.Download,
            Hash = bytes.GetHashSHA512()
        });
        var mime = "application/pdf";
        var filename = $"{Guid.NewGuid()}.pdf";

        if (binary == null)
            return Ok(new DocumentDto() { Documento = Convert.ToBase64String(bytes) });
        else
            return Results.File(bytes!, mime, filename);
    }

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<bool>, NotFound>> PostUploadFirmaAsync(
    HttpContext context,
    [FromRoute] string? id,
    [FromServices] IMediator handler,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] ILogger<RelModule> logger,
    [FromServices] IRelStorageService storageService)
    {
        var form = await context.Request.ReadFormAsync();
        var file = form.Files.Where(s => s.Name != "file" || s.Name != "pdf").FirstOrDefault();
        if (file == null)
            throw new ValidationException("Estensione sbagliata. Passare un pdf valido.");
        var authInfo = context.GetAuthInfo();
        var size = file!.Length;
        if (size > 0)
        {
            var key = RelTestataKey.Deserialize(id!);
            if (authInfo.IdEnte != key.IdEnte)
                throw new DomainException("Wrong Id Ente");

            if (key.TipologiaFattura!.ToLower().Contains("semestrale")
                || key.TipologiaFattura!.ToLower().Contains("annuale"))
                return NotFound();

            var fileName = file.FileName;
            var extension = Path.GetExtension(fileName);
            if (extension != ".pdf")
            {
                var msg = $"Estensione sbagliata. Passare un pdf valido per la firma. {fileName} per Ente: {authInfo.IdEnte}";
                logger.LogError(msg);
                throw new ValidationException("Estensione sbagliata. Passare un pdf valido.");
            }

            bool? result = false;
            using var memoryStream = new MemoryStream();
            {
                await file.CopyToAsync(memoryStream);
                await storageService.AddDocument(key, memoryStream);
                result = await handler.Send(new RelUploadCreateCommand(authInfo)
                {
                    Anno = key.Anno,
                    IdContratto = key.IdContratto,
                    IdEnte = key.IdEnte,
                    IdUtente = authInfo.Id,
                    Mese = key.Mese,
                    TipologiaFattura = key.TipologiaFattura,
                    DataEvento = DateTime.UtcNow.ItalianTime(),
                    Azione = RelAzioneDocumento.Upload,
                    Hash = memoryStream.ToArray().GetHashSHA512()
                });
            }
            return Ok(result.Value);
        }
        return Ok(false);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<RelTestataDto>, NotFound>> GetRelTestataByRicercaAsync(
    HttpContext context,
    [FromBody] RelTestataRicercaRequest request,
    [FromQuery] int page,
    [FromQuery] int pageSize,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {

        var authInfo = context.GetAuthInfo();
        var rel = await handler.Send(request.Map(authInfo, page, pageSize));
        if (rel == null || rel.Count == 0)
            return NotFound();
        return Ok(rel);

    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<RelTestataDettaglioDto>, NotFound>> GetRelTestataByIdAsync(
    HttpContext context,
    [FromRoute] string? id,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var request = new RelTestataByIdRequest()
        {
            IdTestata = id
        };

        var rel = await handler.Send(request.Map(authInfo));
        if (rel == null)
            return NotFound();
        return Ok(rel);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> DownloadRelDocumentoAsync(
    HttpContext context,
    [FromRoute] string? id,
    [FromQuery] string? tipo,
    [FromServices] IDocumentBuilder documentBuilder,
    [FromServices] IMediator handler,
    [FromServices] IStringLocalizer<Localization> localizer
)
    {
        var authInfo = context.GetAuthInfo();
        var request = new RelTestataByIdRequest()
        {
            IdTestata = id
        };
        var rel = await handler.Send(request.Map(authInfo));
        if (rel == null
            || rel.TipologiaFattura!.ToLower().Contains("var")
            || rel.TipologiaFattura!.ToLower().Contains("semestrale")
            || rel.TipologiaFattura!.ToLower().Contains("annuale"))
            return NotFound();

        if (tipo != null && tipo == "pdf")
        {
            var bytes = documentBuilder.CreateModuloRelPdf(rel.Map()!);
            var filename = $"{Guid.NewGuid()}.pdf";
            var mime = "application/pdf";
            return Results.File(bytes!, mime, filename);
        }
        else
        {
            var content = documentBuilder.CreateModuloRelHtml(rel.Map()!);
            var mime = "text/html";
            return Results.Text(content!, mime);
        }
    }


    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [Authorize()]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> GetRelRigheDocumentAsync(
    HttpContext context,
    [FromRoute] string? id,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler,
    [FromQuery] bool? binary = null)
    {
        var authInfo = context.GetAuthInfo();
        var request = new RelRigheByIdRequest()
        {
            IdTestata = id
        };

        var rels = await handler.Send(request.Map(authInfo));
        if (rels == null || !rels.Any())
            return NotFound();

        var stream = await rels!.ToStream<RigheRelDto, RigheRelDtoEnteMap>();
        if (stream.Length == 0)
            return NotFound();

        var filename = $"{Guid.NewGuid()}.csv";
        var mimeCsv = "text/csv";

        stream.Position = 0;
        return Results.Stream(stream, mimeCsv, filename);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [Authorize()]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> GetRelRicercaDocumentAsync(
    HttpContext context,
    [FromBody] RelTestataRicercaRequest request,
    [FromQuery] bool? binary,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var rels = await handler.Send(request.Map(authInfo, null, null));
        if (rels == null || rels.Count == 0)
            return NotFound();

        var mime = "application/vnd.ms-excel";
        var filename = $"{Guid.NewGuid()}.xlsx";

        var dataSet = rels.RelTestate!.FillOneSheet();
        var content = dataSet.ToExcel();
        if (binary == null)
            return Ok(new DocumentDto() { Documento = Convert.ToBase64String(content.ToArray()) });
        else
            return Results.File(content!, mime, filename);
    }
    #endregion
}