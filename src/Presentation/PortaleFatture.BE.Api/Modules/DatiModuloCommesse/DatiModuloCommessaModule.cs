using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Api.Modules.DatiFatturazioni.Extensions;
using PortaleFatture.BE.Api.Modules.DatiFatturazioni.Payload.Request;
using PortaleFatture.BE.Api.Modules.DatiModuloCommesse.Extensions;
using PortaleFatture.BE.Api.Modules.DatiModuloCommesse.Payload;
using PortaleFatture.BE.Api.Modules.DatiModuloCommesse.Payload.Request;
using PortaleFatture.BE.Api.Modules.DatiModuloCommesse.Payload.Response;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries;
using PortaleFatture.BE.Infrastructure.Common.Documenti;
using PortaleFatture.BE.Infrastructure.Common.Documenti.Common;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Common.SelfCare.Queries;
using static Microsoft.AspNetCore.Http.TypedResults;

namespace PortaleFatture.BE.Api.Modules.DatiModuloCommesse;

public partial class DatiModuloCommessaModule
{
    #region PagoPA
    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<DatiModuloCommessaResponse>, NotFound>> GetPagoPADatiModuloCommessaAsync(
    HttpContext context,
    [FromQuery] string? idEnte,
    [FromQuery] string? prodotto,
    [FromQuery] long? idTipoContratto,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        authInfo.Prodotto = prodotto;
        authInfo.IdEnte = idEnte;
        authInfo.IdTipoContratto = idTipoContratto;

        await VerificaContratto(handler, localizer, authInfo); 
 
        var modulo = await handler.Send(new DatiModuloCommessaQueryGet(authInfo));
        var response = modulo!.Mapper(authInfo);
        response!.Modifica = response.Modifica && !response.ModuliCommessa!.IsNullNotAny();
        return Ok(response);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> PagoPADatiModuloCommessaRicercaDocumentAsync(
    HttpContext context,
    [FromBody] EnteRicercaModuloCommessaByDescrizioneRequest req,
    [FromQuery] bool? binary,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var moduli = await handler.Send(new DatiModuloCommessaQueryGetByDescrizione(authInfo)
        {
            AnnoValidita = req.Anno,
            MeseValidita = req.Mese,
            Prodotto = req.Prodotto,
            IdEnti = req.IdEnti
        });

        if (moduli == null)
            return NotFound();

        var mime = "application/vnd.ms-excel";
        var filename = $"{Guid.NewGuid()}.xlsx";

        var dataSet = moduli.FillOneSheet();
        var content = dataSet.ToExcel();
        if (binary == null)
            return Ok(new DocumentDto() { Documento = Convert.ToBase64String(content.ToArray()) });
        else
            return Results.File(content!, mime, filename);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<ModuloCommessaByRicercaDto>>, NotFound>> PagoPADatiModuloCommessaRicercaAsync(
    HttpContext context,
    [FromBody] EnteRicercaModuloCommessaByDescrizioneRequest req,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var moduli = await handler.Send(new DatiModuloCommessaQueryGetByDescrizione(authInfo)
        {
            AnnoValidita = req.Anno,
            MeseValidita = req.Mese,
            Prodotto = req.Prodotto,
            IdEnti = req.IdEnti
        });

        if (moduli == null)
            return NotFound();
        return Ok(moduli);
    }


    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<DatiModuloCommessaResponse>, NotFound>> CreatePagoPADatiModuloCommessaAsync(
    HttpContext context,
    [FromBody] DatiModuloCommessaPagoPACreateRequest req,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        authInfo.IdEnte = req.IdEnte;
        authInfo.Prodotto = req.Prodotto;
        authInfo.IdTipoContratto = req.IdTipoContratto;

        var fatturabile = req.Fatturabile ?? true;

        await VerificaContratto(handler, localizer, authInfo);

        var modulo = await handler.Send(new DatiModuloCommessaQueryGet(authInfo)); // posso solo modificare, non inserire
        if (modulo == null || modulo.DatiModuloCommessa!.IsNullNotAny())
            throw new DomainException(localizer["DatiModuloCommessaDoNotCreate"]);

        var command = req.Mapper(authInfo, authInfo.IdTipoContratto.Value) ?? throw new ValidationException(localizer["DatiModuloCommessaInvalidMapping"]);
        foreach (var cmd in command.DatiModuloCommessaListCommand!)
        {
            cmd.IdEnte = authInfo.IdEnte;
            cmd.Fatturabile = fatturabile;
        }

        modulo = await handler.Send(command) ?? throw new DomainException(localizer["DatiModuloCommessaInvalidMapping"]);
        var response = modulo!.Mapper(authInfo);
        return Ok(response);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<DatiModuloCommessaResponse>, NotFound>> GetPagoPADatiModuloCommessaByAnnoMeseAsync(
    HttpContext context,
    [FromRoute] int anno,
    [FromRoute] int mese,
    [FromQuery] string? idEnte,
    [FromQuery] string? prodotto,
    [FromQuery] long? idTipoContratto,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        authInfo.IdEnte = idEnte;
        authInfo.Prodotto = prodotto;
        authInfo.IdTipoContratto = idTipoContratto;

        await VerificaContratto(handler, localizer, authInfo);

        var modulo = await handler.Send(new DatiModuloCommessaQueryGet(authInfo)
        {
            AnnoValidita = anno,
            MeseValidita = mese
        });

        var response = modulo!.Mapper(authInfo);
        response!.Modifica = response.Modifica && !response.ModuliCommessa!.IsNullNotAny();
        return Ok(response);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<ModuloCommessaDocumentoDto>, NotFound>> GetPagoPADatiModuloCommessaDocumentoAsync(
    HttpContext context,
    [FromRoute] int? anno,
    [FromRoute] int? mese,
    [FromQuery] string? idEnte,
    [FromQuery] string? prodotto,
    [FromQuery] long? idTipoContratto,
    [FromServices] IMediator handler,
    [FromServices] IWebHostEnvironment hostingEnvironment,
    [FromServices] IStringLocalizer<Localization> localizer)
    {
        var authInfo = context.GetAuthInfo();
        authInfo.IdEnte = idEnte;
        authInfo.Prodotto = prodotto;
        authInfo.IdTipoContratto = idTipoContratto;

        await VerificaContratto(handler, localizer, authInfo);

        var dati = await handler.Send(new DatiModuloCommessaDocumentoQueryGet(authInfo)
        {
            AnnoValidita = anno,
            MeseValidita = mese
        });
        if (dati == null)
            return NotFound();
        return Ok(dati);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> DownloadPagoPADatiModuloCommessaDocumentoAsync(
    HttpContext context,
    [FromRoute] int? anno,
    [FromRoute] int? mese,
    [FromQuery] string? idEnte,
    [FromQuery] string? prodotto,
    [FromQuery] long? idTipoContratto,
    [FromQuery] string? tipo,
    [FromServices] IMediator handler,
    [FromServices] IDocumentBuilder documentBuilder,
    [FromServices] IStringLocalizer<Localization> localizer)
    {
        var authInfo = context.GetAuthInfo();
        authInfo.IdEnte = idEnte;
        authInfo.Prodotto = prodotto;
        authInfo.IdTipoContratto = idTipoContratto;

        await VerificaContratto(handler, localizer, authInfo);

        var dati = await handler.Send(new DatiModuloCommessaDocumentoQueryGet(authInfo)
        {
            AnnoValidita = anno,
            MeseValidita = mese
        });

        if (dati == null)
            return NotFound();

        if (tipo != null && tipo == "pdf")
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

    private async Task VerificaContratto(
        IMediator handler,
        IStringLocalizer<Localization> localizer,
        IAuthenticationInfo authInfo)
    {
        var contratto = await handler.Send(new ContrattoQueryGetById(authInfo)) ??
              throw new DomainException(localizer["DatiModuloCommessaEmptyContratto"]);

        if (authInfo.IdTipoContratto != contratto.IdTipoContratto || authInfo.Prodotto != contratto.Prodotto)
            throw new DomainException(localizer["DatiModuloCommessaEmptyContratto"]);
    }

    #endregion

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
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
        var response = modulo!.Mapper(authInfo);
        return Ok(response);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
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
        var response = modulo!.Mapper(authInfo);
        return Ok(response);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
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

        var response = modulo!.Mapper(authInfo);
        return Ok(response);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
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

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
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
        return Ok(modulo.Select(x => x.Mapper()));
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
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

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
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

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
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

        if (tipo != null && tipo == "pdf")
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