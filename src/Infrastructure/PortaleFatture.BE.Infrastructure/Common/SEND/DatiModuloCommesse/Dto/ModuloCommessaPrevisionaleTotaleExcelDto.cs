using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;

public class ModuloCommessaPrevisionaleTotaleExcelDto
{
    [Header(caption: "Modifica", Order = 1)]
    public bool Modifica { get; set; }

    [Header(caption: "Anno Validità", Order = 2)]
    public int AnnoValidita { get; set; }

    [Header(caption: "Mese Validità", Order = 3)]
    public int MeseValidita { get; set; }

    [Header(caption: "Id Ente", Order = 4)]
    public string? IdEnte { get; set; }

    [Header(caption: "Ragione Sociale", Order = 5)]
    public string? RagioneSociale { get; set; }

    [Header(caption: "Id Tipo Contratto", Order = 6)]
    public long IdTipoContratto { get; set; }

    [Header(caption: "Stato", Order = 7)]
    public string? Stato { get; set; }

    [Header(caption: "Prodotto", Order = 8)]
    public string? Prodotto { get; set; }

    [Header(caption: "Totale", Order = 9)]
    public decimal? Totale { get; set; }

    [Header(caption: "Data Inserimento", Order = 10)]
    public DateTime? DataInserimento { get; set; }

    [Header(caption: "Data Chiusura", Order = 11)]
    public DateTime? DataChiusura { get; set; }

    [Header(caption: "Data Chiusura Legale", Order = 12)]
    public DateTime? DataChiusuraLegale { get; set; }

    [Header(caption: "Totale Digitale Naz", Order = 13)]
    public decimal? TotaleDigitaleNaz { get; set; }

    [Header(caption: "Totale Digitale Internaz", Order = 14)]
    public decimal? TotaleDigitaleInternaz { get; set; }

    [Header(caption: "Totale Analogico AR Naz", Order = 15)]
    public decimal? TotaleAnalogicoARNaz { get; set; }

    [Header(caption: "Totale Analogico AR Internaz", Order = 16)]
    public decimal? TotaleAnalogicoARInternaz { get; set; }

    [Header(caption: "Totale Analogico 890 Naz", Order = 17)]
    public decimal? TotaleAnalogico890Naz { get; set; }

    [Header(caption: "Totale Notifiche Digitale Naz", Order = 18)]
    public int? TotaleNotificheDigitaleNaz { get; set; }

    [Header(caption: "Totale Notifiche Digitale Internaz", Order = 19)]
    public int? TotaleNotificheDigitaleInternaz { get; set; }

    [Header(caption: "Totale Notifiche Analogico AR Naz", Order = 20)]
    public int? TotaleNotificheAnalogicoARNaz { get; set; }

    [Header(caption: "Totale Notifiche Analogico AR Internaz", Order = 21)]
    public int? TotaleNotificheAnalogicoARInternaz { get; set; }

    [Header(caption: "Totale Notifiche Analogico 890 Naz", Order = 22)]
    public int? TotaleNotificheAnalogico890Naz { get; set; }

    [Header(caption: "Totale Notifiche Digitale", Order = 23)]
    public int? TotaleNotificheDigitale { get; set; }

    [Header(caption: "Totale Notifiche Analogico", Order = 24)]
    public int? TotaleNotificheAnalogico { get; set; }

    [Header(caption: "Totale Notifiche", Order = 25)]
    public int? TotaleNotifiche { get; set; }

    [Header(caption: "Source", Order = 26)]
    public string? Source { get; set; }

    [Header(caption: "Quarter", Order = 27)]
    public string? Quarter { get; set; }

    [Header(caption: "Valori Regione", Order = 28)]
    public List<ValoriRegioneDto>? ValoriRegione { get; set; }

    [Header(caption: "Tipologia Contratto", Order = 29)]
    public string? TipologiaContratto { get; set; }

    [Header(caption: "Data Contratto", Order = 30)]
    public DateTime? DataContratto { get; set; }
}