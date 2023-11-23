using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Api.Modules.Auth.Extensions;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Extensions;
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
    [FromServices] IStringLocalizer<Localization> localizer)
    {  
        var authInfos = await profileService.GetInfo(selfcareToken); 
        return Ok(authInfos.Mapper(usersService, tokensService));
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}")]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private Results<Ok<AuthenticationInfo>, NotFound> ProfiloAsync(
    HttpContext context,
    [FromServices] IIdentityUsersService usersService,
    [FromServices] ITokenService tokensService,
    [FromServices] IStringLocalizer<Localization> localizer)
    {
        return Ok(context.GetAuthInfo());
    }
}