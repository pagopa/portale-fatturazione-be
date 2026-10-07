using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Dto;

/// <summary>
/// Contenuto del "Financial Report" APP IO (api/appio/financialreports/detail/download): le righe
/// del financial report e le posizioni dei documenti, con lo stesso filtro.
/// </summary>
public sealed class AppIoFinancialReportDettaglioDto
{
    /// <summary>
    /// Righe di [be].[vwAppioFinancialReport].
    /// </summary>
    public IEnumerable<AppIoFinancialReportValoreDto>? FinancialReports { get; set; }

    /// <summary>
    /// Righe di [be].[vwAppioFinancialReportPositions].
    /// </summary>
    public IEnumerable<AppIoFinancialReportPosizioneExcelDto>? Posizioni { get; set; }
}

/// <summary>
/// Riga di [be].[vwAppioFinancialReport]: transazioni e valore per contratto, trimestre e articolo.
/// È il foglio "financial-report" del download api/appio/financialreports/detail/download.
/// </summary>
public sealed class AppIoFinancialReportValoreDto
{
    /// <summary>Identificativo del contratto (recipient_id a DB).</summary>
    [HeaderPagoPA(caption: "recipient_id", Order = 1)]
    public string? RecipientId { get; set; }

    /// <summary>Nome dell'ente.</summary>
    [HeaderPagoPA(caption: "name", Order = 2)]
    public string? Name { get; set; }

    /// <summary>Categoria.</summary>
    [HeaderPagoPA(caption: "category", Order = 3)]
    public string? Category { get; set; }

    /// <summary>Numero di transazioni del trimestre.</summary>
    [HeaderPagoPA(caption: "current_trx", Order = 4)]
    public int? CurrentTrx { get; set; }

    /// <summary>Valore.</summary>
    [HeaderPagoPA(caption: "value", Order = 5)]
    public decimal? Value { get; set; }

    /// <summary>Codice dell'articolo.</summary>
    [HeaderPagoPA(caption: "codice_articolo", Order = 6)]
    public string? CodiceArticolo { get; set; }

    /// <summary>Anno e trimestre, nel formato 'AAAA_T': decide il foglio, non è una colonna.</summary>
    public string? YearQuarter { get; set; }
}

/// <summary>
/// Riga di [be].[vwAppioFinancialReportPositions] con tutte le colonne della vista, più il nome
/// dell'ente. È il foglio "positions" del download api/appio/financialreports/detail/download.
/// </summary>
public sealed class AppIoFinancialReportPosizioneExcelDto
{
    /// <summary>Nome dell'ente (dal financial report, in mancanza dal contratto).</summary>
    [HeaderPagoPA(caption: "name", Order = 1)]
    public string? Name { get; set; }

    /// <summary>Identificativo del contratto.</summary>
    [HeaderPagoPA(caption: "contract_id", Order = 2)]
    public string? ContractId { get; set; }

    /// <summary>Tipo del documento.</summary>
    [HeaderPagoPA(caption: "tipo_doc", Order = 3)]
    public string? TipoDoc { get; set; }

    /// <summary>Partita IVA.</summary>
    [HeaderPagoPA(caption: "vat_code", Order = 4)]
    public string? VatCode { get; set; }

    /// <summary>Codice fiscale.</summary>
    [HeaderPagoPA(caption: "cod_fisc", Order = 5)]
    public string? CodFisc { get; set; }

    /// <summary>Valuta.</summary>
    [HeaderPagoPA(caption: "valuta", Order = 6)]
    public string? Valuta { get; set; }

    /// <summary>Identificativo del documento.</summary>
    [HeaderPagoPA(caption: "id", Order = 7)]
    public int? Id { get; set; }

    /// <summary>Numero del documento.</summary>
    [HeaderPagoPA(caption: "numero", Order = 8)]
    public string? Numero { get; set; }

    /// <summary>Data del documento.</summary>
    [HeaderPagoPA(caption: "data", Order = 9)]
    public DateTime? Data { get; set; }

    /// <summary>Codifica dell'articolo.</summary>
    [HeaderPagoPA(caption: "codifica_art", Order = 10)]
    public string? CodificaArt { get; set; }

    /// <summary>Progressivo della riga.</summary>
    [HeaderPagoPA(caption: "progressivo_riga", Order = 11)]
    public int ProgressivoRiga { get; set; }

    /// <summary>Codice dell'articolo.</summary>
    [HeaderPagoPA(caption: "codice_articolo", Order = 12)]
    public string? CodiceArticolo { get; set; }

    /// <summary>Descrizione della riga.</summary>
    [HeaderPagoPA(caption: "descrizione_riga", Order = 13)]
    public string? DescrizioneRiga { get; set; }

    /// <summary>Prezzo unitario (testo a DB).</summary>
    [HeaderPagoPA(caption: "prezzo_unit", Order = 14)]
    public string? PrezzoUnit { get; set; }

    /// <summary>Quantità.</summary>
    [HeaderPagoPA(caption: "q_ta", Order = 15)]
    public int? Quantita { get; set; }

    /// <summary>Importo.</summary>
    [HeaderPagoPA(caption: "importo", Order = 16)]
    public decimal? Importo { get; set; }

    /// <summary>Codice IVA.</summary>
    [HeaderPagoPA(caption: "cod_iva", Order = 17)]
    public string? CodIva { get; set; }

    /// <summary>Percentuale IVA (testo a DB).</summary>
    [HeaderPagoPA(caption: "percent_iva", Order = 18)]
    public string? PercentIva { get; set; }

    /// <summary>IVA (testo a DB).</summary>
    [HeaderPagoPA(caption: "iva", Order = 19)]
    public string? Iva { get; set; }

    /// <summary>Codice SDI.</summary>
    [HeaderPagoPA(caption: "codice_sdi", Order = 20)]
    public string? CodiceSdi { get; set; }

    /// <summary>Riferimento fattura.</summary>
    [HeaderPagoPA(caption: "rif_fattura", Order = 21)]
    public string? RifFattura { get; set; }

    /// <summary>Anno e trimestre, nel formato 'AAAA_T': decide il foglio, non è una colonna.</summary>
    public string? YearQuarter { get; set; }
}
