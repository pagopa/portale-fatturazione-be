using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Core.Entities.SEND.Tipologie;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.SEND.Scadenziari;
using PortaleFatture.BE.Infrastructure.Common.SEND.Tipologie.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.QueryHandlers
{
    public class DatiModuloCommessaPrevisionaleQueryByAnnoHandler(
     IFattureDbContextFactory factory,
     IStringLocalizer<Localization> localizer,
     IScadenziarioService scadenziarioService,
     ILogger<DatiModuloCommessaPrevisionaleQueryByAnnoHandler> logger) : IRequestHandler<DatiModuloCommessaPrevisionaleQueryGetByAnno, IEnumerable<ModuloCommessaPrevisionaleTotaleDto>?>
    {
        private readonly IFattureDbContextFactory _factory = factory;
        private readonly ILogger<DatiModuloCommessaPrevisionaleQueryByAnnoHandler> _logger = logger;
        private readonly IStringLocalizer<Localization> _localizer = localizer;
        private readonly IScadenziarioService _scadenziarioService = scadenziarioService;

        public async Task<IEnumerable<ModuloCommessaPrevisionaleTotaleDto>?> Handle(DatiModuloCommessaPrevisionaleQueryGetByAnno command, CancellationToken ct)
        {
            IEnumerable<ModuloCommessaPrevisionaleByAnnoDto>? dati;
            IEnumerable<CategoriaSpedizione>? categorie;
            List<ModuloCommessaPrevisionaleTotaleDto>? moduloCommessa = [];
            using (var uow = await _factory.Create(true, cancellationToken: ct))
            {
                dati = await uow.Query(new DatiModuloCommessaPrevisionaleQueryGetByAnnoPersistence(
                    command.AuthenticationInfo.IdEnte,
                    command.AnnoValidita,
                    command.MeseValidita,
                    command.AuthenticationInfo.Prodotto), ct);
                categorie = await uow.Query(new SpedizioneQueryGetAllPersistence(), ct);
            }

            if (categorie!.IsNullNotAny())
            {
                var msg = "Provide categorie and tipo commessa in configuration!";
                _logger.LogError(msg);
                throw new ConfigurationException(msg);
            }

            if (dati!.IsNullNotAny())
                return null;

            Dictionary<string, ModuloCommessaPrevisionaleTotaleDto> dic = [];

            foreach (var modulo in dati!)
            {
                IEnumerable<ValoriRegioneDto>? valoriRegioni;
                using (var uow = await _factory.Create(cancellationToken: ct))
                {
                    valoriRegioni = await uow.Query(new DatiValoriRegioneModuloCommessaQueryGetPersistence(
                        command.AuthenticationInfo.IdEnte,
                        modulo.AnnoValidita,
                        modulo.MeseValidita), ct);
                }

                if (!dic.TryGetValue($"{modulo.AnnoValidita}_{modulo.MeseValidita}", out var _prev))
                {
                    _prev = new ModuloCommessaPrevisionaleTotaleDto
                    {
                        Modifica = modulo.Modifica,
                        AnnoValidita = modulo.AnnoValidita,
                        MeseValidita = modulo.MeseValidita,
                        IdEnte = modulo.FkIdEnte,
                        IdTipoContratto = modulo.FkIdTipoContratto,
                        Stato = modulo.FkIdStato,
                        Prodotto = modulo.FkProdotto,
                        DataInserimento = modulo.DataInserimento,
                        Source = modulo.Source,
                        Quarter = modulo.Quarter,
                        DataChiusura = modulo.DataChiusura, 
                        DataChiusuraLegale = modulo.DataChiusuraLegale,
                    };
                    dic.Add($"{modulo.AnnoValidita}_{modulo.MeseValidita}", _prev);
                }

                switch (modulo.FkIdTipoSpedizione)
                {
                    case 1: // Analogico A/R
                        _prev.TotaleAnalogicoARNaz = modulo.ValoreNazionali;
                        _prev.TotaleAnalogicoARInternaz = modulo.ValoreInternazionali;
                        _prev.TotaleNotificheAnalogicoARNaz = modulo.NumeroNotificheNazionali;
                        _prev.TotaleNotificheAnalogicoARInternaz = modulo.NumeroNotificheInternazionali;
                        _prev.TotaleNotificheAnalogico = ModuloCommessaExtensions.SommaTotali(
                                _prev.TotaleNotificheAnalogico,
                                modulo.NumeroNotificheNazionali,
                                modulo.NumeroNotificheInternazionali);
                        _prev.Totale = ModuloCommessaExtensions.SommaTotali(
                            _prev.Totale,
                            modulo.ValoreNazionali,
                            modulo.ValoreInternazionali);
                        _prev.TotaleNotifiche = ModuloCommessaExtensions.SommaTotali(
                            _prev.TotaleNotifiche,
                            modulo.NumeroNotificheNazionali,
                            modulo.NumeroNotificheInternazionali);
                        break;
                    case 2: // Analog. L. 890/82
                        _prev.TotaleAnalogico890Naz = modulo.ValoreNazionali;
                        _prev.TotaleNotificheAnalogico890Naz = modulo.NumeroNotificheNazionali;
                        _prev.TotaleNotificheAnalogico = ModuloCommessaExtensions.SommaTotali(
                             _prev.TotaleNotificheAnalogico,
                             modulo.NumeroNotificheNazionali,
                             modulo.NumeroNotificheInternazionali);
                        _prev.Totale = ModuloCommessaExtensions.SommaTotali(
                              _prev.Totale,
                              modulo.ValoreNazionali,
                              modulo.ValoreInternazionali);
                        _prev.TotaleNotifiche = ModuloCommessaExtensions.SommaTotali(
                            _prev.TotaleNotifiche,
                            modulo.NumeroNotificheNazionali,
                            modulo.NumeroNotificheInternazionali);
                        break;
                    case 3: // Digitale
                        _prev.TotaleDigitaleNaz = modulo.ValoreNazionali;
                        _prev.TotaleDigitaleInternaz = modulo.ValoreInternazionali;
                        _prev.TotaleNotificheDigitaleNaz = modulo.NumeroNotificheNazionali;
                        _prev.TotaleNotificheDigitaleInternaz = modulo.NumeroNotificheInternazionali;
                        _prev.TotaleNotificheDigitale = ModuloCommessaExtensions.SommaTotali(
                             _prev.TotaleNotificheDigitale,
                             modulo.NumeroNotificheNazionali,
                             modulo.NumeroNotificheInternazionali);
                        _prev.Totale = ModuloCommessaExtensions.SommaTotali(
                             _prev.Totale,
                             modulo.ValoreNazionali,
                             modulo.ValoreInternazionali);
                        _prev.TotaleNotifiche = ModuloCommessaExtensions.SommaTotali(
                          _prev.TotaleNotifiche,
                          modulo.NumeroNotificheNazionali,
                          modulo.NumeroNotificheInternazionali);
                        break;
                    default:
                        throw new ConfigurationException("Errore nella id tipo spedizione");
                } 
          
                if (!valoriRegioni.IsNullNotAny())
                    _prev.ValoriRegione = valoriRegioni?.ToList()!;
            }
            foreach (var item in dic)
            {
                moduloCommessa.Add(item.Value);
            }

            return moduloCommessa;
        }
    }
}