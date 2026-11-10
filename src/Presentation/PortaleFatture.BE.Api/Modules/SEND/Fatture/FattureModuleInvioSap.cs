using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Extensions;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Response;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;

namespace PortaleFatture.BE.Api.Modules.Fatture;

/// <summary>
/// Invio a SAP (PF-965, figlio di PF-735): dieci rotte <c>api/fatture/invio-sap/*</c>, solo ADMIN pagoPA.
/// Letture dalle viste <c>be.vw*</c>; scritture del Genera in be-invio-sap.md §2.1 e §8.
/// </summary>
public partial class FattureModule
{
    #region invio sap

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(typeof(IReadOnlyList<InvioSapGrigliaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    private async Task<IResult> PostInvioSapGrigliaAsync(HttpContext context, [FromBody] InvioSapGrigliaRequest? request, [FromServices] IMediator handler)
    {
        var righe = (await handler.Send(request.Map(context.GetAuthInfo()))).ToList();
        return righe.Count == 0 ? Results.NotFound() : Results.Ok(righe.Map());
    }

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(typeof(InvioSapDettaglioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    private async Task<IResult> PostInvioSapDettaglioAsync(HttpContext context, [FromBody] InvioSapDettaglioRequest? request, [FromServices] IMediator handler)
    {
        var lista = await handler.Send(request.Map(context.GetAuthInfo()));
        return lista.Count == 0 ? Results.NotFound() : Results.Ok(lista.Map());
    }

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(typeof(InvioSapGeneraResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(InvioSapConflittoResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> PutInvioSapGeneraAsync(HttpContext context, [FromBody] InvioSapGeneraRequest? request, [FromServices] IMediator handler)
    {
        var esito = await handler.Send(request.Map(context.GetAuthInfo()));
        return esito.ToResult();
    }

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    private async Task<IResult> PostInvioSapDownloadAsync(HttpContext context, [FromBody] InvioSapGrigliaRequest? request, [FromServices] IMediator handler)
    {
        var query = request.Map(context.GetAuthInfo());
        var righe = (await handler.Send(query)).Select(InvioSapGrigliaExcelDto.Da).ToList();
        return righe.Count == 0 ? Results.NotFound() : righe.ToExcel("Risultati Invio SAP", $"InvioSAP_Risultati_{query.Anno}");
    }

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    private async Task<IResult> PostInvioSapDownloadReinviateAsync(HttpContext context, [FromBody] InvioSapReinviateRequest? request, [FromServices] IMediator handler)
    {
        var query = request.Map(context.GetAuthInfo());
        var righe = (await handler.Send(query)).Select(InvioSapReinviataExcelDto.Da).ToList();
        return righe.Count == 0 ? Results.NotFound() : righe.ToExcel("Fatture re-inviate", $"InvioSAP_Reinviate_{query.Anno}");
    }

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(typeof(InvioSapLockResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    private async Task<IResult> GetInvioSapLockAsync(HttpContext context, [FromServices] IMediator handler)
    {
        var lockAttivo = await handler.Send(new InvioSapLockQuery(context.GetAuthInfo()));
        return lockAttivo is null ? Results.NoContent() : Results.Ok(lockAttivo.Map());
    }

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(typeof(IReadOnlyList<InvioSapStatoResponse>), StatusCodes.Status200OK)]
    private IResult GetInvioSapStati(HttpContext context) => Results.Ok(InvioSapExtensions.Stati());

    /// <summary>Valori del filtro Anno: gli anni presenti in griglia, dal piu' recente. Lista vuota = 200 [].</summary>
    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(typeof(IReadOnlyList<int>), StatusCodes.Status200OK)]
    private async Task<IResult> GetInvioSapAnniAsync(HttpContext context, [FromServices] IMediator handler)
        => Results.Ok((await handler.Send(new InvioSapAnniQuery(context.GetAuthInfo()))).ToList());

    /// <summary>Valori del filtro Mese: i mesi presenti in griglia per l'anno del body. Lista vuota = 200 [].</summary>
    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(typeof(IReadOnlyList<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    private async Task<IResult> PostInvioSapMesiAsync(HttpContext context, [FromBody] InvioSapMesiRequest? request, [FromServices] IMediator handler)
        => Results.Ok((await handler.Send(request.Map(context.GetAuthInfo()))).ToList());

    /// <summary>Valori del filtro Tipologia: le tipologie presenti in griglia per l'anno (e i mesi) del body, nell'ordine del ciclo. Lista vuota = 200 [].</summary>
    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    private async Task<IResult> PostInvioSapTipologieAsync(HttpContext context, [FromBody] InvioSapTipologieRequest? request, [FromServices] IMediator handler)
        => Results.Ok((await handler.Send(request.Map(context.GetAuthInfo()))).ToList());

    #endregion invio sap
}
