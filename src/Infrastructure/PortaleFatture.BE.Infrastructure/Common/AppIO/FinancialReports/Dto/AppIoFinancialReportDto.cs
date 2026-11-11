namespace PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Dto;

/// <summary>
/// Riga padre della griglia dei documenti emessi APP IO: un contratto in un trimestre, con le righe
/// dei suoi documenti. Si ottiene raggruppando le <see cref="AppIoFinancialReportRigaDto"/> per
/// <see cref="Key"/>, come indica la vista ("per la vista padre a FE vanno aggregate le righe per
/// onboardingtokenid, anno, trimestre").
/// </summary>
public sealed class AppIoFinancialReportDto
{
    /// <summary>
    /// Chiave della riga: contratto e trimestre, separati da '|'.
    /// </summary>
    public string Key => $"{ContractId}|{YearQuarter}";

    /// <summary>
    /// Identificativo del contratto (onboardingtokenid).
    /// </summary>
    public string? ContractId { get; set; }

    /// <summary>
    /// Identificativo dell'ente.
    /// </summary>
    public string? InstitutionId { get; set; }

    /// <summary>
    /// Nome dell'ente.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Prodotto del contratto.
    /// </summary>
    public string? Prodotto { get; set; }

    /// <summary>
    /// Codice fiscale dell'ente.
    /// </summary>
    public string? CodiceFiscale { get; set; }

    /// <summary>
    /// Anno di riferimento.
    /// </summary>
    public int? Year { get; set; }

    /// <summary>
    /// Trimestre di riferimento (1-4).
    /// </summary>
    public int? Quarter { get; set; }

    /// <summary>
    /// Anno e trimestre di riferimento, nel formato 'AAAA_T'.
    /// </summary>
    public string? YearQuarter { get; set; }

    /// <summary>
    /// Totale del contratto nel trimestre, dalla vista.
    /// </summary>
    public decimal? ImportoTotale { get; set; }

    /// <summary>
    /// Righe dei documenti del trimestre, nell'ordine di numero e articolo.
    /// </summary>
    public List<AppIoFinancialReportPosizioneDto> Posizioni { get; set; } = [];
}

/// <summary>
/// Riga di un documento emesso APP IO, mostrata nella riga espansa della griglia.
/// </summary>
public sealed class AppIoFinancialReportPosizioneDto
{
    /// <summary>
    /// Numero del documento.
    /// </summary>
    public string? Numero { get; set; }

    /// <summary>
    /// Data del documento.
    /// </summary>
    public DateTime? Data { get; set; }

    /// <summary>
    /// Codice dell'articolo fatturato.
    /// </summary>
    public string? CodiceArticolo { get; set; }

    /// <summary>
    /// Quantità.
    /// </summary>
    public long? Quantita { get; set; }

    /// <summary>
    /// Importo della riga.
    /// </summary>
    public decimal? Importo { get; set; }

    /// <summary>
    /// Codice IVA.
    /// </summary>
    public string? CodIva { get; set; }

    /// <summary>
    /// Condizioni di pagamento.
    /// </summary>
    public string? Condizioni { get; set; }

    /// <summary>
    /// Causale.
    /// </summary>
    public string? Causale { get; set; }
}

/// <summary>
/// Righe padre della griglia dei documenti emessi APP IO, con il loro numero. La griglia non è
/// paginata lato server (la pagina frontend pagina in memoria), quindi il conteggio è quello delle
/// righe padre restituite.
/// </summary>
public sealed class AppIoFinancialReportListDto
{
    /// <summary>
    /// Righe padre che soddisfano il filtro.
    /// </summary>
    public IEnumerable<AppIoFinancialReportDto>? FinancialReports { get; set; }

    /// <summary>
    /// Numero delle righe padre restituite.
    /// </summary>
    public int Count { get; set; }
}
