using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse.Commands;

namespace PortaleFatture.BE.Infrastructure.Common.DatiModuloCommesse;

public static class DatiModuloCommessaValidator
{

    public static (string, string[]) Validate(DatiModuloCommessaCreateCommand cmd)
    { 
        return Validate(
             cmd.NumeroNotificheNazionali,
             cmd.NumeroNotificheInternazionali, 
             cmd.AnnoValidita,
             cmd.MeseValidita, 
             cmd.Stato
           );
    }

    private static (string, string[]) Validate(
        int? numeroNotificheNazionali,
        int? numeroNotificheInternazionali, 
        int? annoValidita,
        int? meseValidita,
        string? stato)
    {
        var hereNow = DateTime.UtcNow.ItalianTime();

        if (numeroNotificheNazionali == null || numeroNotificheNazionali < 0)
            return ("DatiModuloCommessaInvalid", Array.Empty<string>());

        if (numeroNotificheInternazionali == null || numeroNotificheInternazionali < 0)
            return ("DatiModuloCommessaInvalid", Array.Empty<string>()); 

        if (annoValidita == null || annoValidita != hereNow.Year)
            return ("DatiModuloCommessaDataInvalid", Array.Empty<string>());

        if (meseValidita == null || meseValidita != hereNow.Month)
            return ("DatiModuloCommessaDataInvalid", Array.Empty<string>());

        if (string.IsNullOrEmpty(stato))
            return ("DatiModuloCommessaStatoInvalid", Array.Empty<string>());

        return (null, null)!;
    } 
}