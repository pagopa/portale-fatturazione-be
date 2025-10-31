using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Api.Modules.SEND.DatiFatturazioni.Payload.Request;
using PortaleFatture.BE.Api.Modules.SEND.DatiModuloCommesse.Extensions;
using PortaleFatture.BE.Api.Modules.SEND.DatiModuloCommesse.Payload.Request;
using PortaleFatture.BE.Api.Modules.SEND.DatiModuloCommesse.Payload.Response;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;
using PortaleFatture.BE.Infrastructure.Common.SEND.SelfCare.Queries;
using static Microsoft.AspNetCore.Http.TypedResults;

namespace PortaleFatture.BE.Api.Modules.DatiModuloCommesse;

public partial class DatiModuloCommessaModule
{
    #region PagoPA

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<bool>, NotFound>> CreateDatiModuloCommessaPrevisionalePagoPAAsync(
    HttpContext context,
    [FromBody] List<DatiModuloCommessaCreateRequest> req,
    [FromQuery] string idEnte,
    [FromQuery] int idTipoContratto,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        authInfo.IdEnte = idEnte;
        authInfo.IdTipoContratto = idTipoContratto;
        var idente = idEnte;

        var moduliCommessa = await DatiModuloCommessaViewModelExtensions.ValidateModuloCommessaPrevisionale(handler, req, authInfo);

        //verifica se ci sono obbligatori non inseriti 

        //var check = true;
        //if (moduliCommessa.IsNullNotAny() || moduliCommessa!.Where(x => x.Source!.ToLower().Contains("obbliga")).Select(x => x.Totale).Any(x => !x.HasValue))
        //{
        //    foreach (var sreq in req)
        //    {
        //        var filter = moduliCommessa!.Where(x => x.AnnoValidita == sreq.Anno && x.MeseValidita == sreq.Mese).FirstOrDefault();
        //        if (filter == null)
        //            check = check && false;
        //    }
        //}

        //if (!check)
        //    throw new DomainException(localizer["DatiModuloCommessaObbligatoriNotInserted"]);



        var verify = true;
        foreach (var sreq in req)
        {

            var command = sreq.Mapper(authInfo) ?? throw new ValidationException(localizer["DatiModuloCommessaInvalidMapping"]);

            command.Anno = sreq.Anno;
            command.Mese = sreq.Mese;

            foreach (var cmd in command.DatiModuloCommessaListCommand!)
            {
                cmd.IdEnte = idente;
                cmd.AnnoValidita = sreq.Anno;
                cmd.MeseValidita = sreq.Mese;
            }

            command.ValoriRegioni = sreq.ValoriRegioni;

            if (command.ValoriRegioni != null)
            {
                foreach (var cmd in command.ValoriRegioni)
                {
                    cmd.Internalistitutionid = idente;
                    cmd.Anno = sreq.Anno;
                    cmd.Mese = sreq.Mese;
                }
            }

            var modulo = await handler.Send(command) ?? throw new DomainException(localizer["DatiModuloCommessaInvalidMapping"]);
            if (modulo == null)
                verify = verify && false;
        }
        if (!verify)
            throw new DomainException(localizer["DatiModuloCommessaErrorInsertPrevisionale"]);

        return Ok(true);
    }


    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> PagoPADatiModuloCommessaRicercaDocumentoReportAsync(
    HttpContext context,
    [FromBody] EnteRicercaModuloCommessaByDescrizioneRequest req,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var reports = await handler.Send(new ModuloCommessaPrevisionaleDownloadQueryGet(authInfo)
        {
            Anno = req.Anno,
            Mese = req.Mese,
            IdEnti = req.IdEnti
        });

        if (reports.IsNullNotAny())
            return NotFound();

        var mime = "application/vnd.ms-excel";
        var filename = $"{Guid.NewGuid()}.xlsx";

        var dataSet = reports.FillOneSheet();
        var content = dataSet.ToExcel();

        return Results.File(content!, mime, filename);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<ModuloCommessaDocumentoDto>, NotFound>> GetDatiModuloCommessaDocumentoPagoPAAsync(
    HttpContext context,
    [FromRoute] int? anno,
    [FromRoute] int? mese,
    [FromQuery] string? idEnte,
    [FromServices] IMediator handler,
    [FromServices] IWebHostEnvironment hostingEnvironment,
    [FromServices] IStringLocalizer<Localization> localizer)
    {
        var authInfo = context.GetAuthInfo();
        authInfo.Prodotto = "prod-pn";
        authInfo.IdEnte = idEnte;

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
    private async Task<Results<Ok<IEnumerable<ModuloCommessaRegioneDto>>, NotFound>> GetDatiModuloCommessaListaObbligatoriPagoPAAsync(
      HttpContext context,
      [FromServices] IStringLocalizer<Localization> localizer,
      [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var regioni = await handler.Send(new DatiRegioniModuloCommessaQueryGet(authInfo)
        {

        });

        if (regioni.IsNullNotAny())
            return NotFound();

        return Ok(regioni);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<ModuloCommessaAnnoMeseDto>>, NotFound>> GetPagoPADatiModuloCommessaAnnoMeseAsync(
      HttpContext context,
      [FromServices] IStringLocalizer<Localization> localizer,
      [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        authInfo.Prodotto = "prod-pn";

        var annomesi = await handler.Send(new DatiModuloCommessaGetAnniMesi(authInfo));
        if (annomesi.IsNullNotAny())
            return NotFound();

        return Ok(annomesi);
    }


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
    private async Task<Results<Ok<ModuloCommessaPrevisionaleTotaleDto>, NotFound>> GetPagoPADatiModuloCommessav2Async(
    HttpContext context,
    [FromQuery] string? idEnte,
    [FromQuery] string? prodotto,
    [FromQuery] int? anno,
    [FromQuery] int? mese,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();

        //await VerificaContratto(handler, localizer, authInfo);

        var moduli = await handler.Send(new DatiModuloCommessaQueryGetByDescrizione(authInfo)
        {
            AnnoValidita = anno,
            MeseValidita = mese,
            Prodotto = prodotto == "" ? null : prodotto,
            IdEnti = [idEnte!]
        });

        if (moduli.IsNullNotAny())
            return NotFound();
        return Ok(moduli!.FirstOrDefault());
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
            Prodotto = req.Prodotto == "" ? null : req.Prodotto,
            IdEnti = req.IdEnti
        });

        if (moduli == null || moduli.Count() == 0)
            return NotFound();

        var mime = "application/vnd.ms-excel";
        var filename = $"{Guid.NewGuid()}.xlsx";

        var dataSet = moduli.Select(x => x.ToViewModel()).FillOneSheet();
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
    private async Task<Results<Ok<IEnumerable<ModuloCommessaPrevisionaleTotaleDto>>, NotFound>> PagoPADatiModuloCommessaRicercaAsync(
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
            Prodotto = req.Prodotto == "" ? null : req.Prodotto,
            IdEnti = req.IdEnti,
            RecuperaRegioni = false
        });

        if (moduli.IsNullNotAny())
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
    private async Task<Results<Ok<ModuloCommessaPrevisionaleObbligatoriResponse>, NotFound>> GetPagoPADatiModuloCommessaByAnnoMesev2Async(
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

        var moduli = await handler.Send(new DatiModuloCommessaPrevisionaleQueryGetByAnno(authInfo)
        {
            AnnoValidita = anno,
            MeseValidita = mese
        });

        if (moduli.IsNullNotAny())
            return NotFound();

        var modulo = moduli!.FirstOrDefault();
        var aderente = await handler.Send(new DatiModuloCommessaAderentiQueryGet(authInfo)
        {
            IdEnte = authInfo.IdEnte,
        });
        var totali = await handler.Send(new DatiModuloCommessaTotaleQueryGet(authInfo)
        {
            AnnoValidita = anno,
            MeseValidita = mese
        });


        return Ok(new ModuloCommessaPrevisionaleObbligatoriResponse()
        {
            DescrizioneMacrocategoriaVendita = aderente.SottocategoriaVendita,
            MacrocategoriaVendita = aderente.TipoDistribuzione,
            Lista = [modulo],
            Totali = totali!.ToList()
        });
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
    [FromQuery] int? idTipoContratto,
    [FromServices] IMediator handler,
    [FromServices] IWebHostEnvironment hostingEnvironment,
    [FromServices] IStringLocalizer<Localization> localizer)
    {
        var authInfo = context.GetAuthInfo();
        authInfo.IdEnte = idEnte;
        authInfo.Prodotto = prodotto;
        authInfo.IdTipoContratto = idTipoContratto;

        await VerificaContratto(handler, localizer, authInfo);

        var dati = await handler.Send(new DatiModuloCommessaDocumentoPagoPAQueryGet(authInfo)
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

        //var dati = await handler.Send(new DatiModuloCommessaDocumentoPagoPAQueryGet(authInfo)
        //{
        //    AnnoValidita = anno,
        //    MeseValidita = mese
        //});
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
        if (modulo == null || modulo.Count() == 0)
            return NotFound();
        return Ok(modulo.Select(x => x.Mapper()));
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<ModuloCommessaPrevisionaleTotaleDto>>, NotFound>> GetDatiModuloCommessaPrevisionaleByAnnoAsync(
    HttpContext context,
    [FromRoute] int? anno,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var modulo = await handler.Send(new DatiModuloCommessaPrevisionaleQueryGetByAnno(authInfo)
        {
            AnnoValidita = anno,
        });
        if (modulo.IsNullNotAny())
            return NotFound();
        return Ok(modulo);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<bool>, NotFound>> GetDatiModuloCommessaOkObbligatoriAsync(
    HttpContext context,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var modulo = await handler.Send(new DatiModuloCommessaPrevisionaleQueryGetByAnno(authInfo)
        {
            AnnoValidita = null,
        });
        if (modulo.IsNullNotAny())
            return NotFound();
        var verifica = modulo!
            .Where(x => x.Source!.Contains("obbliga", StringComparison.CurrentCultureIgnoreCase));
        var almenoUnoNull = verifica
            .Any(x => !x.TotaleNotifiche.HasValue);
        return Ok(almenoUnoNull);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<ModuloCommessaRegioneDto>>, NotFound>> GetDatiModuloCommessaListaObbligatoriAsync(
      HttpContext context,
      [FromServices] IStringLocalizer<Localization> localizer,
      [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var regioni = await handler.Send(new DatiRegioniModuloCommessaQueryGet(authInfo)
        {

        });

        if (regioni.IsNullNotAny())
            return NotFound();

        return Ok(regioni);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<ModuloCommessaPrevisionaleObbligatoriResponse>, NotFound>> GetDatiModuloCommessaByAnnoMesePrevisionaleAsync(
    HttpContext context,
    [FromRoute] int anno,
    [FromRoute] int mese,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var moduli = await handler.Send(new DatiModuloCommessaPrevisionaleQueryGetByAnno(authInfo)
        {
            AnnoValidita = anno,
            MeseValidita = mese
        });

        if (moduli.IsNullNotAny())
            return NotFound();

        var modulo = moduli!.FirstOrDefault();
        var aderente = await handler.Send(new DatiModuloCommessaAderentiQueryGet(authInfo)
        {
            IdEnte = authInfo.IdEnte,
        });
        var totali = await handler.Send(new DatiModuloCommessaTotaleQueryGet(authInfo)
        {
            AnnoValidita = anno,
            MeseValidita = mese
        });


        return Ok(new ModuloCommessaPrevisionaleObbligatoriResponse()
        {
            DescrizioneMacrocategoriaVendita = aderente.SottocategoriaVendita,
            MacrocategoriaVendita = aderente.TipoDistribuzione,
            Lista = [modulo],
            Totali = totali!.ToList()
        });
    }

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<bool>, NotFound>> CreateDatiModuloCommessaPrevisionaleAsync(
    HttpContext context,
    [FromBody] List<DatiModuloCommessaCreateRequest> req,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var idente = authInfo.IdEnte;

        var moduliCommessa = await DatiModuloCommessaViewModelExtensions.ValidateModuloCommessaPrevisionale(handler, req, authInfo);

        // se mese di rieferimento è >19 ti blocco?

        //verifica se ci sono obbligatori non inseriti 
        //var moduliCommessaObbligatori = moduliCommessa!.Where(x => x.Source!.ToLower().Contains("obbliga")); 
        //var filtraObbligatoriConValoriNull = moduliCommessaObbligatori.Where(x => x.TotaleNotifiche == null); 

        //if (moduliCommessaObbligatori.IsNullNotAny())
        //    throw new DomainException(localizer["DatiModuloCommessaObbligatoriNotInserted"]); 


        ////verifica se il periodo di riferimento è corretto
        //var (annoAttuale, meseAttuale, giornoAttuale, adesso) = Time.YearMonthDay();
        //var calendario = await handler.Send(new DatiPrevisionaleCalendarioQuery(authInfo)
        //{
        //    AnnoRiferimento = annoAttuale,
        //    MeseRiferimento = meseAttuale
        //});

        //var giornoAttualeDateOnly = new DateTime(annoAttuale, meseAttuale, giornoAttuale);

        //var filteredRequests = req.Where(x =>
        //    calendario.Any(c => 
        //        x.Anno == c.AnnoRiferimento &&
        //        x.Mese == c.MeseRiferimento && 
        //        c.Datavalidita.Date <= giornoAttualeDateOnly.Date
        //    )
        //).ToList();

        //if (filteredRequests.Count != req.Count)
        //    throw new ValidationException(localizer["DatiModuloCommessaInvalidMapping"]);

        var verify = true;
        foreach (var sreq in req)
        {

            var command = sreq.Mapper(authInfo) ?? throw new ValidationException(localizer["DatiModuloCommessaInvalidMapping"]);

            command.Anno = sreq.Anno;
            command.Mese = sreq.Mese;

            foreach (var cmd in command.DatiModuloCommessaListCommand!)
            {
                cmd.IdEnte = idente;
                cmd.AnnoValidita = sreq.Anno;
                cmd.MeseValidita = sreq.Mese;
            }

            command.ValoriRegioni = sreq.ValoriRegioni;

            if (command.ValoriRegioni != null)
            {
                foreach (var cmd in command.ValoriRegioni)
                {
                    cmd.Internalistitutionid = idente;
                    cmd.Anno = sreq.Anno;
                    cmd.Mese = sreq.Mese;
                }
            }

            var modulo = await handler.Send(command) ?? throw new DomainException(localizer["DatiModuloCommessaInvalidMapping"]);
            if (modulo == null)
                verify = verify && false;
        }
        if (!verify)
            throw new DomainException(localizer["DatiModuloCommessaErrorInsertPrevisionale"]);

        return Ok(true);
    }


    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<ModuloCommessaPrevisionaleObbligatoriResponse>, NotFound>> GetDatiModuloCommessaRegioniAsync(
      HttpContext context,
      [FromServices] IStringLocalizer<Localization> localizer,
      [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var modulo = await handler.Send(new DatiModuloCommessaPrevisionaleQueryGetByAnno(authInfo)
        {
            AnnoValidita = null,
        });
        if (modulo.IsNullNotAny())
            return NotFound();
        var filtered = modulo!
            .Where(x => x.Source!.Contains("obbliga", StringComparison.CurrentCultureIgnoreCase))
            .ToList();

        var lista = filtered.Any(x => !x.Totale.HasValue)
            ? filtered
            : [];

        if (lista.IsNullNotAny())
            return NotFound();

        var aderente = await handler.Send(new DatiModuloCommessaAderentiQueryGet(authInfo)
        {
            IdEnte = authInfo.IdEnte,
        });

        return Ok(new ModuloCommessaPrevisionaleObbligatoriResponse()
        {
            DescrizioneMacrocategoriaVendita = aderente.SottocategoriaVendita,
            MacrocategoriaVendita = aderente.TipoDistribuzione,
            Lista = lista
        });
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
        if (modulo == null || modulo.Count() == 0)
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
        if (anni.IsNullNotAny())
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