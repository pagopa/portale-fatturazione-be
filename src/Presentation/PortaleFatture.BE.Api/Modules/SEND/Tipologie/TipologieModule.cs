﻿using System.Net.Http;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Org.BouncyCastle.Utilities;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Api.Modules.SEND.DatiFatturazioni.Extensions;
using PortaleFatture.BE.Api.Modules.SEND.DatiFatturazioni.Payload.Response;
using PortaleFatture.BE.Api.Modules.SEND.Tipologie;
using PortaleFatture.BE.Api.Modules.SEND.Tipologie.Extensions;
using PortaleFatture.BE.Api.Modules.SEND.Tipologie.Payload.Payload.Request;
using PortaleFatture.BE.Api.Modules.SEND.Tipologie.Payload.Payload.Response;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Common.SEND.Scadenziari.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries;
using PortaleFatture.BE.Infrastructure.Gateway.Storage;
using static Microsoft.AspNetCore.Http.TypedResults;

namespace PortaleFatture.BE.Api.Modules.Tipologie;
public partial class TipologieModule
{

    static string GetFileNameFromUri(string uri)
    {
        return uri.Substring(uri.LastIndexOf('/') + 1).Split('?')[0];
    }

    [AllowAnonymous]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> GetManualeDownload(
    HttpContext context,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IHttpClientFactory clientFactory,
    [FromServices] IManualiStorageSASService sASService)
    {
        var fileUri = sASService.GetSASToken();
        var httpClient = clientFactory.CreateClient();
        using var response = await httpClient.GetAsync(fileUri, HttpCompletionOption.ResponseHeadersRead);

        if (!response.IsSuccessStatusCode)
        {
            return NotFound();
        } 
 
        var mime = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        var fileName = GetFileNameFromUri(fileUri);
 
        var stream = await response.Content.ReadAsStreamAsync();

        var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream); 
        memoryStream.Seek(0, SeekOrigin.Begin); 
        return Results.File(memoryStream, mime, fileName);
    }

    [AllowAnonymous]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private Results<Ok<string>, NotFound> GetManuale(
    HttpContext context,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IManualiStorageSASService sASService)
    {
        return Ok(sASService.GetSASToken());
    }

    [AllowAnonymous]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<DateTime>, NotFound>> GetTimeAsync(
    HttpContext context,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        return Ok(await Task.Run(() => DateTime.UtcNow.ItalianTime()));
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}")]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<CalendarioContestazioniResponse>>, NotFound>> GetScadenziarioContestazioniByDescrizioneAsync(
    HttpContext context,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var scadenziario = await handler.Send(new CalendarioContestazioneQueryGetAll(authInfo));
        if (scadenziario.IsNullNotAny())
            return NotFound();
        return Ok(scadenziario.Mapper());
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<string>>, NotFound>> AllEntiByDescrizioneAsync(
    HttpContext context,
    [FromBody] EnteRicercaByRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var enti = await handler.Send(new EnteQueryGetByRagioneSociale(authInfo, request.Descrizione, request.Prodotto, request.Profilo));
        if (enti.IsNullNotAny())
            return NotFound();
        return Ok(enti);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<EnteResponse>>, NotFound>> AllEntiCompletiByDescrizioneAsync(
    HttpContext context,
    [FromBody] EnteRicercaByDescrizioneRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var enti = await handler.Send(new EnteQueryGetByDescrizione(authInfo, request.Descrizione));
        if (enti.IsNullNotAny())
            return NotFound();
        return Ok(enti.Select(x => x.Map()));
    }


    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<EnteResponse>>, NotFound>> AllEntiCompletiFornitoriByTipoAsync(
    HttpContext context,
    [FromBody] EnteRicercaByRecapitistiConsolidatoriRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var enti = await handler.Send(new EnteQueryGetByRecapitistiConsolidatori(authInfo, request.Tipo));
        if (enti.IsNullNotAny())
            return NotFound();
        return Ok(enti.Select(x => x.Map()));
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCareConsolidatorePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<EnteResponse>>, NotFound>> AllEntiCompletiConsolidatoreByDescrizioneAsync(
    HttpContext context,
    [FromBody] EnteRicercaByDescrizioneRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var enti = await handler.Send(new EnteQueryGetByDescrizione(authInfo, request.Descrizione));
        if (enti.IsNullNotAny())
            return NotFound();
        return Ok(enti.Select(x => x.Map()));
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}")]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<string>>, NotFound>> GetAllTipoProfiloAsync(
    HttpContext context,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var tipiProfilo = await handler.Send(new ProfiloQueryGetAll());
        if (tipiProfilo.IsNullNotAny())
            throw new ConfigurationException(localizer["DatiTipoProfiloMissing"]);
        return Ok(tipiProfilo);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}")]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<TipoContrattoResponse>>, NotFound>> GetAllTipoContrattoAsync(
    HttpContext context,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var tipiContratto = await handler.Send(new TipoContrattoQueryGetAll());
        if (tipiContratto.IsNullNotAny())
            throw new ConfigurationException(localizer["DatiTipoContrattoMissing"]);
        return Ok(tipiContratto.Mapper());
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}")]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<TipoCommessaResponse>>, NotFound>> GetAllTipoCommessaAsync(
    HttpContext context,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var tipiCommessa = await handler.Send(new TipoCommessaQueryGetAll());
        if (tipiCommessa.IsNullNotAny())
            throw new ConfigurationException(localizer["DatiTipoCommessaMissing"]);
        return Ok(tipiCommessa.Mapper());
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}")]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<ProdottoResponse>>, NotFound>> GetAllProdottoAsync(
    HttpContext context,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var prodotti = await handler.Send(new ProdottoQueryGetAll());
        if (prodotti.IsNullNotAny())
            throw new ConfigurationException(localizer["DatiProdottoMissing"]);
        return Ok(prodotti.Mapper());
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}")]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<CategoriaSpedizioneResponse>>, NotFound>> GetAllCategoriaSpedizioneAsync(
    HttpContext context,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var categorie = await handler.Send(new SpedizioneQueryGetAll());
        if (categorie.IsNullNotAny())
            throw new ConfigurationException(localizer["DatiProdottoMissing"]);
        return Ok(categorie.Mapper());
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}")]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<TipoContestazione>>, NotFound>> GetAllTipologiaContestazioniAsync(
    HttpContext context,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var tipologie = await handler.Send(new TipoContestazioneGetAll());
        if (tipologie.IsNullNotAny())
            throw new ConfigurationException(localizer["TipoContestazioneMissing"]);
        return Ok(tipologie);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}")]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<FlagContestazione>>, NotFound>> GetAllFlagContestazioniAsync(
    HttpContext context,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var flags = await handler.Send(new FlagContestazioneQueryGetAll());
        if (flags.IsNullNotAny())
            throw new ConfigurationException(localizer["FlagContestazioneMissing"]);
        return Ok(flags);
    }
}