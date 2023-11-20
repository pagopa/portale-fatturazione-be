using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Api.Modules.DatiFatturazioni.Extensions;
using PortaleFatture.BE.Api.Modules.DatiModuloCommesse.Extensions;
using PortaleFatture.BE.Api.Modules.DatiModuloCommesse.Payload;
using PortaleFatture.BE.Api.Modules.DatiModuloCommesse.Payload.Response;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries;
using static Microsoft.AspNetCore.Http.TypedResults;

namespace PortaleFatture.BE.Api.Modules.DatiModuloCommesse;

public partial class DatiModuloCommessaModule
{
    [AllowAnonymous]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<DatiModuloCommessaResponse>, NotFound>> CreateDatiModuloCommessaAsync(
    HttpContext context,
    [FromBody] DatiModuloCommessaCreateRequest req,
    [FromQuery] string? idente,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var command = req.Mapper();
        if (command == null)
            throw new ValidationException(localizer["xxx"]);

        // generate fake id ente
        if (string.IsNullOrEmpty(idente))
            idente = DatiFatturazioneExtensions.GenerateFakeIdEnte();

        foreach (var cmd in command.DatiModuloCommessaListCommand!) 
            cmd.IdEnte = idente;

        var modulo = await handler.Send(command);
        if (modulo == null)
            throw new DomainException(localizer["xxx"]);
        return Ok(modulo.Mapper()); 
    }

    [AllowAnonymous]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<DatiModuloCommessaResponse>, NotFound>> GetDatiModuloCommessaAsync(
      HttpContext context, 
      [FromQuery] string? idente,
      [FromServices] IStringLocalizer<Localization> localizer,
      [FromServices] IMediator handler)
    {  
        if (string.IsNullOrEmpty(idente))
            throw new DomainException(localizer["xxx"]); 

        var modulo = await handler.Send(new DatiModuloCommessaQueryGet()
        {
             IdEnte = idente
        });
        if (modulo == null)
            NotFound(localizer["xxx"]);

        return Ok(modulo!.Mapper());
    }
}