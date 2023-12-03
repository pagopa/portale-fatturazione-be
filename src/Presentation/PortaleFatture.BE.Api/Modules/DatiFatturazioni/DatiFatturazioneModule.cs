using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PortaleFatture.BE.Api.Modules.DatiFatturazioni.Extensions;
using PortaleFatture.BE.Api.Modules.DatiFatturazioni.Payload.Request;
using PortaleFatture.BE.Api.Modules.DatiFatturazioni.Payload.Response;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries;
using PortaleFatture.BE.Infrastructure.Extensions;
using static Microsoft.AspNetCore.Http.TypedResults;

namespace PortaleFatture.BE.Api.Modules.DatiFatturazioni;

public partial class DatiFatturazioneModule
{
    [Authorize(Roles = $"{Ruolo.ADMIN}")]
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

    [Authorize(Roles = $"{Ruolo.ADMIN}")]
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

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}")]
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