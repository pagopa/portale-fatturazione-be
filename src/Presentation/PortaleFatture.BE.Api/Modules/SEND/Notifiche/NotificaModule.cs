using System.Globalization;
using System.Net;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using CsvHelper;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Api.Infrastructure.Documenti;
using PortaleFatture.BE.Api.Modules.SEND.DatiFatturazioni.Payload.Request;
using PortaleFatture.BE.Api.Modules.SEND.Notifiche.Extensions;
using PortaleFatture.BE.Api.Modules.SEND.Notifiche.Payload.Request;
using PortaleFatture.BE.Api.Modules.SEND.Notifiche.Payload.Response;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Common;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Services;
using PortaleFatture.BE.Infrastructure.Common.SEND.SelfCare.Queries;
using static Microsoft.AspNetCore.Http.TypedResults;

namespace PortaleFatture.BE.Api.Modules.Notifiche;


public partial class NotificaModule
{
    #region pagoPA-Enti
    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}")]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<string>>, NotFound>> GetAnniNotificaAsync(
    HttpContext context,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();

        var anni = await handler.Send(new NotificheAnniQuery(authInfo));
        if (anni.IsNullNotAny())
            return NotFound();
        return Ok(anni);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}")]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<NotificheMeseResponse>>, NotFound>> PostMesiNotificaAsync(
    HttpContext context,
    [FromBody] NotificheMesiRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();

        var mesi = await handler.Send(new NotificheMesiQuery(authInfo)
        {
            Anno = request.Anno
        });

        if (mesi.IsNullNotAny())
            return NotFound();

        return Ok(mesi!.Select(x => new NotificheMeseResponse()
        {
            Mese = x,
            Descrizione = Convert.ToInt32(x).GetMonth()
        }));
    }

    #endregion
    #region pagoPA
    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<NotificaDto>, NotFound>> GetPagoPANotificheByRicercaAsyncv2(
    HttpContext context,
    [FromBody] NotificheRicercaRequestPagoPA request,
    [FromQuery] int page,
    [FromQuery] int pageSize,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var notifiche = await handler.Send(request.Mapv2(authInfo, page, pageSize));
        if (notifiche == null || notifiche.Count == 0)
            return NotFound();
        return Ok(notifiche);
    }


    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<NotificaDto>, NotFound>> GetPagoPANotificheByRicercaAsync(
    HttpContext context,
    [FromBody] NotificheRicercaRequestPagoPA request,
    [FromQuery] int page,
    [FromQuery] int pageSize,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var notifiche = await handler.Send(request.Map(authInfo, page, pageSize));
        if (notifiche == null || notifiche.Count == 0)
            return NotFound();
        return Ok(notifiche);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<ContestazioneResponse>, NotFound>> GetPagoPAContestazioneAsync(
    HttpContext context,
    [FromRoute] string idNotifica,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var command = new AzioneContestazioneQueryGetByIdNotifica(authInfo, idNotifica);
        var azione = await handler.Send(command);

        Contestazione contestazione;
        if (azione!.Contestazione == null)
            contestazione = new Contestazione()
            {
                IdNotifica = idNotifica,
                StatoContestazione = azione!.Notifica!.StatoContestazione,
                Anno = Convert.ToInt16(azione!.Notifica.Anno),
                Mese = Convert.ToInt16(azione!.Notifica.Mese),
            };
        else
            contestazione = azione!.Contestazione;

        var response = new ContestazioneResponse()
        {
            Contestazione = contestazione,
            Modifica = azione!.CreazionePermessa,
            Chiusura = azione!.ChiusuraPermessa,
            Risposta = azione!.RispostaPermessa
        };
        return Ok(response);
    }

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<Contestazione>, NotFound>> UpdatePagoPAContestazioneAsync(
    HttpContext context,
    [FromBody] ContestazionePagoPAUpdateRequest req,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();

        var command = new ContestazioneUpdatePagoPACommand(authInfo, req.IdNotifica)
        {
            NoteSend = req.NoteSend,
            Onere = req.Onere,
            StatoContestazione = req.StatoContestazione
        };

        var contestazione = await handler.Send(command);
        return Ok(contestazione);
    }


    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> GetPagoPANotificheRicercaDocumentAsync(
    HttpContext context,
    [FromBody] NotificheRicercaRequestPagoPA request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler,
    [FromQuery] bool? binary = null)
    {
        var authInfo = context.GetAuthInfo();
        var notifiche = await handler.Send(request.Map(authInfo, null, null));
        if (notifiche == null || notifiche.Count == 0)
            return NotFound();

        var stream = await notifiche.Notifiche!.ToStream<SimpleNotificaDto, SimpleNotificaPagoPADtoMap>();
        if (stream.Length == 0)
            return NotFound();

        var filename = $"{Guid.NewGuid()}.csv";
        var mimeCsv = "text/csv";
        stream.Position = 0;
        return Results.Stream(stream, mimeCsv, filename);
    }
    #endregion

    #region consolidatore

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.SelfCareConsolidatorePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]

    private async Task<Results<Ok<Contestazione>, NotFound>> UpdateConsolidatoreContestazioneAsync(
    HttpContext context,
    [FromBody] ContestazioneConsolidatori req,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();

        var command = new ContestazioneUpdateConsolidatoreCommand(authInfo, req.IdNotifica)
        {
            NoteConsolidatore = req.Risposta,
            StatoContestazione = req.StatoContestazione
        };

        var contestazione = await handler.Send(command);
        return Ok(contestazione);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCareConsolidatorePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<ContestazioneResponse>, NotFound>> GetConsolidatoriContestazioneAsync(
    HttpContext context,
    [FromRoute] string idNotifica,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var command = new AzioneContestazioneQueryGetByIdNotifica(authInfo, idNotifica);
        var azione = await handler.Send(command);

        Contestazione contestazione;
        if (azione!.Contestazione == null)
            contestazione = new Contestazione()
            {
                IdNotifica = idNotifica,
                StatoContestazione = azione!.Notifica!.StatoContestazione,
                Anno = Convert.ToInt16(azione!.Notifica.Anno),
                Mese = Convert.ToInt16(azione!.Notifica.Mese),
            };
        else
            contestazione = azione!.Contestazione;

        var response = new ContestazioneResponse()
        {
            Contestazione = contestazione,
            Modifica = azione!.CreazionePermessa,
            Chiusura = azione!.ChiusuraPermessa,
            Risposta = azione!.RispostaPermessa
        };
        return Ok(response);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCareConsolidatorePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> GetConsolidatoriNotificheRicercaDocumentAsync(
    HttpContext context,
    [FromBody] NotificheRicercaRequest request,
    [FromQuery] bool? binary,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var notifiche = await handler.Send(request.Map3(authInfo, null, null));
        if (notifiche == null || notifiche.Count == 0)
            return NotFound();


        if (binary == null)
        {
            byte[] data;
            using (var stream = new MemoryStream())
            using (TextWriter textWriter = new StreamWriter(stream))
            using (var csv = new CsvWriter(textWriter, new CultureInfo("it-IT")))
            {
                csv.WriteRecords(notifiche.Notifiche!);
                textWriter.Flush();
                data = stream.ToArray();
            }
            return Ok(new DocumentDto() { Documento = Convert.ToBase64String(data) });
        }
        else if (binary == true)
        {
            var mime = "application/vnd.ms-excel";
            var filename = $"{Guid.NewGuid()}.xlsx";
            var dataSet = notifiche.Notifiche!.FillOneSheetRECCON();
            var content = dataSet.ToExcel();
            return Results.File(content!, mime, filename);
        }
        else
        {
            var mime = "text/csv";
            var filename = $"{Guid.NewGuid()}.csv";
            byte[] data;
            using (var stream = new MemoryStream())
            using (TextWriter textWriter = new StreamWriter(stream))
            using (var csv = new CsvWriter(textWriter, new CultureInfo("it-IT")))
            {
                csv.WriteRecords(notifiche.Notifiche!);
                textWriter.Flush();
                data = stream.ToArray();
            }
            return Results.File(data!, mime, filename);
        }
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCareConsolidatorePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<int?>, NotFound>> GetConsolidatoriNotificheCountAsync(
    HttpContext context,
   [FromServices] IStringLocalizer<Localization> localizer,
   [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var command = new ReportNotificheQueryCount(authInfo) { };

        var count = await handler.Send(command);
        return Ok(count);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCareConsolidatorePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<NotificaRECCONDto>, NotFound>> GetConsolidatoriNotificheByRicercaAsync(
    HttpContext context,
    [FromBody] NotificheRicercaRequest request,
    [FromQuery] int page,
    [FromQuery] int pageSize,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var notifiche = await handler.Send(request.Map3(authInfo, page, pageSize));
        if (notifiche == null || notifiche.Count == 0)
            return NotFound();
        return Ok(notifiche);
    }

    #endregion
    #region recapitisti

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCareRecapitistaPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> GetRecapitistaNotificheRicercaDocumentAsync(
    HttpContext context,
    [FromBody] NotificheRicercaRequest request,
    [FromQuery] bool? binary,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var notifiche = await handler.Send(request.Map2(authInfo, null, null));
        if (notifiche == null || notifiche.Count == 0)
            return NotFound();


        if (binary == null)
        {
            byte[] data;
            using (var stream = new MemoryStream())
            using (TextWriter textWriter = new StreamWriter(stream))
            using (var csv = new CsvWriter(textWriter, new CultureInfo("it-IT")))
            {
                csv.WriteRecords(notifiche.Notifiche!);
                textWriter.Flush();
                data = stream.ToArray();
            }
            return Ok(new DocumentDto() { Documento = Convert.ToBase64String(data) });
        }
        else if (binary == true)
        {
            var mime = "application/vnd.ms-excel";
            var filename = $"{Guid.NewGuid()}.xlsx";
            var dataSet = notifiche.Notifiche!.FillOneSheetRECCON();
            var content = dataSet.ToExcel();
            return Results.File(content!, mime, filename);
        }
        else
        {
            var mime = "text/csv";
            var filename = $"{Guid.NewGuid()}.csv";
            byte[] data;
            using (var stream = new MemoryStream())
            using (TextWriter textWriter = new StreamWriter(stream))
            using (var csv = new CsvWriter(textWriter, new CultureInfo("it-IT")))
            {
                csv.WriteRecords(notifiche.Notifiche!);
                textWriter.Flush();
                data = stream.ToArray();
            }
            return Results.File(data!, mime, filename);
        }
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCareRecapitistaPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<ContestazioneResponse>, NotFound>> GetRecapitistiContestazioneAsync(
    HttpContext context,
    [FromRoute] string idNotifica,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var command = new AzioneContestazioneQueryGetByIdNotifica(authInfo, idNotifica);
        var azione = await handler.Send(command);

        Contestazione contestazione;
        if (azione!.Contestazione == null)
            contestazione = new Contestazione()
            {
                IdNotifica = idNotifica,
                StatoContestazione = azione!.Notifica!.StatoContestazione,
                Anno = Convert.ToInt16(azione!.Notifica.Anno),
                Mese = Convert.ToInt16(azione!.Notifica.Mese),
            };
        else
            contestazione = azione!.Contestazione;

        var response = new ContestazioneResponse()
        {
            Contestazione = contestazione,
            Modifica = azione!.CreazionePermessa,
            Chiusura = azione!.ChiusuraPermessa,
            Risposta = azione!.RispostaPermessa
        };
        return Ok(response);
    }


    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCareRecapitistaPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<int?>, NotFound>> GetRecapitistiNotificheCountAsync(
    HttpContext context,
   [FromServices] IStringLocalizer<Localization> localizer,
   [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var command = new ReportNotificheQueryCount(authInfo) { };

        var count = await handler.Send(command);
        return Ok(count);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCareRecapitistaPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<NotificaRECCONDto>, NotFound>> GetRecapitistiNotificheByRicercaAsync(
    HttpContext context,
    [FromBody] NotificheRicercaRequest request,
    [FromQuery] int page,
    [FromQuery] int pageSize,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var notifiche = await handler.Send(request.Map2(authInfo, page, pageSize));
        if (notifiche == null || notifiche.Count == 0)
            return NotFound();
        return Ok(notifiche);
    }


    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.SelfCareRecapitistaPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<Contestazione>, NotFound>> UpdateRecapitistaContestazioneAsync(
    HttpContext context,
    [FromBody] ContestazioneRecapitisti req,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();

        var command = new ContestazioneUpdateRecapitistaCommand(authInfo, req.IdNotifica)
        {
            NoteRecapitista = req.Risposta,
            StatoContestazione = req.StatoContestazione
        };

        var contestazione = await handler.Send(command);
        return Ok(contestazione);
    }
    #endregion

    #region enti
    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<NotificaDto>, NotFound>> GetNotificheByRicercaAsync(
    HttpContext context,
    [FromBody] NotificheRicercaRequest request,
    [FromQuery] int page,
    [FromQuery] int pageSize,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var notifiche = await handler.Send(request.Map(authInfo, page, pageSize));
        if (notifiche == null || notifiche.Count == 0)
            return NotFound();
        return Ok(notifiche);
    }


    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> GetNotificheRicercaDocumentAzureFunctionAsync(
    HttpContext context,
    [FromBody] NotificheRicercaRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler,
    [FromServices] IPortaleFattureOptions options,
    [FromServices] IFunctionNotificheCaller fn,
    [FromQuery] bool? binary = null)
    {
        var authInfo = context.GetAuthInfo();
 
        var ente = await handler.Send(new EnteQueryCodiceSDIGetById(authInfo));
        if (ente == null)
            return NotFound();

        request.IdEnte = ente.IdEnte;
        request.RagioneSociale = ente.RagioneSociale;
        request.IdContratto = ente.IdContratto;

        // evita richieste duplicate se già c'è una richiesta in corso
        var istanza = new ReportNotificheByIdHashQueryCommand(authInfo)
        {
            Json = request.Serialize(),
        };

        var report = await handler.Send(istanza);
        if (report != null && report.DataInserimento > DateTime.UtcNow.ItalianTime().AddHours(-1))
        {
            return Results.Json(new
            {
                message = "Attendi l'esecuzione della richiesta precedente.",
            }, statusCode: 300);
        }

        var instanceId = Guid.NewGuid().ToString(); 

        var idReport = await handler.Send(new ReportNotificheCreateCommand(authInfo)
        {
            UniqueId = instanceId,
            Json = request.Serialize(),
            Anno = request.Anno!.Value,
            Mese = request.Mese!.Value,
            ContractId = ente.IdContratto,
            Storage = options.StorageNotifiche!.AccountName,
            NomeDocumento = null,
            Link = null
        });
        if (!idReport.HasValue)
            return BadRequest();

        request.IdReport = idReport; 
        request.InstanceId = instanceId;

        var resultCallFunction = await fn.CallAzureFunction(request);
        if (resultCallFunction == null)
        { 
            await handler.Send(new ReportNotificheUpdateCommand(authInfo)
            { 
                NomeDocumento = null,
                Stato = 3, // errore
                StatoAtteso = 0,
                UniqueId = instanceId
            });
            return BadRequest(new { message = "Errore durante l'esecuzione della funzione." });
        } 

        var uniqueId = resultCallFunction.InstanceId;
        if (string.IsNullOrEmpty(uniqueId))
            return BadRequest(); 
        else
        {
            var updateCommand = new ReportNotificheUpdateByIdCommand(authInfo)
            {
                UniqueId = uniqueId,
                IdReport = idReport.Value,
                LinkDocumento = $"{options.StorageNotifiche.BlobContainerName}/{ente.IdEnte}/{ente.IdContratto}/{uniqueId}"
            };

            var guid = await handler.Send(updateCommand);
            if (string.IsNullOrEmpty(guid))
                return NotFound();
            return Ok(resultCallFunction);
        }
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> GetNotificheRicercaDocumentAsync(
    HttpContext context,
    [FromBody] NotificheRicercaRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler,
    [FromServices] IPortaleFattureOptions options,
    [FromQuery] bool? binary = null)
    {
        var authInfo = context.GetAuthInfo();
        var tempAnno = 2025;
        int[] tempMese = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
        var tempIdEnte = "53b40136-65f2-424b-acfb-7fae17e35c60";
        var tempName = "inps";


        if (request.Anno == tempAnno && tempMese.Contains(request.Mese!.Value) && authInfo.IdEnte == tempIdEnte)
        {
            var ente = await handler.Send(new EnteQueryGetById(authInfo));
            if (ente == null) return NotFound();
            if (!ente.Descrizione!.ToLower()!.Contains(tempName, StringComparison.CurrentCultureIgnoreCase))
            {
                return NotFound();
            }
            var pmese = request.Mese!.Value.ToString().Length == 1 ? $"0{request.Mese!.Value}" : request.Mese!.Value.ToString();
            var blobNameDetailed = $"Notifiche _Istituto Nazionale Previdenza Sociale - INPS_{pmese} _2025.csv";
            var storageSharedKeyCredential = new StorageSharedKeyCredential(options.StoragePagoPAFinancial!.AccountName, options.StoragePagoPAFinancial!.AccountKey);
            var blobContainerName = "temp";

            BlobSasBuilder sasBuilderDetailed = new()
            {
                BlobContainerName = blobContainerName,
                BlobName = blobNameDetailed,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(30)
            };

            sasBuilderDetailed.SetPermissions(BlobSasPermissions.Read);
            var sasTokenDetailed = sasBuilderDetailed.ToSasQueryParameters(storageSharedKeyCredential).ToString();
            var blobUrlDetailed = $"https://{options.StoragePagoPAFinancial!.AccountName}.blob.core.windows.net/{blobContainerName}/{blobNameDetailed}";

            var uri = $"{blobUrlDetailed}?{sasTokenDetailed}";
            var blobUri = new Uri(uri);
            var blobClient = new BlobClient(blobUri);

            BlobDownloadInfo download = await blobClient.DownloadAsync();
            var stream = new MemoryStream();
            await download.Content.CopyToAsync(stream);
            var mimeCsv = "text/csv";
            stream.Position = 0;
            return Results.Stream(stream, mimeCsv, blobNameDetailed);
        }
        else
        {
            var notifiche = await handler.Send(request.Map(authInfo, null, null));
            if (notifiche == null || notifiche.Count == 0)
                return NotFound();

            var stream = await notifiche.Notifiche!.ToStream<SimpleNotificaDto, SimpleNotificaEnteDtoMap>();
            if (stream.Length == 0)
                return NotFound();

            var filename = $"{Guid.NewGuid()}.csv";
            var mimeCsv = "text/csv";
            stream.Position = 0;
            return Results.Stream(stream, mimeCsv, filename);
        }
    }

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<Contestazione>, NotFound>> CreateContestazioneAsync(
    HttpContext context,
    [FromBody] ContestazioneCreateRequest req,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();

        var command = new ContestazioneCreateCommand(authInfo)
        {
            IdNotifica = req.IdNotifica,
            NoteEnte = req.NoteEnte,
            TipoContestazione = req.TipoContestazione
        };

        var contestazione = await handler.Send(command);
        return Ok(contestazione);
    }

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<Contestazione>, NotFound>> UpdateContestazioneAsync(
    HttpContext context,
    [FromBody] ContestazioneUpdateRequest req,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();

        var command = new ContestazioneUpdateCommand(authInfo, req.IdNotifica)
        {
            NoteEnte = req.NoteEnte,
            RispostaEnte = req.RispostaEnte,
            StatoContestazione = req.StatoContestazione,
            Onere = req.Onere
        };

        var contestazione = await handler.Send(command);
        return Ok(contestazione);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<ContestazioneResponse>, NotFound>> GetContestazioneAsync(
    HttpContext context,
    [FromRoute] string idNotifica,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var command = new AzioneContestazioneQueryGetByIdNotifica(authInfo, idNotifica);
        var azione = await handler.Send(command);

        Contestazione contestazione;
        if (azione!.Contestazione == null)
            contestazione = new Contestazione()
            {
                IdNotifica = idNotifica,
                StatoContestazione = azione!.Notifica!.StatoContestazione,
                Anno = Convert.ToInt16(azione!.Notifica.Anno),
                Mese = Convert.ToInt16(azione!.Notifica.Mese),
            };
        else
            contestazione = azione!.Contestazione;

        var response = new ContestazioneResponse()
        {
            Contestazione = contestazione,
            Modifica = azione!.CreazionePermessa,
            Chiusura = azione!.ChiusuraPermessa,
            Risposta = azione!.RispostaPermessa
        };
        return Ok(response);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<int?>, NotFound>> GetRichiesteNotificheCountAsync(
    HttpContext context,
   [FromServices] IStringLocalizer<Localization> localizer,
   [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var command = new ReportNotificheQueryCount(authInfo) { };

        var count = await handler.Send(command);
        return Ok(count);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<ReportNotificheListCountDto>, NotFound>> PostRichiesteNotificheAsync(
    HttpContext context,
    [FromQuery] int page,
    [FromQuery] int pageSize,
    [FromBody] ReportNotificheRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var command = new ReportNotificheQueryCommand(authInfo)
        {

            Init = request.Init,
            End = request.End,
            Ordinamento = request.Ordinamento,
            Page = page,
            Size = pageSize
        };

        var reports = await handler.Send(command);
        if (reports == null || reports.Items.IsNullNotAny())
            return NotFound();

        return Ok(reports);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<string>, NotFound, BadRequest>> GetRichiesteNotificheDownloadAsync(
    HttpContext context,
    [FromQuery] int idReport,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler,
    [FromServices] IBlobStorageNotifiche storage)
    {
        var authInfo = context.GetAuthInfo();
        var command = new ReportNotificheByIdQueryCommand(authInfo)
        {
            IdReport = idReport,
        };

        var report = await handler.Send(command);
        if (report == null)
            return NotFound();

        var token = storage.GetSasToken(report.InternalOrganizationId!, report.Anno, report.Mese, report.UniqueId!, report.NomeDocumento!);
        if (string.IsNullOrEmpty(token))
            return NotFound();
        else
        {
            var result = await handler.Send(new ReportNotificheUpdateLettoCommand(authInfo)
            {
                IdReport = idReport,
                Letto = 1,
                StatoAtteso = 0
            });
            if (result == 1)
                return Ok(token);
            else
                return BadRequest();
        }
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<DurableFunctionResponse>, NotFound, BadRequest<string>>> PostVerificaNotificheDownloadAsync(
    HttpContext context,
    [FromBody] NotificheRiceraVerificaRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler,
    [FromServices] IFunctionNotificheCaller fn,
    [FromServices] IBlobStorageNotifiche storage)
    {
        var authInfo = context.GetAuthInfo();
        if(authInfo.IdEnte != request.IdEnte)
            return NotFound();

        var resultCallFunction  = await fn.CallDurableFunctionWebhook(request.StatusQueryGetUri!);
        if (resultCallFunction.Item1 == null)
            return BadRequest(resultCallFunction.Item2); 
 
        return Ok(resultCallFunction.Item1);
    }
    #endregion
}