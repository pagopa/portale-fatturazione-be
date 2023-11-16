using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Api.Modules.DatiFatturazioni.Extensions;
using PortaleFatture.BE.Api.Modules.DatiFatturazioni.Payload.Request;
using PortaleFatture.BE.Api.Modules.DatiFatturazioni.Payload.Response;
using PortaleFatture.BE.Infrastructure.Common.DatiFatturazioni.Queries;
using static Microsoft.AspNetCore.Http.TypedResults;

namespace PortaleFatture.BE.Api.Modules.DatiFatturazioni;

public partial class DatiFatturazioneModule
{ 
    [AllowAnonymous]
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
        // generate fake id ente
        if (string.IsNullOrEmpty(idente))
            idente = DatiFatturazioneExtensions.GenerateFakeIdEnte();
        var command = req.Mapper(idente); 
        var createdDatiFatturazione = await handler.Send(command);
        return Ok(createdDatiFatturazione.Mapper());
    }

    [AllowAnonymous]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<DatiFatturazioneResponse>, BadRequest>> UpdateDatiFatturazioneAsync(
    HttpContext context,
    [FromBody] DatiFatturazioneUpdateRequest req,
    [FromRoute] string? idEnte,
    [FromServices] IMediator handler)
    {
        // verifica ente
        var command = req.Mapper();
        var updatedDatiFatturazione = await handler.Send(command);
        return Ok(updatedDatiFatturazione.Mapper());
    }

    [AllowAnonymous]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<DatiFatturazioneResponse>, BadRequest, NotFound>> GetDatiFatturazioneByIdEnteAsync(
    HttpContext context,
    [FromRoute] string idente,
    [FromServices] IMediator handler)
    {
        var datiCommessa = await handler.Send(new DatiFatturazioneQueryGetByIdEnte() { IdEnte = idente });
        if (datiCommessa == null)
            return NotFound();
        return Ok(datiCommessa!.Mapper());
    }
}