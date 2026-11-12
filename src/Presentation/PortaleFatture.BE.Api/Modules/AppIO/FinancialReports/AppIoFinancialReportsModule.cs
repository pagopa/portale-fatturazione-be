using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Api.Modules.AppIO.FinancialReports.Extensions;
using PortaleFatture.BE.Api.Modules.AppIO.FinancialReports.Request;
using PortaleFatture.BE.Api.Modules.AppIO.FinancialReports.Response;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Extensions;
using PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Queries;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti.Common;
using PortaleFatture.BE.Infrastructure.Gateway.Storage.AppIO;
using static Microsoft.AspNetCore.Http.TypedResults;

namespace PortaleFatture.BE.Api.Modules.AppIO.FinancialReports;

[FeatureFlag(FeatureFlags.AppIO)]
public partial class AppIoFinancialReportsModule : Module, IRegistrableModule
{
    private const string _mimeExcel = "application/vnd.ms-excel";

    /// <summary>
    /// Endpoint per il recupero degli anni in cui sono presenti documenti emessi AppIO.
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
        var years = await handler.Send(new AppIoFinancialReportYearsQuery(context.GetAuthInfo()));
        if (years.IsNullNotAny())
            return NotFound();
        return Ok(years.MapYears());
    }

    /// <summary>
    /// Endpoint per il recupero dei trimestri dei documenti emessi AppIO di un anno.
    /// </summary>
    /// <param name="context">Il contesto HTTP della richiesta.</param>
    /// <param name="request">La richiesta con l'anno; body assente = tutti i trimestri.</param>
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
    [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AppIoFinancialReportsQuartersRequest? request,
    [FromServices] IMediator handler)
    {
        var quarters = await handler.Send((request ?? new AppIoFinancialReportsQuartersRequest()).Map(context.GetAuthInfo()));
        if (quarters.IsNullNotAny())
            return NotFound();
        return Ok(quarters.MapQuarters());
    }

    /// <summary>
    /// Endpoint per il recupero dei codici fiscali degli enti AppIO (filtro "Codice Fiscale").
    /// </summary>
    /// <param name="context">Il contesto HTTP della richiesta.</param>
    /// <param name="handler">Il mediatore per gestire la logica della richiesta.</param>
    /// <returns>I codici fiscali, in ordine crescente.</returns>
    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<List<string>>, NotFound>> GetAppIoFinancialReportsCodiciFiscali(
    HttpContext context,
    [FromServices] IMediator handler)
    {
        var codici = await handler.Send(new AppIoFinancialReportCodiciFiscaliQuery(context.GetAuthInfo()));
        if (codici.IsNullNotAny())
            return NotFound();
        return Ok(codici.ToList());
    }

    /// <summary>
    /// Endpoint per la griglia dei documenti emessi AppIO: una riga per contratto e trimestre, con le
    /// righe dei documenti. Non è paginata: la pagina frontend pagina in memoria, come per pagoPA.
    /// </summary>
    /// <param name="context">Il contesto HTTP della richiesta.</param>
    /// <param name="request">La richiesta con i filtri; body assente = trimestre più recente.</param>
    /// <param name="handler">Il mediatore per gestire la logica della richiesta.</param>
    /// <returns>Le righe padre e il loro numero.</returns>
    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<AppIoFinancialReportsListResponse>, NotFound>> PostAppIoFinancialReports(
    HttpContext context,
    [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AppIoFinancialReportsRequest? request,
    [FromServices] IMediator handler)
    {
        var reports = await handler.Send((request ?? new AppIoFinancialReportsRequest()).Map(context.GetAuthInfo()));
        if (reports == null || reports.Count == 0)
            return NotFound();
        return Ok(reports.Map());
    }

    /// <summary>
    /// Endpoint per il download "Documenti Contabili" AppIO: un foglio con una riga per riga di documento.
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
    [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AppIoFinancialReportsRequest? request,
    [FromServices] IMediator handler)
    {
        var righe = await handler.Send((request ?? new AppIoFinancialReportsRequest()).MapExcel(context.GetAuthInfo()));
        if (righe.IsNullNotAny())
            return NotFound();

        var content = righe.FillPagoPAOneSheet("Documenti Contabili").ToExcel();
        content.Seek(0, SeekOrigin.Begin);
        return Results.Stream(content, _mimeExcel, $"{Guid.NewGuid()}.xlsx");
    }

    /// <summary>
    /// Endpoint per il dettaglio di un documento emesso AppIO (contratto e trimestre).
    /// </summary>
    /// <param name="context">Il contesto HTTP della richiesta.</param>
    /// <param name="request">La richiesta con contratto e trimestre, entrambi obbligatori.</param>
    /// <param name="handler">Il mediatore per gestire la logica della richiesta.</param>
    /// <returns>Il dettaglio: anagrafica e righe.</returns>
    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<AppIoFinancialReportsDettaglioResponse>, BadRequest, NotFound>> PostAppIoFinancialReportsDetail(
    HttpContext context,
    [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AppIoFinancialReportsDettaglioRequest? request,
    [FromServices] IMediator handler)
    {
        if (!request.IsValida())
            return BadRequest();
        var dettaglio = (await handler.Send(request!.Map(context.GetAuthInfo()))).Componi();
        return dettaglio == null ? NotFound() : Ok(dettaglio.Map());
    }

    /// <summary>
    /// Endpoint per il download del dettaglio di un documento emesso AppIO: un foglio con anagrafica e
    /// righe, una riga per riga di documento.
    /// </summary>
    /// <param name="context">Il contesto HTTP della richiesta.</param>
    /// <param name="request">La richiesta con contratto e trimestre, entrambi obbligatori.</param>
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
    [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AppIoFinancialReportsDettaglioRequest? request,
    [FromServices] IMediator handler)
    {
        if (!request.IsValida())
            return Results.BadRequest();

        var righe = await handler.Send(request!.Map(context.GetAuthInfo()));
        if (righe.IsNullNotAny())
            return NotFound();

        var content = righe.FillPagoPAOneSheet("Dettaglio").ToExcel();
        content.Seek(0, SeekOrigin.Begin);
        return Results.Stream(content, _mimeExcel, $"{Guid.NewGuid()}.xlsx");
    }

    /// <summary>
    /// Endpoint per il download del file del financial report AppIO di un trimestre, letto dal blob
    /// storage. Il percorso del file (file_path) resta nel backend: al client arriva solo il contenuto.
    /// </summary>
    /// <param name="context">Il contesto HTTP della richiesta.</param>
    /// <param name="request">La richiesta con anno e trimestre, entrambi obbligatori.</param>
    /// <param name="handler">Il mediatore per gestire la logica della richiesta.</param>
    /// <param name="storage">Il servizio di lettura dal blob storage AppIO.</param>
    /// <returns>Il file; 404 se il trimestre non ha documenti o il file non c'è, 503 se lo storage non è configurato per quel percorso.</returns>
    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    private async Task<IResult> PostAppIoFinancialReportsFileDownload(
    HttpContext context,
    [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] AppIoFinancialReportsFileRequest? request,
    [FromServices] IMediator handler,
    [FromServices] IAppIoFinancialReportStorageService storage)
    {
        if (!request.IsValida())
            return Results.BadRequest();

        var filePath = await handler.Send(request!.Map(context.GetAuthInfo()));
        if (string.IsNullOrWhiteSpace(filePath))
            return NotFound();
        if (!storage.CanRead(filePath))
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);

        var content = await storage.OpenReadAsync(filePath, context.RequestAborted);
        if (content == null)
            return NotFound();
        return Results.Stream(content, filePath.MimeType(), request.FileName(filePath));
    }
}
