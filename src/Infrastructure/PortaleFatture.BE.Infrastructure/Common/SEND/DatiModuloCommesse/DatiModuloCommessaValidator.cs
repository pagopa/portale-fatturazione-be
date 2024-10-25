using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Commands;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse;

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
        var (annoFatturazione, meseFatturazione, _, _) = Time.YearMonthDayFatturazione();

        if (numeroNotificheNazionali == null || numeroNotificheNazionali < 0)
            return ("DatiModuloCommessaInvalid", Array.Empty<string>());

        if (numeroNotificheInternazionali == null || numeroNotificheInternazionali < 0)
            return ("DatiModuloCommessaInvalid", Array.Empty<string>());

        if (annoValidita == null || annoValidita != annoFatturazione)
            return ("DatiModuloCommessaDataInvalid", Array.Empty<string>());

        if (meseValidita == null || meseValidita != meseFatturazione)
            return ("DatiModuloCommessaDataInvalid", Array.Empty<string>());

        if (string.IsNullOrEmpty(stato))
            return ("DatiModuloCommessaStatoInvalid", Array.Empty<string>());

        return (null, null)!;
    }
}