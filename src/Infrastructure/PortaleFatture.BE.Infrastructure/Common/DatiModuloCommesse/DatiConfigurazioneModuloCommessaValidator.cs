using PortaleFatture.BE.Core.Entities.Tipologie;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse;

public static class DatiConfigurazioneModuloCommessaValidator
{
    public static (string, string[]) ValidateCategorieConfiguazione(this IEnumerable<CategoriaSpedizione> model, DatiConfigurazioneModuloCommessaCreateCommand command)
    {
        var dataCreazione = DateTime.UtcNow.ItalianTime();
        if (model.IsNullNotAny() || command == null || command.Tipi!.IsNullNotAny() || command.Categorie!.IsNullNotAny())
            return ("xxx", Array.Empty<string>());

        foreach (var cat in model)
        {
            var foundCat = command.Categorie!.Where(x => x.IdCategoriaSpedizione == cat.Id).FirstOrDefault();
            if (foundCat == null)
                return ("xxx", Array.Empty<string>());
            foundCat.Descrizione = cat.Descrizione;
            foundCat.DataCreazione = dataCreazione;
            foundCat.DataInizioValidita = dataCreazione;
            foreach (var tp in cat.TipoSpedizione!)
            {
                var foundTipo = command.Tipi!.Where(x => x.TipoSpedizione == tp.Id).FirstOrDefault();
                if (foundTipo == null)
                    return ("xxx", Array.Empty<string>());
                foundTipo.Descrizione = tp.Descrizione;
                foundTipo.DataCreazione = dataCreazione;
                foundTipo.DataInizioValidita = dataCreazione;
            }
        }
        if (model.Count() != command.Categorie!.Count() || model.SelectMany(x => x.TipoSpedizione!).Count() != command.Tipi!.Count())
            return ("xxx", Array.Empty<string>());

        return (null, null)!;
    }

    public static (string, string[]) ValidateTipo(DatiConfigurazioneModuloCommessaCreateTipoCommand cmd)
    {
        return ValidateTipo(
            cmd.MediaNotificaInternazionale,
            cmd.MediaNotificaNazionale,
            cmd.Descrizione);
    }

    public static (string, string[]) ValidateCategoria(DatiConfigurazioneModuloCommessaCreateCategoriaCommand cmd)
    {
        return ValidateCategoria(
            cmd.Percentuale,
            cmd.Descrizione);
    }

    private static (string, string[]) ValidateTipo(
      decimal? mediaNotificaInternazionale,
      decimal? mediaNotificaNazionale,
      string? descrizione)
    {
        if (!mediaNotificaInternazionale.HasValue || mediaNotificaInternazionale.Value < 0)
            return ("MediaNotificaInternazionaleInvalid", Array.Empty<string>());

        if (!mediaNotificaNazionale.HasValue || mediaNotificaNazionale.Value < 0)
            return ("MediaNotificaNazionaleInvalid", Array.Empty<string>());

        if (!string.IsNullOrEmpty(descrizione) && descrizione!.Length > 250)
            return ("DescrizioneConfiguazioneModuloCommessaInvalid", Array.Empty<string>());

        return (null, null)!;
    }

    private static (string, string[]) ValidateCategoria(
      int? percentuale,
      string? descrizione)
    {
        if (percentuale < 0)
            return ("xxx", Array.Empty<string>());

        if (!string.IsNullOrEmpty(descrizione) && descrizione.Length > 250)
            return ("DescrizioneConfiguazioneModuloCommessaInvalid", Array.Empty<string>());

        return (null, null)!;
    }
}