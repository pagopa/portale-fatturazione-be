using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Api.Modules.SEND.Messaggi.Extensions;
using PortaleFatture.BE.Api.Modules.SEND.Messaggi.Payload.Request;
using PortaleFatture.BE.Api.Modules.SEND.Notifiche.Extensions;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Queries;
using PortaleFatture.BE.Infrastructure.Gateway.Storage;
using static Microsoft.AspNetCore.Http.TypedResults;

namespace PortaleFatture.BE.Api.Modules.Messaggi;

public partial class MessaggiModule
{
    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<int> GetMessaggiPagoPACountAsync(
    HttpContext context,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var count = await handler.Send(new CountMessaggiQueryGetByIdUtente(authInfo));
        if (count.HasValue)
            return count.Value;
        else
            return 0;
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<MessaggioListaDto>, NotFound>> PostMessaggiPagoPAAsync(
    HttpContext context,
    [FromBody] MessaggioRicercaRequest request,
    [FromQuery] int page,
    [FromQuery] int pageSize,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var lista = await handler.Send(request.Map(authInfo, page, pageSize));
        if (lista == null || lista.Count == 0)
            return NotFound();
        return Ok(lista);
    }


    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task PostDownloadMessaggioPagoPAAsync(
    HttpContext context,
    [FromBody] MessaggioRicercaByIdRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler,
    [FromServices] IDocumentStorageService storageService)
    {
        var authInfo = context.GetAuthInfo();
        var messaggio = await handler.Send(request.Map(authInfo));
        if (messaggio == null)
        {
            await Results.NotFound("Data not found").ExecuteAsync(context);
            return;
        }

        if (messaggio.Hash != messaggio.Rhash)
        {
            await handler.Send(request.Map3(authInfo));
            await Results.StatusCode(410).ExecuteAsync(context);
            return;
        }

        byte[] bytes;

        if (messaggio.TipologiaDocumento!.Contains("report"))
            bytes = await storageService.ReadBytes(messaggio.LinkDocumento!);
        else
            bytes = await storageService.ReadMessageBytes(messaggio.Map());

        var mime = messaggio.ContentType;
        MimeMapping.Extensions.TryGetValue(mime!, out string? _extension);
        var filename = $"{Guid.NewGuid()}{_extension}";
        await context.Download(bytes, mime!, filename);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<bool?> PostReadMessaggioPagoPAAsync(
    HttpContext context,
    [FromBody] MessaggioRicercaByIdRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler,
    [FromServices] IDocumentStorageService storageService)
    {
        var authInfo = context.GetAuthInfo();
        return await handler.Send(request.Map2(authInfo));
    }
}