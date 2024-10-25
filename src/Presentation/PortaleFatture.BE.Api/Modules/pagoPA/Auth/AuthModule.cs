using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Api.Modules.SEND.Auth.Extensions;
using PortaleFatture.BE.Api.Modules.SEND.Auth.Payload;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Gateway;
using static Microsoft.AspNetCore.Http.TypedResults;

namespace PortaleFatture.BE.Api.Modules.pagoPA.Auth;

public partial class AuthModule : Module, IRegistrableModule
{ 
    [AllowAnonymous]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<List<ProfileInfo>>, NotFound>> LoginPagoPAProfiliAsync(
    HttpContext context,
    [FromBody] PagoPaLoginRequest request,
    [FromServices] IProfileService profileService,
    [FromServices] IAesEncryption encryption,
    [FromServices] IMediator handler,
    [FromServices] ITokenService tokensService,
    [FromServices] IIdentityUsersService usersService,
    [FromServices] IStringLocalizer<Localization> localizer)
    {
        var authInfo = await profileService.GetPagoPAInfo(request.IdToken, request.AccessToken)!;
        return Ok(authInfo!.MapperPagoPAProfili(usersService, tokensService, encryption));
    }
}