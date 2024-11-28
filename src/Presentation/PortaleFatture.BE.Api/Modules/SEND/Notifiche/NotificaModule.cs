using System.Globalization;
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
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Queries;
using static Microsoft.AspNetCore.Http.TypedResults;

namespace PortaleFatture.BE.Api.Modules.Notifiche;


public partial class NotificaModule
{
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
    [FromBody] NotificheRicercaRequestPagoPA request,
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

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCareConsolidatorePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<NotificaRECCONDto>, NotFound>> GetConsolidatoriNotificheByRicercaAsync(
    HttpContext context,
    [FromBody] NotificheRicercaRequestPagoPA request,
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
    private async Task<IResult> GetNotificheRicercaDocumentAsync(
    HttpContext context,
    [FromBody] NotificheRicercaRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler,
    [FromQuery] bool? binary = null)
    {
        var authInfo = context.GetAuthInfo();
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
    #endregion
}