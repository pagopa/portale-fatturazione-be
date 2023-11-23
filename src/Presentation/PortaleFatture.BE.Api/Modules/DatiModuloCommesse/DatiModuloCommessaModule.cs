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
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries;
using PortaleFatture.BE.Infrastructure.Extensions;
using static Microsoft.AspNetCore.Http.TypedResults;

namespace PortaleFatture.BE.Api.Modules.DatiModuloCommesse;

public partial class DatiModuloCommessaModule
{
    [Authorize(Roles = $"{Ruolo.ADMIN}")]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<DatiModuloCommessaResponse>, NotFound>> CreateDatiModuloCommessaAsync(
    HttpContext context,
    [FromBody] DatiModuloCommessaCreateRequest req, 
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var idente = authInfo.IdEnte;

        var command = req.Mapper(authInfo) ?? throw new ValidationException(localizer["xxx"]); 
        foreach (var cmd in command.DatiModuloCommessaListCommand!) 
            cmd.IdEnte = idente;

        var modulo = await handler.Send(command) ?? throw new DomainException(localizer["xxx"]);
        return Ok(modulo.Mapper()); 
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}")]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<DatiModuloCommessaResponse>, NotFound>> GetDatiModuloCommessaAsync(
      HttpContext context,  
      [FromServices] IStringLocalizer<Localization> localizer,
      [FromServices] IMediator handler)
    { 
        var authInfo = context.GetAuthInfo();
        var modulo = await handler.Send(new DatiModuloCommessaQueryGet(authInfo));
        if (modulo == null)
            NotFound(localizer["xxx"]);

        return Ok(modulo!.Mapper());
    }
}