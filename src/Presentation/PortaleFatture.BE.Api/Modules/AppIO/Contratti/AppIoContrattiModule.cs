using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Api.Modules.AppIO.Contratti.Extensions;
using PortaleFatture.BE.Api.Modules.AppIO.Contratti.Request;
using PortaleFatture.BE.Api.Modules.AppIO.Contratti.Response;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti.Common;
using static Microsoft.AspNetCore.Http.TypedResults;

namespace PortaleFatture.BE.Api.Modules.AppIO.Contratti;

public partial class AppIoContrattiModule : Module, IRegistrableModule
{
    /// <summary>
    /// Endpoint per il recupero degli anni dei contratti AppIO.
    /// </summary>
    /// <param name="context">Il contesto HTTP della richiesta.</param>
    /// <param name="handler">Il mediatore per gestire la logica della richiesta.</param>
    /// <returns>Un risultato HTTP contenente la lista degli anni dei contratti AppIO.</returns>
    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<List<string>>, NotFound>> GetAppIoContrattiYears(
    HttpContext context,
    [FromServices] IMediator handler)
    {
        // Recupera gli anni dei contratti AppIO
        var quarters = await handler.Send(new AppIoContrattiQuartersRequest().Map(context.GetAuthInfo()));

        // Se non ci sono anni disponibili, restituisce NotFound
        if (quarters.IsNullNotAny())
            return NotFound();

        // Mappa gli anni dei contratti AppIO e restituisce la lista
        return Ok(quarters.MapYears());
    }

    /// <summary>
    /// Endpoint per il recupero dei trimestri dei contratti AppIO in base all'anno specificato.
    /// </summary>
    /// <param name="context">Il contesto HTTP della richiesta.</param>
    /// <param name="request">La richiesta contenente i parametri per il recupero dei trimestri dei contratti AppIO.</param>
    /// <param name="handler">Il mediatore per gestire la logica della richiesta.</param>
    /// <returns>Un risultato HTTP contenente la lista dei trimestri dei contratti AppIO.</returns>
    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<List<AppIoContrattiQuartersResponse>>, NotFound>> PostAppIoContrattiQuarters(
    HttpContext context,
    [FromBody] AppIoContrattiQuartersRequest request,
    [FromServices] IMediator handler)
    {
        // Recupera i trimestri dei contratti AppIO per l'anno specificato
        var quarters = await handler.Send(request.Map(context.GetAuthInfo()));
        
        // Se non ci sono trimestri disponibili, restituisce NotFound
        if (quarters.IsNullNotAny())
            return NotFound();

        // Mappa i trimestri dei contratti AppIO e restituisce la lista
        return Ok(quarters.Map());
    }

    /// <summary>
    /// Endpoint per il recupero dei contratti AppIO in base al nome specificato.
    /// </summary>
    /// <param name="context">Il contesto HTTP della richiesta.</param>
    /// <param name="request">La richiesta contenente i parametri per il recupero dei contratti AppIO.</param>
    /// <param name="handler">Il mediatore per gestire la logica della richiesta.</param>
    /// <returns>Un risultato HTTP contenente la lista dei contratti AppIO.</returns>
    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<AppIoContrattoNomeResponse>>, NotFound>> PostAppIoContrattiByName(
    HttpContext context,
    [FromBody] AppIoContrattiNameRequest request,
    [FromServices] IMediator handler)
    {
        // Recupera i contratti AppIO per il nome specificato
        var contratti = await handler.Send(request.Map(context.GetAuthInfo()));

        // Se non ci sono contratti disponibili, restituisce NotFound
        if (contratti.IsNullNotAny())
            return NotFound();

        // Mappa i contratti AppIO e restituisce la lista
        return Ok(contratti.Map());
    }

    /// <summary>
    /// Endpoint per il recupero dei contratti AppIO con paginazione.
    /// </summary>
    /// <param name="context">Il contesto HTTP della richiesta.</param>
    /// <param name="request">La richiesta contenente i parametri per il recupero dei contratti AppIO.</param>
    /// <param name="page">Il numero della pagina da recuperare.</param>
    /// <param name="pageSize">Il numero di elementi per pagina.</param>
    /// <param name="handler">Il mediatore per gestire la logica della richiesta.</param>
    /// <returns>Un risultato HTTP contenente la lista dei contratti AppIO paginata.</returns>
    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<AppIoContrattiListResponse>, NotFound>> PostAppIoContratti(
    HttpContext context,
    [FromBody] AppIoContrattiRequest request,
    [FromQuery] int page,
    [FromQuery] int pageSize,
    [FromServices] IMediator handler)
    {
        // Recupera i contratti AppIO con paginazione
        var contratti = await handler.Send(request.Map(context.GetAuthInfo(), page, pageSize));

        // Se non ci sono contratti disponibili, restituisce NotFound
        if (contratti == null || contratti.Count == 0)
            return NotFound();
        
        // Mappa i contratti AppIO e restituisce la lista
        return Ok(contratti.Map());
    }

    /// <summary>
    /// Endpoint per il download dei contratti AppIO in formato Excel.
    /// </summary>
    /// <param name="context">Il contesto HTTP della richiesta.</param>
    /// <param name="request">La richiesta contenente i parametri per il download dei contratti AppIO.</param>
    /// <param name="handler">Il mediatore per gestire la logica della richiesta.</param>
    /// <returns>Un risultato HTTP contenente il file Excel dei contratti AppIO.</returns>
    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> PostAppIoContrattiDownload(
    HttpContext context,
    [FromBody] AppIoContrattiRequest request,
    [FromServices] IMediator handler)
    {
        // Recupera i contratti AppIO per il download
        var contratti = await handler.Send(request.Map(context.GetAuthInfo()));

        // Se non ci sono contratti disponibili, restituisce NotFound
        if (contratti == null || contratti.Count == 0)
            return NotFound();

        // Genera il file Excel dei contratti AppIO e restituisce lo stream del file
        var content = contratti.Contratti!.FillPagoPAOneSheet("Contratti AppIO").ToExcel();

        // Assicura che il flusso sia posizionato all'inizio prima di restituirlo
        content.Seek(0, SeekOrigin.Begin);

        // Restituisce il file Excel come risposta con un nome univoco basato su un GUID
        return Results.Stream(content, "application/vnd.ms-excel", $"{Guid.NewGuid()}.xlsx");
    }
}
