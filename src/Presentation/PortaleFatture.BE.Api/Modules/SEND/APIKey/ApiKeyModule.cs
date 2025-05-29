using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Api.Modules.SEND.APIKey.Payload.Request;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.ApiKeys.Queries;
using static Microsoft.AspNetCore.Http.TypedResults;

namespace PortaleFatture.BE.Api.Modules.SEND.APIKey;

public partial class ApiKeyModule
{

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<bool>, BadRequest>> DeleteApiKeysIps(
       HttpContext context,
       [FromBody] IpsRequest req,
       [FromServices] IStringLocalizer<Localization> localizer,
       [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo(); 

        var result = await handler.Send(new DeleteIpsCommand(authInfo)
        {
            IpAddress = req.IpAddress
        });

        if (!result.HasValue || result <= 0)
            return BadRequest();
        return Ok(result > 0);
    }

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<bool>, BadRequest>> PostApiKeysIps(
       HttpContext context,
       [FromBody] IpsRequest req,
       [FromServices] IStringLocalizer<Localization> localizer,
       [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();


        if (!req.IpAddress.VerifyIp())
            throw new ValidationException($"L'indirizzo IP '{req.IpAddress}' non è valido.");

        var result = await handler.Send(new CreateIpsCommand(authInfo)
        {
            IpAddress = req.IpAddress
        });

        if (!result.HasValue || result <= 0)
            return BadRequest();
        return Ok(result > 0);
    } 

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<ApiKeyIpsDto>>, NotFound>> GetApiKeysIps(
   HttpContext context,
   [FromServices] IStringLocalizer<Localization> localizer,
   [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();

        var apikeys = await handler.Send(new ApiKeyIpsQueryGet(authInfo) { });

        if (apikeys == null || !apikeys.Any())
            return NotFound();
        return Ok(apikeys);
    }

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<ApiKeyDto>>, NotFound>> GetApiKeys(
       HttpContext context,
       [FromServices] IStringLocalizer<Localization> localizer,
       [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();

        var apikeys = await handler.Send(new ApiKeyQueryGet(authInfo) { });

        if (apikeys == null || !apikeys.Any())
            return NotFound();
        return Ok(apikeys);
    }

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<bool>, BadRequest>> PostCreateApiKey(
       HttpContext context,
       [FromBody] CreateORModifyApiKeyRequest req,
       [FromServices] IStringLocalizer<Localization> localizer,
       [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();

        var result = await handler.Send(new CreateORModifyApiKeyCommand(authInfo)
        {
            ApiKey = req.ApiKey,
            Attiva = req.Attiva, 
            Refresh = req.Refresh
        });

        if (!result.HasValue || result <= 0)
            return BadRequest();
        return Ok(result > 0);
    }
}