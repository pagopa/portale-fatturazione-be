using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Api.Modules.DatiCommesse.Extensions;
using PortaleFatture.BE.Api.Modules.DatiCommesse.Payload;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiCommesse.Queries;
using static Microsoft.AspNetCore.Http.TypedResults;

namespace PortaleFatture.BE.Api.Modules.DatiCommesse;

public partial class TipoContrattoModule
{
    [AllowAnonymous]
    [EnableCors("portalefatture")] 
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<TipoContrattoResponse>>, NotFound>> GetTipoContrattoAsync(
    HttpContext context,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var tipiContratto = await handler.Send(new TipoContrattoQueryGetAll());
        if (tipiContratto.IsNullNotAny())
             throw new ConfigurationException(localizer["DatiTipoContrattoMissing"]);
        return Ok(tipiContratto.Mapper()); 
    }
}