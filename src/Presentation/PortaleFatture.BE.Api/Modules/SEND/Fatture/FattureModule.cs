using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Api.Infrastructure.Documenti;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Extensions;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Request;
using PortaleFatture.BE.Api.Modules.SEND.Fatture.Payload.Response;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Fatture.Service;
using PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries;
using PortaleFatture.BE.Infrastructure.Gateway.Storage;
using static Microsoft.AspNetCore.Http.TypedResults;

namespace PortaleFatture.BE.Api.Modules.Fatture;

public partial class FattureModule
{

    #region pagoPA

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<string>>, NotFound>> GetAnniFattureAsync(
    HttpContext context,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
        {
            var authInfo = context.GetAuthInfo();

            var anni = await handler.Send(new FattureAnniQuery(authInfo));
            if (anni.IsNullNotAny())
                return NotFound();
            return Ok(anni);
        }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<FattureMeseResponse>>, NotFound>> PostMesiFattureAsync(
    HttpContext context,
    [FromBody] FattureMesiRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();

        var mesi = await handler.Send(new FattureMesiQuery(authInfo)
        {
            Anno = request.Anno
        });

        if (mesi.IsNullNotAny())
            return NotFound();

        return Ok(mesi!.Select(x => new FattureMeseResponse()
        {
            Mese = x,
            Descrizione = Convert.ToInt32(x).GetMonth()
        }));
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<bool>, NotFound>> PostResettaFatturePipelineSapAsync(
    HttpContext context,
    [FromBody] FatturaPipelineSapRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var resetta = await handler.Send(request.Map2(authInfo, invio: false));
        return Ok(resetta);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<object> PostFatturePipelineSapAsync(
    HttpContext context,
    [FromBody] FatturaPipelineSapRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] ISynapseService synapseService,
    [FromServices] IPortaleFattureOptions options,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var result = await synapseService.InviaASapFatture(options.Synapse!.PipelineNameSAP, request.Map());
     
        if (result == true)
        {
            var command = request.Map2(authInfo, invio: true);
            command.FatturaInviata = null;
            command.StatoAtteso = 0;
            result = await handler.Send(command);
        }
        if (!result)
        {
            await Results.StatusCode(500).ExecuteAsync(context);
            return null!;
        }
           
        return Ok(result);  
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<FatturaInvioSap>>, NotFound>> PostFattureInvioSapAsync(
    HttpContext context,
    [FromBody] FatturaInvioSapRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var invioSap = await handler.Send(new FattureInvioSapQuery(authInfo)
        {
            Anno = request.Anno,
            Mese = request.Mese
        });

        if (invioSap == null || !invioSap!.Any())
            return NotFound();

        var maxOrdineByAzione = invioSap
            .GroupBy(f => f.Azione)
            .Select(g => g.OrderByDescending(f => f.Ordine).First())
            .Where(c => c.Azione != 2); // diverso da disabilitato

        if(maxOrdineByAzione.IsNullNotAny())
            return NotFound();

        return Ok(maxOrdineByAzione);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<bool>, BadRequest>> PostCancellazioneFatture(
    HttpContext context,
    [FromBody] FatturaCancellazioneRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        // verifica se c'è malleabilità

        var result = await handler.Send(new FatturaCancellazioneCommand(authInfo, request.IdFatture, request.Cancellazione));
        if (result.Value == false)
            return BadRequest(); 
        return Ok(result.Value);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<string>>, NotFound>> PostTipologiaFatture(
    HttpContext context,
    [FromBody] TipologiaFattureRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var tipoFattura = await handler.Send(new TipoFatturaQueryGetAll(authInfo, request.Anno, request.Mese, request.Cancellata == null ? false : request.Cancellata.Value));
        if (tipoFattura.IsNullNotAny())
            return NotFound();
        return Ok(tipoFattura);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<FattureListaDto>, NotFound>> PostFattureByRicercaAsync(
    HttpContext context,
    [FromBody] FatturaRicercaRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var fatture = await handler.Send(request.Map(authInfo));
        if (fatture == null || !fatture!.Any())
            return NotFound();
        return Ok(fatture);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> PostFattureExcelByRicercaAsync(
    HttpContext context,
    [FromBody] FatturaRicercaRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var fatture = await handler.Send(request.Map(authInfo));
        if (fatture == null || !fatture!.Any())
            return NotFound();
        var mime = "application/vnd.ms-excel";
        var filename = $"{Guid.NewGuid()}.xlsx";

        var dataSet = fatture.Map()!.FillOneSheetv2();
        var content = dataSet.ToExcel();
        var result = new DisposableStreamResult(content, mime)
        {
            FileDownloadName = filename
        };
        return Results.Stream(result.FileStream, result.ContentType, result.FileDownloadName);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> PostFattureReportByRicercaAsync(
    HttpContext context,
    [FromBody] FatturaRicercaRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] ILogger<FattureModule> logger,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo(); 

        var reports = await request.ReportFatture(handler, authInfo);

        if (reports.Count > 0)
        {
            var fileBytes = reports.CreateZip(logger);
            var filename = $"{Guid.NewGuid()}.zip";
            return Results.File(fileBytes!, MimeMapping.ZIP, filename);
        }

        return NotFound();
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> PostFatturePrenotazioneReportByRicercaAsync(
        HttpContext context,
        [FromBody] FatturaRicercaRequest request,
        [FromServices] IStringLocalizer<Localization> localizer,
        [FromServices] ILogger<FattureModule> logger,
        [FromServices] IMediator handler,
        [FromServices] IDocumentStorageService storageService)
    {
        var authInfo = context.GetAuthInfo();
 
        var contentType = MimeMapping.ZIP;
        var contentLanguage = LanguageMapping.IT;

        var (command, key) = request.Mapv2(authInfo, contentType, contentLanguage);

        var result = await handler.Send(command);

        if (result.HasValue && result == true)
        {
            await Task.Run(async () =>
            {
                var reports = await request.ReportFatture(handler, authInfo);
                if (reports.Count > 0) // copia file e poi aggiorna messaggio stato=2 con link
                {
                    bool? result = false;
                    var memoryStream = reports.CreateMemoryStreamZip(logger);
                    result = await storageService.AddDocumentMessagePagoPA(memoryStream, key, contentType, contentLanguage);
                    if (result == true)
                    {
                        var updateCommand = new MessaggioUpdateCommand(authInfo)
                        {
                            Hash = command.Hash,
                            LinkDocumento = command.LinkDocumento
                        };
                        await handler.Send(updateCommand);
                    }
                }
            });
        }
        else
        {
            var msg = $"Errore nella richiesta del documento!";
            logger.LogError(msg);
            throw new DomainException(msg);
        }
        return Ok();
    }
    #endregion

    #region ente
    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<string>>, NotFound>> PostTipologiaEnteFatture(
    HttpContext context,
    [FromBody] TipologiaFattureRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var tipoFattura = await handler.Send(new TipoFatturaQueryGetAllByIdEnte(authInfo, request.Anno, request.Mese));
        if (tipoFattura.IsNullNotAny())
            return NotFound();
        return Ok(tipoFattura);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<FattureListaDto>, NotFound>> PostFattureEnteByRicercaAsync(
    HttpContext context,
    [FromBody] FatturaRicercaEnteRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var fatture = await handler.Send(request.Map(authInfo));
        if (fatture == null || !fatture!.Any())
            return NotFound();
        return Ok(fatture);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> PostFattureEnteExcelByRicercaAsync(
    HttpContext context,
    [FromBody] FatturaRicercaEnteRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var fatture = await handler.Send(request.Map(authInfo));
        if (fatture == null || !fatture!.Any())
            return NotFound();
        var mime = "application/vnd.ms-excel";
        var filename = $"{Guid.NewGuid()}.xlsx";

        var dataSet = fatture.Map()!.FillOneSheetv2();
        var content = dataSet.ToExcel();
        var result = new DisposableStreamResult(content, mime)
        {
            FileDownloadName = filename
        };
        return Results.Stream(result.FileStream, result.ContentType, result.FileDownloadName);
    }
    #endregion
}