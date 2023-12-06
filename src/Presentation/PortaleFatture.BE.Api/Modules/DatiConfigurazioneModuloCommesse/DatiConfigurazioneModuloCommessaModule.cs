using System.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Api.Modules.DatiConfigurazioneModuloCommesse.Extensions;
using PortaleFatture.BE.Api.Modules.DatiConfigurazioneModuloCommesse.Request;
using PortaleFatture.BE.Api.Modules.DatiConfigurazioneModuloCommesse.Response;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries;
using static Microsoft.AspNetCore.Http.TypedResults;

namespace PortaleFatture.BE.Api.Modules.DatiConfigurazioneModuloCommesse;

public partial class DatiConfigurazioneModuloCommessaModule
{
    [AllowAnonymous]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<DatiConfigurazioneModuloCommessaResponse>, NotFound>> GetDatiConfigurazioneModuloCommessaAsync(
    HttpContext context,
    [FromQuery] int idTipoContratto,
    [FromQuery] string prodotto,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var configurazione = await handler.Send(new DatiConfigurazioneModuloCommessaQueryGet() { Prodotto = prodotto, IdTipoContratto = idTipoContratto });
        if (configurazione == null)
            throw new NotFoundException(localizer["DatiConfigurazioneModuloCommessaMissing"]);
        return Ok(configurazione.Mapper());
    }

    [AllowAnonymous]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<DatiConfigurazioneModuloCommessaResponse>, NotFound>> CreateDatiConfigurazioneModuloCommessaAsync(
    HttpContext context,
    [FromBody] DatiConfigurazioneModuloCommessaCreateRequest req,
    [FromQuery] string? adminKey,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler,
    [FromServices] IPortaleFattureOptions options)
    {
        if (options.AdminKey != adminKey)
            throw new SecurityException();
        var command = req.Mapper() ?? throw new ValidationException(localizer["DatiConfigurazioneModuloCommessaInvalid"]);
        var configurazione = await handler.Send(command);
        return configurazione == null
            ? throw new DomainException(localizer["DatiConfigurazioneModuloCommessaError"])
            : (Results<Ok<DatiConfigurazioneModuloCommessaResponse>, NotFound>)Ok(configurazione.Mapper());
    }
} 