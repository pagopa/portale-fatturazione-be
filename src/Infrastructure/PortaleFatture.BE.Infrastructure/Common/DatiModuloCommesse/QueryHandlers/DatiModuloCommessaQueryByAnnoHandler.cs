﻿using MediatR;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Core.Entities.Scadenziari;
using PortaleFatture.BE.Core.Entities.Tipologie;
using PortaleFatture.BE.Core.Exceptions;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Core.Resources;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Queries.Persistence;
using PortaleFatture.BE.Infrastructure.Common.Persistence.Schemas;
using PortaleFatture.BE.Infrastructure.Common.Scadenziari;
using PortaleFatture.BE.Infrastructure.Common.Scadenziari.Queries;
using PortaleFatture.BE.Infrastructure.Common.Tipologie.Queries.Persistence;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.QueryHandlers
{
    public class DatiModuloCommessaQueryByAnnoHandler(
     IFattureDbContextFactory factory,
     IStringLocalizer<Localization> localizer,
     IScadenziarioService scadenziarioService,
     ILogger<DatiModuloCommessaQueryByAnnoHandler> logger) : IRequestHandler<DatiModuloCommessaQueryGetByAnno, IEnumerable<ModuloCommessaByAnnoDto>?>
    {
        private readonly IFattureDbContextFactory _factory = factory;
        private readonly ILogger<DatiModuloCommessaQueryByAnnoHandler> _logger = logger;
        private readonly IStringLocalizer<Localization> _localizer = localizer;
        private readonly IScadenziarioService _scadenziarioService = scadenziarioService;

        public async Task<IEnumerable<ModuloCommessaByAnnoDto>?> Handle(DatiModuloCommessaQueryGetByAnno command, CancellationToken ct)
        {
            var (annoFatturazione, meseFatturazione, _, _) = Time.YearMonthDayFatturazione();

            command.AnnoValidita = command.AnnoValidita != null ? command.AnnoValidita : annoFatturazione;
            IEnumerable<DatiModuloCommessaTotale>? dati;
            IEnumerable<CategoriaSpedizione>? categorie;
            using (var uow = await _factory.Create(true, cancellationToken: ct))
            {
                dati = await uow.Query(new DatiModuloCommessaQueryGetByAnnoPersistence(
                    command.AuthenticationInfo.IdEnte,
                    command.AnnoValidita.Value,
                    command.AuthenticationInfo.IdTipoContratto,
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

            var dicCategorie = categorie!
                 .Select(x => new KeyValuePair<int, string>(x.Id, x.Tipo!))
                 .ToDictionary(x => x.Key, x => x.Value);

            Dictionary<int, ModuloCommessaByAnnoDto> lista = new();
            foreach (var dato in dati!)
            {
                lista.TryGetValue(dato.MeseValidita, out var modulo);
                modulo ??= new()
                {
                    Prodotto = dato.Prodotto,
                    AnnoValidita = dato.AnnoValidita,
                    IdEnte = dato.IdEnte,
                    IdTipoContratto = dato.IdTipoContratto,
                    MeseValidita = dato.MeseValidita,
                    Stato = dato.Stato,
                    Totali = []
                };

                modulo.Totali!.TryGetValue(dato.IdCategoriaSpedizione, out var totMese);
                totMese = new()
                {
                    IdCategoriaSpedizione = dato.IdCategoriaSpedizione,
                    Tipo = dicCategorie[dato.IdCategoriaSpedizione],
                    TotaleCategoria = dato.TotaleCategoria
                };
                modulo.Totali[dato.IdCategoriaSpedizione] = totMese;
                modulo.Totale = modulo.Totali.Sum(x => x.Value.TotaleCategoria);
 
                var (valid, scadenziario) = await _scadenziarioService.GetScadenziario(
                     command.AuthenticationInfo,
                     TipoScadenziario.DatiModuloCommessa,
                     dato.AnnoValidita,
                     dato.MeseValidita);

                modulo.Modifica = dato.Stato == StatoModuloCommessa.ApertaCaricato && valid;
                lista[dato.MeseValidita] = modulo;
            }

            return lista.Select(x => x.Value);
        }
    }
}