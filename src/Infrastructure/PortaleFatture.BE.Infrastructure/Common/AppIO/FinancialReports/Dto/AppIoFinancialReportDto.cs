namespace PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Dto;

/// <summary>
/// Documento contabile APP IO della griglia: la testata (contratto, trimestre, numero) con le sue
/// posizioni. Si ottiene raggruppando le <see cref="AppIoFinancialReportRigaDto"/> per <see cref="Key"/>.
/// </summary>
public sealed class AppIoFinancialReportDto
{
    /// <summary>
    /// Chiave del documento: contratto, trimestre e numero, separati da '|'.
    /// </summary>
    public string Key => $"{ContractId}|{YearQuarter}|{Numero}";

    /// <summary>
    /// Nome dell'ente.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Identificativo del contratto.
    /// </summary>
    public string? ContractId { get; set; }

    /// <summary>
    /// Tipo del documento contabile.
    /// </summary>
    public string? TipoDoc { get; set; }

    /// <summary>
    /// Partita IVA, in mancanza il codice fiscale.
    /// </summary>
    public string? VatCode { get; set; }

    /// <summary>
    /// Valuta del documento.
    /// </summary>
    public string? Valuta { get; set; }

    /// <summary>
    /// Identificativo del documento.
    /// </summary>
    public int? Id { get; set; }

    /// <summary>
    /// Numero del documento.
    /// </summary>
    public string? Numero { get; set; }

    /// <summary>
    /// Data del documento.
    /// </summary>
    public DateTime? Data { get; set; }

    /// <summary>
    /// Anno e trimestre di riferimento, nel formato 'AAAA_T'.
    /// </summary>
    public string? YearQuarter { get; set; }

    /// <summary>
    /// Posizioni del documento, nell'ordine del progressivo di riga.
    /// </summary>
    public List<AppIoFinancialReportPosizioneDto> Posizioni { get; set; } = [];
}

/// <summary>
/// Posizione (riga) di un documento contabile APP IO, mostrata nella riga espansa della griglia.
/// </summary>
public sealed class AppIoFinancialReportPosizioneDto
{
    /// <summary>
    /// Categoria della posizione, dal financial report.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Progressivo della riga nel documento.
    /// </summary>
    public int ProgressivoRiga { get; set; }

    /// <summary>
    /// Codice dell'articolo fatturato.
    /// </summary>
    public string? CodiceArticolo { get; set; }

    /// <summary>
    /// Descrizione della riga.
    /// </summary>
    public string? DescrizioneRiga { get; set; }

    /// <summary>
    /// Quantità.
    /// </summary>
    public int? Quantita { get; set; }

    /// <summary>
    /// Importo della riga.
    /// </summary>
    public decimal? Importo { get; set; }

    /// <summary>
    /// Codice IVA.
    /// </summary>
    public string? CodIva { get; set; }
}

/// <summary>
/// Documenti contabili APP IO della griglia, con il loro numero. La griglia non è paginata lato
/// server (la pagina frontend pagina in memoria), quindi il conteggio è quello dei documenti
/// restituiti.
/// </summary>
public sealed class AppIoFinancialReportListDto
{
    /// <summary>
    /// Documenti contabili che soddisfano il filtro.
    /// </summary>
    public IEnumerable<AppIoFinancialReportDto>? FinancialReports { get; set; }

    /// <summary>
    /// Numero dei documenti contabili restituiti.
    /// </summary>
    public int Count { get; set; }
}
