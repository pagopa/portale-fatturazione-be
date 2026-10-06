using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Api.Modules.AppIO.FinancialReports.Extensions;
using PortaleFatture.BE.Api.Modules.AppIO.FinancialReports.Request;
using PortaleFatture.BE.Api.Modules.AppIO.FinancialReports.Response;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti.Common;
using static Microsoft.AspNetCore.Http.TypedResults;

namespace PortaleFatture.BE.Api.Modules.AppIO.FinancialReports;

public partial class AppIoFinancialReportsModule : Module, IRegistrableModule
{
    private const string _mimeExcel = "application/vnd.ms-excel";

    /// <summary>
    /// Endpoint per il recupero degli anni in cui sono presenti documenti contabili AppIO.
    /// </summary>
    /// <param name="context">Il contesto HTTP della richiesta.</param>
    /// <param name="handler">Il mediatore per gestire la logica della richiesta.</param>
    /// <returns>La lista degli anni, dal più recente.</returns>
    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<List<string>>, NotFound>> GetAppIoFinancialReportsYears(
    HttpContext context,
    [FromServices] IMediator handler)
    {
        var quarters = await handler.Send(new AppIoFinancialReportsQuartersRequest().Map(context.GetAuthInfo()));
        if (quarters.IsNullNotAny())
            return NotFound();
        return Ok(quarters.MapYears());
    }

    /// <summary>
    /// Endpoint per il recupero dei trimestri dei documenti contabili AppIO di un anno.
    /// </summary>
    /// <param name="context">Il contesto HTTP della richiesta.</param>
    /// <param name="request">La richiesta con l'anno.</param>
    /// <param name="handler">Il mediatore per gestire la logica della richiesta.</param>
    /// <returns>Le voci del filtro "Trimestre".</returns>
    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<List<AppIoFinancialReportsQuartersResponse>>, NotFound>> PostAppIoFinancialReportsQuarters(
    HttpContext context,
    [FromBody] AppIoFinancialReportsQuartersRequest request,
    [FromServices] IMediator handler)
    {
        var quarters = await handler.Send(request.Map(context.GetAuthInfo()));
        if (quarters.IsNullNotAny())
            return NotFound();
        return Ok(quarters.MapQuarters());
    }

    /// <summary>
    /// Endpoint per la griglia dei documenti contabili AppIO: documenti con le loro posizioni. Non è
    /// paginata: la pagina frontend pagina in memoria, come per pagoPA.
    /// </summary>
    /// <param name="context">Il contesto HTTP della richiesta.</param>
    /// <param name="request">La richiesta con i filtri.</param>
    /// <param name="handler">Il mediatore per gestire la logica della richiesta.</param>
    /// <returns>I documenti e il loro numero.</returns>
    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<AppIoFinancialReportsListResponse>, NotFound>> PostAppIoFinancialReports(
    HttpContext context,
    [FromBody] AppIoFinancialReportsRequest request,
    [FromServices] IMediator handler)
    {
        var reports = await handler.Send(request.Map(context.GetAuthInfo()));
        if (reports == null || reports.Count == 0)
            return NotFound();
        return Ok(reports.Map());
    }

    /// <summary>
    /// Endpoint per il download "Documenti Contabili" AppIO: un foglio con una riga per posizione.
    /// </summary>
    /// <param name="context">Il contesto HTTP della richiesta.</param>
    /// <param name="request">La richiesta con i filtri, gli stessi della griglia.</param>
    /// <param name="handler">Il mediatore per gestire la logica della richiesta.</param>
    /// <returns>Il file Excel.</returns>
    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> PostAppIoFinancialReportsDocument(
    HttpContext context,
    [FromBody] AppIoFinancialReportsRequest request,
    [FromServices] IMediator handler)
    {
        var righe = await handler.Send(request.MapExcel(context.GetAuthInfo()));
        if (righe.IsNullNotAny())
            return NotFound();

        var content = righe.FillPagoPAOneSheet("Documenti Contabili").ToExcel();
        content.Seek(0, SeekOrigin.Begin);
        return Results.Stream(content, _mimeExcel, $"{Guid.NewGuid()}.xlsx");
    }

    /// <summary>
    /// Endpoint per il download "Financial Report" AppIO: per ogni trimestre un foglio con il
    /// financial report e uno con le posizioni.
    /// </summary>
    /// <param name="context">Il contesto HTTP della richiesta.</param>
    /// <param name="request">La richiesta con i filtri, gli stessi della griglia.</param>
    /// <param name="handler">Il mediatore per gestire la logica della richiesta.</param>
    /// <returns>Il file Excel.</returns>
    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> PostAppIoFinancialReportsDetailDownload(
    HttpContext context,
    [FromBody] AppIoFinancialReportsRequest request,
    [FromServices] IMediator handler)
    {
        var dettaglio = await handler.Send(request.MapDettaglio(context.GetAuthInfo()));
        var dataSet = dettaglio.MapExcel();
        if (dataSet == null)
            return NotFound();

        var content = dataSet.ToExcel();
        content.Seek(0, SeekOrigin.Begin);
        return Results.Stream(content, _mimeExcel, $"{Guid.NewGuid()}.xlsx");
    }
}
