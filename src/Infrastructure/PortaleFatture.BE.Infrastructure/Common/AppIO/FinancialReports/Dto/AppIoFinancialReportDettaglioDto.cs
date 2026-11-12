using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Dto;

/// <summary>
/// Riga di [be].[vwAppioFinalReportDetails]: anagrafica del contratto e riga del documento.
/// È la forma piatta letta dal DB: la pagina di dettaglio la raggruppa in testata e righe
/// (<see cref="AppIoFinancialReportDettaglioDto"/>), il download la esporta così com'è.
/// </summary>
public sealed class AppIoFinancialReportDettaglioRigaDto
{
    /// <summary>Nome dell'ente.</summary>
    [HeaderPagoPA(caption: "NomeEnte", Order = 1)]
    public string? Name { get; set; }

    /// <summary>Identificativo del contratto (onboardingtokenid).</summary>
    [HeaderPagoPA(caption: "ContractId", Order = 2)]
    public string? ContractId { get; set; }

    /// <summary>Prodotto del contratto.</summary>
    [HeaderPagoPA(caption: "Prodotto", Order = 3)]
    public string? Prodotto { get; set; }

    /// <summary>Data di creazione del contratto.</summary>
    [HeaderPagoPA(caption: "DataCreazione", Order = 4)]
    public DateTime? DataCreazione { get; set; }

    /// <summary>Piano tariffario.</summary>
    [HeaderPagoPA(caption: "PricingPlan", Order = 5)]
    public string? PricingPlan { get; set; }

    /// <summary>Partita IVA.</summary>
    [HeaderPagoPA(caption: "PartitaIva", Order = 6)]
    public string? PartitaIva { get; set; }

    /// <summary>Codice fiscale (tributario) dell'ente.</summary>
    [HeaderPagoPA(caption: "CodiceFiscale", Order = 7)]
    public string? CodiceFiscale { get; set; }

    /// <summary>Data dell'ultimo aggiornamento del contratto.</summary>
    [HeaderPagoPA(caption: "UltimoAggiornamento", Order = 8)]
    public DateTime? UltimoAggiornamento { get; set; }

    /// <summary>Anno e trimestre, nel formato 'AAAA_T'.</summary>
    [HeaderPagoPA(caption: "YearQuarter", Order = 9)]
    public string? YearQuarter { get; set; }

    /// <summary>Anno di riferimento.</summary>
    [HeaderPagoPA(caption: "Year", Order = 10)]
    public int? Year { get; set; }

    /// <summary>Trimestre di riferimento (1-4).</summary>
    [HeaderPagoPA(caption: "Quarter", Order = 11)]
    public int? Quarter { get; set; }

    /// <summary>Totale del contratto nel trimestre, dalla vista.</summary>
    [HeaderPagoPA(caption: "ImportoTotale", Order = 12, Style = XCellStyle.StandardNumberDecimal)]
    public decimal? ImportoTotale { get; set; }

    /// <summary>Numero del documento.</summary>
    [HeaderPagoPA(caption: "Numero", Order = 13)]
    public string? Numero { get; set; }

    /// <summary>Tipo del documento.</summary>
    [HeaderPagoPA(caption: "TipoDoc", Order = 14)]
    public string? TipoDoc { get; set; }

    /// <summary>Valuta.</summary>
    [HeaderPagoPA(caption: "Valuta", Order = 15)]
    public string? Valuta { get; set; }

    /// <summary>Codice aggiuntivo.</summary>
    [HeaderPagoPA(caption: "CodiceAggiuntivo", Order = 16)]
    public string? CodiceAggiuntivo { get; set; }

    /// <summary>Data del documento; null se a DB non è nel formato 'AAAA-MM-GG'.</summary>
    [HeaderPagoPA(caption: "DataReport", Order = 17)]
    public DateTime? DataReport { get; set; }

    /// <summary>Data di riferimento; null se a DB non è nel formato 'AAAA-MM-GG'.</summary>
    [HeaderPagoPA(caption: "DataRiferimento", Order = 18)]
    public DateTime? DataRiferimento { get; set; }

