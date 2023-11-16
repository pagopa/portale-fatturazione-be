﻿using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Api.Modules.DatiFatturazioni.Extensions;
using PortaleFatture.BE.Api.Modules.DatiFatturazioni.Payload.Response;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries;
using static Microsoft.AspNetCore.Http.TypedResults;

namespace PortaleFatture.BE.Api.Modules.DatiFatturazioni;

public partial class TipologieModule
{
    [AllowAnonymous]
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

    [AllowAnonymous]
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

    [AllowAnonymous]
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

    [AllowAnonymous]
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
}