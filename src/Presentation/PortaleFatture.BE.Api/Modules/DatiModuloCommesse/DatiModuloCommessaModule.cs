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
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries;
using PortaleFatture.BE.Infrastructure.Common.Documenti;
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

        var command = req.Mapper(authInfo) ?? throw new ValidationException(localizer["DatiModuloCommessaInvalidMapping"]);
        foreach (var cmd in command.DatiModuloCommessaListCommand!)
            cmd.IdEnte = idente;

        var modulo = await handler.Send(command) ?? throw new DomainException(localizer["DatiModuloCommessaInvalidMapping"]);
        var response = modulo!.Mapper();
        return Ok(response);
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
        var response = modulo!.Mapper();
        return Ok(response);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}")]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<DatiModuloCommessaResponse>, NotFound>> GetDatiModuloCommessaByAnnoMeseAsync(
    HttpContext context,
    [FromRoute] int anno,
    [FromRoute] int mese,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var modulo = await handler.Send(new DatiModuloCommessaQueryGet(authInfo)
        {
            AnnoValidita = anno,
            MeseValidita = mese
        });

        var response = modulo!.Mapper();
        return Ok(response);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}")]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<DatiModuloCommessaByAnnoResponse>>, NotFound>> GetDatiModuloCommessaByAnnoAsync(
    HttpContext context,
    [FromRoute] int? anno,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var modulo = await handler.Send(new DatiModuloCommessaQueryGetByAnno(authInfo)
        {
            AnnoValidita = anno,
        }); 
        if (modulo == null)
            return NotFound(); 
        return Ok(modulo.Select(x => x.Mapper()));
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}")]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<DatiModuloCommessaParzialiTotaleResponse>>, NotFound>> GetDatiModuloCommessaParzialiByAnnoAsync(
    HttpContext context,
    [FromRoute] int? anno,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var modulo = await handler.Send(new DatiModuloCommessaParzialiQueryGetByAnno(authInfo)
        {
            AnnoValidita = anno,
        });
        if (modulo == null)
            return NotFound();
        return Ok(modulo.Select(x=>x.Mapper()));
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}")]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<string>>, NotFound>> GetDatiModuloCommessaAnniAsync(
    HttpContext context,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var anni = await handler.Send(new DatiModuloCommessaGetAnni(authInfo));
        if (anni == null)
            return NotFound();
        return Ok(anni);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}")]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<ModuloCommessaDocumentoDto>, NotFound>> GetDatiModuloCommessaDocumentoAsync(
    HttpContext context,
    [FromRoute] int? anno,
    [FromRoute] int? mese,
    [FromServices] IMediator handler,
    [FromServices] IWebHostEnvironment hostingEnvironment,
    [FromServices] IStringLocalizer<Localization> localizer)
    {
        var authInfo = context.GetAuthInfo();
        var dati = await handler.Send(new DatiModuloCommessaDocumentoQueryGet(authInfo)
        {
            AnnoValidita = anno,
            MeseValidita = mese
        });
        if (dati == null)
            return NotFound();
        return Ok(dati);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}")]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> DownloadDatiModuloCommessaDocumentoAsync(
    HttpContext context,
    [FromRoute] int? anno,
    [FromRoute] int? mese,
    [FromQuery] string? tipo,
    [FromServices] IMediator handler,
    [FromServices] IDocumentBuilder documentBuilder,
    [FromServices] IStringLocalizer<Localization> localizer)
    {
        var authInfo = context.GetAuthInfo();
        var dati = await handler.Send(new DatiModuloCommessaDocumentoQueryGet(authInfo)
        {
            AnnoValidita = anno,
            MeseValidita = mese
        });
        if (dati == null)
            return NotFound();

        if(tipo != null && tipo == "pdf")
        {
            var bytes = documentBuilder.CreateModuloCommessaPdf(dati!);
            var filename = $"{Guid.NewGuid()}.pdf";
            var mime = "application/pdf";
            return Results.File(bytes!, mime, filename);
        }
        else
        {
            var content = documentBuilder.CreateModuloCommessaHtml(dati!);
            var mime = "text/html";
            return Results.Text(content!, mime);
        } 
    }
}