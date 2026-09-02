using Azure.AI.TextAnalytics;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Api.Modules.SEND.LanguageService.Payload.Request;
using PortaleFatture.BE.Api.Modules.SEND.LanguageService.Payload.Response;
using PortaleFatture.BE.Api.Modules.SEND.Messaggi.Extensions;
using PortaleFatture.BE.Api.Modules.SEND.Notifiche.Extensions;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Common.Language.Service;
using PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Queries;
using PortaleFatture.BE.Infrastructure.Gateway.Storage;
using static Microsoft.AspNetCore.Http.TypedResults;

namespace PortaleFatture.BE.Api.Modules.LanguageService;

public partial class LanguageService
{

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    private async Task<Results<Ok<LanguageServiceResponsePII>, BadRequest, NotFound, ProblemHttpResult>> PostPiidAsync(
    HttpContext context,
    [FromBody] LanguageServiceRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] ILanguageService languageServiceHandler)
    {
        if(request.testo == null || request.testo.Length == 0)
            return BadRequest();

        if (!languageServiceHandler.IsConfigured)
            return ServizioNonConfigurato();

        var piiEntities = await languageServiceHandler.DetectPersonalIdentifiableInformationAsync(request.testo);

        // check if piiEntities is null or empty then return NotFound
        if (piiEntities == null || !piiEntities.Any())
            return NotFound();

        // convert piientities to PiiCollection
        var response = new LanguageServiceResponsePII
        {
            RedactedString = piiEntities.RedactedText
        };

        return Ok(response);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    private async Task<Results<Ok<LanguageServiceResponseLanguageDetection>, BadRequest, NotFound, ProblemHttpResult>> PostLanguageDetectionAsync(
    HttpContext context,
    [FromBody] LanguageServiceRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] ILanguageService languageServiceHandler)
    {
        if (request.testo == null || request.testo.Length == 0)
            return BadRequest();

        if (!languageServiceHandler.IsConfigured)
            return ServizioNonConfigurato();

        DetectedLanguage? detectedLanguage = await languageServiceHandler.DetectLanguageAsync(request.testo);

        // check if detectedLanguage is null then return NotFound
        if (detectedLanguage == null)
            return NotFound();

        // convert detectedLanguage to LanguageServiceResponseLanguageDetection
        var response = new LanguageServiceResponseLanguageDetection
        (
            detectedLanguage.Value.Name,
            detectedLanguage.Value.ConfidenceScore
        );

        return Ok(response);
    }


    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    private async Task<Results<Ok<LanguageServiceResponseSummarizeText>, BadRequest, NotFound, ProblemHttpResult>> PostSummarizeTextAsync(
    HttpContext context,
    [FromBody] LanguageServiceRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] ILanguageService languageServiceHandler)
    {
        if (request.testo == null || request.testo.Length == 0)
            return BadRequest();

        if (!languageServiceHandler.IsConfigured)
            return ServizioNonConfigurato();

        var summarizeOperation = await languageServiceHandler.SummarizeTextAsync(request.testo);

        // check if summarizeOperation is null then return NotFound
        if (summarizeOperation == null)
            return NotFound();

        // Mappatura verso il contratto NOSTRO: il tipo dell'SDK Azure non esce dall'API. La struttura
        // e' annidata (collection -> risultati per documento -> summaries), ma qui il documento e' uno
        // solo — quello che il chiamante ha inviato — quindi si appiattisce ai testi delle sintesi.
        var sintesi = summarizeOperation
            .SelectMany(collection => collection)
            .SelectMany(risultato => risultato.Summaries)
            .Select(summary => summary.Text)
            .Where(testo => !string.IsNullOrWhiteSpace(testo))
            .ToList();

        // Nessuna sintesi prodotta: e' un risultato vuoto, non un errore (i guasti del servizio
        // arrivano qui come eccezione e diventano 502/504).
        if (sintesi.Count == 0)
            return NotFound();

        return Ok(new LanguageServiceResponseSummarizeText { Sintesi = sintesi });
    }

    /// <summary>
    /// Risposta per l'ambiente in cui Azure AI Language non e' configurato: **503 Service Unavailable**,
    /// non 500 e non 404.
    ///
    /// La distinzione conta per chi consuma l'API: 404 direbbe "non ho trovato PII in questo testo",
    /// 500 "qualcosa e' andato storto"; 503 dice "questa funzione qui non e' disponibile", che e' un
    /// fatto di ambiente e non un esito dell'elaborazione. E' anche l'unico dei tre su cui ha senso
    /// che il client non riprovi con altri input.
    /// </summary>
    private static ProblemHttpResult ServizioNonConfigurato() => Problem(
        detail: "Il servizio Azure AI Language non e' configurato su questo ambiente.",
        statusCode: StatusCodes.Status503ServiceUnavailable);
}