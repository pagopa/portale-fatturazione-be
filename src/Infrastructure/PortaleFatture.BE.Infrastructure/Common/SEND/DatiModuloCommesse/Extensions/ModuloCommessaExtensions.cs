using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Extensions;

public static class ModuloCommessaExtensions
{
    public static int? SommaTotali(params int?[] valori)
    { 
        if (valori.All(v => v == null))
            return null; 
        return valori.Where(v => v != null).Sum(v => v.Value);
    }
    public static decimal? SommaTotali(params decimal?[] valori)
    {
        if (valori.All(v => v == null))
            return null;
        return valori.Where(v => v != null).Sum(v => v.Value);
    }

    public static ModuloCommessaPrevisionaleTotaleViewModel? ToViewModel(this ModuloCommessaPrevisionaleTotaleDto dto)
    {
        if (dto == null) return null;
       
        return new ModuloCommessaPrevisionaleTotaleViewModel
        {
            AnnoValidita = dto.AnnoValidita,
            MeseValidita = dto.MeseValidita,
            IdEnte = dto.IdEnte,
            RagioneSociale = dto.RagioneSociale,
            IdTipoContratto = dto.IdTipoContratto,
            Stato = dto.Stato,
            Prodotto = dto.Prodotto,
            Totale = dto.Totale,
            DataInserimento = dto.DataInserimento,
            DataChiusura = dto.DataChiusura,
            DataChiusuraLegale = dto.DataChiusuraLegale,
            TotaleDigitaleNaz = dto.TotaleDigitaleNaz,
            TotaleDigitaleInternaz = dto.TotaleDigitaleInternaz,
            TotaleAnalogicoARNaz = dto.TotaleAnalogicoARNaz,
            TotaleAnalogicoARInternaz = dto.TotaleAnalogicoARInternaz,
            TotaleAnalogico890Naz = dto.TotaleAnalogico890Naz,
            TotaleNotificheDigitaleNaz = dto.TotaleNotificheDigitaleNaz,
            TotaleNotificheDigitaleInternaz = dto.TotaleNotificheDigitaleInternaz,
            TotaleNotificheAnalogicoARNaz = dto.TotaleNotificheAnalogicoARNaz,
            TotaleNotificheAnalogicoARInternaz = dto.TotaleNotificheAnalogicoARInternaz,
            TotaleNotificheAnalogico890Naz = dto.TotaleNotificheAnalogico890Naz,
            TotaleNotificheDigitale = dto.TotaleNotificheDigitale,
            TotaleNotificheAnalogico = dto.TotaleNotificheAnalogico,
            TotaleNotifiche = dto.TotaleNotifiche,
            Source = dto.Source,
            Quarter = dto.Quarter
        };
    }
} 