using PortaleFatture.BE.Core.Auth;
using PortaleFatture.BE.Core.Extensions;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiFatturazioni.Commands;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiFatturazioni;

public static class DatiFatturazioneValidator
{
    public static (string, string[]) Validate(DatiFatturazioneUpdateCommand cmd)
    {
        return Validate(
            cmd.Cup,
            cmd.CodCommessa,
            cmd.DataDocumento,
            cmd.SplitPayment,
            cmd.IdDocumento,
            cmd.Map,
            cmd.TipoCommessa,
            cmd.Pec,
            cmd.Contatti);
    }

    public static (string, string[]) Validate(DatiFatturazioneCreateCommand cmd)
    {
        return Validate(
            cmd.Cup,
            cmd.CodCommessa,
            cmd.DataDocumento,
            cmd.SplitPayment,
            cmd.IdDocumento,
            cmd.Map,
            cmd.TipoCommessa,
            cmd.Pec,
            cmd.Contatti);
    }

    private static (string, string[]) Validate(
        string? cup,
        string? codCommessa,
        DateTimeOffset? dataDocumento,
        bool? splitPayment,
        string? idDocumento,
        string? map,
        string? tipoCommessa,
        string? pec)
    {
        if (!string.IsNullOrEmpty(cup) && cup.Length > 15)
            return ("DatiFatturazioneCupInvalid", Array.Empty<string>());

        if (!string.IsNullOrEmpty(codCommessa) && codCommessa.Length > 100)
            return ("DatiFatturazioneCodiceInvalid", Array.Empty<string>());

        if (!string.IsNullOrEmpty(idDocumento) && idDocumento.Length > 20)
            return ("DatiFatturazioneIdDocumentoInvalid", Array.Empty<string>());

        if (!string.IsNullOrEmpty(map) && map.Length > 100)
            return ("DatiFatturazioneMapInvalid", Array.Empty<string>());

        if (!string.IsNullOrEmpty(tipoCommessa) && tipoCommessa.Length > 1)
            return ("DatiFatturazioneTipoCommessaInvalid", Array.Empty<string>());

        if (string.IsNullOrEmpty(pec) || pec.IsNotValidEmail())
            return ("DatiFatturazionePecInvalid", Array.Empty<string>());

        return (null, null)!;
    }

    private static (string, string[]) Validate(
        string? cup,
        string? codCommessa,
        DateTimeOffset? dataDocumento,
        bool? splitPayment,
        string? idDocumento,
        string? map,
        string? tipoCommessa,
        string? pec,
        List<DatiFatturazioneContattoCreateCommand>? contatti)
    {
        if (!contatti!.IsNullNotAny())
        {
            if (contatti!.Count > 3)
                return ("DatiFatturazioneContattoInvalid", Array.Empty<string>());
            foreach (var contatto in contatti)
            {
                if (contatto.Email!.IsNotValidEmail())
                    return ("DatiFatturazioneContattoEmailInvalid", Array.Empty<string>());
            }
        }
        else  // deve esistere almeno un contatto
            return ("DatiFatturazioneContattoInvalid", Array.Empty<string>());

        return Validate(cup, codCommessa, dataDocumento, splitPayment, idDocumento, map, tipoCommessa, pec);
    }
}