using PortaleFatture.BE.Core.Entities.SEND.DatiModuloCommesse.Dto;
using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.SEND.DatiModuloCommesse.Dto;
public class ModuloCommessaPrevisionaleDownloadDto
{
    //[Header(caption: "Modifica", Order = 1)]
    public bool Modifica { get; set; }

    [Header(caption: "Anno", Order = 2)]
    public int AnnoValidita { get; set; }

    [Header(caption: "Mese", Order = 3)]
    public int MeseValidita { get; set; }

    [Header(caption: "Id Ente", Order = 4)]
    public string? IdEnte { get; set; }

    [Header(caption: "Ragione Sociale", Order = 5)]
    public string? RagioneSociale { get; set; } 
    public long IdTipoContratto { get; set; }

    [Header(caption: "Stato", Order = 7)]
    public string? Stato { get; set; } 
    public string? Prodotto { get; set; }

    [Header(caption: "Totale Euro", Order = 11)]
    public decimal? Totale { get; set; }

    [Header(caption: "Data Inserimento", Order = 9)]
    public DateTime? DataInserimento { get; set; }

    [Header(caption: "Data Chiusura", Order = 10)]
    public DateTime? DataChiusura { get; set; }

    //[Header(caption: "Data Chiusura Legale", Order = 12)]
    public DateTime? DataChiusuraLegale { get; set; }

    [Header(caption: "Totale Euro Digitale Naz", Order = 13)]
    public decimal? TotaleDigitaleNaz { get; set; }

    [Header(caption: "Totale Euro Digitale Internaz", Order = 14)]
    public decimal? TotaleDigitaleInternaz { get; set; }

    [Header(caption: "Totale Euro Analogico AR Naz", Order = 15)]
    public decimal? TotaleAnalogicoARNaz { get; set; }

    [Header(caption: "Totale Euro Analogico AR Internaz", Order = 16)]
    public decimal? TotaleAnalogicoARInternaz { get; set; }

    [Header(caption: "Totale Euro Analogico 890 Naz", Order = 17)]
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

    [Header(caption: "Stato", Order = 26)]
    public string? Source { get; set; }

    [Header(caption: "Trimestre", Order = 27)]
    public string? Quarter { get; set; } 
 
    public List<ValoriRegioneDto>? ValoriRegione { get; set; }

    [Header(caption: "Tipo Contratto", Order = 29)]
    public string? TipologiaContratto { get; set; }

    [Header(caption: "Data Contratto", Order = 30)]
    public DateTime? DataContratto { get; set; }
}

public class ModuloCommessaPrevisionaleDownloadDtov2
{
    [Header(caption: "Anno", Order = 1)]
    public int Anno { get; set; }

    [Header(caption: "Mese", Order = 2)]
    public int Mese { get; set; }

    [Header(caption: "ID Ente", Order = 3)]
    public string? IdEnte { get; set; }

    [Header(caption: "Ente", Order = 4)]
    public string? Ente { get; set; }

    [Header(caption: "Tipo Report", Order = 5)]
    public string? TipoReport { get; set; }

    [Header(caption: "Totale Modulo Commessa", Order = 6)]
    public int? TotaleModuloCommessa { get; set; }

    [Header(caption: "AR", Order = 7)]
    public int? AR { get; set; }

    [Header(caption: "890", Order = 8)]
    public int? A890 { get; set; }

    [Header(caption: "Totale Regioni", Order = 9)]
    public int? TotaleRegioni { get; set; }

    [Header(caption: "Regione", Order = 10)]
    public string? Regione { get; set; }

    [Header(caption: "Calcolato", Order = 11)]
    public bool? Calcolato { get; set; }

    [Header(caption: "AR Regioni %", Order = 12)]
    public decimal? ArRegioniPerc { get; set; }

    [Header(caption: "890 Regioni %", Order = 13)]
    public decimal? Regioni890Perc { get; set; }

    [Header(caption: "Totale Regioni %", Order = 14)]
    public decimal? TotaleRegioniPerc { get; set; }

    [Header(caption: "Totale Copertura Regionale", Order = 15)]
    public string? TotaleCoperturaRegionale { get; set; }
}