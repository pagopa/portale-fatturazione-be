using PortaleFatture.BE.Infrastructure.Common.pagoPA.Documenti.Common;

namespace PortaleFatture.BE.Infrastructure.Common.AppIO.Contratti.Dto;

/// <summary>
/// Contratto APP IO letto dalla vista [be].[vwAppioContracts]: una riga per contratto e trimestre.
/// Gli attributi <see cref="HeaderPagoPAAttribute"/> definiscono intestazioni e ordine delle colonne
/// dell'export Excel (rotta api/appio/contracts/download).
/// </summary>
public sealed class AppIoContratto
{
    /// <summary>
    /// Identificativo del contratto.
    /// </summary>
    [HeaderPagoPA(caption: "ContractId", Order = 1)]
    public string? ContractId { get; set; }

    /// <summary>
    /// Nome dell'ente titolare del contratto.
    /// </summary>
    [HeaderPagoPA(caption: "Name", Order = 2)]
    public string? Name { get; set; }

    /// <summary>
    /// Codice fiscale associato al contratto.
    /// </summary>
    [HeaderPagoPA(caption: "TaxCode", Order = 3)]
    public string? TaxCode { get; set; }

    /// <summary>
    /// Partita IVA associata al contratto.
    /// </summary>
    [HeaderPagoPA(caption: "VatCode", Order = 4)]
    public string? VatCode { get; set; }

    /// <summary>
    /// Gruppo IVA associato al contratto (decimal(18,2) a DB).
    /// </summary>
    [HeaderPagoPA(caption: "VatGroup", Order = 5)]
    public decimal? VatGroup { get; set; }

    /// <summary>
    /// Codice SDI associato al contratto.
    /// </summary>
    [HeaderPagoPA(caption: "SdiCode", Order = 6)]
    public string? SdiCode { get; set; }

    /// <summary>
    /// Anno e mese di riferimento, nel formato 'AAAAMM' (es. 202604: il mese successivo alla fine del trimestre).
    /// </summary>
    [HeaderPagoPA(caption: "YearMonth", Order = 7)]
    public string? YearMonth { get; set; }

    /// <summary>
    /// Anno e trimestre di riferimento, nel formato 'AAAA_T' (es. 2026_1).
    /// </summary>
    [HeaderPagoPA(caption: "YearQuarter", Order = 8)]
    public string? YearQuarter { get; set; }
}
