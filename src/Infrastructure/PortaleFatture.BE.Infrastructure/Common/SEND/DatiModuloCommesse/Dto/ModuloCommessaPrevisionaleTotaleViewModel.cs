using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;


public class ModuloCommessaPrevisionaleTotaleViewModel
{

    [Header(caption: "Anno", Order = 1)]
    public int AnnoValidita { get; set; }
    [Header(caption: "Mese", Order = 2)]
    public int MeseValidita { get; set; }
    [Header(caption: "Id Ente", Order = 3)]
    public string? IdEnte { get; set; }
    [Header(caption: "Ragione Sociale", Order = 4)]
    public string? RagioneSociale { get; set; }
    [Header(caption: "Id Contratto", Order = 5)]
    public long IdTipoContratto { get; set; }
    [Header(caption: "Stato", Order = 6)]
    public string? Stato { get; set; }
    [Header(caption: "Prodotto", Order = 7)]
    public string? Prodotto { get; set; }
    [Header(caption: "Totale Euro", Order = 10)]
    public decimal? Totale { get; set; }
    [Header(caption: "Data Inserimento", Order = 8)]
    public DateTime? DataInserimento { get; set; }
    [Header(caption: "Data Chiusura", Order = 9)]
    public DateTime? DataChiusura { get; set; }
    [Header(caption: "Data Legale", Order = 11)]
    public DateTime? DataChiusuraLegale { get; set; }
    [Header(caption: "Totale Euro Digitale Naz", Order = 12)]
    public decimal? TotaleDigitaleNaz { get; set; }
    [Header(caption: "Totale Euro Digitale Internaz", Order = 13)]
    public decimal? TotaleDigitaleInternaz { get; set; }
    [Header(caption: "Totale Euro Analogico AR Naz", Order = 14)]
    public decimal? TotaleAnalogicoARNaz { get; set; }
    [Header(caption: "Totale Euro Analogico AR Internaz", Order = 15)]
    public decimal? TotaleAnalogicoARInternaz { get; set; }
    [Header(caption: "Totale Euro Analogico 890 Naz", Order = 16)]
    public decimal? TotaleAnalogico890Naz { get; set; }
    [Header(caption: "Totale Notifiche Digitale Naz", Order = 17)]
    public int? TotaleNotificheDigitaleNaz { get; set; }
    [Header(caption: "Totale Notifiche Digitale Internaz", Order = 18)]
    public int? TotaleNotificheDigitaleInternaz { get; set; }
    [Header(caption: "Totale Notifiche Analogico AR Naz", Order = 19)]
    public int? TotaleNotificheAnalogicoARNaz { get; set; }
    [Header(caption: "Totale Notifiche Analogico AR Internaz", Order = 20)]
    public int? TotaleNotificheAnalogicoARInternaz { get; set; }
    [Header(caption: "Totale Notifiche Analogico 890 Naz", Order = 21)]
    public int? TotaleNotificheAnalogico890Naz { get; set; }
    [Header(caption: "Totale Notifiche Digitale", Order = 22)]
    public int? TotaleNotificheDigitale { get; set; }
    [Header(caption: "Totale Notifiche Analogico", Order = 23)]
    public int? TotaleNotificheAnalogico { get; set; }
    [Header(caption: "Totale Notifiche", Order = 24)]
    public int? TotaleNotifiche { get; set; }

    [Header(caption: "Stato", Order = 25)]
    public string? Source { get; set; }

    [Header(caption: "Trimestre", Order = 26)]
    public string? Quarter { get; set; }

    [Header(caption: "Tipo Contratto", Order = 27)]
    public string? TipologiaContratto { get; set; }
}