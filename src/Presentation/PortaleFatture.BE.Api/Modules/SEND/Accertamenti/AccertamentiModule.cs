using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Api.Infrastructure.Documenti;
using PortaleFatture.BE.Api.Modules.SEND.Accertamenti.Extensions;
using PortaleFatture.BE.Api.Modules.SEND.Accertamenti.Payload.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;
using PortaleFatture.BE.Infrastructure.Common.SEND.Report.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Report.Queries;
using PortaleFatture.BE.Infrastructure.Gateway.Storage;
using static Microsoft.AspNetCore.Http.TypedResults;

namespace PortaleFatture.BE.Api.Modules.Fatture;

public partial class AccertamentiModule
{

    #region pagoPA

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<ReportDto>>, NotFound>> PostReportAccertamentiByRicercaAsync(
    HttpContext context,
    [FromBody] AccertamentiReportRicercaRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var reports = await handler.Send(request.Map(authInfo));
        if (reports == null || !reports!.Any())
            return NotFound();
        return Ok(reports);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> PostPrenotazioneReportByRicercaAsync(
    HttpContext context,
    [FromBody] AccertamentoReportByIdRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler,
    [FromServices] IDocumentStorageService storageService)
    {
        var authInfo = context.GetAuthInfo();
        var report = await handler.Send(request.Map(authInfo));
        if (report == null)
            return NotFound();

        var contentType = report.ContentType;
        var contentLanguage = LanguageMapping.IT;

        var command = report.Mapv2(authInfo, contentType, contentLanguage);
        var result = await handler.Send(command);    // copio il report nei messaggi

        if (result.HasValue && result == true)
        {
            return Ok();
        }
        else
        {
            return BadRequest();
        }
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> PostDownloadReportAccertamentiAsync(
    HttpContext context,
    [FromBody] AccertamentoReportByIdRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler,
    [FromServices] IDocumentStorageService storageService)
    {
        var authInfo = context.GetAuthInfo();
        var report = await handler.Send(request.Map(authInfo));
        if (report == null)
            return NotFound();
        var bytes = await storageService.ReadBytes(report.LinkDocumento!);

        var mime = report.ContentType;
        MimeMapping.Extensions.TryGetValue(mime!, out string? _extension);
        var filename = $"{Guid.NewGuid()}{_extension}";
        return Results.File(bytes!, mime, filename);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IEnumerable<MatriceCostoRecapitistiDataDto>?> GetDataMatriceRecapitistiAsync(
    HttpContext context,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        return await handler.Send(new MatriceCostoRecapitistiData(authInfo));
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> PostMatriceRecapitistiAsync(
    HttpContext context,
    [FromBody] MatriceCostoRecapitistiRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler,
    [FromServices] IDocumentStorageService storageService)
    {
        var authInfo = context.GetAuthInfo();
        var report = await handler.Send(new MatriceCostoRecapitisti(authInfo, request.DataInizioValidita, request.DataFineValidita));

        if (report == null || !report!.Any())
            return NotFound();
        var mime = "application/vnd.ms-excel";
        var filename = $"{Guid.NewGuid()}.xlsx";

        var dataSet = report!.FillOneSheetv2();
        var content = dataSet.ToExcel();
        var result = new DisposableStreamResult(content, mime)
        {
            FileDownloadName = filename
        };
        return Results.Stream(result.FileStream, result.ContentType, result.FileDownloadName);
    }

    #endregion
}