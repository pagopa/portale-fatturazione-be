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
    private async Task<Results<Ok<LanguageServiceResponsePII>, BadRequest, NotFound>> PostPiidAsync(
    HttpContext context,
    [FromBody] LanguageServiceRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] ILanguageService languageServiceHandler)
    {
        if(request.testo == null || request.testo.Length == 0)
            return BadRequest();

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
    private async Task<Results<Ok<LanguageServiceResponseLanguageDetection>, BadRequest, NotFound>> PostLanguageDetectionAsync(
    HttpContext context,
    [FromBody] LanguageServiceRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] ILanguageService languageServiceHandler)
    {
        if (request.testo == null || request.testo.Length == 0)
            return BadRequest();

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
    private async Task<Results<Ok<List<AbstractiveSummarizeResultCollection>>, BadRequest, NotFound>> PostSummarizeTextAsync(
HttpContext context,
[FromBody] LanguageServiceRequest request,
[FromServices] IStringLocalizer<Localization> localizer,
[FromServices] ILanguageService languageServiceHandler)
    {
        if (request.testo == null || request.testo.Length == 0)
            return BadRequest();

        var summarizeOperation = await languageServiceHandler.SummarizeTextAsync(request.testo);

        // check if summarizeOperation is null then return NotFound
        if (summarizeOperation == null)
            return NotFound();


        return Ok(summarizeOperation);
    }
}