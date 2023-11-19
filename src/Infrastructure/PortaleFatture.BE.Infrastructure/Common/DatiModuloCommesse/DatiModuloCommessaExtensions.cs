using System.Security.Cryptography;
using PortaleFatture.BE.Core.Entities.DatiModuloCommesse;
using PortaleFatture.BE.Core.Entities.Tipologie;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse;

public static class DatiModuloCommessaExtensions
{
    public static DatiModuloCommessaTotaleCreateListCommand GetTotali(
        this DatiModuloCommessaCreateListCommand command,
        IEnumerable<CategoriaSpedizione>? categorie,
        DatiConfigurazioneModuloCommessa? confModuloCommessa,
        string? idEnte,
        int anno,
        int mese,
        long idTipoContratto,
        string? prodotto,
        string? stato)
    {
        DatiModuloCommessaTotaleCreateListCommand tCommand = new();

        var categorieTotale = categorie!
            .Select(x => new KeyValuePair<int, decimal>(x.Id, 0M))
            .ToDictionary(x => x.Key, x => x.Value);

        var categorieTipiPrezziInter = confModuloCommessa!.Tipi!
            .Select(x => new KeyValuePair<int, decimal>(x.IdTipoSpedizione, x.MediaNotificaInternazionale))
            .ToDictionary(x => x.Key, x => x.Value);

        var categorieTipiPrezziNaz = confModuloCommessa!.Tipi!
            .Select(x => new KeyValuePair<int, decimal>(x.IdTipoSpedizione, x.MediaNotificaNazionale))
            .ToDictionary(x => x.Key, x => x.Value);

        var categoriePercentuali = confModuloCommessa!.Categorie!
            .Select(x => new KeyValuePair<int, decimal>(x.IdCategoriaSpedizione, 0M))
            .ToDictionary(x => x.Key, x => x.Value);
 
        foreach (var cmd in command.DatiModuloCommessaListCommand!) // per id tipo spedizione
        { 
            var idCategoria = categorie!.SelectMany(x => x.TipoSpedizione!).Where(x => x.Id == cmd.IdTipoSpedizione).FirstOrDefault()!.Id;

            var fNotifica = categorieTotale.TryGetValue(idCategoria, out decimal totale);
            var fprezzoInter = categorieTipiPrezziInter.TryGetValue(cmd.IdTipoSpedizione, out decimal prezzoInter);
            var fprezzoNaz = categorieTipiPrezziNaz.TryGetValue(cmd.IdTipoSpedizione, out decimal prezzoNaz);
            var prezzo = cmd.NumeroNotificheInternazionali * prezzoInter + cmd.NumeroNotificheNazionali * prezzoNaz;

            if (fNotifica)
                categorieTotale[idCategoria] += prezzo;
            else
                categorieTotale.Add(idCategoria, prezzo);
        }


        tCommand.DatiModuloCommessaTotaleListCommand = new();
        foreach (var keyValue in categorieTotale)
        {
            var fpercent = categoriePercentuali.TryGetValue(keyValue.Key, out decimal percent);
            var apercent = (keyValue.Value / 100M) * percent;
            tCommand.DatiModuloCommessaTotaleListCommand.Add(new DatiModuloCommessaTotaleCreateCommand()
            {
                IdCategoriaSpedizione = keyValue.Key,
                IdEnte = idEnte,
                AnnoValidita = anno,
                MeseValidita = mese,
                IdTipoContratto = idTipoContratto,
                Prodotto = prodotto,
                Stato = stato,
                TotaleCategoria = apercent
            });
        }
        return tCommand;
    }
}