using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Common.SEND.Banner.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Banner.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Banner.QueryHandlers;
using static Microsoft.AspNetCore.Http.TypedResults;


namespace PortaleFatture.BE.Api.Modules.Banner;

public partial class BannerModule
{
    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<BannerDto>, NotFound>> GetBannerAsync(
    HttpContext context,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        BannerDto? banner = await handler.Send(new BannerQuery(authInfo));
        if (banner == null)
            return NotFound();
        return Ok(banner);
    }

}