    /// <summary>Codice dell'articolo fatturato.</summary>
    [HeaderPagoPA(caption: "CodiceArticolo", Order = 19)]
    public string? CodiceArticolo { get; set; }

    /// <summary>Quantità.</summary>
    [HeaderPagoPA(caption: "Quantita", Order = 20)]
    public long? Quantita { get; set; }

    /// <summary>Importo della riga.</summary>
    [HeaderPagoPA(caption: "Importo", Order = 21)]
    public decimal? Importo { get; set; }

    /// <summary>Codice IVA.</summary>
    [HeaderPagoPA(caption: "CodIva", Order = 22)]
    public string? CodIva { get; set; }

    /// <summary>Condizioni di pagamento.</summary>
    [HeaderPagoPA(caption: "Condizioni", Order = 23)]
    public string? Condizioni { get; set; }

    /// <summary>Causale.</summary>
    [HeaderPagoPA(caption: "Causale", Order = 24)]
    public string? Causale { get; set; }
}

/// <summary>
/// Dettaglio di un documento emesso APP IO (contratto e trimestre): testata con l'anagrafica del
/// contratto e righe dei documenti.
/// </summary>
public sealed class AppIoFinancialReportDettaglioDto
{
    /// <summary>Nome dell'ente.</summary>
    public string? Name { get; set; }

    /// <summary>Identificativo del contratto (onboardingtokenid).</summary>
    public string? ContractId { get; set; }

    /// <summary>Prodotto del contratto.</summary>
    public string? Prodotto { get; set; }

    /// <summary>Data di creazione del contratto.</summary>
    public DateTime? DataCreazione { get; set; }

    /// <summary>Piano tariffario.</summary>
    public string? PricingPlan { get; set; }

    /// <summary>Partita IVA.</summary>
    public string? PartitaIva { get; set; }

    /// <summary>Codice fiscale (tributario) dell'ente.</summary>
    public string? CodiceFiscale { get; set; }

    /// <summary>Data dell'ultimo aggiornamento del contratto.</summary>
    public DateTime? UltimoAggiornamento { get; set; }

    /// <summary>Anno e trimestre, nel formato 'AAAA_T'.</summary>
    public string? YearQuarter { get; set; }

    /// <summary>Anno di riferimento.</summary>
    public int? Year { get; set; }

    /// <summary>Trimestre di riferimento (1-4).</summary>
    public int? Quarter { get; set; }

    /// <summary>Totale del contratto nel trimestre, dalla vista.</summary>
    public decimal? ImportoTotale { get; set; }

    /// <summary>Righe dei documenti, nell'ordine di numero e articolo.</summary>
    public List<AppIoFinancialReportDettaglioPosizioneDto> Posizioni { get; set; } = [];
}

/// <summary>
/// Riga di documento del dettaglio di un documento emesso APP IO.
/// </summary>
public sealed class AppIoFinancialReportDettaglioPosizioneDto
{
    /// <summary>Numero del documento.</summary>
    public string? Numero { get; set; }

    /// <summary>Tipo del documento.</summary>
    public string? TipoDoc { get; set; }

    /// <summary>Valuta.</summary>
    public string? Valuta { get; set; }

    /// <summary>Codice aggiuntivo.</summary>
    public string? CodiceAggiuntivo { get; set; }

    /// <summary>Data del documento.</summary>
    public DateTime? DataReport { get; set; }

    /// <summary>Data di riferimento.</summary>
    public DateTime? DataRiferimento { get; set; }

    /// <summary>Codice dell'articolo fatturato.</summary>
    public string? CodiceArticolo { get; set; }

    /// <summary>Quantità.</summary>
    public long? Quantita { get; set; }

    /// <summary>Importo della riga.</summary>
    public decimal? Importo { get; set; }

    /// <summary>Codice IVA.</summary>
    public string? CodIva { get; set; }

    /// <summary>Condizioni di pagamento.</summary>
    public string? Condizioni { get; set; }

    /// <summary>Causale.</summary>
    public string? Causale { get; set; }
}
