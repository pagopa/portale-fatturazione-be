using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Api.Modules.AppIO.FinancialReports.Response;

/// <summary>
/// Risposta della griglia dei documenti contabili APP IO. Stessa forma del gemello pagoPA
/// (count + financialReports con le posizioni), così la pagina frontend si riusa.
/// </summary>
public class AppIoFinancialReportsListResponse
{
    /// <summary>
    /// Documenti contabili che soddisfano il filtro.
    /// </summary>
    [JsonPropertyOrder(-1)]
    public IEnumerable<AppIoFinancialReportResponse>? FinancialReports { get; set; }

    /// <summary>
    /// Numero dei documenti restituiti (la griglia è paginata dal frontend).
    /// </summary>
    [JsonPropertyOrder(-2)]
    public int Count { get; set; }
}

/// <summary>
/// Documento contabile APP IO: testata e posizioni.
/// </summary>
public class AppIoFinancialReportResponse
{
    /// <summary>Chiave del documento: contratto, trimestre e numero, separati da '|'.</summary>
    public string? Key { get; set; }

    /// <summary>Nome dell'ente.</summary>
    public string? Name { get; set; }

    /// <summary>Identificativo del contratto.</summary>
    public string? ContractId { get; set; }

    /// <summary>Tipo del documento contabile.</summary>
    public string? TipoDoc { get; set; }

    /// <summary>Partita IVA, in mancanza il codice fiscale.</summary>
    public string? VatCode { get; set; }

    /// <summary>Valuta del documento.</summary>
    public string? Valuta { get; set; }

    /// <summary>Identificativo del documento.</summary>
    public int? Id { get; set; }

    /// <summary>Numero del documento.</summary>
    public string? Numero { get; set; }

    /// <summary>Data del documento.</summary>
    public DateTime? Data { get; set; }

    /// <summary>Anno e trimestre di riferimento, nel formato 'AAAA_T'.</summary>
    public string? YearQuarter { get; set; }

    /// <summary>Posizioni del documento (riga espansa della griglia).</summary>
    public IEnumerable<AppIoFinancialReportPosizioneResponse>? Posizioni { get; set; }
}

/// <summary>
/// Posizione di un documento contabile APP IO.
/// </summary>
public class AppIoFinancialReportPosizioneResponse
{
    /// <summary>Categoria, dal financial report.</summary>
    public string? Category { get; set; }

    /// <summary>Progressivo della riga nel documento.</summary>
    public int ProgressivoRiga { get; set; }

    /// <summary>Codice dell'articolo fatturato.</summary>
    public string? CodiceArticolo { get; set; }

    /// <summary>Descrizione della riga.</summary>
    public string? DescrizioneRiga { get; set; }

    /// <summary>Quantità.</summary>
    public int? Quantita { get; set; }

    /// <summary>Importo della riga.</summary>
    public decimal? Importo { get; set; }

    /// <summary>Codice IVA.</summary>
    public string? CodIva { get; set; }
}
