using System.Data;
using System.Globalization;
using CsvHelper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Api.Modules.Asseverazione.Extensions;
using PortaleFatture.BE.Api.Modules.DatiFatturazioni.Payload.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Asseverazione.Dto;
using PortaleFatture.BE.Infrastructure.Common.Asseverazione.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Documenti.Common;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Common.Notifiche.Queries;
using static Microsoft.AspNetCore.Http.TypedResults;


namespace PortaleFatture.BE.Api.Modules.Asseverazione;

public partial class AsseverazioneModule
{
    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<bool>, NotFound>> PostUploadAsync(
    HttpContext context,
    [FromServices] IMediator handler,
    [FromServices] IStringLocalizer<Localization> localizer)
    {
        var form = await context.Request.ReadFormAsync();
        var file = form.Files.Where(s => s.Name != "file" || s.Name != "xlsx").FirstOrDefault() ??
            throw new Core.Exceptions.ValidationException("Estensione sbagliata. Passare un xlsx valido.");
        var authInfo = context.GetAuthInfo();
        var size = file!.Length;
        if (size > 0)
        { 
            var fileName = file.FileName;
            var extension = Path.GetExtension(fileName);
            if (extension != ".xlsx")
                throw new Core.Exceptions.ValidationException("Estensione sbagliata. Passare un xlsx valido.");
           
            using var memoryStream = new MemoryStream();
            {
                await file.CopyToAsync(memoryStream);
                var dt = memoryStream.ReadAsseverazioneExcel();
                return Ok(await handler.Send(dt.Mapper(authInfo)));
            } 
        }
        return Ok(false);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<EnteAsserverazioneDto>>, NotFound>> PostAsseverazioneAsync(
      HttpContext context,
      [FromServices] IStringLocalizer<Localization> localizer,
      [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var asseverazione = await handler.Send(new AsseverazioneQueryGet(authInfo));
        if (asseverazione!.IsNullNotAny())
            return NotFound();
        return Ok(asseverazione);
    }


    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> PostAsseverazioneExportDocumentoAsync(
    HttpContext context,
    [FromQuery] bool? binary,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var asseverazione = await handler.Send(new AsseverazioneQueryGet(authInfo));
        if (asseverazione!.IsNullNotAny())
            return NotFound();

        var export = asseverazione!.Select(x => x.Mapper());

        if (binary == null)
        {
            var dataSet = export!.FillOneSheetv2();
            var content = dataSet.ToExcelData();
            return Ok(new DocumentDto() { Documento = Convert.ToBase64String(content.ToArray()) });
        }
        else if (binary == true)
        {
            var mime = "application/vnd.ms-excel";
            var filename = $"{Guid.NewGuid()}.xlsx";
            var dataSet = export!.FillOneSheetv2();
            var content = dataSet.ToExcelData();
            return Results.File(content!, mime, filename);
        }
        else
        {
            var mime = "text/csv";
            var filename = $"{Guid.NewGuid()}.csv";
            byte[] data;
            using (var stream = new MemoryStream())
            using (TextWriter textWriter = new StreamWriter(stream))
            using (var csv = new CsvWriter(textWriter, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(export!);
                textWriter.Flush();
                data = stream.ToArray();
            }
            return Results.File(data!, mime, filename);
        }
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> PostAsseverazioneDocumentoAsync(
    HttpContext context,
    [FromQuery] bool? binary,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var asseverazione = await handler.Send(new AsseverazioneQueryGet(authInfo));
        if (asseverazione!.IsNullNotAny())
            return NotFound();

        if (binary == null)
        { 
            var dataSet = asseverazione!.FillOneSheetv2();
            var content = dataSet.ToExcel();
            return Ok(new DocumentDto() { Documento = Convert.ToBase64String(content.ToArray()) });
        }
        else if (binary == true)
        {
            var mime = "application/vnd.ms-excel";
            var filename = $"{Guid.NewGuid()}.xlsx";
            var dataSet = asseverazione!.FillOneSheetv2();
            var content = dataSet.ToExcel();
            return Results.File(content!, mime, filename);
        }
        else
        {
            var mime = "text/csv";
            var filename = $"{Guid.NewGuid()}.csv";
            byte[] data;
            using (var stream = new MemoryStream())
            using (TextWriter textWriter = new StreamWriter(stream))
            using (var csv = new CsvWriter(textWriter, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(asseverazione!);
                textWriter.Flush();
                data = stream.ToArray();
            }
            return Results.File(data!, mime, filename);
        }
    }
}