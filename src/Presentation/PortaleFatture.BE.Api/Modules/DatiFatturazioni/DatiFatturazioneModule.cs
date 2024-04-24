using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Api.Modules.DatiFatturazioni.Extensions;
using PortaleFatture.BE.Api.Modules.DatiFatturazioni.Payload.Request;
using PortaleFatture.BE.Api.Modules.DatiFatturazioni.Payload.Response;
using PortaleFatture.BE.Api.Modules.Tipologie.Payload.Payload.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Dto;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries;
using PortaleFatture.BE.Infrastructure.Common.Documenti.Common;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using static Microsoft.AspNetCore.Http.TypedResults;

namespace PortaleFatture.BE.Api.Modules.DatiFatturazioni;

public partial class DatiFatturazioneModule
{
    #region PAGOPA

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)] 
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> PagoPADatiFatturazioneByDescrizioneDocumentAsync(
    HttpContext context,
    [FromBody] EnteRicercaByDescrizioneProfiloRequest request,
    [FromQuery] bool? binary,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var fatturazione = await handler.Send(new DatiFatturazioneQueryGetByDescrizione(authInfo, request.IdEnti!,
            request.Prodotto,
            request.Profilo,
            null));
        if (fatturazione == null || !fatturazione.Any())
            return NotFound();

        var mime = "application/vnd.ms-excel";
        var filename = $"{Guid.NewGuid()}.xlsx";

        var dataSet = fatturazione.FillOneSheet();
        var content = dataSet.ToExcel();
        if (binary == null)
            return Ok(new DocumentDto() { Documento = Convert.ToBase64String(content.ToArray()) });
        else
            return Results.File(content!, mime, filename);

    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)] 
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<DatiFatturazioneEnteDto>>, NotFound>> PagoPADatiFatturazioneByDescrizioneAsync(
    HttpContext context,
    [FromBody] EnteRicercaByDescrizioneProfiloRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var fatturazione = await handler.Send(new DatiFatturazioneQueryGetByDescrizione(authInfo, request.IdEnti!,
            request.Prodotto,
            request.Profilo,
            null));
        if (fatturazione == null || !fatturazione.Any())
            return NotFound();

        return Ok(fatturazione);
    }

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<DatiFatturazioneResponse>, BadRequest>> CreatePagoPaDatiFatturazioneAsync(
    HttpContext context,
    [FromBody] DatiFatturazionePagoPACreateRequest req,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var command = req.Mapper(authInfo!);
        var createdDatiFatturazione = await handler.Send(command);
        return Ok(createdDatiFatturazione.Mapper());
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<DatiFatturazioneResponse>, BadRequest, NotFound>> GetPagoPADatiFatturazioneByIdEnteAsync(
    HttpContext context,
    [FromQuery] string? idente,
    [FromQuery] string? prodotto,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        authInfo.IdEnte = idente;
        authInfo.Prodotto = prodotto;
        var datiCommessa = await handler.Send(new DatiFatturazioneQueryGetByIdEnte(authInfo));
        if (datiCommessa == null)
            return NotFound();
        return Ok(datiCommessa!.Mapper());
    }

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<DatiFatturazioneResponse>, BadRequest>> UpdatePagoPADatiFatturazioneAsync(
    HttpContext context,
    [FromBody] DatiFatturazionePagoPAUpdateRequest req,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var command = req.Mapper(authInfo);
        var updatedDatiFatturazione = await handler.Send(command);
        return Ok(updatedDatiFatturazione.Mapper());
    }

    #endregion PAGOPA

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<DatiFatturazioneResponse>, BadRequest>> CreateDatiFatturazioneAsync(
    HttpContext context,
    [FromBody] DatiFatturazioneCreateRequest req,
    [FromQuery] string? idente,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var command = req.Mapper(authInfo!);
        var createdDatiFatturazione = await handler.Send(command);
        return Ok(createdDatiFatturazione.Mapper());
    }

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<DatiFatturazioneResponse>, BadRequest>> UpdateDatiFatturazioneAsync(
    HttpContext context,
    [FromBody] DatiFatturazioneUpdateRequest req,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var command = req.Mapper(authInfo);
        var updatedDatiFatturazione = await handler.Send(command);
        return Ok(updatedDatiFatturazione.Mapper());
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<DatiFatturazioneResponse>, BadRequest, NotFound>> GetDatiFatturazioneByIdEnteAsync(
    HttpContext context,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var datiCommessa = await handler.Send(new DatiFatturazioneQueryGetByIdEnte(authInfo));
        if (datiCommessa == null)
            return NotFound();
        return Ok(datiCommessa!.Mapper());
    }
}