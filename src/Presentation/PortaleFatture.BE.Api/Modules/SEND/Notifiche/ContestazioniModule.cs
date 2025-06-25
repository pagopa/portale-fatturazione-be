using System.Text;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using PortaleFatture.BE.Api.Infrastructure;
using PortaleFatture.BE.Api.Infrastructure.Documenti;
using PortaleFatture.BE.Api.Modules.SEND.Notifiche.Extensions;
using PortaleFatture.BE.Api.Modules.SEND.Notifiche.Payload.Request;
using PortaleFatture.BE.Api.Modules.SEND.Notifiche.Payload.Response;
using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Entities.SEND.Notifiche;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Identity;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Services;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;
using PortaleFatture.BE.Infrastructure.Common.SEND.Notifiche.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.SelfCare.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Gateway.Storage.pagoPA;
using static Microsoft.AspNetCore.Http.TypedResults;

namespace PortaleFatture.BE.Api.Modules.SEND.Notifiche;

public partial class ContestazioniModule
{
    #region pagoPA

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<IResult> GetReportsContestazioniAsync(
        HttpContext context,
        [FromQuery] int idReport,
        [FromQuery] string? tipoReport,
        [FromServices] IContestazioniStorageService storageService,
        [FromServices] IStringLocalizer<Localization> localizer,
        [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var report = await handler.Send(new ContestazioniReportStepQuery(authInfo)
        {
            IdReport = idReport,
        });

        if (report?.Steps == null || !report.Steps.Any())
            return NotFound();

        if (string.IsNullOrEmpty(tipoReport))
            tipoReport = "json";

        var values = report.Steps.Map(storageService);

        var filename = $"{idReport}_expires_{DateTimeOffset.UtcNow.AddHours(1)}";

        if (tipoReport.ToLower() == "json")
        {
            var mime = "application/json";
            var json = values.Serialize();
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json))
            {
                Position = 0
            };
            return Results.Stream(stream, mime, $"{filename}.json");
        }
        else
        {
            if (values?.Steps == null || !values.Steps.Any())
                return NotFound();

            IEnumerable<ReportContestazioneStepsWithLinkDto> r = values.Steps;
            var stream = await r.ToStream<ReportContestazioneStepsWithLinkDto, ReportContestazioneStepsDtoMap>();
            var mimeCsv = "text/csv";
            stream.Position = 0;
            return Results.Stream(stream, mimeCsv, $"{filename}.csv");
        }
    }

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<ContestazioneStep>>, NotFound>> GetStepsContestazioniAsync(
    HttpContext context,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();

        var steps = await handler.Send(new ContestazioniStepsQuery(authInfo));
        if (steps.IsNullNotAny())
            return NotFound();
        return Ok(steps);
    }


    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<List<(string, bool)>>, NotFound>> UploadTestContestazioniAsync(
    HttpContext context,
    [FromForm] UploadContestazioni cont,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IDocumentStorageSASService storage,
    [FromServices] IMediator handler)
    {
        var results = new List<(string, bool)>();
        var chunkSize = 1 * 1024 * 1024; // 1 MB
        var (chunks, totalChunks) = await cont.FileChunk!.DivideFileIntoChunks(chunkSize);
        var chunkIndex = 0;
        foreach (var chunk in chunks)
        {
            cont.TotalChunks = totalChunks;
            cont.FileChunk = chunk;
            cont.ChunkIndex = chunkIndex;
            chunkIndex++;
            var up = await context.MapAndValidate(cont, localizer);
            var documentKey = new DocumentiContestazioniSASSStorageKey(
                    storage.BlobContainerContestazioniName,
                    cont.IdEnte,
                    cont.ContractId,
                    cont.FileId,
                    cont.FileChunk!.FileName);

            var result = await storage.UploadContestazioni(documentKey, up);
            results.Add(result);
        }
        return Ok(results);
    }

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<(string, bool)>, NotFound>> UploadContestazioniAsync(
    HttpContext context,
    [FromForm] UploadContestazioni cont,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IDocumentStorageSASService storage,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var up = await context.MapAndValidate(cont, localizer);
        var documentKey = new DocumentiContestazioniSASSStorageKey(
                storage.BlobContainerContestazioniName,
                cont.IdEnte,
                cont.ContractId,
                cont.FileId,
                cont.FileChunk!.FileName);

        var result = await storage.UploadContestazioni(documentKey, up);
        if (result.Item2)
        {
            var insert = await handler.Send(new ContestazioneReportCreateCommand(authInfo)
            {
                Storage = storage.StorageContestazioniName,
                NomeDocumento = null,
                LinkDocumento = $"{documentKey.DocumentReportLinkJson()}",
                UniqueId = cont.FileId,
                Anno = Convert.ToInt32(cont.Anno),
                Mese = Convert.ToInt32(cont.Mese),
                ContentLanguage = LanguageMapping.IT,
                ContentType = MimeMapping.JSON,
                InternalOrganizationId = cont.IdEnte,
                ContractId = cont.ContractId,
                FileCaricato = cont.FileChunk!.FileName!.Replace(".csv", "*.csv"),
                IsUploadedByEnte = false
            });
            if (insert.Value)
                return Ok(result);
            else
                throw new UploadException("Errore nel salvare la contestazione.");
        }
        return Ok(result);
    }

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<string>>, NotFound>> GetAnniContestazioniAsync(
    HttpContext context,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();

        var anni = await handler.Send(new ContestazioniAnniQuery(authInfo));
        if (anni.IsNullNotAny())
            return NotFound();
        return Ok(anni);
    }

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<List<ContestazioniMeseResponse>>, NotFound>> PostMesiContestazioniAsync(
    HttpContext context,
    [FromBody] ContestazioniMesi request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();

        var mesi = await handler.Send(new ContestazioniMesiQuery(authInfo)
        {
            Anno = request.Anno
        });

        if (mesi.IsNullNotAny())
            return NotFound();
        return Ok(mesi!.Map());
    }

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<ContestazioneEnte>>, NotFound>> PostEntiContestazioniAsync(
    HttpContext context,
    [FromBody] ContestazioniEnti request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var enti = await handler.Send(new ContestazioniEntiQuery(authInfo)
        {
            Descrizione = request.Descrizione
        });

        if (enti.IsNullNotAny())
            return NotFound();
        return Ok(enti);
    }

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<ContestazioneRecap>>, NotFound>> PostRecapContestazioniAsync(
    HttpContext context,
    [FromBody] ContestazioniRecapRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var recap = await handler.Send(new ContestazioniRecapQuery(authInfo)
        {
            Anno = request.Anno,
            Mese = request.Mese,
            ContractId = request.ContractId,
            IdEnte = request.IdEnte
        });

        if (recap.IsNullNotAny())
            return NotFound();
        return Ok(recap);
    }

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<ReportContestazioniList>, NotFound>> PostReportsContestazioniAsync(
    HttpContext context,
    [FromQuery] int page,
    [FromQuery] int pageSize,
    [FromBody] ContestazioniReportRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var recap = await handler.Send(new ContestazioniReportQuery(authInfo)
        {
            Anno = request.Anno,
            Mese = request.Mese,
            IdEnti = request.IdEnti,
            IdTipologiaReports = request.IdTipologiaReports,
            Page = page,
            Size = pageSize
        });

        if (recap == null || recap.Count == 0)
            return NotFound();
        return Ok(recap);
    }

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<TipologiaReport>>, NotFound>> GetTipoReportContestazioniAsync(
    HttpContext context,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var tipologia = await handler.Send(new TipologiaReportQuery(authInfo));
        if (tipologia.IsNullNotAny())
            return NotFound();
        return Ok(tipologia);
    }

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<ReportContestazioneStepsDto>>, NotFound>> GetReportsStepsContestazioniAsync(
      HttpContext context,
      [FromQuery] int idReport,
      [FromServices] IStringLocalizer<Localization> localizer,
      [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var report = await handler.Send(new ContestazioniReportStepQuery(authInfo)
        {
            IdReport = idReport,
        });

        if (report!.Steps.IsNullNotAny())
            return NotFound();

        return Ok(report.Steps);
    }

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.PagoPAPolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<string>, NotFound>> PostReportsDocumentContestazioniAsync(
    HttpContext context,
    [FromBody] ContestazioniDocumentRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IContestazioniStorageService storageService,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var idReport = request.IdReport;
        var step = request.Step;

        var report = await handler.Send(new ContestazioniReportStepQuery(authInfo)
        {
            IdReport = idReport,
        });

        if (report!.Steps.IsNullNotAny())
            return NotFound();

        if (step.HasValue)
        {
            var sstep = report!.Steps!.Where(x => x.Step == step).FirstOrDefault();
            if (sstep == null)
                return NotFound();

            return Ok(storageService.GetSASToken(sstep.Link!, sstep.NomeDocumento!));
        }
        else
            return Ok(storageService.GetSASToken(report.ReportContestazione!.LinkDocumento!, report.ReportContestazione!.NomeDocumento!));
    }
    #endregion

    #region Enti
    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<string>>, NotFound>> GetAnniContestazioniEntiAsync(
    HttpContext context,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();

        var anni = await handler.Send(new ContestazioniAnniQuery(authInfo));
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
    private async Task<Results<Ok<List<ContestazioniMeseResponse>>, NotFound>> PostMesiContestazioniEntiAsync(
    HttpContext context,
    [FromBody] ContestazioniMesi request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();

        var mesi = await handler.Send(new ContestazioniMesiQuery(authInfo)
        {
            Anno = request.Anno
        });

        if (mesi.IsNullNotAny())
            return NotFound();
        return Ok(mesi!.Map());
    }


    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<TipologiaReport>>, NotFound>> GetTipoReportContestazioniEntiAsync(
    HttpContext context,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var tipologia = await handler.Send(new TipologiaReportQuery(authInfo));
        if (tipologia.IsNullNotAny())
            return NotFound();
        return Ok(tipologia);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<ContestazioneStep>>, NotFound>> GetStepsContestazioniEnteAsync(
    HttpContext context,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();

        var steps = await handler.Send(new ContestazioniStepsQuery(authInfo));
        if (steps.IsNullNotAny())
            return NotFound();
        return Ok(steps);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<ReportContestazioniList>, NotFound>> PostReportsContestazioniEnteAsync(
    HttpContext context,
    [FromQuery] int page,
    [FromQuery] int pageSize,
    [FromBody] ContestazioniReportEnteRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var recap = await handler.Send(new ContestazioniReportQuery(authInfo)
        {
            Anno = request.Anno,
            Mese = request.Mese,
            IdEnti = new string[] { authInfo.IdEnte! },
            IdTipologiaReports = request.IdTipologiaReports,
            Page = page,
            Size = pageSize
        });

        if (recap == null || recap.Count == 0)
            return NotFound();
        return Ok(recap);
    }


    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<ContestazioneRecap>>, NotFound>> PostRecapContestazioniEnteAsync(
    HttpContext context,
    [FromBody] ContestazioniRecapEnteRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IFattureDbContextFactory factory,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        // recupera contract id 
        string? contractId; 
        using var uow = await factory.Create();
        {
            var contratto = await uow.Query(new EnteCodiceSDIQueryGetByIdPersistence(authInfo.IdEnte));
            contractId = contratto?.IdContratto;
        } 

        var recap = await handler.Send(new ContestazioniRecapQuery(authInfo)
        {
            Anno = request.Anno,
            Mese = request.Mese,
            ContractId = contractId,
            IdEnte = authInfo.IdEnte
        });

        if (recap.IsNullNotAny())
            return NotFound();
        return Ok(recap);
    }

    [Authorize(Roles = $"{Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<(string, bool)>, NotFound>> UploadContestazioniEnteAsync(
    HttpContext context,
    [FromForm] UploadContestazioniEnte cont,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IDocumentStorageSASService storage,
    [FromServices] IFattureDbContextFactory factory,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        // recupera contract id 
        string? contractId;
        using var uow = await factory.Create();
        {
            var contratto = await uow.Query(new EnteCodiceSDIQueryGetByIdPersistence(authInfo.IdEnte));
            contractId = contratto?.IdContratto;
        }

        var up = await context.MapAndValidate(cont.Map(authInfo.IdEnte!, contractId!), localizer);
        var documentKey = new DocumentiContestazioniSASSStorageKey(
                storage.BlobContainerContestazioniName,
                authInfo.IdEnte,
                contractId,
                cont.FileId,
                cont.FileChunk!.FileName);

        var result = await storage.UploadContestazioni(documentKey, up);
        if (result.Item2)
        {
            var insert = await handler.Send(new ContestazioneReportCreateCommand(authInfo)
            {
                Storage = storage.StorageContestazioniName,
                NomeDocumento = null,
                LinkDocumento = $"{documentKey.DocumentReportLinkJson()}",
                UniqueId = cont.FileId,
                Anno = Convert.ToInt32(cont.Anno),
                Mese = Convert.ToInt32(cont.Mese),
                ContentLanguage = LanguageMapping.IT,
                ContentType = MimeMapping.JSON,
                InternalOrganizationId = authInfo.IdEnte,
                ContractId = contractId,
                FileCaricato = cont.FileChunk!.FileName!.Replace(".csv", "*.csv"),
                IsUploadedByEnte = true // Indica che il file è stato caricato dall'ente 
            });

            if (insert.Value)
                return Ok(result);
            else
                throw new UploadException("Errore nel salvare la contestazione.");
        }
        return Ok(result);
    }

    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<IEnumerable<ReportContestazioneStepsDto>>, NotFound>> GetReportsStepsContestazioniEnteAsync(
    HttpContext context,
    [FromQuery] int idReport,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var report = await handler.Send(new ContestazioniReportStepQuery(authInfo)
        {
            IdReport = idReport,
        });

        if(report == null || report.Steps.IsNullNotAny())
            return NotFound(); 

        return Ok(report.Steps);
    }


    [Authorize(Roles = $"{Ruolo.OPERATOR}, {Ruolo.ADMIN}", Policy = Module.SelfCarePolicy)]
    [EnableCors(CORSLabel)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    private async Task<Results<Ok<string>, NotFound>> PostReportsDocumentContestazioniEnteAsync(
    HttpContext context,
    [FromBody] ContestazioniDocumentRequest request,
    [FromServices] IStringLocalizer<Localization> localizer,
    [FromServices] IContestazioniStorageService storageService,
    [FromServices] IMediator handler)
    {
        var authInfo = context.GetAuthInfo();
        var idReport = request.IdReport;
        var step = request.Step;

        var report = await handler.Send(new ContestazioniReportStepQuery(authInfo)
        {
            IdReport = idReport,
        });

        if (report is null || report!.Steps.IsNullNotAny())
            return NotFound();

        if (step.HasValue)
        {
            var sstep = report!.Steps!.Where(x => x.Step == step).FirstOrDefault();
            if (sstep == null)
                return NotFound();

            return Ok(storageService.GetSASToken(sstep.Link!, sstep.NomeDocumento!));
        }
        else
            return Ok(storageService.GetSASToken(report.ReportContestazione!.LinkDocumento!, report.ReportContestazione!.NomeDocumento!));
    }
    #endregion
}

