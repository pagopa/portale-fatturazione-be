using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;

/// <summary>
/// Data Transfer Object (DTO) che rappresenta il report di gestione delle fatture, specificamente progettato per l'esportazione in formato Excel.
/// </summary>
public class GestioneFattureReportExcelDto
{

    [HeaderAttributev2(caption: "Id Ente", Order = 1)]
    public string? IdEnte { get; set; }

    [HeaderAttributev2(caption: "Ragione Sociale", Order = 2)]
    public string? RagioneSociale { get; set; }

    [HeaderAttributev2(caption: "Id Contratto", Order = 3)]
    public string? IdContratto { get; set; }
    
    [HeaderAttributev2(caption: "Tipologia Fattura", Order = 4)]
    public string? TipologiaFattura { get; set; }

    [HeaderAttributev2(caption: "Num. Fattura", Order = 5)]
    public long? NumeroFattura { get; set; } // ft.Progressivo è bigint

    [HeaderAttributev2(caption: "Tipo Documento", Order = 6)]
    public string? TipoDocumento { get; set; }

    [HeaderAttributev2(caption: "Anno", Order = 7)]
    public int? Anno { get; set; }

    [HeaderAttributev2(caption: "Mese", Order = 8)]
    public int? Mese { get; set; }

    [HeaderAttributev2(caption: "Totale Notifiche Analogiche", Order = 9)]
    public int? TotaleNotificheAnalogiche { get; set; }

    [HeaderAttributev2(caption: "Totale Notifiche Digitali", Order = 10)]
    public int? TotaleNotificheDigitali { get; set; }

    [HeaderAttributev2(caption: "Totale Notifiche", Order = 11)]
    public int? TotaleNotifiche { get; set; }

    [HeaderAttributev2(caption: "Totale Imponibile Analogico", Order = 11)]
    public decimal? TotaleImponibileAnalogico { get; set; }

    [HeaderAttributev2(caption: "Totale Imponibile Digitale", Order = 12)]
    public decimal? TotaleImponibileDigitale { get; set; }

    [HeaderAttributev2(caption: "Totale Imponibile", Order = 13)]
    public decimal? TotaleImponibile { get; set; }

    [HeaderAttributev2(caption: "Totale Ivato Analogico", Order = 14)]
    public decimal? TotaleIvatoAnalogico { get; set; }

    [HeaderAttributev2(caption: "Totale Ivato Digitale", Order = 15)]
    public decimal? TotaleIvatoDigitale { get; set; }

    [HeaderAttributev2(caption: "Totale Ivato", Order = 16)]
    public decimal? TotaleIvato { get; set; }

    [HeaderAttributev2(caption: "Firmata", Order = 17)]
    public string? Firmata { get; set; }

    [HeaderAttributev2(caption: "Totale Fattura Imponibile", Order = 18)]
    public decimal? TotaleFatturaImponibile { get; set; }

    [HeaderAttributev2(caption: "Tipo Contratto", Order = 19)]
    public string? TipoContratto { get; set; }

    [HeaderAttributev2(caption: "Stato", Order = 20)]
    public string? Stato { get; set; }
}
