using PortaleFatture.BE.Infrastructure.Common.SEND.Documenti.Common;

/// <summary>
/// Data Transfer Object (DTO) che rappresenta il report di gestione delle fatture, specificamente progettato per l'esportazione in formato Excel.
/// </summary>
/// <remarks>
/// L'Order degli HeaderAttributev2 e' l'UNICA cosa che decide l'ordine delle colonne nel file
/// (GetHeadersv2 fa OrderBy su quello). Tenere gli Order univoci e allineati all'ordine di
/// dichiarazione: fino al 08/09/2026 c'era un Order = 11 duplicato, e siccome OrderBy di LINQ e'
/// stabile l'ordine reale usciva giusto per caso, deciso dall'ordine di dichiarazione e non dai
/// numeri. Aggiungendo o spostando una colonna, rinumerare.
/// </remarks>
public class GestioneFattureReportExcelDto
{
    [HeaderAttributev2(caption: "Stato", Order = 1)]
    public string? Stato { get; set; }

    /// <summary>
    /// Storico delle note dell'azione di staging, gia' appiattito in un'unica stringa: una
    /// occorrenza per riga, separate da '\n'.
    /// </summary>
    /// <remarks>
    /// Style = XCellStyle.Wrapper NON e' decorativo: e' il CellFormat con WrapText = true, l'unico
    /// che fa rendere i '\n' come a-capo dentro la cella. Senza, Excel appiattisce tutto su una riga
    /// sola. Se un domani si sposta questa colonna, lo stile va con lei.
    /// </remarks>
    [HeaderAttributev2(caption: "Note", Order = 2, Style = XCellStyle.Wrapper)]
    public string? Note { get; set; }

    [HeaderAttributev2(caption: "Id Ente", Order = 3)]
    public string? IdEnte { get; set; }

    [HeaderAttributev2(caption: "Ragione Sociale", Order = 4)]
    public string? RagioneSociale { get; set; }

    [HeaderAttributev2(caption: "Id Contratto", Order = 5)]
    public string? IdContratto { get; set; }

    [HeaderAttributev2(caption: "Tipologia Fattura", Order = 6)]
    public string? TipologiaFattura { get; set; }

    [HeaderAttributev2(caption: "Num. Fattura", Order = 7)]
    public long? NumeroFattura { get; set; } // ft.Progressivo è bigint

    [HeaderAttributev2(caption: "Tipo Documento", Order = 8)]
    public string? TipoDocumento { get; set; }

    [HeaderAttributev2(caption: "Anno", Order = 9)]
    public int? Anno { get; set; }

    [HeaderAttributev2(caption: "Mese", Order = 10)]
    public int? Mese { get; set; }

    [HeaderAttributev2(caption: "Totale Notifiche Analogiche", Order = 11)]
    public int? TotaleNotificheAnalogiche { get; set; }

    [HeaderAttributev2(caption: "Totale Notifiche Digitali", Order = 12)]
    public int? TotaleNotificheDigitali { get; set; }

    [HeaderAttributev2(caption: "Totale Notifiche", Order = 13)]
    public int? TotaleNotifiche { get; set; }

    [HeaderAttributev2(caption: "Totale Imponibile Analogico", Order = 14)]
    public decimal? TotaleImponibileAnalogico { get; set; }

    [HeaderAttributev2(caption: "Totale Imponibile Digitale", Order = 15)]
    public decimal? TotaleImponibileDigitale { get; set; }

    [HeaderAttributev2(caption: "Totale Imponibile", Order = 16)]
    public decimal? TotaleImponibile { get; set; }

    [HeaderAttributev2(caption: "Totale Ivato Analogico", Order = 17)]
    public decimal? TotaleIvatoAnalogico { get; set; }

    [HeaderAttributev2(caption: "Totale Ivato Digitale", Order = 18)]
    public decimal? TotaleIvatoDigitale { get; set; }

    [HeaderAttributev2(caption: "Totale Ivato", Order = 19)]
    public decimal? TotaleIvato { get; set; }

    [HeaderAttributev2(caption: "Firmata", Order = 20)]
    public string? Firmata { get; set; }

    [HeaderAttributev2(caption: "Totale Fattura Imponibile", Order = 21)]
    public decimal? TotaleFatturaImponibile { get; set; }

    [HeaderAttributev2(caption: "Tipo Contratto", Order = 22)]
    public string? TipoContratto { get; set; }
}
