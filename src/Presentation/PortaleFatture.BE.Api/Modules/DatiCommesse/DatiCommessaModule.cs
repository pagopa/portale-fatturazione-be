using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using PortaleFatture.BE.Api.Modules.DatiCommesse.Extensions;
using PortaleFatture.BE.Api.Modules.DatiCommesse.Payload;
using PortaleFatture.BE.Infrastructure.Common.DatiCommesse.Queries;
using static Microsoft.AspNetCore.Http.TypedResults;

namespace PortaleFatture.BE.Api.Modules.DatiCommesse;

public partial class DatiCommessaModule
{
    [AllowAnonymous]
    [EnableCors("portalefatture")] 
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<DatiCommessaResponse>, BadRequest>> CreateDatiCommessaAsync(
    HttpContext context,
    [FromBody] DatiCommessaCreateRequest req,
    [FromQuery] string? idente,
    [FromServices] IMediator handler)
    {
        // generate fake id ente
        if (string.IsNullOrEmpty(idente))
            idente = DatiCommessaExtensions.GenerateFakeIdEnte();
        var createdDatiCommessa = await handler.Send(req.Mapper(idente));
        return Ok(createdDatiCommessa.Mapper());
    }

    [AllowAnonymous]
    [EnableCors("portalefatture")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<DatiCommessaResponse>, BadRequest>> UpdateDatiCommessaAsync(
    HttpContext context,
    [FromBody] DatiCommessaUpdateRequest req,
    [FromRoute] string? idEnte,
    [FromServices] IMediator handler)
    {
        // verifica ente
        var updatedDatiCommessa = await handler.Send(req.Mapper());
        return Ok(updatedDatiCommessa.Mapper());
    }

    [AllowAnonymous]
    [EnableCors("portalefatture")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<DatiCommessaResponse>, BadRequest>> GetDatiCommessaByIdAsync(
    HttpContext context,
    [FromRoute] long id,
    [FromServices] IMediator handler)
    {
        var datiCommessa = await handler.Send(new DatiCommessaQueryGetById() { Id = id });
        return Ok(datiCommessa.Mapper());
    }

    [AllowAnonymous]
    [EnableCors("portalefatture")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<DatiCommessaResponse>>, BadRequest>> GetDatiCommessaAllByIdEnteAsync(
    HttpContext context,
    [FromRoute] string idente,
    [FromServices] IMediator handler)
    {
        var datiCommessa = await handler.Send(new DatiCommessaQueryGetAllByIdEnte() { IdEnte = idente });
        return Ok(datiCommessa!.Select(x => x.Mapper()));
    }
}