using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Dto;

/// <summary>
/// Riga di [be].[vwAppioFinalReport] (modello dati dell'11/11/2026, PF-1016): una per riga di
/// documento. È la forma piatta letta dal DB: la griglia la raggruppa per contratto e trimestre, il
/// download "Documenti Contabili" (api/appio/financialreports/document) la esporta così com'è.
/// </summary>
public sealed class AppIoFinancialReportRigaDto
{
    /// <summary>
    /// Identificativo del contratto (onboardingtokenid).
    /// </summary>
    [HeaderPagoPA(caption: "ContractId", Order = 1)]
    public string? ContractId { get; set; }

    /// <summary>
    /// Identificativo dell'ente (internalistitutionid).
    /// </summary>
    [HeaderPagoPA(caption: "InstitutionId", Order = 2)]
    public string? InstitutionId { get; set; }

    /// <summary>
    /// Nome dell'ente.
    /// </summary>
    [HeaderPagoPA(caption: "Name", Order = 3)]
    public string? Name { get; set; }

    /// <summary>
    /// Prodotto del contratto.
    /// </summary>
    [HeaderPagoPA(caption: "Prodotto", Order = 4)]
    public string? Prodotto { get; set; }

    /// <summary>
    /// Codice fiscale dell'ente.
    /// </summary>
    [HeaderPagoPA(caption: "CodiceFiscale", Order = 5)]
    public string? CodiceFiscale { get; set; }

    /// <summary>
    /// Anno di riferimento.
    /// </summary>
    [HeaderPagoPA(caption: "Year", Order = 6)]
    public int? Year { get; set; }

    /// <summary>
    /// Trimestre di riferimento (1-4).
    /// </summary>
    [HeaderPagoPA(caption: "Quarter", Order = 7)]
    public int? Quarter { get; set; }

    /// <summary>
    /// Anno e trimestre di riferimento, nel formato 'AAAA_T' (es. 2026_2).
    /// </summary>
    [HeaderPagoPA(caption: "YearQuarter", Order = 8)]
    public string? YearQuarter { get; set; }

    /// <summary>
    /// Totale del contratto nel trimestre, calcolato dalla vista: è lo stesso su tutte le righe del
    /// trimestre, anche di documenti (Numero) diversi.
    /// </summary>
    [HeaderPagoPA(caption: "ImportoTotale", Order = 9, Style = XCellStyle.StandardNumberDecimal)]
    public decimal? ImportoTotale { get; set; }

    /// <summary>
    /// Numero del documento.
    /// </summary>
    [HeaderPagoPA(caption: "Numero", Order = 10)]
    public string? Numero { get; set; }

    /// <summary>
    /// Data del documento; null se a DB non è nel formato 'AAAA-MM-GG'.
    /// </summary>
    [HeaderPagoPA(caption: "Data", Order = 11)]
    public DateTime? Data { get; set; }

    /// <summary>
    /// Data e ora dell'ultimo aggiornamento della riga da parte di DATA (gold_updated_at). Senza
    /// attributo Header: va alla griglia, non nell'Excel.
    /// </summary>
    public DateTime? GoldUpdatedAt { get; set; }

    /// <summary>
    /// Codice dell'articolo fatturato.
    /// </summary>
    [HeaderPagoPA(caption: "CodiceArticolo", Order = 12)]
    public string? CodiceArticolo { get; set; }

    /// <summary>
    /// Quantità.
    /// </summary>
    [HeaderPagoPA(caption: "Quantita", Order = 13)]
    public long? Quantita { get; set; }

    /// <summary>
    /// Importo della riga.
    /// </summary>
    [HeaderPagoPA(caption: "Importo", Order = 14, Style = XCellStyle.StandardNumberDecimal)]
    public decimal? Importo { get; set; }

    /// <summary>
    /// Codice IVA.
    /// </summary>
    [HeaderPagoPA(caption: "CodIva", Order = 15)]
    public string? CodIva { get; set; }

    /// <summary>
    /// Condizioni di pagamento.
    /// </summary>
    [HeaderPagoPA(caption: "Condizioni", Order = 16)]
    public string? Condizioni { get; set; }

    /// <summary>
    /// Causale.
    /// </summary>
    [HeaderPagoPA(caption: "Causale", Order = 17)]
    public string? Causale { get; set; }
}
