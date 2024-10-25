using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.SEND.Scadenziari;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.Scadenziari.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.Scadenziari.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.Scadenziari.QueryHandlers;

public class CalendarioContestazioneQueryHandler(
 IFattureDbContextFactory factory,
 IStringLocalizer<Localization> localizer,
 ILogger<CalendarioContestazioneQueryHandler> logger) : IRequestHandler<CalendarioContestazioneQueryGet, CalendarioContestazione?>
{
    private readonly IFattureDbContextFactory _factory = factory;
    private readonly ILogger<CalendarioContestazioneQueryHandler> _logger = logger;
    private readonly IStringLocalizer<Localization> _localizer = localizer;
    public async Task<CalendarioContestazione?> Handle(CalendarioContestazioneQueryGet request, CancellationToken ct)
    {

        using var rs = await _factory.Create(cancellationToken: ct);
        var calendario = await rs.Query(new CalendarioContestazioneQueryGetPersistence(request), ct);
        var (annoAttuale, meseAttuale, giornoAttuale, adesso) = Time.YearMonthDay();
        if (calendario == null)
            return new CalendarioContestazione()
            {
                Adesso = adesso,
                AnnoContestazione = request.Anno,
                MeseContestazione = request.Mese,
                Valid = false,
                ValidVerifica = false
            };

        var tValid = adesso >= calendario!.DataInizio && adesso <= calendario!.DataFine;
        var tVerifica = adesso >= calendario!.DataInizio && adesso <= calendario!.DataVerifica;
        var tVisualizzazione = adesso >= calendario!.DataInizio;

        CalendarioContestazione calendarioContestazione = new()
        {
            Valid = tValid,
            ValidVerifica = tVerifica,
            DataInizio = calendario.DataInizio,
            DataFine = calendario.DataFine,
            DataVerifica = calendario.DataVerifica,
            AnnoContestazione = calendario.AnnoContestazione,
            MeseContestazione = calendario.MeseContestazione,
            Adesso = adesso,
            ValidVisualizzazione = tVisualizzazione
        };
        return calendarioContestazione;
    }
}