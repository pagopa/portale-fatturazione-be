using DocumentFormat.OpenXml.Bibliography;
using System.Text.RegularExpressions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Api.Infrastructure.Documenti;
using PortaleFatture.BE.Api.Modules.Fatture.Extensions;
using PortaleFatture.BE.Api.Modules.Notifiche.Payload.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiRel;
using PortaleFatture.BE.Core.Entities.Notifiche;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Documenti.Common;
using PortaleFatture.BE.Infrastructure.Common.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries;

using static Microsoft.AspNetCore.Http.TypedResults;

namespace PortaleFatture.BE.Api.Modules.Fatture;

public partial class FattureModule
{

    #region pagoPA
    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<string>>, NotFound>> PostTipologiaFatture(
    HttpContext context,
    [FromBody] TipologiaFattureRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var tipoFattura = await handler.Send(new TipoFatturaQueryGetAll(authInfo, request.Anno, request.Mese));
        if (tipoFattura.IsNullNotAny())
            return NotFound();
        return Ok(tipoFattura);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<FattureListaDto>, NotFound>> PostFattureByRicercaAsync(
    HttpContext context,
    [FromBody] FatturaRicercaRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var fatture = await handler.Send(request.Map(authInfo));
        if (fatture == null || !fatture!.Any())
            return NotFound();
        return Ok(fatture);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> PostFattureExcelByRicercaAsync(
    HttpContext context,
    [FromBody] FatturaRicercaRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var fatture = await handler.Send(request.Map(authInfo));
        if (fatture == null || !fatture!.Any())
            return NotFound();
        var mime = "application/vnd.ms-excel";
        var filename = $"{Guid.NewGuid()}.xlsx";

        var dataSet = fatture.Map()!.FillOneSheetv2();
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
    private async Task<IResult> PostFattureReportByRicercaAsync(
    HttpContext context,
    [FromBody] FatturaRicercaRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] ILogger<FattureModule> logger,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        if (request.TipologiaFattura!.IsNullNotAny())
            return NotFound();

        Dictionary<string, byte[]>? reports = [];

        foreach (var tipologia in request.TipologiaFattura!)
        {
            var month = request.Mese.GetMonth();
            var year = request.Anno;
            switch (tipologia)
            {
                case TipologiaFattura.PRIMOSALDO:
                    var fatture = await handler.Send(request.Mapv2(authInfo, tipologia));
                    if (fatture.IsNotEmpty())
                        reports.Add($"Lista {tipologia} {year} {month}", fatture!.ReportFattureRel(month, tipologia));
                    break;
                case TipologiaFattura.SECONDOSALDO:
                    fatture = await handler.Send(request.Mapv2(authInfo, tipologia));
                    if (fatture.IsNotEmpty())
                        reports.Add($"Lista {tipologia} {year} {month}", fatture!.ReportFattureRel(request.Mese.GetMonth(), tipologia));
                    break;
                case TipologiaFattura.ANTICIPO:
                    var commesse = await handler.Send(request.Mapv3(authInfo));
                    if (commesse.IsNotEmpty())
                        reports.Add($"Lista ANTICIPO {year} {month}", commesse!.ReportFattureModuloCommessa(request.Mese.GetMonth()));
                    break;
                case TipologiaFattura.ACCONTO:
                    var acconto = await handler.Send(request.Mapv4(authInfo));
                    if (acconto.IsNotEmpty())
                        reports.Add($"Lista ACCONTO {year} {month}", acconto!.ReportFattureAnticipo(request.Mese.GetMonth()));
                    break;
                default:
                    break;
            }
        }

        if (reports.Count > 0)
        {
            var fileBytes = reports.CreateZip(logger);
            var mime = "application/zip";
            var filename = $"{Guid.NewGuid()}.zip";
            return Results.File(fileBytes!, mime, filename);
        }

        return NotFound();
    }
    #endregion
}