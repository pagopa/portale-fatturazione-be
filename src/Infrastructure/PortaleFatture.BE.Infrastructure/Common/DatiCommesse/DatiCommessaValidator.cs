using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.DatiCommesse.Commands;

namespace PortaleFatture.BE.Infrastructure.Common.DatiCommesse;

public static class DatiCommessaValidator
{
    public static (string, string[]) Validate(DatiCommessaUpdateCommand cmd)
    {
        return Validate(
            cmd.Cup,
            cmd.Cig,
            cmd.CodCommessa,
            cmd.DataDocumento,
            cmd.FlagOrdineContratto,
            cmd.SplitPayment,
            cmd.IdTipoContratto,
            cmd.IdDocumento,
            cmd.Map,
            cmd.Contatti);
    }

    public static (string, string[]) Validate(DatiCommessaCreateCommand cmd)
    {
        if (string.IsNullOrEmpty(cmd.IdEnte) || cmd.IdEnte.Length > 50)
            return ("", Array.Empty<string>());

        return Validate(
            cmd.Cup,
            cmd.Cig,
            cmd.CodCommessa,
            cmd.DataDocumento,
            cmd.FlagOrdineContratto,
            cmd.SplitPayment,
            cmd.IdTipoContratto,
            cmd.IdDocumento,
            cmd.Map,
            cmd.Contatti); 
    }

    private static (string, string[]) Validate(
        string? cup,
        string? cig,
        string? codCommessa,
        DateTimeOffset? dataDocumento,
        string? flagOrdineContratto,
        bool? splitPayment,
        long? idTipoContratto,
        string? idDocumento,
        string? map)
    {
        if (string.IsNullOrEmpty(cup) || cup.Length > 15)
            return ("DatiCommessaCupInvalid", Array.Empty<string>());

        if (string.IsNullOrEmpty(cig) || cig.Length > 10)
            return ("DatiCommessaCigInvalid", Array.Empty<string>());

        if (string.IsNullOrEmpty(codCommessa) || codCommessa.Length > 100)
            return ("DatiCommessaCodiceInvalid", Array.Empty<string>());

        if (string.IsNullOrEmpty(flagOrdineContratto) || flagOrdineContratto.Length > 1)
            return ("DatiCommessaFlagOrdineContrattoInvalid", Array.Empty<string>());

        if (idTipoContratto > 0)
            return ("DatiCommessaIdTipoContrattoInvalid", Array.Empty<string>());

        if (string.IsNullOrEmpty(idDocumento) || idDocumento.Length > 20)
            return ("DatiCommessaIdDocumentoInvalid", Array.Empty<string>());

        if (!string.IsNullOrEmpty(map) && map.Length > 100)
            return ("DatiCommessaMapInvalid", Array.Empty<string>()); 

        return (null, null)!;
    }

    private static (string, string[]) Validate(
        string? cup,
        string? cig,
        string? codCommessa,
        DateTimeOffset? dataDocumento,
        string? flagOrdineContratto,
        bool? splitPayment,
        long? idTipoContratto,
        string? idDocumento,
        string? map, 
        List<DatiCommessaContattoCreateCommand>? contatti)
    {  
        if (!contatti!.IsNullNotAny())
        {
            if (contatti!.Count > 3)
                return ("DatiCommessaContattoInvalid", Array.Empty<string>());
            foreach (var contatto in contatti)
            {
                if (contatto.Email!.IsNotValidEmail())
                    return ("DatiCommessaContattoEmailInvalid", Array.Empty<string>());
            }
        }

        return Validate(cup, cig, codCommessa, dataDocumento, flagOrdineContratto, splitPayment, idTipoContratto, idDocumento, map);
    }
}