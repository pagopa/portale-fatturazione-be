using System.Security.Cryptography;
using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse;
using PortaleFatture.BE.Core.Entities.SEND.Tipologie;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Commands;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse;

public static class DatiModuloCommessaExtensions
{
    internal static string Map(this long idTipoContratto)
    {
        if (idTipoContratto == 1)
            return "PAL";
        else if (idTipoContratto == 2)
            return "PAC";
        return string.Empty;
    }

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

        var categoriePercentuali = confModuloCommessa!.Categorie!
            .Select(x => new KeyValuePair<int, int>(x.IdCategoriaSpedizione, x.Percentuale))
            .ToDictionary(x => x.Key, x => x.Value);

        var tipiSpedizionePrezziInter = confModuloCommessa!.Tipi!
            .Select(x => new KeyValuePair<int, decimal>(x.IdTipoSpedizione, x.MediaNotificaInternazionale))
            .ToDictionary(x => x.Key, x => x.Value);

        var tipiSpedizionePrezziNaz = confModuloCommessa!.Tipi!
            .Select(x => new KeyValuePair<int, decimal>(x.IdTipoSpedizione, x.MediaNotificaNazionale))
            .ToDictionary(x => x.Key, x => x.Value);

        var numeroTotaleNotifiche = command.DatiModuloCommessaListCommand!.Select(x => x.NumeroNotificheNazionali + x.NumeroNotificheInternazionali).Sum();
        Dictionary<long, ParzialiTipoCommessa>? parzialiTipoCommessa = [];
        foreach (var cmd in command.DatiModuloCommessaListCommand!) // per id tipo spedizione
        {

            var idCategoria = categorie!.SelectMany(x => x.TipoSpedizione!).Where(x => x.Id == cmd.IdTipoSpedizione).FirstOrDefault()!.IdCategoriaSpedizione;

            var fNotifica = categorieTotale.TryGetValue(idCategoria, out var totale);
            var fprezzoInter = tipiSpedizionePrezziInter.TryGetValue(cmd.IdTipoSpedizione, out var prezzoInter);
            var fprezzoNaz = tipiSpedizionePrezziNaz.TryGetValue(cmd.IdTipoSpedizione, out var prezzoNaz);

            var categoriaDigitale = categorie!.Where(x => x.Tipo!.Contains("digitale", StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault(); // seleziono digitale

            var isCategoriaDigitale = categoriaDigitale!.Id == idCategoria;


            decimal prezzo;

            if (isCategoriaDigitale)
            {
                parzialiTipoCommessa.TryAdd(cmd.IdTipoSpedizione, new ParzialiTipoCommessa()
                {
                    PrezzoInternazionali = prezzoInter,
                    PrezzoNazionali = prezzoNaz,
                    ValoreInternazionali = prezzoInter * cmd.NumeroNotificheInternazionali,
                    ValoreNazionali = prezzoNaz * cmd.NumeroNotificheNazionali
                });

                categorieTotale[idCategoria] = prezzoNaz * numeroTotaleNotifiche;
            }
            else // analogico
            {
                parzialiTipoCommessa.TryAdd(cmd.IdTipoSpedizione, new ParzialiTipoCommessa()
                {
                    PrezzoInternazionali = prezzoInter,
                    PrezzoNazionali = prezzoNaz,
                    ValoreInternazionali = cmd.NumeroNotificheInternazionali * prezzoInter,
                    ValoreNazionali = cmd.NumeroNotificheNazionali * prezzoNaz,
                }); ;

                prezzo = cmd.NumeroNotificheInternazionali * prezzoInter + cmd.NumeroNotificheNazionali * prezzoNaz;

                if (fNotifica)
                    categorieTotale[idCategoria] += prezzo;
                else
                    categorieTotale.Add(idCategoria, prezzo);
            }
        }


        tCommand.DatiModuloCommessaTotaleListCommand = [];
        foreach (var keyValue in categorieTotale)
        {
            var fpercent = categoriePercentuali.TryGetValue(keyValue.Key, out var percent);
            var apercent = keyValue.Value / 100M * percent;
            tCommand.DatiModuloCommessaTotaleListCommand.Add(new DatiModuloCommessaTotaleCreateCommand()
            {
                IdCategoriaSpedizione = keyValue.Key,
                IdEnte = idEnte,
                AnnoValidita = anno,
                MeseValidita = mese,
                IdTipoContratto = idTipoContratto,
                Prodotto = prodotto,
                Stato = stato,
                TotaleCategoria = apercent,
                PercentualeCategoria = percent,
                Totale = keyValue.Value
            });
        }

        tCommand.ParzialiTipoCommessa = parzialiTipoCommessa;
        return tCommand;
    }
}