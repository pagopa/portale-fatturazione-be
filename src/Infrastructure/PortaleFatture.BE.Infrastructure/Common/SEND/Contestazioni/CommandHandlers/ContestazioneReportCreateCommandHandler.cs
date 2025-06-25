using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.Messaggi;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Commands.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Commands;
using PortaleFatture.BE.Infrastructure.Common.SEND.Messaggi.Commands.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Contestazioni.CommandHandlers;

public class ContestazioneReportCreateCommandHandler(
 IFattureDbContextFactory factory,
 IMediator handler,
 IStringLocalizer<Localization> localizer,
 ILogger<ContestazioneReportCreateCommandHandler> logger) : IRequestHandler<ContestazioneReportCreateCommand, bool?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<ContestazioneReportCreateCommandHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    private readonly IMediator _handler = handler;

    public async Task<bool?> Handle(ContestazioneReportCreateCommand command, CancellationToken ct)
    {
        var authInfo = command.AuthenticationInfo;
        command.UtenteId = authInfo!.Id;
        using var uow = await _factory.Create(true, cancellationToken: ct);

        var tipologiaDocumentiContestazione = await uow.Query(new TipologiaReportQueryPersistence(new TipologiaReportQuery(authInfo)), ct);
        TipologiaReport? tipoDocumento = null;
        if (command.IsUploadedByEnte)
        {
            tipoDocumento = (from p in tipologiaDocumentiContestazione
                             where (p.CategoriaDocumento!.Contains("contestazione", StringComparison.CurrentCultureIgnoreCase) &&
                              p.CategoriaDocumento!.Contains("ente", StringComparison.CurrentCultureIgnoreCase))
                             select p).FirstOrDefault();
        }
        else
        {
            tipoDocumento = (from p in tipologiaDocumentiContestazione
                             where (p.CategoriaDocumento!.Contains("contestazione", StringComparison.CurrentCultureIgnoreCase) &&
                              p.CategoriaDocumento!.Contains("supporto", StringComparison.CurrentCultureIgnoreCase))
                             select p).FirstOrDefault();
        }

        if (tipoDocumento == null)
        {
            uow.Rollback();
            return false;
        }

        command.IdTipologiaReport = tipoDocumento.IdTipologiaReport;
        command.Prodotto = authInfo.Prodotto;
        var id = await uow.Execute(new ContestazioneReportCreateCommandPersistence(command, _localizer), ct);
        if (id > 0)
        {
            var reportId = id;
            id = await uow.Execute(new ContestazioneReportStepCreateCommandPersistence(new ContestazioneReportStepCreateCommand(command.AuthenticationInfo)
            {
                IdReport = reportId,
                LinkDocumento = command.LinkDocumento,
                NomeDocumento = command.FileCaricato,
                Step = command.Stato,
                Storage = command.Storage,
            }, _localizer), ct);

            if (id > 0)
            {
                id = await uow.Execute(new MessaggioCreateCommandPersistence(new MessaggioCreateCommand(command.AuthenticationInfo)
                {
                    Anno = command.Anno,
                    Hash = command.Hash,
                    Mese = command.Mese,
                    CategoriaDocumento = tipoDocumento.CategoriaDocumento,
                    TipologiaDocumento = tipoDocumento.TipologiaDocumento,
                    ContentLanguage = command.ContentLanguage,
                    ContentType = command.ContentType,
                    LinkDocumento = command.LinkDocumento,
                    IdEnte = command.InternalOrganizationId,
                    IdReport = reportId,
                    IdUtente = command.UtenteId,
                    GruppoRuolo = authInfo.GruppoRuolo,
                    Json = command.Json,
                    Prodotto = authInfo.Prodotto,
                    Stato = command.Stato,
                    Auth = authInfo!.Auth
                }, _localizer), ct);

                if (id > 0)
                {
                    uow.Commit();
                    return true;
                }
                else
                {
                    uow.Rollback();
                    return false;
                }
            }
            else
            {
                uow.Rollback();
                return false;
            }
        }
        else
        {
            uow.Rollback();
            return false;
        }
    }
}