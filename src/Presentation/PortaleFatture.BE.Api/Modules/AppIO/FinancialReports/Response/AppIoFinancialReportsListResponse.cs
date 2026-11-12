using System.Text.Json.Serialization;

namespace PortaleFatture.BE.Api.Modules.AppIO.FinancialReports.Response;

/// <summary>
/// Risposta della griglia dei documenti emessi APP IO: una riga padre per contratto e trimestre,
/// con le righe dei documenti.
/// </summary>
public class AppIoFinancialReportsListResponse
{
    /// <summary>
    /// Righe padre che soddisfano il filtro.
    /// </summary>
    [JsonPropertyOrder(-1)]
    public IEnumerable<AppIoFinancialReportResponse>? FinancialReports { get; set; }

    /// <summary>
    /// Numero delle righe padre restituite (la griglia è paginata dal frontend).
    /// </summary>
    [JsonPropertyOrder(-2)]
    public int Count { get; set; }
}

/// <summary>
/// Riga padre della griglia dei documenti emessi APP IO: un contratto in un trimestre.
/// </summary>
public class AppIoFinancialReportResponse
{
    /// <summary>Chiave della riga: contratto e trimestre, separati da '|'.</summary>
    public string? Key { get; set; }

    /// <summary>Identificativo del contratto (da rimandare al dettaglio).</summary>
    public string? ContractId { get; set; }

    /// <summary>Identificativo dell'ente.</summary>
    public string? InstitutionId { get; set; }

    /// <summary>Nome dell'ente.</summary>
    public string? Name { get; set; }

    /// <summary>Prodotto del contratto.</summary>
    public string? Prodotto { get; set; }

    /// <summary>Codice fiscale dell'ente.</summary>
    public string? CodiceFiscale { get; set; }

    /// <summary>Anno di riferimento.</summary>
    public int? Year { get; set; }

    /// <summary>Trimestre di riferimento (1-4).</summary>
    public int? Quarter { get; set; }

    /// <summary>Anno e trimestre, nel formato 'AAAA_T' (da rimandare al dettaglio).</summary>
    public string? YearQuarter { get; set; }

    /// <summary>Totale del contratto nel trimestre, dalla vista.</summary>
    public decimal? ImportoTotale { get; set; }

    /// <summary>Righe dei documenti (riga espansa della griglia).</summary>
    public IEnumerable<AppIoFinancialReportPosizioneResponse>? Posizioni { get; set; }
}

/// <summary>
/// Riga di un documento emesso APP IO.
/// </summary>
public class AppIoFinancialReportPosizioneResponse
{
    /// <summary>Numero del documento.</summary>
    public string? Numero { get; set; }

    /// <summary>Data del documento.</summary>
    public DateTime? Data { get; set; }

    /// <summary>
    /// Data del report: data e ora dell'ultimo aggiornamento della riga da parte di DATA
    /// (gold_updated_at della vista). Al frontend arriva come <c>dataReport</c>.
    /// </summary>
    public DateTime? DataReport { get; set; }

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
