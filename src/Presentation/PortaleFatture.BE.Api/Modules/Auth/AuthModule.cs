using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Api.Modules.Auth.Extensions;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Common.Utenti.Commands;
using PortaleFatture.BE.Infrastructure.Extensions;
using PortaleFatture.BE.Infrastructure.Gateway;
using static Microsoft.AspNetCore.Http.TypedResults;

namespace PortaleFatture.BE.Api.Modules.Auth;

public partial class AuthModule : Module, IRegistrableModule
{
    [AllowAnonymous]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<List<ProfileInfo>>, NotFound>> LoginAsync(
    HttpContext context,
    [FromQuery] string? selfcareToken,
    [FromServices] IIdentityUsersService usersService,
    [FromServices] IProfileService profileService,
    [FromServices] ITokenService tokensService,
    [FromServices] IAesEncryption encryption,
    [FromServices] IStringLocalizer<Localization> localizer)
    {
        var authInfos = await profileService.GetInfo(selfcareToken);
        return Ok(authInfos.Mapper(usersService, tokensService, encryption));
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}")]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<UtenteInfo>, NotFound>> ProfiloAsync(
    HttpContext context,
    [FromServices] IIdentityUsersService usersService,
    [FromServices] ITokenService tokensService,
    [FromServices] IAesEncryption encryption,
    [FromServices] IMediator handler,
    [FromServices] IStringLocalizer<Localization> localizer)
    {
        var authInfo = context.GetAuthInfo(); 
        var modulo = await handler.Send(new UtenteCreateCommand(authInfo)) ?? throw new DomainException(localizer["UtenteProfiloError"]);
        return Ok(authInfo.Mapper(modulo.DataPrimo, modulo.DataUltimo, encryption));
    }
}