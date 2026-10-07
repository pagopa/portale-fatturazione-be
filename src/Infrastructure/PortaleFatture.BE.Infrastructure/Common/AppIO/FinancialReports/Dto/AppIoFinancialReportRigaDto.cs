using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.FinancialReports.Dto;

/// <summary>
/// Riga di un documento contabile APP IO: una posizione di [be].[vwAppioFinancialReportPositions]
/// arricchita con categoria e nome da [be].[vwAppioFinancialReport] e [be].[vwAppioContracts].
/// È la forma piatta letta dal DB: la griglia la raggruppa per documento, il download
/// "Documenti Contabili" (api/appio/financialreports/document) la esporta così com'è.
/// </summary>
public sealed class AppIoFinancialReportRigaDto
{
    /// <summary>
    /// Nome dell'ente: quello del financial report, in mancanza quello del contratto.
    /// </summary>
    [HeaderPagoPA(caption: "Name", Order = 1)]
    public string? Name { get; set; }

    /// <summary>
    /// Identificativo del contratto.
    /// </summary>
    [HeaderPagoPA(caption: "ContractId", Order = 2)]
    public string? ContractId { get; set; }

    /// <summary>
    /// Tipo del documento contabile.
    /// </summary>
    [HeaderPagoPA(caption: "TipoDoc", Order = 3)]
    public string? TipoDoc { get; set; }

    /// <summary>
    /// Partita IVA, in mancanza il codice fiscale.
    /// </summary>
    [HeaderPagoPA(caption: "VatCode", Order = 4)]
    public string? VatCode { get; set; }

    /// <summary>
    /// Valuta del documento.
    /// </summary>
    [HeaderPagoPA(caption: "Valuta", Order = 5)]
    public string? Valuta { get; set; }

    /// <summary>
    /// Identificativo del documento.
    /// </summary>
    [HeaderPagoPA(caption: "Id", Order = 6)]
    public int? Id { get; set; }

    /// <summary>
    /// Numero del documento.
    /// </summary>
    [HeaderPagoPA(caption: "Numero", Order = 7)]
    public string? Numero { get; set; }

    /// <summary>
    /// Data del documento.
    /// </summary>
    [HeaderPagoPA(caption: "Data", Order = 8)]
    public DateTime? Data { get; set; }

    /// <summary>
    /// Anno e trimestre di riferimento, nel formato 'AAAA_T' (es. 2026_1).
    /// </summary>
    [HeaderPagoPA(caption: "YearQuarter", Order = 9)]
    public string? YearQuarter { get; set; }

    /// <summary>
    /// Categoria della posizione, dal financial report; null se il report manca.
    /// </summary>
    [HeaderPagoPA(caption: "Category", Order = 10)]
    public string? Category { get; set; }

    /// <summary>
    /// Progressivo della riga nel documento.
    /// </summary>
    [HeaderPagoPA(caption: "ProgressivoRiga", Order = 11)]
    public int ProgressivoRiga { get; set; }

    /// <summary>
    /// Codice dell'articolo fatturato.
    /// </summary>
    [HeaderPagoPA(caption: "CodiceArticolo", Order = 12)]
    public string? CodiceArticolo { get; set; }

    /// <summary>
    /// Descrizione della riga.
    /// </summary>
    [HeaderPagoPA(caption: "DescrizioneRiga", Order = 13)]
    public string? DescrizioneRiga { get; set; }

    /// <summary>
    /// Quantità.
    /// </summary>
    [HeaderPagoPA(caption: "Quantita", Order = 14)]
    public int? Quantita { get; set; }

    /// <summary>
    /// Importo della riga.
    /// </summary>
    [HeaderPagoPA(caption: "Importo", Order = 15)]
    public decimal? Importo { get; set; }

    /// <summary>
    /// Codice IVA.
    /// </summary>
    [HeaderPagoPA(caption: "CodIva", Order = 16)]
    public string? CodIva { get; set; }
}
