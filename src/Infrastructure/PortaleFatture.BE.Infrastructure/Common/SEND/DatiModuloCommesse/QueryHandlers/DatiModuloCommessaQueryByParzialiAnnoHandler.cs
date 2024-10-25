using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse;
using PortaleFatture.BE.Core.Entities.SEND.Scadenziari;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Scadenziari;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.QueryHandlers
{
    public class DatiModuloCommessaQueryByParzialiAnnoHandler(
     IFattureDbContextFactory factory,
     IStringLocalizer<Localization> localizer,
     IScadenziarioService scadenziarioService,
     ILogger<DatiModuloCommessaQueryByParzialiAnnoHandler> logger) : IRequestHandler<DatiModuloCommessaParzialiQueryGetByAnno, IEnumerable<DatiModuloCommessaParzialiTotale>?>
    {
        private readonly IFattureDbContextFactory _factory = factory;
        private readonly ILogger<DatiModuloCommessaQueryByParzialiAnnoHandler> _logger = logger;
        private readonly IStringLocalizer<Localization> _localizer = localizer;
        private readonly IScadenziarioService _scadenziarioService = scadenziarioService;

        public async Task<IEnumerable<DatiModuloCommessaParzialiTotale>?> Handle(DatiModuloCommessaParzialiQueryGetByAnno command, CancellationToken ct)
        {
            var (annoFatturazione, _, _, _) = Time.YearMonthDayFatturazione();
            command.AnnoValidita = command.AnnoValidita != null ? command.AnnoValidita : annoFatturazione;
            var idEnte = command.AuthenticationInfo.IdEnte;
            var prodotto = command.AuthenticationInfo.Prodotto;
            var ruolo = command.AuthenticationInfo.Ruolo;
            using var uow = await _factory.Create(true, cancellationToken: ct);
            var parzialiTotale = await uow.Query(new DatiModuloCommessaParzialiTotaleQueryGetByIdPersistence(idEnte, command.AnnoValidita.Value, prodotto, ruolo), ct);
            if (parzialiTotale!.IsNullNotAny())
                return null;

            foreach (var parziali in parzialiTotale!)
            {
                var (valid, _) = await _scadenziarioService.GetScadenziario(command.AuthenticationInfo, TipoScadenziario.DatiModuloCommessa, parziali.AnnoValidita, parziali.MeseValidita);
                parziali.Modifica = parziali.Stato == StatoModuloCommessa.ApertaCaricato && valid;
            }
            return parzialiTotale;
        }
    }
}