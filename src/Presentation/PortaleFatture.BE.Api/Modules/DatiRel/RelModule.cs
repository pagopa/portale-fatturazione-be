using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Api.Modules.DatiFatturazioni.Payload.Request;
using PortaleFatture.BE.Api.Modules.DatiRel.Extensions;
using PortaleFatture.BE.Api.Modules.Notifiche.Extensions;
using PortaleFatture.BE.Api.Modules.Notifiche.Payload.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiRel;
using PortaleFatture.BE.Core.Entities.DatiRel.Dto;
using PortaleFatture.BE.Core.Entities.Notifiche;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;
using PortaleFatture.BE.Infrastructure.Common.DatiRel.Commands;
using PortaleFatture.BE.Infrastructure.Common.DatiRel.Dto;
using PortaleFatture.BE.Infrastructure.Common.Documenti;
using PortaleFatture.BE.Infrastructure.Common.Documenti.Common;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Gateway.Storage;
using PortaleFatture.BE.Infrastructure.Common.DatiRel.Extensions;
using static Microsoft.AspNetCore.Http.TypedResults; 
using CsvHelper;
using System.Globalization;

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
        if (rel == null)
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
    [FromQuery] bool? binary,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var idEnte = id!.Split("_")[0];
        authInfo.IdEnte = idEnte;
        var request = new RelRigheByIdRequest()
        {
            IdTestata = id
        };

        var rels = await handler.Send(request.Map(authInfo));
        if (rels == null || rels.Count() == 0)
            return NotFound();

        var mime = "application/vnd.ms-excel";
        var filename = $"{Guid.NewGuid()}.xlsx";

        var dataSet = rels.FillOneSheet();
        var content = dataSet.ToExcel();
        if (binary == null)
        {
            mime = "text/csv";
            filename = $"{Guid.NewGuid()}.csv";
            byte[] data;
            using (var stream = new MemoryStream())
            using (TextWriter textWriter = new StreamWriter(stream))
            using (var csv = new CsvWriter(textWriter, new CultureInfo("it-IT")))
            {
                csv.WriteRecords(rels!);
                textWriter.Flush();
                data = stream.ToArray();
            }
            return Results.File(data!, mime, filename);
        }
        else
            return Results.File(content!, mime, filename);
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
    private async Task<Results<Ok<IEnumerable<RelUpload>>, NotFound>> PostDownloadLogAsync(
    HttpContext context,
    [FromBody] RelUploadByIdRequest? request,
    [FromServices] IMediator handler,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IRelStorageService storageService)
    {
        var authInfo = context.GetAuthInfo();

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
        if (rel == null || rel.Caricata != 1)
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
    [FromServices] IRelStorageService storageService)
    {
        var form = await context.Request.ReadFormAsync();
        var file = form.Files.Where(s => s.Name != "file" || s.Name != "pdf").FirstOrDefault();
        if (file == null)
            throw new PortaleFatture.BE.Core.Exceptions.ValidationException("Estensione sbagliata. Passare un pdf valido.");
        var authInfo = context.GetAuthInfo();
        var size = file!.Length;
        if (size > 0)
        {
            var key = RelTestataKey.Deserialize(id!);
            if (authInfo.IdEnte != key.IdEnte)
                throw new DomainException("Wrong Id Ente");
            var fileName = file.FileName;
            var extension = Path.GetExtension(fileName);
            if (extension != ".pdf")
                throw new PortaleFatture.BE.Core.Exceptions.ValidationException("Estensione sbagliata. Passare un pdf valido.");
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
        //return NotFound(); // da eliminare   IMPEDIMENT #328
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
        if (rel == null)
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
    [FromQuery] bool? binary,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var request = new RelRigheByIdRequest()
        {
            IdTestata = id
        };

        var rels = await handler.Send(request.Map(authInfo));
        if (rels == null || rels.Count() == 0)
            return NotFound();

        var mime = "application/vnd.ms-excel";
        var filename = $"{Guid.NewGuid()}.xlsx";

        var dataSet = rels.FillOneSheet();
        var content = dataSet.ToExcel();
        if (binary == null)
        {
            mime = "text/csv";
            filename = $"{Guid.NewGuid()}.csv";
            byte[] data;
            using (var stream = new MemoryStream())
            using (TextWriter textWriter = new StreamWriter(stream))
            using (var csv = new CsvWriter(textWriter, new CultureInfo("it-IT")))
            {
                csv.WriteRecords(rels!);
                textWriter.Flush();
                data = stream.ToArray();
            }
            return Results.File(data!, mime, filename);
        } 
        else
            return Results.File(content!, mime, filename);
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
        //return NotFound(); // da eliminare   IMPEDIMENT #328
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