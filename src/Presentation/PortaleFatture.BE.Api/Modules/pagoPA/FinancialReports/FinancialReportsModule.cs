using System.Data;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Api.Modules.pagoPA.AnagraficaPSP.Extensions;
using PortaleFatture.BE.Api.Modules.pagoPA.FinancialReports.Extensions;
using PortaleFatture.BE.Api.Modules.pagoPA.FinancialReports.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti.Common;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.FinancialReports.Dto;
using PortaleFatture.BE.Infrastructure.Gateway.Storage.pagoPA;
using static Microsoft.AspNetCore.Http.TypedResults;

namespace PortaleFatture.BE.Api.Modules.pagoPA.FinancialReports;


public partial class FinancialReportsModule : Module, IRegistrableModule
{
    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<GridFinancialReportListDto>, NotFound>> PostFinancialReportsList(
    HttpContext context,
    [FromBody] FinancialReportsRequest request,
    [FromServices] IMediator handler,
    [FromServices] IStringLocalizer<Localization> localizer)
    {
        var authInfo = context.GetAuthInfo();
        var reports = await handler.Send(request.Map(authInfo));
        if (reports == null || reports.Count == 0)
            return NotFound();
        return Ok(reports);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<FinancialReportsQuarterByIdResponse>, NotFound>> PostFinancialReportsByPSP(
    HttpContext context,
    [FromBody] FinancialReportsPSPRequest request,
    [FromServices] IMediator handler,
    [FromServices] IDocumentStorageSASService sasService,
    [FromServices] IStringLocalizer<Localization> localizer)
    {
        var authInfo = context.GetAuthInfo();
        var reports = await handler.Send(request.Map(authInfo));
        if (reports == null || reports.Count == 0)
            return NotFound();
        var anagrafica = await handler.Send(request.Mapv2(authInfo));
        if (anagrafica == null || anagrafica.Count == 0)
            return NotFound(); 
   
        return Ok(reports.Map(anagrafica, sasService));
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<List<FinancialReportsQuartersResponse>>, NotFound>> PostFinancialReportsQuarters(
    HttpContext context,
    [FromBody] FinancialReportsQuartersRequest request,
    [FromServices] IMediator handler,
    [FromServices] IStringLocalizer<Localization> localizer)
    {
        var authInfo = context.GetAuthInfo();
        var quarters = await handler.Send(request.Map(authInfo));
        if (quarters.IsNullNotAny())
            return NotFound();
        var values = quarters.Select(x => new FinancialReportsQuartersResponse()
        {
            Quarter = "Q" + x.Replace(request.Year + "_", string.Empty),
            Value = x
        }).OrderBy(y => y.Value).ToList();

        return Ok(values);
    }


    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<List<string>>, NotFound>> GetFinancialReportsYears(
    HttpContext context,
    [FromServices] IMediator handler,
    [FromServices] IStringLocalizer<Localization> localizer)
    {
        var authInfo = context.GetAuthInfo();
        var request = new FinancialReportsQuartersRequest();
        var quarters = await handler.Send(request.Map(authInfo));
        if (quarters.IsNullNotAny())
            return NotFound();  

        var values = quarters.Select(x => x.Split("_")[0]).Distinct().OrderByDescending(y => y).ToList();

        return Ok(values);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> PostFinancialReportsDocument(
    HttpContext context,
    [FromBody] FinancialReportsRequest request,
    [FromServices] IMediator handler,
    [FromServices] IStringLocalizer<Localization> localizer)
    {
        var authInfo = context.GetAuthInfo();
        var reports = await handler.Send(request.Mapv3(authInfo));
        if (reports.IsNullNotAny())
            return NotFound();

        var mime = "application/vnd.ms-excel";
        var filename = $"{Guid.NewGuid()}.xlsx";

        var dataSet = reports!.FillPagoPAOneSheet("Documenti Contabili");
        var content = dataSet.ToExcel();

        content.Seek(0, SeekOrigin.Begin);
        return Results.Stream(content!, mime, filename);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> PostFinancialReportsPdndDocument(
    HttpContext context,
    [FromBody] FinancialReportsRequest request,
    [FromServices] IMediator handler,
    [FromServices] IStringLocalizer<Localization> localizer)
    {
        var authInfo = context.GetAuthInfo();
        var reports = await handler.Send(request.Mapv2(authInfo));   

        var data  = reports.Map();
        if (data == null)
            return NotFound();

        var content = data!.ToExcel();

        var mime = "application/vnd.ms-excel";
        var filename = $"{Guid.NewGuid()}.xlsx";
        content.Seek(0, SeekOrigin.Begin);
        return Results.Stream(content!, mime, filename);
    }
}