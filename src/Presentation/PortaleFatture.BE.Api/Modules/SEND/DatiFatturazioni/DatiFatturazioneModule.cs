using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Api.Modules.SEND.DatiFatturazioni.Extensions;
using PortaleFatture.BE.Api.Modules.SEND.DatiFatturazioni.Payload.Request;
using PortaleFatture.BE.Api.Modules.SEND.DatiFatturazioni.Payload.Response;
using PortaleFatture.BE.Api.Modules.SEND.Tipologie.Payload.Payload.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiFatturazioni.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.SelfCare.Queries;
using PortaleFatture.BE.Infrastructure.Gateway;
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
    private async Task<Results<Ok<IEnumerable<TipologiaContrattoDto>>, NotFound>> GetTipologiaContratto(
    HttpContext context,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var tipologia = await handler.Send(new TipologiaContrattoQuery(authInfo));
        if (tipologia.IsNullNotAny())
            return NotFound();
        return Ok(tipologia);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<DatiContractCodiceSDIResponse>, BadRequest, NotFound>> GetContractCodiceSDIAsync(
    HttpContext context,
    [FromQuery] string? idente,
    [FromQuery] string? prodotto,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        authInfo.IdEnte = idente;
        authInfo.Prodotto = prodotto;
        var ente = await handler.Send(new EnteQueryCodiceSDIGetById(authInfo));
        if (ente == null)
            return NotFound();

        return Ok(new DatiContractCodiceSDIResponse()
        {
            ContractCodiceSDI = ente.CodiceSDI
        });
    }

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<bool>, BadRequest, NotFound>> VerificaCodiceSDIDatiFatturazione(
    HttpContext context,
    [FromBody] DatiFatturazioneVerificaCodiceSDI req,
    [FromServices] ISelfCareOnBoardingHttpClient onBoardingHttpClient,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        authInfo.IdEnte = req.IdEnte;
        var contratto = await handler.Send(new EnteQueryCodiceSDIGetById(authInfo)) ?? throw new ValidationException("Ente non trovato!");
        var skipVerifica = (req.CodiceSDI == contratto.CodiceSDI);
        var (okValidation, msgValidation) = await onBoardingHttpClient.RecipientCodeVerification(
            contratto,
            req.CodiceSDI,
            skipVerifica);

        if (okValidation)
            return Ok(true);
        else
            throw new ValidationException(msgValidation);
    }


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

        var fatturazione = await handler.Send(new DatiFatturazioneQueryGetByDescrizione(
            authInfo,
            request.IdEnti!,
            request.Prodotto,
            request.Profilo,
            request.IdTipoContratto,
            null,
            null));

        if (fatturazione == null || fatturazione.Count == 0)
            return NotFound();

        var mime = "application/vnd.ms-excel";
        var filename = $"{Guid.NewGuid()}.xlsx";

        var dataSet = fatturazione.DatiFatturazioneEnte!.FillOneSheet();
        var content = dataSet.ToExcel();

        return Results.Stream(content!, mime, filename);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<DatiFatturazioneEnteWithCountDto>, NotFound>> PagoPADatiFatturazioneByDescrizioneAsync(
    HttpContext context,
    [FromBody] EnteRicercaByDescrizioneProfiloRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var fatturazione = await handler.Send(new DatiFatturazioneQueryGetByDescrizione(
            authInfo,
            request.IdEnti!,
            request.Prodotto,
            request.Profilo,
            request.IdTipoContratto,
            request.Page,
            request.Size));

        if (fatturazione == null || fatturazione.Count == 0)
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
        var ente = await handler.Send(new EnteQueryCodiceSDIGetById(authInfo));
        if (ente == null)
            return NotFound();
        var datiFatturazione = await handler.Send(new DatiFatturazioneQueryGetByIdEnte(authInfo));
        if (datiFatturazione == null)
            return NotFound();
        return Ok(datiFatturazione!.Mapper(ente.CodiceSDI));
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
        var ente = await handler.Send(new EnteQueryCodiceSDIGetById(authInfo));
        if (ente == null)
            return NotFound();
        var datiFatturazione = await handler.Send(new DatiFatturazioneQueryGetByIdEnte(authInfo));
        if (datiFatturazione == null)
            return NotFound();
        return Ok(datiFatturazione!.Mapper(ente.CodiceSDI));
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<DatiContractCodiceSDIResponse>, BadRequest, NotFound>> GetContractCodiceSDIByIdEnteAsync(
    HttpContext context,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var ente = await handler.Send(new EnteQueryCodiceSDIGetById(authInfo));
        if (ente == null)
            return NotFound();
        return Ok(new DatiContractCodiceSDIResponse()
        {
            ContractCodiceSDI = ente.CodiceSDI
        });
    }

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<bool>, BadRequest, NotFound>> VerificaCodiceSDIDatiFatturazioneEnte(
    HttpContext context,
    [FromBody] DatiFatturazioneVerificaCodiceSDIEnte req,
    [FromServices] ISelfCareOnBoardingHttpClient onBoardingHttpClient,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var contratto = await handler.Send(new EnteQueryCodiceSDIGetById(authInfo)) ?? throw new ValidationException("Ente non trovato!");
        var skipVerifica = (req.CodiceSDI == contratto.CodiceSDI);
        var (okValidation, msgValidation) = await onBoardingHttpClient.RecipientCodeVerification(
            contratto,
            req.CodiceSDI,
            skipVerifica);

        if (okValidation)
            return Ok(true);
        else
            throw new ValidationException(msgValidation);
    }
}