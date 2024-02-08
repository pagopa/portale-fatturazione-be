using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.Scadenziari;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.Scadenziari.Queries;
using PortaleFatture.BE.Infrastructure.Common.Scadenziari.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.Scadenziari.QueryHandlers;

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
        CalendarioContestazione calendarioContestazione = new();
        using var rs = await _factory.Create(cancellationToken: ct);
        var calendario = await rs.Query(new CalendarioContestazioneQueryGetPersistence(request), ct);
        var (annoAttuale, meseAttuale, giornoAttuale, adesso) = Time.YearMonthDay();
        if (calendario == null)
            return new CalendarioContestazione()
            {
                Adesso = adesso,
                AnnoContestazione = request.Anno,
                MeseContestazione = request.Mese,
                Valid = false
            };
   
        var find = adesso >= calendario!.DataInizio && adesso <= calendario!.DataFine;
        calendarioContestazione.Valid = find;
        calendarioContestazione.DataInizio = calendario.DataInizio;
        calendarioContestazione.DataFine = calendario.DataFine;
        calendarioContestazione.AnnoContestazione = calendario.AnnoContestazione;
        calendarioContestazione.MeseContestazione = calendario.MeseContestazione;
        calendarioContestazione.Adesso = adesso;
        return calendarioContestazione;
    }
}